// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 约束规则的一个规则项（写入用）。
/// </summary>
/// <param name="TargetId">目标：角色 ID、权限码或用户 ID。</param>
/// <param name="GroupNo">组号。</param>
/// <param name="Remark">备注。</param>
public sealed record ConstraintItemDefinition(string TargetId, int GroupNo, string? Remark);

/// <summary>
/// 约束规则的内容（新建与修改共用）。
/// </summary>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">约束类型。</param>
/// <param name="TargetType">目标类型。</param>
/// <param name="MaxAllowed">数量上限。</param>
/// <param name="Conditions">条件类的条件。</param>
/// <param name="ViolationAction">违规处理方式。</param>
/// <param name="Priority">优先级。</param>
/// <param name="Status">状态。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="Description">说明。</param>
/// <param name="Remark">备注。</param>
/// <param name="Items">规则项（整组替换）。</param>
public sealed record ConstraintRuleDefinition(
    string Code,
    string Name,
    ConstraintType Type,
    ConstraintTargetType TargetType,
    int MaxAllowed,
    IReadOnlyList<PolicyConditionDefinition> Conditions,
    ConstraintViolationAction ViolationAction,
    int Priority,
    ConstraintRuleStatus Status,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset? ExpiresAt,
    string? Description,
    string? Remark,
    IReadOnlyList<ConstraintItemDefinition> Items);

