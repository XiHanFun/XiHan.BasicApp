// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 修改编号规则。
/// </summary>
/// <remarks>
/// <c>PUT /api/numbering-rules/{id}</c>。只能改本侧的（另一侧 <c>SETTINGS_DENIED_001</c>）。
/// 发过号后格式（前缀、分隔符、日期格式、流水位数、重置周期、时区）冻结，改了即 <c>SETTINGS_CONFLICT_009</c>；没发过号时改周期或时区会清掉周期基线；
/// 流水位数要装得下已保留的流水值（重置可能先推过）。并发发号改过规则行时并发戳不符，整笔失败。
/// </remarks>
/// <param name="rules">规则仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesUpdate)]
public sealed class UpdateNumberingRule(IRepository<SysNumberingRule, Guid> rules) : IOperation<UpdateNumberingRuleRequest, NumberingRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.update";

    /// <inheritdoc />
    public async ValueTask<Result<NumberingRuleResponse>> HandleAsync(UpdateNumberingRuleRequest request, InvocationContext context)
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

        var prefix = string.IsNullOrEmpty(request.Prefix) ? null : request.Prefix;
        var separator = request.Separator ?? string.Empty;
        var timeZoneId = request.TimeZoneId.Trim();
        var formatChanged = !string.Equals(prefix, rule.Prefix, StringComparison.Ordinal)
            || !string.Equals(separator, rule.Separator, StringComparison.Ordinal)
            || request.DateFormat != rule.DateFormat
            || request.SerialLength != rule.SerialLength
            || request.ResetCycle != rule.ResetCycle
            || !string.Equals(timeZoneId, rule.TimeZoneId, StringComparison.Ordinal);
        if (formatChanged)
        {
            if (rule.HasAllocated)
            {
                return Error.Conflict(SettingsErrors.NumberingRuleFrozen, SettingsMessages.NumberingRuleFrozen, MessageArguments.Of(("code", rule.Code)));
            }

            var valid = NumberingFormatter.Validate(request.DateFormat, request.ResetCycle, timeZoneId);
            if (valid.IsFailure)
            {
                return valid.Error!;
            }

            var capacity = NumberingRuleService.CheckCapacity(rule.CurrentValue, request.SerialLength);
            if (capacity.IsFailure)
            {
                return capacity.Error!;
            }

            if (request.ResetCycle != rule.ResetCycle || !string.Equals(timeZoneId, rule.TimeZoneId, StringComparison.Ordinal))
            {
                rule.CurrentPeriod = string.Empty;
                rule.CurrentPeriodOrdinal = SysNumberingRule.NoPeriodBaseline;
            }

            rule.Prefix = prefix;
            rule.Separator = separator;
            rule.DateFormat = request.DateFormat;
            rule.SerialLength = request.SerialLength;
            rule.ResetCycle = request.ResetCycle;
            rule.TimeZoneId = timeZoneId;
        }

        rule.Name = request.Name.Trim();
        rule.AllowTenantUse = SettingsScope.IsPlatform(owner) && request.AllowTenantUse;
        rule.Order = request.Order;
        rule.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        rule.Status = request.Status;
        var updated = await rules.UpdateAsync(rule, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : NumberingRuleResponse.From(rule);
    }
}
