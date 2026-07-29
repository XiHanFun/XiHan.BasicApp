// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 多租户业务编号生成器实现，编排作用域解析、幂等、事务、并发重试与格式快照。
/// </summary>
/// <remarks>
/// 正确性分为三层：进程内规则键锁降低同节点竞争，规则 <c>RowVersion</c> 处理跨节点乐观并发，
/// 永久分配记录的唯一索引保证相同幂等范围最终只保留一次分配。生成器本身不保存可变业务状态，可由 DI 并发调用。
/// </remarks>
public sealed class NumberGenerator : INumberGenerator
{
    /// <summary>
    /// 单次发号允许执行的最大乐观并发尝试次数；包含首次尝试，最后一次失败后不再继续退避重试。
    /// </summary>
    private const int MaximumRetryCount = 5;

    /// <summary>
    /// 乐观锁冲突随机退避的最短毫秒数（含），用于降低多个竞争者立即再次碰撞的概率。
    /// </summary>
    private const int MinimumRetryDelayMilliseconds = 10;

    /// <summary>
    /// 乐观锁冲突随机退避的最长毫秒数（含），限制单次重试对接口延迟的影响。
    /// </summary>
    private const int MaximumRetryDelayMilliseconds = 80;

    /// <summary>
    /// 规则仓储，用于按明确作用域解析规则并持久化带 <c>RowVersion</c> 的流水推进。
    /// </summary>
    private readonly INumberingRuleRepository _ruleRepository;

    /// <summary>
    /// 永久分配记录仓储，负责幂等事实查询、分配审计写入和历史最大流水校验。
    /// </summary>
    private readonly INumberingAllocationRepository _allocationRepository;

    /// <summary>
    /// 无共享可变状态的格式与周期计算器，用于校验规则、计算周期以及重建幂等结果。
    /// </summary>
    private readonly INumberingFormatter _formatter;

    /// <summary>
    /// 进程内规则键锁提供器；只降低当前节点竞争，不能替代数据库乐观锁和唯一索引。
    /// </summary>
    private readonly NumberingLockProvider _lockProvider;

    /// <summary>
    /// 当前租户上下文，用于捕获可信请求租户并在全局发号时临时切换到平台数据库。
    /// </summary>
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 当前用户上下文，仅用于记录发号操作人；后台任务没有登录用户时允许为空身份。
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 工作单元管理器，为每次并发尝试创建独立事务并保证规则推进与分配记录原子提交。
    /// </summary>
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 可替换时间源，为规则时区换算和测试周期边界提供统一的 UTC 当前时间。
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 结构化日志记录器，记录成功区间、并发冲突、重试耗尽和脱敏后的失败上下文。
    /// </summary>
    private readonly ILogger<NumberGenerator> _logger;