/// <summary>
/// 约束规则的写入规则。
/// </summary>
/// <remarks>
/// 对应 4.x 的 ConstraintRuleDomainService。
/// 编码同一侧唯一、不含空白；目标类型须是约束类型用得了的
/// （职责分离与条件只约束角色，互斥只约束权限码，基数约束角色或用户）；至少一个规则项，职责分离与互斥至少两个，目标不能重复，
/// 且须是这一侧可用的（启用中的角色、目录里作用侧相符的权限码、这一侧的用户）；条件类的条件按条件引擎编译。写入后这一侧的规则缓存失效。
/// </remarks>
/// <param name="rules">规则仓储。</param>
/// <param name="items">规则项仓储。</param>
/// <param name="roles">角色查找。</param>
/// <param name="users">用户仓储。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="policies">访问策略服务（条件的校验与编译共用）。</param>
/// <param name="cache">规则缓存。</param>
/// <param name="ids">标识生成器。</param>
public sealed class ConstraintRuleService(
    IRepository<SysConstraintRule, Guid> rules,
    IRepository<SysConstraintRuleItem, Guid> items,
    RoleService roles,
    IRepository<SysUser, Guid> users,
    PermissionCatalog catalog,
    AccessPolicyService policies,
    ConstraintRules cache,
    IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 按 ID 取这一侧的规则。
    /// </summary>
    public async Task<Maybe<SysConstraintRule>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await rules.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var rule) && string.Equals(rule.OwnerTenantId, owner, StringComparison.Ordinal) ? Maybe.Some(rule) : Maybe.None<SysConstraintRule>();
    }

    /// <summary>
    /// 一批规则的规则项。
    /// </summary>
    public async Task<ILookup<Guid, SysConstraintRuleItem>> ItemsOfAsync(string owner, IReadOnlyCollection<Guid> ruleIds, CancellationToken cancellationToken)
    {
        if (ruleIds.Count == 0)
        {
            return Array.Empty<SysConstraintRuleItem>().ToLookup(static i => i.RuleId);
        }

        var list = ruleIds.ToList();
        var rows = await items.ListAsync(EntityQuery.All<SysConstraintRuleItem>().Where(new Specification<SysConstraintRuleItem>(i => i.OwnerTenantId == owner && list.Contains(i.RuleId))), cancellationToken).ConfigureAwait(false);
        return rows.OrderBy(static i => i.GroupNo).ThenBy(static i => i.CreatedAt).ToLookup(static i => i.RuleId);
    }

    /// <summary>
    /// 新建。
    /// </summary>
    public async Task<Result<SysConstraintRule>> CreateAsync(string owner, ConstraintRuleDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var rule = new SysConstraintRule { Id = ids.NewId(), OwnerTenantId = owner };
        var applied = await ApplyAsync(rule, definition, cancellationToken).ConfigureAwait(false);
        if (!applied.TryGetValue(out var normalized))
        {
            return applied.Error!;
        }

        var added = await rules.AddAsync(rule, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var written = await ReplaceItemsAsync(rule, normalized, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return written.Error!;
        }

        await cache.InvalidateAsync(owner, cancellationToken).ConfigureAwait(false);
        return rule;
    }

    /// <summary>
    /// 整体修改（含启停）。
    /// </summary>
    public async Task<Result> UpdateAsync(SysConstraintRule rule, ConstraintRuleDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(definition);
        var applied = await ApplyAsync(rule, definition, cancellationToken).ConfigureAwait(false);
        if (!applied.TryGetValue(out var normalized))
        {
            return applied.Error!;
        }

        var updated = await rules.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        var written = await ReplaceItemsAsync(rule, normalized, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return written;
        }

        await cache.InvalidateAsync(rule.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 启用或停用约束规则。
    /// </summary>
    /// <remarks>
    /// 启用时重新校验规则项（目标可能已停用或删除，4.x 同）。
    /// </remarks>
    public async Task<Result> SetStatusAsync(SysConstraintRule rule, ConstraintRuleStatus status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (status == ConstraintRuleStatus.Active)
        {
            var current = (await ItemsOfAsync(rule.OwnerTenantId, [rule.Id], cancellationToken).ConfigureAwait(false))[rule.Id]
                .Select(static i => new ConstraintItemDefinition(i.TargetId, i.GroupNo, i.Remark))
                .ToList();
            var checkedItems = await CheckItemsAsync(rule.OwnerTenantId, rule.Type, rule.TargetType, current, cancellationToken).ConfigureAwait(false);
            if (checkedItems.IsFailure)
            {
                return checkedItems.Error!;
            }
        }

        rule.Status = status;
        var updated = await rules.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        await cache.InvalidateAsync(rule.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 删除（连同规则项）。
    /// </summary>
    public async Task<Result> DeleteAsync(SysConstraintRule rule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var cleared = await ReplaceItemsAsync(rule, [], cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return cleared;
        }

        var removed = await rules.RemoveAsync(rule, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        await cache.InvalidateAsync(rule.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 取规则项目标的显示名与编码。
    /// </summary>
    /// <remarks>
    /// 角色取名称与编码，权限码取目录里的显示名，用户取显示名与登录名。
    /// </remarks>
    public async Task<IReadOnlyDictionary<(ConstraintTargetType Type, string TargetId), (string Name, string? Code)>> TargetNamesAsync(string owner, IEnumerable<(ConstraintTargetType Type, string TargetId)> targets, CancellationToken cancellationToken)
    {
        var list = targets.Distinct().ToList();
        var names = new Dictionary<(ConstraintTargetType, string), (string, string?)>();
        var roleIds = list.Where(static t => t.Type == ConstraintTargetType.Role).Select(static t => Guid.TryParse(t.TargetId, out var id) ? id : Guid.Empty).Where(static id => id != Guid.Empty).Distinct().ToList();
        foreach (var role in await roles.FindManyAsync(roleIds, owner, cancellationToken).ConfigureAwait(false))
        {
            names[(ConstraintTargetType.Role, role.Id.ToString("D"))] = (role.Name, role.Code);
        }

        var userIds = list.Where(static t => t.Type == ConstraintTargetType.User).Select(static t => Guid.TryParse(t.TargetId, out var id) ? id : Guid.Empty).Where(static id => id != Guid.Empty).Distinct().ToList();
        foreach (var user in await users.FindManyAsync(userIds, cancellationToken).ConfigureAwait(false))
        {
            names[(ConstraintTargetType.User, user.Id.ToString("D"))] = (user.DisplayName, user.UserName);
        }

        foreach (var (_, code) in list.Where(static t => t.Type == ConstraintTargetType.Permission))
        {
            if (catalog.Find(code) is { } descriptor)
            {
                names[(ConstraintTargetType.Permission, code)] = (descriptor.DisplayName, code);
            }
        }

        return names;
    }

    private async Task<Result<IReadOnlyList<ConstraintItemDefinition>>> ApplyAsync(SysConstraintRule rule, ConstraintRuleDefinition definition, CancellationToken cancellationToken)
    {
        var code = definition.Code.Trim();
        if (code.Length == 0 || code.Any(char.IsWhiteSpace))
        {
            return Error.Validation(AccessErrors.ConstraintCodeInvalid, AccessMessages.ConstraintCodeInvalid, MessageArguments.Of(("code", definition.Code)));
        }

        if (definition.EffectiveAt is { } from && definition.ExpiresAt is { } to && to <= from)
        {
            return Error.Validation(AccessErrors.PolicyWindowInvalid, AccessMessages.PolicyWindowInvalid);
        }

        string? conditions = null;
        if (definition.Type == ConstraintType.Condition)
        {
            if (definition.Conditions.Count == 0)
            {
                return Error.Validation(AccessErrors.ConstraintConditionsRequired, AccessMessages.ConstraintConditionsRequired, MessageArguments.Of(("maxGroups", SysAccessPolicy.MaxGroups), ("maxConditions", SysAccessPolicy.MaxConditions)));
            }

            var compiled = policies.Compile(definition.Conditions);
            if (compiled.IsFailure)
            {
                return compiled.Error!.Code == AccessErrors.PolicyConditionsExceeded
                    ? Error.Validation(AccessErrors.ConstraintConditionsRequired, AccessMessages.ConstraintConditionsRequired, MessageArguments.Of(("maxGroups", SysAccessPolicy.MaxGroups), ("maxConditions", SysAccessPolicy.MaxConditions)))
                    : compiled.Error!;
            }

            conditions = JsonSerializer.Serialize(AccessPolicyService.ViewOf(Guid.Empty, string.Empty, string.Empty, AccessPolicyAction.LogOnly, 0, null, null, definition.Conditions).Conditions, AccessJsonContext.Default.ListPolicyClauseView);
        }

        var checkedItems = await CheckItemsAsync(rule.OwnerTenantId, definition.Type, definition.TargetType, definition.Items, cancellationToken).ConfigureAwait(false);
        if (!checkedItems.TryGetValue(out var normalized))
        {
            return checkedItems.Error!;
        }

        var taken = await rules.ListAsync(EntityQuery.All<SysConstraintRule>().Where(new Specification<SysConstraintRule>(r => r.OwnerTenantId == rule.OwnerTenantId && r.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        if (taken.Count > 0 && taken[0].Id != rule.Id)
        {
            return Error.Conflict(AccessErrors.ConstraintCodeTaken, AccessMessages.ConstraintCodeTaken, MessageArguments.Of(("code", code)));
        }

        rule.Code = code;
        rule.Name = definition.Name.Trim();
        rule.Type = definition.Type;
        rule.TargetType = definition.TargetType;
        rule.MaxAllowed = definition.Type == ConstraintType.Condition ? 1 : definition.MaxAllowed;
        rule.Conditions = conditions;
        rule.ViolationAction = definition.ViolationAction;
        rule.Priority = definition.Priority;
        rule.Status = definition.Status;
        rule.EffectiveAt = definition.EffectiveAt;
        rule.ExpiresAt = definition.ExpiresAt;
        rule.Description = Trimmed(definition.Description);
        rule.Remark = Trimmed(definition.Remark);
        return Result.Success(normalized);
    }

    // 目标类型、项数、重复与目标可用；角色与用户的 ID 规整成小写的标准写法
    private async Task<Result<IReadOnlyList<ConstraintItemDefinition>>> CheckItemsAsync(string owner, ConstraintType type, ConstraintTargetType targetType, IReadOnlyList<ConstraintItemDefinition> definitions, CancellationToken cancellationToken)
    {
        var fits = type switch
        {
            ConstraintType.MutualExclusion => targetType == ConstraintTargetType.Permission,
            ConstraintType.Cardinality => targetType is ConstraintTargetType.Role or ConstraintTargetType.User,
            _ => targetType == ConstraintTargetType.Role,
        };
        if (!fits)
        {
            return Error.Validation(AccessErrors.ConstraintTargetMismatch, AccessMessages.ConstraintTargetMismatch, MessageArguments.Of(("type", type.ToString()), ("targetType", targetType.ToString())));
        }

        var min = type is ConstraintType.Ssd or ConstraintType.Dsd or ConstraintType.MutualExclusion ? 2 : 1;
        if (definitions.Count < min)
        {
            return Error.Validation(AccessErrors.ConstraintItemsTooFew, AccessMessages.ConstraintItemsTooFew, MessageArguments.Of(("min", min)));
        }

        var normalized = new List<ConstraintItemDefinition>(definitions.Count);
        foreach (var item in definitions)
        {
            var raw = item.TargetId.Trim();
            string target;
            if (targetType == ConstraintTargetType.Permission)
            {
                target = raw;
            }
            else if (Guid.TryParse(raw, out var id))
            {
                target = id.ToString("D");
            }
            else
            {
                return Error.Validation(AccessErrors.ConstraintTargetInvalid, AccessMessages.ConstraintTargetInvalid, MessageArguments.Of(("target", raw)));
            }

            if (normalized.Any(n => string.Equals(n.TargetId, target, StringComparison.Ordinal)))
            {
                return Error.Validation(AccessErrors.ConstraintItemDuplicate, AccessMessages.ConstraintItemDuplicate, MessageArguments.Of(("target", target)));
            }

            normalized.Add(new ConstraintItemDefinition(target, item.GroupNo, Trimmed(item.Remark)));
        }

        var missing = await FirstUnavailableAsync(owner, targetType, normalized.Select(static n => n.TargetId).ToList(), cancellationToken).ConfigureAwait(false);
        return missing is null
            ? normalized
            : Error.Validation(AccessErrors.ConstraintTargetInvalid, AccessMessages.ConstraintTargetInvalid, MessageArguments.Of(("target", missing)));
    }

    // 这一侧可用的目标：启用中的角色、目录里作用侧相符的权限码、注册地是这一侧的用户
    private async Task<string?> FirstUnavailableAsync(string owner, ConstraintTargetType targetType, IReadOnlyList<string> targets, CancellationToken cancellationToken)
    {
        switch (targetType)
        {
            case ConstraintTargetType.Role:
                {
                    var found = (await roles.FindManyAsync([.. targets.Select(Guid.Parse)], owner, cancellationToken).ConfigureAwait(false))
                        .Where(static r => r.Status == RoleStatus.Active)
                        .Select(static r => r.Id.ToString("D"))
                        .ToHashSet(StringComparer.Ordinal);
                    return targets.FirstOrDefault(t => !found.Contains(t));
                }

            case ConstraintTargetType.User:
                {
                    var found = (await users.FindManyAsync([.. targets.Select(Guid.Parse)], cancellationToken).ConfigureAwait(false))
                        .Where(u => string.Equals(u.HomeTenantId, owner, StringComparison.Ordinal))
                        .Select(static u => u.Id.ToString("D"))
                        .ToHashSet(StringComparer.Ordinal);
                    return targets.FirstOrDefault(t => !found.Contains(t));
                }

            default:
                {
                    var platform = RoleService.IsPlatform(owner);
                    return targets.FirstOrDefault(t => catalog.Find(t) is not { } descriptor || !PermissionCatalog.AllowsSide(descriptor, platform));
                }
        }
    }

    private async Task<Result> ReplaceItemsAsync(SysConstraintRule rule, IReadOnlyList<ConstraintItemDefinition> definitions, CancellationToken cancellationToken)
    {
        var existing = (await ItemsOfAsync(rule.OwnerTenantId, [rule.Id], cancellationToken).ConfigureAwait(false))[rule.Id].ToList();
        if (existing.Count > 0)
        {
            foreach (var outcome in await items.RemoveRangeAsync(existing, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        if (definitions.Count == 0)
        {
            return Result.Success();
        }

        var rows = definitions.Select(d => new SysConstraintRuleItem { Id = ids.NewId(), OwnerTenantId = rule.OwnerTenantId, RuleId = rule.Id, TargetId = d.TargetId, GroupNo = d.GroupNo, Remark = d.Remark }).ToList();
        foreach (var outcome in await items.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
