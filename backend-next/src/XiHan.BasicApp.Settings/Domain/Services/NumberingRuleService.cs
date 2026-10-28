// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 编号规则的查找与维护规则：租户看得见自己的规则与供租户使用的平台规则，只能改自己的；编码在所属一侧内唯一。
/// </summary>
/// <param name="rules">规则仓储。</param>
/// <param name="allocations">发号记录仓储。</param>
public sealed class NumberingRuleService(IRepository<SysNumberingRule, Guid> rules, IRepository<SysNumberingAllocation, Guid> allocations)
{
    /// <summary>
    /// 这条规则对这一侧是否可见：自己的；平台主体看平台的；租户看供租户使用的平台规则。
    /// </summary>
    public static bool IsVisible(SysNumberingRule rule, string owner)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return SettingsScope.IsOwnedBy(rule, owner) || (SettingsScope.IsPlatform(rule.OwnerTenantId) && rule.AllowTenantUse);
    }

    /// <summary>
    /// 按主键找这一侧可见的规则。
    /// </summary>
    public async Task<Maybe<SysNumberingRule>> FindVisibleAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await rules.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var rule) && IsVisible(rule, owner) ? Maybe.Some(rule) : Maybe.None<SysNumberingRule>();
    }

    /// <summary>
    /// 编码在这一侧是否已被占用。
    /// </summary>
    public async Task<bool> IsCodeTakenAsync(string code, string owner, CancellationToken cancellationToken)
        => await rules.CountAsync(EntityQuery.All<SysNumberingRule>().Where(new Specification<SysNumberingRule>(r => r.OwnerTenantId == owner && r.Code == code)), cancellationToken).ConfigureAwait(false) > 0;

    /// <summary>
    /// 某个周期里已发出的最大流水值；没发过为 0。
    /// </summary>
    public async Task<long> MaxIssuedAsync(Guid ruleId, string periodKey, CancellationToken cancellationToken)
    {
        var rows = await allocations.ListAsync(
            EntityQuery.All<SysNumberingAllocation>().Where(new Specification<SysNumberingAllocation>(a => a.RuleId == ruleId && a.PeriodKey == periodKey)).OrderBy(static a => a.EndValue, descending: true).Page(0, 1),
            cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? 0 : rows[0].EndValue;
    }

    /// <summary>
    /// 校验流水位数装得下已经保留的流水值（重置可能把当前值推到一个大数）。
    /// </summary>
    public static Result CheckCapacity(long reserved, int serialLength)
        => reserved <= NumberingFormatter.MaxValue(serialLength)
            ? Result.Success()
            : Error.Validation(SettingsErrors.NumberingCapacity, SettingsMessages.NumberingCapacity, MessageArguments.Of(("value", reserved), ("length", serialLength)));
}
