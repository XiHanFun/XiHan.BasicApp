// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 业务编号规则领域服务实现。
/// </summary>
/// <remarks>
/// 本类型在当前租户上下文内维护规则不变量，不负责 HTTP 权限判断或切换任意目标租户。
/// 所有写方法都依赖上层应用服务提供事务边界，仓储更新通过实体 <c>RowVersion</c> 参与乐观并发控制。
/// </remarks>
public sealed class NumberingRuleDomainService : INumberingRuleDomainService
{
    /// <summary>
    /// 规则聚合仓储，用于作用域内唯一性检查以及规则创建、更新和软删除持久化。
    /// </summary>
    private readonly INumberingRuleRepository _ruleRepository;

    /// <summary>
    /// 永久分配记录仓储，用于判断规则是否存在历史发号并计算安全重置不可回退的下界。
    /// </summary>
    private readonly INumberingAllocationRepository _allocationRepository;

    /// <summary>
    /// 格式与容量校验器，集中验证日期格式、重置周期、时区和流水位数的安全组合。
    /// </summary>
    private readonly INumberingFormatter _formatter;

    /// <summary>
    /// 当前租户上下文，决定领域操作所维护的规则归属；领域层不接受外部任意租户 ID。
    /// </summary>
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 可替换时间源，用于安全重置时按规则时区判定当前周期。
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 结构化日志记录器，记录规则创建、更新、重置和删除时的领域结果与关键边界。
    /// </summary>
    private readonly ILogger<NumberingRuleDomainService> _logger;