    /// <summary>
    /// 初始化业务编号生成器。
    /// </summary>
    /// <param name="ruleRepository">规则仓储。</param>
    /// <param name="allocationRepository">分配记录仓储。</param>
    /// <param name="formatter">格式策略。</param>
    /// <param name="lockProvider">进程内规则键锁。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="currentUser">当前操作用户；后台任务调用时可以为空身份。</param>
    /// <param name="unitOfWorkManager">工作单元管理器。</param>
    /// <param name="timeProvider">时间提供器。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public NumberGenerator(
        INumberingRuleRepository ruleRepository,
        INumberingAllocationRepository allocationRepository,
        INumberingFormatter formatter,
        NumberingLockProvider lockProvider,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IUnitOfWorkManager unitOfWorkManager,
        TimeProvider timeProvider,
        ILogger<NumberGenerator> logger)
    {
        _ruleRepository = ruleRepository;
        _allocationRepository = allocationRepository;
        _formatter = formatter;
        _lockProvider = lockProvider;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _unitOfWorkManager = unitOfWorkManager;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// 生成一个业务编号，并将失败审计统一记录为结构化日志。
    /// </summary>
    /// <param name="request">单号请求；规则编码和幂等键必填，租户身份取自当前上下文。</param>
    /// <param name="cancellationToken">用于取消锁等待、数据库操作、事务提交和重试退避的取消令牌。</param>
    /// <returns>首次分配或幂等重放得到的单号结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">请求、规则、幂等、容量或并发重试校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberGenerationResult> GenerateAsync(NumberGenerateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return await GenerateCoreAsync(
                request.RuleCode,
                request.Scope,
                request.IdempotencyKey,
                1,
                request.BusinessType,
                request.BusinessId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // 入口统一记录失败可以覆盖请求归一化阶段的异常；取消不属于业务失败，不进入告警日志。
            LogGenerationFailure(request.RuleCode, request.Scope, request.IdempotencyKey, 1, exception);
            throw;
        }
    }

    /// <summary>
    /// 在一次原子分配中生成连续编号，并将失败审计统一记录为结构化日志。
    /// </summary>
    /// <param name="request">批量请求；数量必须在 1 至 1000 之间，规则编码和幂等键必填。</param>
    /// <param name="cancellationToken">用于取消锁等待、数据库操作、事务提交和重试退避的取消令牌。</param>
    /// <returns>首次分配或幂等重放得到的连续编号结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">请求、批量上限、规则、幂等、容量或并发重试校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberGenerationResult> GenerateBatchAsync(NumberBatchGenerateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return await GenerateCoreAsync(
                request.RuleCode,
                request.Scope,
                request.IdempotencyKey,
                request.Count,
                request.BusinessType,
                request.BusinessId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogGenerationFailure(request.RuleCode, request.Scope, request.IdempotencyKey, request.Count, exception);
            throw;
        }
    }

    /// <summary>
    /// 统一执行单号与批量发号流程。
    /// </summary>
    /// <param name="ruleCode">调用方指定的规则编码。</param>
    /// <param name="scope">规则解析策略。</param>
    /// <param name="idempotencyKey">调用方幂等键。</param>
    /// <param name="count">本次连续分配数量。</param>
    /// <param name="businessType">可选业务类型。</param>
    /// <param name="businessId">可选业务标识。</param>
    /// <param name="cancellationToken">用于取消完整发号流程的取消令牌。</param>
    /// <returns>首次分配或幂等重放结果。</returns>
    /// <exception cref="UserFriendlyException">请求无效、规则不可用或分配失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<NumberGenerationResult> GenerateCoreAsync(
        string ruleCode,
        NumberingScope scope,
        string idempotencyKey,
        int count,
        string? businessType,
        string? businessId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 先归一所有外部字符串，确保指纹、查询条件和日志使用相同的规范值。
        var normalized = NormalizeRequest(ruleCode, scope, idempotencyKey, count, businessType, businessId);
        // 必须在任何平台上下文切换前捕获原请求租户；该值参与幂等唯一范围和永久审计。
        var requestTenantId = _currentTenant.Id is > 0 ? _currentTenant.Id.Value : 0L;
        var resolved = await ResolveRuleAsync(normalized.RuleCode, scope, requestTenantId, cancellationToken);
        // 指纹绑定实际规则主键，避免 Auto 在租户规则与同编码全局规则之间切换时错误重放旧结果。
        var fingerprint = BuildFingerprint(normalized, resolved.RuleId);
        var gate = _lockProvider.Get(resolved.OwnerTenantId, resolved.RuleId);

        // 锁只覆盖同一规则的分配临界区；不同规则和不同独立租户库中的相同主键可以并行发号。
        await gate.WaitAsync(cancellationToken);
        try
        {
            // 全局规则必须先切到平台数据库，再开启独立事务；否则库隔离租户可能把平台事务绑定到租户连接。
            if (resolved.OwnerTenantId == 0)
            {
                using var platformScope = _currentTenant.Change(null);
                return await AllocateWithRetryAsync(resolved, requestTenantId, normalized, fingerprint, cancellationToken);
            }

            return await AllocateWithRetryAsync(resolved, requestTenantId, normalized, fingerprint, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// 在当前已选数据库中执行带乐观锁重试的原子分配。
    /// </summary>
    /// <param name="resolved">已经解析的实际规则位置和作用域。</param>
    /// <param name="requestTenantId">切换数据库前捕获的原请求租户。</param>
    /// <param name="request">已经校验并归一的请求。</param>
    /// <param name="fingerprint">绑定实际规则和请求参数的稳定指纹。</param>
    /// <param name="cancellationToken">用于取消事务、数据库操作和退避等待的取消令牌。</param>
    /// <returns>成功提交或幂等重放的分配结果。</returns>
    /// <exception cref="UserFriendlyException">并发冲突重试耗尽或单次分配业务校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<NumberGenerationResult> AllocateWithRetryAsync(
        ResolvedNumberingRule resolved,
        long requestTenantId,
        NormalizedGenerationRequest request,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        Exception? lastConflict = null;
        for (var attempt = 1; attempt <= MaximumRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 每次尝试都使用 requiresNew 独立事务，冲突回滚后下一次才能重新读取最新 RowVersion。
                using var unitOfWork = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(isTransactional: true), requiresNew: true);
                var result = await AllocateOnceAsync(resolved, requestTenantId, request, fingerprint, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);
                return result;
            }
            catch (ConcurrencyConflictException exception) when (attempt < MaximumRetryCount)
            {
                lastConflict = exception;
                // 短随机退避降低多节点在同一节奏上持续碰撞；取消令牌确保请求终止时不继续等待。
                var delay = Random.Shared.Next(MinimumRetryDelayMilliseconds, MaximumRetryDelayMilliseconds + 1);
                _logger.LogWarning(
                    exception,
                    "业务编号乐观锁冲突，准备重试：RuleId={RuleId}, OwnerTenantId={OwnerTenantId}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Attempt={Attempt}, DelayMs={DelayMs}",
                    resolved.RuleId, resolved.OwnerTenantId, requestTenantId, _currentUser.UserId, attempt, delay);
                await Task.Delay(delay, cancellationToken);
            }
            catch (ConcurrencyConflictException exception)
            {
                lastConflict = exception;
            }
        }

        _logger.LogError(
            lastConflict,
            "业务编号并发重试耗尽：RuleId={RuleId}, OwnerTenantId={OwnerTenantId}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, RetryCount={RetryCount}",
            resolved.RuleId, resolved.OwnerTenantId, requestTenantId, _currentUser.UserId, MaximumRetryCount);
        throw new UserFriendlyException("编号生成并发繁忙，请稍后使用相同幂等键重试。", innerException: lastConflict);
    }

    /// <summary>
    /// 在一个独立事务中执行一次幂等检查、规则更新和分配记录写入。
    /// </summary>
    /// <param name="resolved">已经解析的实际规则位置和作用域。</param>
    /// <param name="requestTenantId">原请求租户；平台或单体上下文为 0。</param>
    /// <param name="request">已经校验并归一的请求。</param>
    /// <param name="fingerprint">用于识别同幂等键参数冲突的请求指纹。</param>
    /// <param name="cancellationToken">用于取消本次事务内数据库操作的取消令牌。</param>
    /// <returns>本次新分配或从永久记录重建的结果。</returns>
    /// <exception cref="UserFriendlyException">幂等冲突、规则不存在、规则停用、全局规则未开放或流水耗尽。</exception>
    /// <exception cref="ConcurrencyConflictException">规则行版本或唯一索引发生并发冲突，由上层重试。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<NumberGenerationResult> AllocateOnceAsync(
        ResolvedNumberingRule resolved,
        long requestTenantId,
        NormalizedGenerationRequest request,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var existing = await _allocationRepository.FindByIdempotencyKeyAsync(
            resolved.OwnerTenantId,
            resolved.RuleId,
            requestTenantId,
            request.IdempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            // 幂等键只能重放完全相同的业务请求；不同参数复用同一键必须显式冲突，不能静默返回错误编号。
            if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            {
                throw new UserFriendlyException("幂等键已被不同参数使用，请更换幂等键。");
            }

            _logger.LogInformation(
                "业务编号幂等重放：AllocationId={AllocationId}, RuleId={RuleId}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, StartValue={StartValue}, EndValue={EndValue}, Result=Success",
                existing.BasicId, existing.RuleId, requestTenantId, _currentUser.UserId, existing.StartValue, existing.EndValue);
            return BuildResult(existing, resolved.ResolvedScope, true);
        }

        // 必须在每次重试的独立事务中重新加载，确保 UpdateAsync 携带数据库最新 RowVersion。
        var rule = await _ruleRepository.FindByIdInScopeAsync(resolved.OwnerTenantId, resolved.RuleId, cancellationToken)
            ?? throw new UserFriendlyException("业务编号规则不存在。");
        if (rule.Status != EnableStatus.Enabled)
        {
            throw new UserFriendlyException("业务编号规则已停用。");
        }

        if (resolved.OwnerTenantId == 0 && requestTenantId > 0 && !rule.AllowTenantUse)
        {
            throw new UserFriendlyException("该全局编号规则未向租户开放。");
        }

        // 日期段和周期键必须从同一个 UTC 瞬间转换，避免恰逢周期边界时分别计算出不一致快照。
        var generatedAtUtc = _timeProvider.GetUtcNow();
        var localTime = _formatter.ConvertToRuleTime(generatedAtUtc, rule.TimeZoneId);
        var periodKey = _formatter.GetPeriodKey(localTime, rule.ResetCycle);
        var dateText = _formatter.GetDateText(localTime, rule.DateFormat);
        // 周期变化时从 0 开始计算下一值，但旧周期永久分配记录仍保留用于审计。
        var currentValue = string.Equals(rule.CurrentPeriod, periodKey, StringComparison.Ordinal) ? rule.CurrentValue : 0L;
        var maximum = _formatter.GetMaxValue(rule.SerialLength);
        // 先做减法再比较，避免 currentValue + count 在未来边界扩展时出现整数溢出。
        if (currentValue > maximum - request.Count)
        {
            throw new UserFriendlyException($"编号规则“{rule.RuleCode}”当前周期流水已耗尽，请调整规则或等待下一周期。");
        }

        var startValue = currentValue + 1;
        var endValue = currentValue + request.Count;
        rule.CurrentPeriod = periodKey;
        rule.CurrentValue = endValue;
        rule.HasAllocated = true;

        // 保存完整格式快照，使规则后续元数据变化或删除限制调整时仍能原样重建历史编号。
        var allocation = new SysNumberingAllocation
        {
            RuleId = rule.BasicId,
            RuleCode = rule.RuleCode,
            RequestTenantId = requestTenantId,
            IdempotencyKey = request.IdempotencyKey,
            RequestFingerprint = fingerprint,
            Count = request.Count,
            StartValue = startValue,
            EndValue = endValue,
            PeriodKey = periodKey,
            PrefixSnapshot = rule.Prefix,
            SeparatorSnapshot = rule.Separator,
            DateTextSnapshot = dateText,
            SerialLengthSnapshot = rule.SerialLength,
            GeneratedAtUtc = generatedAtUtc,
            BusinessType = request.BusinessType,
            BusinessId = request.BusinessId
        };

        // 事务边界：规则 RowVersion 更新与永久分配记录插入必须同时提交；任何异常都会回滚流水推进。
        _ = await _ruleRepository.UpdateAsync(rule, cancellationToken);
        allocation = await _allocationRepository.AddAsync(allocation, cancellationToken);

        _logger.LogInformation(
            "业务编号分配成功：AllocationId={AllocationId}, RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Period={Period}, StartValue={StartValue}, EndValue={EndValue}, Count={Count}, Result=Success",
            allocation.BasicId, rule.BasicId, rule.RuleCode, resolved.OwnerTenantId, requestTenantId, _currentUser.UserId, periodKey, startValue, endValue, request.Count);
        return BuildResult(allocation, resolved.ResolvedScope, false);
    }

    /// <summary>
    /// 按请求上下文解析租户私有规则或平台全局规则。
    /// </summary>
    /// <param name="ruleCode">已经归一的规则编码。</param>
    /// <param name="scope">调用方指定的解析策略。</param>
    /// <param name="requestTenantId">原请求租户；平台或单体上下文为 0。</param>
    /// <param name="cancellationToken">用于取消租户库或平台库查询的取消令牌。</param>
    /// <returns>实际规则主键、规则所属租户和最终解析作用域。</returns>
    /// <remarks>
    /// 租户的 <see cref="NumberingScope.Auto"/> 先查私有规则，再回退到已开放的全局规则；
    /// 平台或单体上下文只使用全局规则，且调用方不能通过请求传入其他租户 ID。
    /// </remarks>
    /// <exception cref="UserFriendlyException">作用域与上下文冲突，或未找到可用规则。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<ResolvedNumberingRule> ResolveRuleAsync(
        string ruleCode,
        NumberingScope scope,
        long requestTenantId,
        CancellationToken cancellationToken)
    {
        if (requestTenantId == 0)
        {
            // 平台管理和普通单体应用都没有当前租户，两者统一使用平台库中的全局规则。
            if (scope == NumberingScope.Tenant)
            {
                throw new UserFriendlyException("平台或单体上下文不能使用 Tenant 作用域。");
            }

            var globalRule = await _ruleRepository.FindByCodeInScopeAsync(0, ruleCode, true, cancellationToken)
                ?? throw new UserFriendlyException("未找到可用的全局编号规则。");
            return new ResolvedNumberingRule(globalRule.BasicId, 0, NumberingScope.Global);
        }

        if (scope is NumberingScope.Auto or NumberingScope.Tenant)
        {
            // Auto 的优先级固定为租户私有规则优先，保证租户可用同编码规则覆盖平台默认规则。
            var tenantRule = await _ruleRepository.FindByCodeInScopeAsync(requestTenantId, ruleCode, true, cancellationToken);
            if (tenantRule is not null)
            {
                return new ResolvedNumberingRule(tenantRule.BasicId, requestTenantId, NumberingScope.Tenant);
            }

            if (scope == NumberingScope.Tenant)
            {
                throw new UserFriendlyException("未找到可用的租户私有编号规则。");
            }
        }

        // 回退查询只需在平台上下文读取规则定位信息；真正分配会重新切换平台上下文后再开启独立事务。
        using var platformScope = _currentTenant.Change(null);
        var fallback = await _ruleRepository.FindByCodeInScopeAsync(0, ruleCode, true, cancellationToken);
        if (fallback is null || !fallback.AllowTenantUse)
        {
            throw new UserFriendlyException("未找到向当前租户开放的全局编号规则。");
        }

        return new ResolvedNumberingRule(fallback.BasicId, 0, NumberingScope.Global);
    }

    /// <summary>
    /// 校验并归一外部请求，禁止任意租户标识进入发号契约。
    /// </summary>
    /// <param name="ruleCode">外部规则编码。</param>
    /// <param name="scope">外部作用域。</param>
    /// <param name="idempotencyKey">外部幂等键。</param>
    /// <param name="count">请求生成数量。</param>
    /// <param name="businessType">可选业务类型。</param>
    /// <param name="businessId">可选业务标识。</param>
    /// <returns>去除首尾空白并通过长度、枚举和数量校验的内部请求。</returns>
    /// <exception cref="UserFriendlyException">任一外部字段不满足发号契约。</exception>
    private static NormalizedGenerationRequest NormalizeRequest(
        string ruleCode,
        NumberingScope scope,
        string idempotencyKey,
        int count,
        string? businessType,
        string? businessId)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new UserFriendlyException("编号作用域无效。");
        }

        if (string.IsNullOrWhiteSpace(ruleCode) || ruleCode.Trim().Length > 100)
        {
            throw new UserFriendlyException("规则编码不能为空且不能超过 100 个字符。");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 100)
        {
            throw new UserFriendlyException("幂等键不能为空且不能超过 100 个字符。");
        }

        if (count is < 1 or > NumberingFormatter.MaximumBatchSize)
        {
            throw new UserFriendlyException($"单次生成数量必须在 1 至 {NumberingFormatter.MaximumBatchSize} 之间。");
        }

        var normalizedBusinessType = NormalizeOptional(businessType, 100, "业务类型");
        var normalizedBusinessId = NormalizeOptional(businessId, 100, "业务标识");
        return new NormalizedGenerationRequest(
            ruleCode.Trim(),
            scope,
            idempotencyKey.Trim(),
            count,
            normalizedBusinessType,
            normalizedBusinessId);
    }

    /// <summary>
    /// 生成稳定请求指纹；包含实际规则主键，避免同编码在不同作用域间被误判为同一请求。
    /// </summary>
    /// <param name="request">已经归一的请求。</param>
    /// <param name="ruleId">实际解析到的规则主键。</param>
    /// <returns>规范字段按固定顺序编码后的 SHA-256 十六进制摘要。</returns>
    private static string BuildFingerprint(NormalizedGenerationRequest request, long ruleId)
    {
        // 使用不可出现在普通业务文本中的单元分隔符，避免字段拼接产生边界歧义。
        var canonical = string.Join(
            '\u001F',
            ruleId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.RuleCode,
            ((int)request.Scope).ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.BusinessType ?? string.Empty,
            request.BusinessId ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>
    /// 根据永久格式快照构造返回结果。
    /// </summary>
    /// <param name="allocation">首次分配时保存的永久记录和格式快照。</param>
    /// <param name="resolvedScope">实际解析到的规则作用域。</param>
    /// <param name="isReplay">是否由既有幂等记录重放。</param>
    /// <returns>使用快照重建的完整编号结果。</returns>
    private NumberGenerationResult BuildResult(SysNumberingAllocation allocation, NumberingScope resolvedScope, bool isReplay)
    {
        var numbers = _formatter.FormatRange(
            allocation.PrefixSnapshot,
            allocation.SeparatorSnapshot,
            allocation.DateTextSnapshot,
            allocation.SerialLengthSnapshot,
            allocation.StartValue,
            allocation.EndValue);
        return new NumberGenerationResult(
            allocation.RuleId,
            allocation.RuleCode,
            resolvedScope,
            allocation.RequestTenantId,
            allocation.IdempotencyKey,
            allocation.PeriodKey,
            allocation.StartValue,
            allocation.EndValue,
            numbers,
            allocation.GeneratedAtUtc,
            isReplay);
    }

    /// <summary>
    /// 归一可选业务字段并执行长度校验。
    /// </summary>
    /// <param name="value">待归一的可选文本。</param>
    /// <param name="maximumLength">允许的最大字符数。</param>
    /// <param name="fieldName">用于友好错误消息的字段名称。</param>
    /// <returns>去除首尾空白的文本；空值或纯空白返回 <see langword="null"/>。</returns>
    /// <exception cref="UserFriendlyException">归一后的文本超过 <paramref name="maximumLength"/>。</exception>
    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new UserFriendlyException($"{fieldName}不能超过 {maximumLength} 个字符。");
        }

        return normalized;
    }

    /// <summary>
    /// 记录所有非取消发号失败；幂等键只记录 SHA-256 摘要，避免业务键内容进入日志。
    /// </summary>
    /// <param name="ruleCode">调用方提交的规则编码。</param>
    /// <param name="scope">调用方提交的规则作用域。</param>
    /// <param name="idempotencyKey">调用方幂等键；只计算摘要，不记录原文。</param>
    /// <param name="count">请求生成数量。</param>
    /// <param name="exception">导致发号失败的异常。</param>
    private void LogGenerationFailure(
        string? ruleCode,
        NumberingScope scope,
        string? idempotencyKey,
        int count,
        Exception exception)
    {
        var idempotencyDigest = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey.Trim())));
        _logger.LogWarning(
            exception,
            "业务编号分配失败：RuleCode={RuleCode}, Scope={Scope}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, IdempotencyDigest={IdempotencyDigest}, Count={Count}, Result=Failed",
            ruleCode?.Trim(), scope, _currentTenant.Id ?? 0, _currentUser.UserId, idempotencyDigest, count);
    }

    /// <summary>
    /// 已解析规则定位信息。
    /// </summary>
    /// <param name="RuleId">实际规则主键。</param>
    /// <param name="OwnerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="ResolvedScope">最终解析到的租户或全局作用域。</param>
    private sealed record ResolvedNumberingRule(long RuleId, long OwnerTenantId, NumberingScope ResolvedScope);

    /// <summary>
    /// 已校验并归一的发号请求。
    /// </summary>
    /// <param name="RuleCode">去除首尾空白的规则编码。</param>
    /// <param name="Scope">调用方请求的解析作用域。</param>
    /// <param name="IdempotencyKey">去除首尾空白的幂等键。</param>
    /// <param name="Count">连续分配数量。</param>
    /// <param name="BusinessType">归一后的可选业务类型。</param>
    /// <param name="BusinessId">归一后的可选业务标识。</param>
    private sealed record NormalizedGenerationRequest(
        string RuleCode,
        NumberingScope Scope,
        string IdempotencyKey,
        int Count,
        string? BusinessType,
        string? BusinessId);
}
