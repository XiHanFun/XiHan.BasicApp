// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 一条字段安全规则的定义（新建与修改共用）。
/// </summary>
/// <param name="TargetKind">目标类型。</param>
/// <param name="TargetId">目标 ID。</param>
/// <param name="Entity">实体码。</param>
/// <param name="Field">字段名。</param>
/// <param name="Strategy">读取方式。</param>
/// <param name="KeepHead">部分脱敏保留前几位。</param>
/// <param name="KeepTail">部分脱敏保留后几位。</param>
/// <param name="Replacement">固定文本。</param>
/// <param name="IsEditable">能否改写。</param>
/// <param name="Status">状态。</param>
/// <param name="Remark">备注。</param>
public sealed record FieldRuleDefinition(FieldSecurityTargetKind TargetKind, Guid TargetId, string Entity, string Field, FieldMaskStrategy Strategy, int? KeepHead, int? KeepTail, string? Replacement, bool IsEditable, FieldRuleStatus Status, string? Remark);

/// <summary>
/// 字段安全规则的维护。
/// </summary>
/// <remarks>
/// 见方案 5.9，校验沿用 4.x。
/// 实体与字段须在目录里；非文本字段只能明文只读或隐藏，响应里不可空的字段不能隐藏；明文且可改的规则什么都没限制；
/// 部分脱敏要保留位数（0 到 32，前后不能都是 0）、固定文本要文字（不超过 100 字）；目标须在这一侧且启用（部门只在租户侧）；同一侧同一目标同一字段只有一条。写入后失效该实体的规则缓存。
/// </remarks>
/// <param name="rules">规则仓储。</param>
/// <param name="roles">角色仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="departments">部门仓储（租户库）。</param>
/// <param name="catalog">字段安全实体目录。</param>
/// <param name="cache">规则缓存。</param>
/// <param name="ids">标识生成器。</param>
public sealed class FieldSecurityService(
    IRepository<SysFieldSecurityRule, Guid> rules,
    IRepository<SysRole, Guid> roles,
    IRepository<SysUser, Guid> users,
    IRepository<SysDepartment, Guid> departments,
    FieldSecurityCatalog catalog,
    FieldSecurityRules cache,
    IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 按 ID 取这一侧的规则。
    /// </summary>
    public async Task<Maybe<SysFieldSecurityRule>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
        => (await rules.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var rule) && string.Equals(rule.OwnerTenantId, owner, StringComparison.Ordinal)
            ? Maybe.Some(rule)
            : Maybe.None<SysFieldSecurityRule>();

    /// <summary>
    /// 新建一条规则。
    /// </summary>
    public async Task<Result<SysFieldSecurityRule>> CreateAsync(string owner, FieldRuleDefinition definition, CancellationToken cancellationToken)
    {
        var checkedDefinition = await CheckAsync(owner, definition, null, cancellationToken).ConfigureAwait(false);
        if (!checkedDefinition.TryGetValue(out var normalized))
        {
            return checkedDefinition.Error!;
        }

        var rule = new SysFieldSecurityRule { Id = ids.NewId(), OwnerTenantId = owner };
        Apply(rule, normalized);
        var added = await rules.AddAsync(rule, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        await cache.InvalidateAsync(owner, rule.Entity, cancellationToken).ConfigureAwait(false);
        return rule;
    }

    /// <summary>
    /// 整体修改一条规则（实体可以换，换了即失效新旧两个实体的缓存）。
    /// </summary>
    public async Task<Result<SysFieldSecurityRule>> UpdateAsync(SysFieldSecurityRule rule, FieldRuleDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var checkedDefinition = await CheckAsync(rule.OwnerTenantId, definition, rule.Id, cancellationToken).ConfigureAwait(false);
        if (!checkedDefinition.TryGetValue(out var normalized))
        {
            return checkedDefinition.Error!;
        }

        var previousEntity = rule.Entity;
        Apply(rule, normalized);
        var updated = await rules.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await cache.InvalidateAsync(rule.OwnerTenantId, previousEntity, cancellationToken).ConfigureAwait(false);
        await cache.InvalidateAsync(rule.OwnerTenantId, rule.Entity, cancellationToken).ConfigureAwait(false);
        return rule;
    }

    /// <summary>
    /// 删除一条规则。
    /// </summary>
    public async Task<Result> DeleteAsync(SysFieldSecurityRule rule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var removed = await rules.RemoveAsync(rule, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        await cache.InvalidateAsync(rule.OwnerTenantId, rule.Entity, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 目标没了（删角色）：连同删掉以它为目标的规则。
    /// </summary>
    public async Task<Result> RemoveForTargetAsync(string owner, FieldSecurityTargetKind kind, Guid targetId, CancellationToken cancellationToken)
    {
        var rows = await rules.ListAsync(EntityQuery.All<SysFieldSecurityRule>().Where(new Specification<SysFieldSecurityRule>(r => r.OwnerTenantId == owner && r.TargetKind == kind && r.TargetId == targetId)), cancellationToken).ConfigureAwait(false);
        foreach (var row in rows)
        {
            var removed = await DeleteAsync(row, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 取规则目标的显示名（角色名、用户显示名、部门名）。
    /// </summary>
    /// <remarks>
    /// 目标已不存在的不在结果里。
    /// </remarks>
    public async Task<IReadOnlyDictionary<(FieldSecurityTargetKind Kind, Guid Id), string>> TargetNamesAsync(IReadOnlyCollection<SysFieldSecurityRule> rows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var names = new Dictionary<(FieldSecurityTargetKind, Guid), string>();
        var roleIds = IdsOf(rows, FieldSecurityTargetKind.Role);
        if (roleIds.Count > 0)
        {
            foreach (var role in await roles.ListAsync(EntityQuery.All<SysRole>().Where(new Specification<SysRole>(r => roleIds.Contains(r.Id))), cancellationToken).ConfigureAwait(false))
            {
                names[(FieldSecurityTargetKind.Role, role.Id)] = role.Name;
            }
        }

        var userIds = IdsOf(rows, FieldSecurityTargetKind.User);
        if (userIds.Count > 0)
        {
            foreach (var user in await users.ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => userIds.Contains(u.Id))), cancellationToken).ConfigureAwait(false))
            {
                names[(FieldSecurityTargetKind.User, user.Id)] = $"{user.DisplayName} (@{user.UserName})";
            }
        }

        var departmentIds = IdsOf(rows, FieldSecurityTargetKind.Department);
        if (departmentIds.Count > 0)
        {
            foreach (var department in await departments.ListAsync(EntityQuery.All<SysDepartment>().Where(new Specification<SysDepartment>(d => departmentIds.Contains(d.Id))), cancellationToken).ConfigureAwait(false))
            {
                names[(FieldSecurityTargetKind.Department, department.Id)] = department.Name;
            }
        }

        return names;
    }

    private static List<Guid> IdsOf(IReadOnlyCollection<SysFieldSecurityRule> rows, FieldSecurityTargetKind kind)
        => rows.Where(r => r.TargetKind == kind).Select(static r => r.TargetId).Distinct().ToList();

    private static void Apply(SysFieldSecurityRule rule, FieldRuleDefinition definition)
    {
        rule.TargetKind = definition.TargetKind;
        rule.TargetId = definition.TargetId;
        rule.Entity = definition.Entity;
        rule.Field = definition.Field;
        rule.Strategy = definition.Strategy;
        rule.KeepHead = definition.KeepHead;
        rule.KeepTail = definition.KeepTail;
        rule.Replacement = definition.Replacement;
        rule.IsEditable = definition.IsEditable;
        rule.Status = definition.Status;
        rule.Remark = definition.Remark;
    }

    /// <summary>
    /// 校验并规整：只保留当前读取方式用得到的参数。
    /// </summary>
    private async Task<Result<FieldRuleDefinition>> CheckAsync(string owner, FieldRuleDefinition definition, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (catalog.Find(definition.Entity) is not { } entity)
        {
            return Error.Validation(AccessErrors.FieldEntityUnknown, AccessMessages.FieldEntityUnknown, MessageArguments.Of(("entity", definition.Entity)));
        }

        if (entity.FindField(definition.Field) is not { } field)
        {
            return Error.Validation(AccessErrors.FieldUnknown, AccessMessages.FieldUnknown, MessageArguments.Of(("entity", definition.Entity), ("field", definition.Field)));
        }

        var strategy = definition.Strategy;
        if ((!field.IsText && strategy is not (FieldMaskStrategy.None or FieldMaskStrategy.Hidden)) || (!field.IsNullable && strategy == FieldMaskStrategy.Hidden))
        {
            return Error.Validation(AccessErrors.FieldStrategyNotApplicable, AccessMessages.FieldStrategyNotApplicable, MessageArguments.Of(("field", field.Name), ("strategy", strategy.ToString())));
        }

        if (strategy == FieldMaskStrategy.None && definition.IsEditable)
        {
            return Error.Validation(AccessErrors.FieldRuleNoEffect, AccessMessages.FieldRuleNoEffect);
        }

        int? keepHead = null;
        int? keepTail = null;
        if (strategy == FieldMaskStrategy.PartialMask)
        {
            if (definition.KeepHead is not { } head || definition.KeepTail is not { } tail || head is < 0 or > SysFieldSecurityRule.MaxKeep || tail is < 0 or > SysFieldSecurityRule.MaxKeep || head + tail == 0)
            {
                return Error.Validation(AccessErrors.FieldKeepInvalid, AccessMessages.FieldKeepInvalid, MessageArguments.Of(("max", SysFieldSecurityRule.MaxKeep)));
            }

            (keepHead, keepTail) = (head, tail);
        }

        string? replacement = null;
        if (strategy == FieldMaskStrategy.Redact)
        {
            replacement = definition.Replacement?.Trim();
            if (string.IsNullOrEmpty(replacement) || replacement.Length > SysFieldSecurityRule.MaxReplacementLength)
            {
                return Error.Validation(AccessErrors.FieldReplacementInvalid, AccessMessages.FieldReplacementInvalid, MessageArguments.Of(("max", SysFieldSecurityRule.MaxReplacementLength)));
            }
        }

        var target = await CheckTargetAsync(owner, definition.TargetKind, definition.TargetId, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure)
        {
            return target.Error!;
        }

        var duplicate = await rules.ListAsync(EntityQuery.All<SysFieldSecurityRule>().Where(new Specification<SysFieldSecurityRule>(r =>
            r.OwnerTenantId == owner && r.TargetKind == definition.TargetKind && r.TargetId == definition.TargetId && r.Entity == definition.Entity && r.Field == definition.Field)), cancellationToken).ConfigureAwait(false);
        if (duplicate.Any(r => r.Id != excludeId))
        {
            return Error.Conflict(AccessErrors.FieldRuleTaken, AccessMessages.FieldRuleTaken, MessageArguments.Of(("entity", definition.Entity), ("field", definition.Field)));
        }

        return definition with
        {
            KeepHead = keepHead,
            KeepTail = keepTail,
            Replacement = replacement,
            Remark = string.IsNullOrWhiteSpace(definition.Remark) ? null : definition.Remark.Trim(),
        };
    }

    private async Task<Result> CheckTargetAsync(string owner, FieldSecurityTargetKind kind, Guid targetId, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case FieldSecurityTargetKind.Role:
                if (!(await roles.FindAsync(targetId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var role) || !string.Equals(role.OwnerTenantId, owner, StringComparison.Ordinal))
                {
                    return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", targetId)));
                }

                return role.Status == RoleStatus.Active ? Result.Success() : Disabled(kind, targetId);
            case FieldSecurityTargetKind.User:
                if (!(await users.FindAsync(targetId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || !string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal))
                {
                    return Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", targetId)));
                }

                return Result.Success();
            case FieldSecurityTargetKind.Department:
                if (RoleService.IsPlatform(owner))
                {
                    return Error.Validation(AccessErrors.FieldTargetTenantOnly, AccessMessages.FieldTargetTenantOnly);
                }

                if (!(await departments.FindAsync(targetId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var department))
                {
                    return Error.NotFound(AccessErrors.DepartmentNotFound, AccessMessages.DepartmentNotFound, MessageArguments.Of(("id", targetId)));
                }

                return department.Status == OrganizationStatus.Active ? Result.Success() : Disabled(kind, targetId);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown field security target kind.");
        }
    }

    private static Error Disabled(FieldSecurityTargetKind kind, Guid targetId)
        => Error.Validation(AccessErrors.FieldTargetDisabled, AccessMessages.FieldTargetDisabled, MessageArguments.Of(("kind", kind.ToString()), ("id", targetId)));
}
