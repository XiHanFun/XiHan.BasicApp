// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 安全重置流水。
/// </summary>
/// <remarks>
/// <c>POST /api/numbering-rules/{id}:reset</c>。只能重置本侧的；平台规则要回填规则编码（<c>SETTINGS_VALIDATION_008</c>）；理由必填（进操作审计）。
/// 按当前时刻的周期：下一个值要在位数范围内（<c>SETTINGS_VALIDATION_007</c>），且大于本周期已发出的最大值（<c>SETTINGS_VALIDATION_010</c>，可向前跳、不能回退）；
/// 周期序号不能后退（<c>SETTINGS_CONFLICT_012</c>）。存成当前值 = 下一个值 − 1；期间有并发发号即并发戳不符，整笔失败。
/// </remarks>
/// <param name="rules">规则仓储。</param>
/// <param name="service">规则域服务。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesReset)]
public sealed class ResetNumberingRule(IRepository<SysNumberingRule, Guid> rules, NumberingRuleService service, TimeProvider clock) : IOperation<ResetNumberingRuleRequest, NumberingRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.reset";

    /// <inheritdoc />
    public async ValueTask<Result<NumberingRuleResponse>> HandleAsync(ResetNumberingRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await rules.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule) || !NumberingRuleService.IsVisible(rule, owner))
        {
            return Error.NotFound(SettingsErrors.NumberingRuleNotFound, SettingsMessages.NumberingRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (!SettingsScope.IsOwnedBy(rule, owner))
        {
            return Error.Denied(SettingsErrors.OtherSide, SettingsMessages.OtherSide, MessageArguments.Of(("id", request.Id)));
        }

        if (SettingsScope.IsPlatform(rule.OwnerTenantId) && !string.Equals(request.ConfirmCode?.Trim(), rule.Code, StringComparison.Ordinal))
        {
            return Error.Validation(SettingsErrors.NumberingResetConfirmation, SettingsMessages.NumberingResetConfirmation, MessageArguments.Of(("code", rule.Code)));
        }

        var next = long.Parse(request.NextValue, NumberStyles.None, CultureInfo.InvariantCulture);
        var capacity = NumberingRuleService.CheckCapacity(next, rule.SerialLength);
        if (capacity.IsFailure)
        {
            return capacity.Error!;
        }

        if (!NumberingFormatter.TryResolveTimeZone(rule.TimeZoneId, out var timeZone))
        {
            return Error.Validation(SettingsErrors.TimeZoneInvalid, SettingsMessages.TimeZoneInvalid, MessageArguments.Of(("timeZone", rule.TimeZoneId)));
        }

        var local = NumberingFormatter.ToRuleTime(clock.GetUtcNow(), timeZone);
        var ordinal = NumberingFormatter.PeriodOrdinal(local, rule.ResetCycle);
        if (ordinal < rule.CurrentPeriodOrdinal)
        {
            return Error.Conflict(SettingsErrors.NumberingClockBehind, SettingsMessages.NumberingClockBehind, MessageArguments.Of(("code", rule.Code), ("period", rule.CurrentPeriod)));
        }

        var periodKey = NumberingFormatter.PeriodKey(local, rule.ResetCycle);
        var issued = await service.MaxIssuedAsync(rule.Id, periodKey, context.CancellationToken).ConfigureAwait(false);
        if (next <= issued)
        {
            return Error.Validation(SettingsErrors.NumberingResetBelowIssued, SettingsMessages.NumberingResetBelowIssued, MessageArguments.Of(("issued", issued)));
        }

        rule.CurrentPeriod = periodKey;
        rule.CurrentPeriodOrdinal = ordinal;
        rule.CurrentValue = next - 1;
        var updated = await rules.UpdateAsync(rule, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : NumberingRuleResponse.From(rule);
    }
}
