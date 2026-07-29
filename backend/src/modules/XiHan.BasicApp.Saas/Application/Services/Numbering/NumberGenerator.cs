// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 多租户业务编号生成器实现，编排作用域解析、幂等、事务与格式快照。
/// </summary>
/// <remarks>
/// <para>
/// 流水推进由数据库单条语句完成：先把周期基线单调翻转到当前周期，再在当前周期内累加占用一段连续区间，
/// 最后在同一事务内读回本次写入以确定区间边界。应用层不做「读取当前值 → 计算新值 → 写回」，
/// 因此不存在丢失更新，也不需要乐观锁冲突重试。
/// </para>
/// <para>
/// 事务是正确性的前提而不是可选项：推进语句取得的排他行锁必须持有到提交，读回值才等于本次推进结果。
/// 生成器因此保证事务存在——调用链上已有事务型工作单元时加入它，没有时自行开启并提交；
/// 若两者都不成立（例如处于非事务型工作单元内），仓储的连接级断言会立即失败，而不是静默降级。
/// </para>
/// <para>
/// 调用契约见 <see cref="INumberGenerator"/>：加入调用方事务意味着编号与调用方事务同生共死，
/// 且规则行会被锁定到调用方提交为止。
/// </para>
/// <para>生成器本身不保存可变状态，可由 DI 并发调用。</para>
/// </remarks>
public sealed class NumberGenerator : INumberGenerator
{
    /// <summary>
    /// 请求指纹的字段分隔符，取 ASCII 单元分隔符 0x1F。
    /// </summary>
    /// <remarks>该字符不会出现在规则编码、幂等键或业务标识等普通文本中，因此字段拼接不会产生边界歧义。</remarks>
    private const char FingerprintFieldSeparator = (char)0x1F;

    /// <summary>
    /// 规则仓储，提供作用域解析查询和数据库内原子流水推进。
    /// </summary>
    private readonly INumberingRuleRepository _ruleRepository;

    /// <summary>
    /// 永久分配记录仓储，负责幂等事实查询和分配审计写入。
    /// </summary>
    private readonly INumberingAllocationRepository _allocationRepository;

    /// <summary>
    /// 无共享可变状态的格式与周期计算器，用于校验规则、计算周期以及重建幂等结果。
    /// </summary>
    private readonly INumberingFormatter _formatter;

    /// <summary>
    /// 当前租户上下文，用于捕获可信请求租户并在全局发号时临时切换到平台数据库。
    /// </summary>
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 当前用户上下文，仅用于记录发号操作人；后台任务没有登录用户时允许为空身份。
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 工作单元管理器，用于加入调用方事务或在没有环境事务时自行开启事务。
    /// </summary>
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 可替换时间源，为规则时区换算和测试周期边界提供统一的 UTC 当前时间。
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 结构化日志记录器，记录成功区间、周期翻转、推进拒绝和脱敏后的失败上下文。
    /// </summary>
    private readonly ILogger<NumberGenerator> _logger;