    /// <summary>
    /// 初始化业务编号规则领域服务。
    /// </summary>
    /// <param name="ruleRepository">规则仓储。</param>
    /// <param name="allocationRepository">分配记录仓储。</param>
    /// <param name="formatter">格式策略。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="timeProvider">时间提供器；与发号器共用同一时间源，使周期边界行为可在测试中复现。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public NumberingRuleDomainService(
        INumberingRuleRepository ruleRepository,
        INumberingAllocationRepository allocationRepository,
        INumberingFormatter formatter,
        ICurrentTenant currentTenant,
        TimeProvider timeProvider,
        ILogger<NumberingRuleDomainService> logger)
    {
        _ruleRepository = ruleRepository;
        _allocationRepository = allocationRepository;
        _formatter = formatter;
        _currentTenant = currentTenant;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// 校验并创建当前租户作用域中的编号规则。
    /// </summary>
    /// <param name="command">创建命令，包含规则编码、格式、时区、状态和展示信息。</param>
    /// <param name="cancellationToken">用于取消唯一性查询和持久化操作的取消令牌。</param>
    /// <returns>包含已持久化规则实体的命令结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">规则字段、枚举、格式组合、时区或作用域内编码唯一性校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberingRuleCommandResult> CreateAsync(NumberingRuleCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var ownerTenantId = ResolveOwnerTenantId();
        var ruleCode = NormalizeRuleCode(command.RuleCode);
        ValidateEditableFields(command.RuleName, command.Prefix, command.Separator, command.DateFormat, command.SerialLength, command.ResetCycle, command.TimeZoneId, command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));

        // 唯一性检查必须包含停用规则；停用不释放规则编码，否则恢复旧规则时会出现同编码歧义。
        if (await _ruleRepository.FindByCodeInScopeAsync(ownerTenantId, ruleCode, false, cancellationToken) is not null)
        {
            throw new UserFriendlyException("当前作用域中已存在相同规则编码。");
        }

        var rule = new SysNumberingRule
        {
            RuleCode = ruleCode,
            RuleName = command.RuleName.Trim(),
            Prefix = NormalizeOptional(command.Prefix),
            Separator = command.Separator ?? string.Empty,
            DateFormat = command.DateFormat,
            SerialLength = command.SerialLength,
            ResetCycle = command.ResetCycle,
            TimeZoneId = command.TimeZoneId.Trim(),
            CurrentValue = 0,
            CurrentPeriod = string.Empty,
            CurrentPeriodOrdinal = SysNumberingRule.NoPeriodBaseline,
            HasAllocated = false,
            // 租户私有规则不存在跨租户开放语义，强制落为 false，避免配置含义漂移。
            AllowTenantUse = ownerTenantId == 0 && command.AllowTenantUse,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeOptional(command.Remark)
        };

        var saved = await _ruleRepository.AddAsync(rule, cancellationToken);
        _logger.LogInformation(
            "业务编号规则创建成功：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, AllowTenantUse={AllowTenantUse}",
            saved.BasicId, saved.RuleCode, ownerTenantId, saved.AllowTenantUse);
        return new NumberingRuleCommandResult(saved);
    }

    /// <summary>
    /// 在当前租户作用域内更新规则的可编辑字段。
    /// </summary>
    /// <param name="command">更新命令；规则编码和流水状态不允许通过该命令修改。</param>
    /// <param name="cancellationToken">用于取消规则查询和持久化操作的取消令牌。</param>
    /// <returns>包含更新后规则实体的命令结果。</returns>
    /// <remarks>首次分配后冻结影响编号文本和周期重置的字段；名称、排序、备注及全局开放状态仍可维护。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">规则不存在、无权操作、字段无效、修改冻结格式或位数无法容纳预留值。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberingRuleCommandResult> UpdateAsync(NumberingRuleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEditableFields(command.RuleName, command.Prefix, command.Separator, command.DateFormat, command.SerialLength, command.ResetCycle, command.TimeZoneId, command.Remark);

        var rule = await GetCurrentScopeRuleOrThrowAsync(command.BasicId, cancellationToken);
        // 冻结判断基于已成功分配标记，而不是当前流水值；周期重置后 CurrentValue 可能再次很小。
        if (rule.HasAllocated && IsFrozenFormatChanged(rule, command))
        {
            throw new UserFriendlyException("规则首次发号后，前缀、分隔符、日期格式、流水位数、重置周期和时区不可修改。");
        }

        if (!rule.HasAllocated && rule.CurrentValue > _formatter.GetMaxValue(command.SerialLength))
        {
            // 未发号规则也可能经过安全重置预留号段；缩短位数不能让已保存的下一值落到格式容量之外。
            throw new UserFriendlyException("当前预留流水值超过新的流水位数容量，请先安全重置到有效范围。");
        }

        // 重置周期决定周期键的宽度与周期序号的量级（20260729 / 202607 / 2026 / 0），
        // 规则时区决定同一时刻落在哪个周期。两者任一变化都会让旧基线不再可比：
        // 前者使单调守卫拿两种量级互相比较，后者可能让新算出的周期序号小于库中值而永久拒绝发号。
        // 只有未发号规则能走到这里：已发号规则的这两个字段已被上面的冻结校验拒绝。
        if (rule.ResetCycle != command.ResetCycle
            || !string.Equals(rule.TimeZoneId, command.TimeZoneId.Trim(), StringComparison.Ordinal))
        {
            rule.CurrentPeriod = string.Empty;
            rule.CurrentPeriodOrdinal = SysNumberingRule.NoPeriodBaseline;
            rule.CurrentValue = 0;
        }

        rule.RuleName = command.RuleName.Trim();
        rule.Prefix = NormalizeOptional(command.Prefix);
        rule.Separator = command.Separator ?? string.Empty;
        rule.DateFormat = command.DateFormat;
        rule.SerialLength = command.SerialLength;
        rule.ResetCycle = command.ResetCycle;
        rule.TimeZoneId = command.TimeZoneId.Trim();
        rule.AllowTenantUse = rule.IsGlobal && command.AllowTenantUse;
        rule.Sort = command.Sort;
        rule.Remark = NormalizeOptional(command.Remark);

        var saved = await _ruleRepository.UpdateAsync(rule, cancellationToken);
        _logger.LogInformation(
            "业务编号规则更新成功：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, HasAllocated={HasAllocated}",
            saved.BasicId, saved.RuleCode, saved.TenantId, saved.HasAllocated);
        return new NumberingRuleCommandResult(saved);
    }

    /// <summary>
    /// 更新当前租户作用域内规则的启停状态和可选备注。
    /// </summary>
    /// <param name="command">状态变更命令。</param>
    /// <param name="cancellationToken">用于取消规则查询和持久化操作的取消令牌。</param>
    /// <returns>包含状态变更后规则实体的命令结果。</returns>
    /// <remarks>停用规则只阻止后续分配，不清理当前流水或永久发号记录。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">规则不存在、无权操作或状态值无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberingRuleCommandResult> UpdateStatusAsync(NumberingRuleStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEnum(command.Status, nameof(command.Status));

        var rule = await GetCurrentScopeRuleOrThrowAsync(command.BasicId, cancellationToken);
        rule.Status = command.Status;
        rule.Remark = NormalizeOptional(command.Remark) ?? rule.Remark;
        var saved = await _ruleRepository.UpdateAsync(rule, cancellationToken);

        _logger.LogInformation(
            "业务编号规则状态更新成功：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, Status={Status}",
            saved.BasicId, saved.RuleCode, saved.TenantId, saved.Status);
        return new NumberingRuleCommandResult(saved);
    }

    /// <summary>
    /// 在当前周期已分配最大值约束下安全设置下一流水值。
    /// </summary>
    /// <param name="command">安全重置命令；原因必填，全局规则需要规则编码二次确认。</param>
    /// <param name="cancellationToken">用于取消规则查询、最大分配值查询和持久化操作的取消令牌。</param>
    /// <returns>包含重置后规则实体的命令结果。</returns>
    /// <remarks>
    /// 允许把下一值前移以主动形成空洞；禁止设置为小于等于当前周期永久记录最大结束值的值。
    /// 实体保存“最近已占用值”，因此写入值为调用方期望下一值减 1。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">原因、确认编码、流水容量或防重复区间校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberingRuleCommandResult> ResetAsync(NumberingRuleResetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
        {
            throw new UserFriendlyException("安全重置原因不能为空且不能超过 500 个字符。");
        }

        var rule = await GetCurrentScopeRuleOrThrowAsync(command.BasicId, cancellationToken);
        if (rule.IsGlobal && !string.Equals(rule.RuleCode, command.ConfirmRuleCode?.Trim(), StringComparison.Ordinal))
        {
            // 全局规则由多个租户共享，编码二次确认用于降低误操作其他共享序列的风险。
            throw new UserFriendlyException("全局规则重置确认编码不匹配。");
        }

        var maximum = _formatter.GetMaxValue(rule.SerialLength);
        if (command.NextValue is < 1 || command.NextValue > maximum)
        {
            throw new UserFriendlyException($"下一流水值必须在 1 至 {maximum} 之间。");
        }

        var localTime = _formatter.ConvertToRuleTime(_timeProvider.GetUtcNow(), rule.TimeZoneId);
        var periodKey = _formatter.GetPeriodKey(localTime, rule.ResetCycle);
        var periodOrdinal = _formatter.GetPeriodOrdinal(localTime, rule.ResetCycle);
        // 与发号器的周期翻转同一条单调规则：安全重置也不得把周期基线写回更早的周期。
        // 否则下一次发号会把本节点看不到的那个更新周期当作「新周期」翻转并清零，
        // 使该周期已经发出的编号被从头重发；而下方的防重复检查只统计本次算出的周期，看不到这种越界。
        if (periodOrdinal < rule.CurrentPeriodOrdinal)
        {
            _logger.LogError(
                "业务编号安全重置被拒绝，疑似节点时钟偏差：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, StoredPeriod={StoredPeriod}, StoredOrdinal={StoredOrdinal}, ComputedPeriod={ComputedPeriod}, ComputedOrdinal={ComputedOrdinal}",
                rule.BasicId, rule.RuleCode, rule.TenantId, rule.CurrentPeriod, rule.CurrentPeriodOrdinal, periodKey, periodOrdinal);
            throw new UserFriendlyException(
                $"规则当前周期已推进到 {rule.CurrentPeriod}，本节点计算出 {periodKey}，无法安全重置，请检查服务器时间同步。");
        }

        // 永久分配记录是真实发号边界；规则当前值可能被重置或因周期变化而不能代表历史最大值。
        var maximumAllocated = await _allocationRepository.GetMaximumEndValueAsync(rule.TenantId, rule.BasicId, periodKey, cancellationToken);
        if (command.NextValue <= maximumAllocated)
        {
            throw new UserFriendlyException($"当前周期已发号至 {maximumAllocated}，下一流水值不能回退到可能重复的范围。");
        }

        var previousValue = string.Equals(rule.CurrentPeriod, periodKey, StringComparison.Ordinal) ? rule.CurrentValue : 0L;
        rule.CurrentPeriod = periodKey;
        // 周期键与周期序号必须同源写入：发号器以序号做单调守卫、以键做归属判定，两者一旦不同步会让推进语句永远命中 0 行。
        rule.CurrentPeriodOrdinal = periodOrdinal;
        // 发号器按 CurrentValue + 1 分配，因此这里保存 NextValue - 1，避免重置后跳过调用方指定值。
        rule.CurrentValue = command.NextValue - 1;
        var saved = await _ruleRepository.UpdateAsync(rule, cancellationToken);

        _logger.LogWarning(
            "业务编号规则安全重置成功：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}, Period={Period}, PreviousValue={PreviousValue}, NextValue={NextValue}, Reason={Reason}",
            saved.BasicId, saved.RuleCode, saved.TenantId, periodKey, previousValue, command.NextValue, command.Reason.Trim());
        return new NumberingRuleCommandResult(saved);
    }

    /// <summary>
    /// 删除当前租户作用域内从未发号的规则。
    /// </summary>
    /// <param name="id">规则主键，必须为正数。</param>
    /// <param name="cancellationToken">用于取消规则查询和删除操作的取消令牌。</param>
    /// <remarks>已经发号的规则必须停用并保留，以保证历史分配记录始终可以追溯到规则。</remarks>
    /// <exception cref="UserFriendlyException">规则不存在、无权操作、已经发号或仓储删除失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rule = await GetCurrentScopeRuleOrThrowAsync(id, cancellationToken);
        if (rule.HasAllocated)
        {
            throw new UserFriendlyException("已经发号的规则不能删除，请停用后保留审计记录。");
        }

        if (!await _ruleRepository.DeleteAsync(rule, cancellationToken))
        {
            throw new UserFriendlyException("业务编号规则删除失败。");
        }

        _logger.LogInformation(
            "业务编号规则删除成功：RuleId={RuleId}, RuleCode={RuleCode}, OwnerTenantId={OwnerTenantId}",
            rule.BasicId, rule.RuleCode, rule.TenantId);
    }

    /// <summary>
    /// 取得当前上下文对应的规则所属租户。
    /// </summary>
    /// <returns>当前租户 ID；平台或单体上下文返回 0。</returns>
    private long ResolveOwnerTenantId()
    {
        return _currentTenant.Id is > 0 ? _currentTenant.Id.Value : 0L;
    }

    /// <summary>
    /// 按当前作用域加载规则，阻止租户态通过主键命中全局规则。
    /// </summary>
    /// <param name="id">待加载规则主键。</param>
    /// <param name="cancellationToken">用于取消数据库查询的取消令牌。</param>
    /// <returns>当前租户作用域中匹配的规则实体。</returns>
    /// <exception cref="UserFriendlyException">主键无效，或规则不存在/不属于当前作用域。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<SysNumberingRule> GetCurrentScopeRuleOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new UserFriendlyException("规则主键无效。");
        }

        var ownerTenantId = ResolveOwnerTenantId();
        return await _ruleRepository.FindByIdInScopeAsync(ownerTenantId, id, cancellationToken)
            ?? throw new UserFriendlyException("业务编号规则不存在或无权操作。");
    }

    /// <summary>
    /// 校验并归一规则编码。
    /// </summary>
    /// <param name="ruleCode">调用方提交的规则编码。</param>
    /// <returns>去除首尾空白的稳定规则编码。</returns>
    /// <exception cref="UserFriendlyException">编码为空、超过 100 个字符或包含空白字符。</exception>
    private static string NormalizeRuleCode(string ruleCode)
    {
        if (string.IsNullOrWhiteSpace(ruleCode))
        {
            throw new UserFriendlyException("规则编码不能为空。");
        }

        var normalized = ruleCode.Trim();
        if (normalized.Length > 100 || normalized.Any(char.IsWhiteSpace))
        {
            throw new UserFriendlyException("规则编码不能超过 100 个字符且不能包含空白字符。");
        }

        return normalized;
    }

    /// <summary>
    /// 校验规则可编辑字段和格式安全组合。
    /// </summary>
    /// <param name="ruleName">规则名称。</param>
    /// <param name="prefix">可选编号前缀。</param>
    /// <param name="separator">编号段分隔符。</param>
    /// <param name="dateFormat">日期段格式。</param>
    /// <param name="serialLength">流水位数。</param>
    /// <param name="resetCycle">流水重置周期。</param>
    /// <param name="timeZoneId">规则时区 ID。</param>
    /// <param name="remark">可选备注。</param>
    /// <exception cref="UserFriendlyException">字段长度、格式组合、流水容量或时区校验失败。</exception>
    private void ValidateEditableFields(
        string ruleName,
        string? prefix,
        string? separator,
        NumberingDateFormat dateFormat,
        int serialLength,
        NumberingResetCycle resetCycle,
        string timeZoneId,
        string? remark)
    {
        if (string.IsNullOrWhiteSpace(ruleName) || ruleName.Trim().Length > 100)
        {
            throw new UserFriendlyException("规则名称不能为空且不能超过 100 个字符。");
        }

        if (NormalizeOptional(prefix)?.Length > 50)
        {
            throw new UserFriendlyException("编号前缀不能超过 50 个字符。");
        }

        if ((separator ?? string.Empty).Length > 10)
        {
            throw new UserFriendlyException("分隔符不能超过 10 个字符。");
        }

        if (NormalizeOptional(remark)?.Length > 500)
        {
            throw new UserFriendlyException("备注不能超过 500 个字符。");
        }

        try
        {
            // 复用纯格式器的唯一校验入口，保证规则维护、预览和真实发号接受相同配置集合。
            _formatter.Validate(dateFormat, resetCycle, serialLength, timeZoneId);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new UserFriendlyException(exception.Message, innerException: exception);
        }
    }

    /// <summary>
    /// 判断已经发号的规则是否修改了被冻结的格式字段。
    /// </summary>
    /// <param name="rule">数据库中的现有规则。</param>
    /// <param name="command">调用方提交的更新命令。</param>
    /// <returns>任一影响编号文本或周期重置的字段发生变化时返回 <see langword="true"/>。</returns>
    private static bool IsFrozenFormatChanged(SysNumberingRule rule, NumberingRuleUpdateCommand command)
    {
        return !string.Equals(NormalizeOptional(rule.Prefix), NormalizeOptional(command.Prefix), StringComparison.Ordinal)
            || !string.Equals(rule.Separator, command.Separator ?? string.Empty, StringComparison.Ordinal)
            || rule.DateFormat != command.DateFormat
            || rule.SerialLength != command.SerialLength
            || rule.ResetCycle != command.ResetCycle
            || !string.Equals(rule.TimeZoneId, command.TimeZoneId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 归一可选文本。
    /// </summary>
    /// <param name="value">待归一文本。</param>
    /// <returns>去除首尾空白的文本；空值或纯空白返回 <see langword="null"/>。</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 校验枚举值是否已定义。
    /// </summary>
    /// <typeparam name="TEnum">待校验枚举类型。</typeparam>
    /// <param name="value">枚举值。</param>
    /// <param name="parameterName">用于友好错误消息的参数名称。</param>
    /// <exception cref="UserFriendlyException"><paramref name="value"/> 不是已定义枚举值。</exception>
    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new UserFriendlyException($"{parameterName} 枚举值无效。");
        }
    }
}