    /// <summary>
    /// 初始化业务编号生成器。
    /// </summary>
    /// <param name="ruleRepository">规则仓储。</param>
    /// <param name="allocationRepository">分配记录仓储。</param>
    /// <param name="formatter">格式策略。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="currentUser">当前操作用户；后台任务调用时可以为空身份。</param>
    /// <param name="unitOfWorkManager">工作单元管理器。</param>
    /// <param name="timeProvider">时间提供器。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public NumberGenerator(
        INumberingRuleRepository ruleRepository,
        INumberingAllocationRepository allocationRepository,
        INumberingFormatter formatter,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IUnitOfWorkManager unitOfWorkManager,
        TimeProvider timeProvider,
        ILogger<NumberGenerator> logger)
    {
        _ruleRepository = ruleRepository;
        _allocationRepository = allocationRepository;
        _formatter = formatter;
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
    /// <param name="cancellationToken">用于取消数据库操作和事务提交的取消令牌。</param>
    /// <returns>首次分配或幂等重放得到的单号结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">请求、规则、幂等或容量校验失败。</exception>
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
    /// <param name="cancellationToken">用于取消数据库操作和事务提交的取消令牌。</param>
    /// <returns>首次分配或幂等重放得到的连续编号结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">请求、批量上限、规则、幂等或容量校验失败。</exception>
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

        if (resolved.OwnerTenantId == 0)
        {
            // 平台作用域承担两件独立的事：
            // 一是让仓储的租户写谓词不再追加当前租户条件，使条件更新能命中 TenantId=0 的全局行；
            // 二是满足框架审计在写入分配记录时的租户归属校验。
            // 在库隔离部署下它还额外决定连接选择，因此必须在开启事务之前切换。
            using var platformScope = _currentTenant.Change(null);
            return await AllocateInTransactionAsync(resolved, requestTenantId, normalized, fingerprint, cancellationToken);
        }

        return await AllocateInTransactionAsync(resolved, requestTenantId, normalized, fingerprint, cancellationToken);
    }

    /// <summary>
    /// 在保证存在的数据库事务内执行一次分配。
    /// </summary>
    /// <param name="resolved">已经解析的实际规则位置和作用域。</param>
    /// <param name="requestTenantId">切换数据库前捕获的原请求租户。</param>
    /// <param name="request">已经校验并归一的请求。</param>
    /// <param name="fingerprint">绑定实际规则和请求参数的稳定指纹。</param>
    /// <param name="cancellationToken">用于取消事务和数据库操作的取消令牌。</param>
    /// <returns>本次新分配或从永久记录重建的结果。</returns>
    /// <remarks>
    /// 刻意不使用 <c>requiresNew</c>：框架的事务适配器发现连接上已有事务时会退化为空操作，
    /// 请求「新」工作单元只会得到一个不提交也不回滚的假事务。这里改为加入调用方事务
    /// （<c>Begin</c> 在已有工作单元时返回子工作单元，其 <c>CompleteAsync</c> 为空实现，由外层统一提交），
    /// 没有调用方事务时才真正开启并提交自己的事务。
    /// </remarks>
    /// <exception cref="UserFriendlyException">幂等冲突、规则不可用或流水耗尽。</exception>
    /// <exception cref="InvalidOperationException">处于非事务型工作单元内，连接上没有活动事务。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<NumberGenerationResult> AllocateInTransactionAsync(
        ResolvedNumberingRule resolved,
        long requestTenantId,
        NormalizedGenerationRequest request,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        using var unitOfWork = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(isTransactional: true));
        var result = await AllocateOnceAsync(resolved, requestTenantId, request, fingerprint, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// 在当前事务内执行幂等检查、周期翻转、流水推进和分配记录写入。
    /// </summary>
    /// <param name="resolved">已经解析的实际规则位置和作用域。</param>
    /// <param name="requestTenantId">原请求租户；平台或单体上下文为 0。</param>
    /// <param name="request">已经校验并归一的请求。</param>
    /// <param name="fingerprint">用于识别同幂等键参数冲突的请求指纹。</param>
    /// <param name="cancellationToken">用于取消本次事务内数据库操作的取消令牌。</param>
    /// <returns>本次新分配或从永久记录重建的结果。</returns>
    /// <exception cref="UserFriendlyException">幂等冲突、规则不存在、规则停用、全局规则未开放、时钟偏差或流水耗尽。</exception>
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

        // 这里只取用规则的格式与开放状态：首次发号后格式字段永久冻结，因此即便读取落在调用方事务的旧快照上也不会失真；
        // 流水值和周期基线一律不从这个实体读取，全部交给数据库语句自行判定。
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

        // 日期段、周期键和周期序号必须从同一个 UTC 瞬间换算，避免恰逢周期边界时得到互不一致的快照。
        var generatedTime = _timeProvider.GetUtcNow();
        var localTime = _formatter.ConvertToRuleTime(generatedTime, rule.TimeZoneId);
        var periodKey = _formatter.GetPeriodKey(localTime, rule.ResetCycle);
        var periodOrdinal = _formatter.GetPeriodOrdinal(localTime, rule.ResetCycle);
        var dateText = _formatter.GetDateText(localTime, rule.DateFormat);
        var maximum = _formatter.GetMaxValue(rule.SerialLength);

        // 周期翻转只允许向前：命中 0 行既可能是「已在本周期」也可能是「库中周期更新」，两者都不是错误，
        // 真正的判定交给随后的推进语句——它以周期键等值谓词把落后节点挡在外面。
        var rolledOver = await _ruleRepository.TryRollOverPeriodAsync(
            resolved.OwnerTenantId, resolved.RuleId, periodKey, periodOrdinal, cancellationToken);
        if (rolledOver)
        {
            _logger.LogInformation(
                "业务编号周期翻转：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, Period={Period}, PeriodOrdinal={PeriodOrdinal}",
                resolved.RuleId, rule.RuleCode, resolved.OwnerTenantId, periodKey, periodOrdinal);
        }

        var advanced = await _ruleRepository.TryAdvanceSequenceAsync(
            resolved.OwnerTenantId, resolved.RuleId, periodKey, request.Count, maximum, cancellationToken);
        if (!advanced)
        {
            throw await DescribeAdvanceRejectionAsync(
                resolved, rule.RuleCode, requestTenantId, periodKey, periodOrdinal, request.Count, cancellationToken);
        }

        // 推进语句已在该行取得排他锁并持有到提交，因此这里读到的就是本次推进后的确切值。
        var state = await _ruleRepository.ReadCurrentValueAsync(resolved.OwnerTenantId, resolved.RuleId, cancellationToken)
            ?? throw new UserFriendlyException("业务编号规则不存在。");
        var endValue = state.CurrentValue;
        var startValue = endValue - request.Count + 1;

        // 未发号规则的流水位数尚未冻结，可能在本次读取与推进之间被管理端改小；
        // 以持锁读回的权威位数复核容量，越界则回滚整个事务而不是发出超长编号。
        if (state.SerialLength != rule.SerialLength && endValue > _formatter.GetMaxValue(state.SerialLength))
        {
            throw new UserFriendlyException("业务编号规则的流水位数刚刚被修改，请重试。");
        }

        // 保存完整格式快照，使规则后续元数据变化或删除限制调整时仍能原样重建历史编号。
        var allocation = new SysNumberingAllocation
        {
            RuleId = resolved.RuleId,
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
            GeneratedTime = generatedTime,
            BusinessType = request.BusinessType,
            BusinessId = request.BusinessId
        };
        allocation = await _allocationRepository.AddAsync(allocation, cancellationToken);

        _logger.LogInformation(
            "业务编号分配成功：AllocationId={AllocationId}, RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Period={Period}, StartValue={StartValue}, EndValue={EndValue}, Count={Count}, Result=Success",
            allocation.BasicId, resolved.RuleId, rule.RuleCode, resolved.OwnerTenantId, requestTenantId, _currentUser.UserId, periodKey, startValue, endValue, request.Count);
        return BuildResult(allocation, resolved.ResolvedScope, false);
    }

    /// <summary>
    /// 读取规则实际状态，把「推进语句命中 0 行」翻译成可操作的失败原因。
    /// </summary>
    /// <param name="resolved">已经解析的实际规则位置和作用域。</param>
    /// <param name="ruleCode">规则编码，用于错误文案。</param>
    /// <param name="requestTenantId">原请求租户，用于日志。</param>
    /// <param name="periodKey">本次计算出的周期键。</param>
    /// <param name="periodOrdinal">本次计算出的周期序号。</param>
    /// <param name="count">本次请求数量。</param>
    /// <param name="cancellationToken">用于取消诊断查询的取消令牌。</param>
    /// <returns>描述具体原因的业务异常，由调用方抛出。</returns>
    /// <remarks>
    /// 诊断查询安全：推进语句命中 0 行不是数据库错误，事务仍然可用，因此可以就地读取实际状态。
    /// 各原因分别对应不同处置——停用要改配置、时钟偏差要查时间同步、容量耗尽要调整规则或等下一周期。
    /// </remarks>
    private async Task<UserFriendlyException> DescribeAdvanceRejectionAsync(
        ResolvedNumberingRule resolved,
        string ruleCode,
        long requestTenantId,
        string periodKey,
        long periodOrdinal,
        int count,
        CancellationToken cancellationToken)
    {
        var state = await _ruleRepository.ReadCurrentValueAsync(resolved.OwnerTenantId, resolved.RuleId, cancellationToken);
        if (state is not { } actual)
        {
            return new UserFriendlyException("业务编号规则不存在。");
        }

        if (actual.Status != EnableStatus.Enabled)
        {
            return new UserFriendlyException("业务编号规则已停用。");
        }

        if (actual.CurrentPeriodOrdinal > periodOrdinal)
        {
            // 库中周期已经走在本节点前面，说明本节点时钟落后。继续发号会重复上一周期已发出的编号，必须拒绝。
            _logger.LogError(
                "业务编号周期回退被拒绝，疑似节点时钟偏差：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, RequestTenantId={RequestTenantId}, StoredPeriod={StoredPeriod}, StoredOrdinal={StoredOrdinal}, ComputedPeriod={ComputedPeriod}, ComputedOrdinal={ComputedOrdinal}",
                resolved.RuleId, ruleCode, resolved.OwnerTenantId, requestTenantId,
                actual.CurrentPeriod, actual.CurrentPeriodOrdinal, periodKey, periodOrdinal);
            return new UserFriendlyException(
                $"编号规则“{ruleCode}”当前周期已推进到 {actual.CurrentPeriod}，本节点计算出 {periodKey}，请检查服务器时间同步。");
        }

        if (!string.Equals(actual.CurrentPeriod, periodKey, StringComparison.Ordinal))
        {
            // 序号不落后却键不一致，说明周期键与周期序号不同源，通常是重置周期被改动后未清空周期基线。
            _logger.LogError(
                "业务编号周期基线不一致：RuleId={RuleId}, RuleCode={RuleCode}, StoredPeriod={StoredPeriod}, StoredOrdinal={StoredOrdinal}, ComputedPeriod={ComputedPeriod}, ComputedOrdinal={ComputedOrdinal}",
                resolved.RuleId, ruleCode, actual.CurrentPeriod, actual.CurrentPeriodOrdinal, periodKey, periodOrdinal);
            return new UserFriendlyException($"编号规则“{ruleCode}”的周期基线异常，请联系管理员安全重置该规则。");
        }

        var maximum = _formatter.GetMaxValue(actual.SerialLength);
        if (actual.CurrentValue > maximum - count)
        {
            return new UserFriendlyException($"编号规则“{ruleCode}”当前周期流水已耗尽，请调整规则或等待下一周期。");
        }

        // 走到这里说明状态读起来完全允许推进，但语句确实没命中——只可能是并发窗口内规则又被改动。
        _logger.LogWarning(
            "业务编号推进被拒绝但状态检查全部通过：RuleId={RuleId}, RuleCode={RuleCode}, StoredPeriod={StoredPeriod}, StoredValue={StoredValue}, Count={Count}",
            resolved.RuleId, ruleCode, actual.CurrentPeriod, actual.CurrentValue, count);
        return new UserFriendlyException("业务编号规则刚刚被修改，请使用相同幂等键重试。");
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

        // 回退查询只需在平台上下文读取规则定位信息；真正分配会重新切换平台上下文后再开启事务。
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
            FingerprintFieldSeparator,
            ruleId.ToString(CultureInfo.InvariantCulture),
            request.RuleCode,
            ((int)request.Scope).ToString(CultureInfo.InvariantCulture),
            request.Count.ToString(CultureInfo.InvariantCulture),
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
            allocation.GeneratedTime,
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
