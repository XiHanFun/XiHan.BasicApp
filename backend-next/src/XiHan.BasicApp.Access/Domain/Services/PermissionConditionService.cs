// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 一条条件子句的内容。
/// </summary>
/// <remarks>
/// 新建与修改共用；绑定只在新建时给。
/// </remarks>
/// <param name="GroupNo">组号。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record ConditionClauseDefinition(int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value, string? Description, ConditionStatus Status);

/// <summary>
/// 一个授权绑定：主体在一个权限码上的授予或拒绝。
/// </summary>
/// <param name="Kind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="Permission">权限码。</param>
/// <param name="Action">授予或拒绝。</param>
public sealed record ConditionBinding(GrantSubjectKind Kind, Guid SubjectId, string Permission, PermissionAction Action);

/// <summary>
/// 权限条件的写入规则。
/// </summary>
/// <remarks>
/// 见方案 5.9，对应 4.x 的 PermissionConditionDomainService。
/// 只挂在这一侧确实存在的授权绑定上（授予看框架授予存储，拒绝看规则行）；
/// 角色须启用且不是内置角色；每个绑定最多 5 组、每组最多 10 条；子句按条件引擎编译，编译不过返回引擎的错误（<c>SECURITY_VALIDATION_002</c>）。
/// 停用的子句不校验结构以外的规则，启用时重新校验。
/// </remarks>
/// <param name="conditions">条件仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="roles">角色查找。</param>
/// <param name="roleGrants">角色授予。</param>
/// <param name="userGrants">用户直接授予。</param>
/// <param name="rules">授予规则。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="compiler">条件编译器。</param>
/// <param name="cache">条件缓存（写入后失效）。</param>
/// <param name="ids">标识生成器。</param>
public sealed class PermissionConditionService(
    IRepository<SysPermissionCondition, Guid> conditions,
    IRepository<SysUser, Guid> users,
    RoleService roles,
    RoleGrants roleGrants,
    UserGrants userGrants,
    PermissionRuleWriter rules,
    PermissionCatalog catalog,
    IConditionCompiler compiler,
    PermissionConditions cache,
    IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 按 ID 取这一侧的子句。
    /// </summary>
    public async Task<Maybe<SysPermissionCondition>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await conditions.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var condition) && string.Equals(condition.OwnerTenantId, owner, StringComparison.Ordinal) ? Maybe.Some(condition) : Maybe.None<SysPermissionCondition>();
    }

    /// <summary>
    /// 主体上的全部子句（含停用的）。
    /// </summary>
    public Task<IReadOnlyList<SysPermissionCondition>> ListAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        => conditions.ListAsync(EntityQuery.All<SysPermissionCondition>().Where(new Specification<SysPermissionCondition>(c => c.OwnerTenantId == owner && c.SubjectKind == kind && c.SubjectId == subjectId)), cancellationToken);

    /// <summary>
    /// 主体此刻的授权绑定：授予（框架授予存储，含有效期）与拒绝（规则行）。
    /// </summary>
    public async Task<Result<IReadOnlyList<(string Permission, PermissionAction Action, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt)>>> BindingsAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await CheckSubjectAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false);
        if (!subject.TryGetValue(out var role))
        {
            return subject.Error!;
        }

        var granted = role.TryGetValue(out var found)
            ? await roleGrants.ListAsync(found, cancellationToken).ConfigureAwait(false)
            : await userGrants.ListAsync(subjectId, owner, cancellationToken).ConfigureAwait(false);
        var ruleRows = await rules.ListAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false);
        var grantRules = ruleRows.Where(static r => r.Action == PermissionAction.Grant).ToDictionary(static r => r.Permission, StringComparer.Ordinal);
        IReadOnlyList<(string, PermissionAction, DateTimeOffset?, DateTimeOffset?)> bindings =
        [
            .. granted.Select(p => grantRules.TryGetValue(p, out var rule) ? (p, PermissionAction.Grant, rule.EffectiveAt, rule.ExpiresAt) : (p, PermissionAction.Grant, (DateTimeOffset?)null, (DateTimeOffset?)null)),
            .. ruleRows.Where(static r => r.Action == PermissionAction.Deny).Select(static r => (r.Permission, PermissionAction.Deny, r.EffectiveAt, r.ExpiresAt)),
        ];
        return Result.Success(bindings);
    }

    /// <summary>
    /// 在绑定上新建一条子句。
    /// </summary>
    public async Task<Result<SysPermissionCondition>> CreateAsync(string owner, ConditionBinding binding, ConditionClauseDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(definition);
        var bound = await CheckBindingAsync(owner, binding, cancellationToken).ConfigureAwait(false);
        if (bound.IsFailure)
        {
            return bound.Error!;
        }

        var condition = new SysPermissionCondition
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            SubjectKind = binding.Kind,
            SubjectId = binding.SubjectId,
            Permission = binding.Permission,
            Action = binding.Action,
        };
        var applied = await ApplyAsync(condition, definition, cancellationToken).ConfigureAwait(false);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        var added = await conditions.AddAsync(condition, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        await cache.InvalidateAsync(owner, binding.Kind, binding.SubjectId, cancellationToken).ConfigureAwait(false);
        return condition;
    }

    /// <summary>
    /// 修改一条子句（绑定不变）。
    /// </summary>
    /// <remarks>
    /// 启用时重新校验绑定。
    /// </remarks>
    public async Task<Result> UpdateAsync(SysPermissionCondition condition, ConditionClauseDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Status == ConditionStatus.Active)
        {
            var bound = await CheckBindingAsync(condition.OwnerTenantId, new ConditionBinding(condition.SubjectKind, condition.SubjectId, condition.Permission, condition.Action), cancellationToken).ConfigureAwait(false);
            if (bound.IsFailure)
            {
                return bound;
            }
        }

        var applied = await ApplyAsync(condition, definition, cancellationToken).ConfigureAwait(false);
        if (applied.IsFailure)
        {
            return applied;
        }

        var updated = await conditions.UpdateAsync(condition, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        await cache.InvalidateAsync(condition.OwnerTenantId, condition.SubjectKind, condition.SubjectId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 删除一条子句。
    /// </summary>
    public async Task<Result> DeleteAsync(SysPermissionCondition condition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var removed = await conditions.RemoveAsync(condition, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        await cache.InvalidateAsync(condition.OwnerTenantId, condition.SubjectKind, condition.SubjectId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 绑定没了时连同删除它上面的子句。
    /// </summary>
    /// <remarks>
    /// <paramref name="permissions"/> 为空即主体上的全部，<paramref name="action"/> 为空即授予与拒绝两侧。
    /// </remarks>
    public async Task<Result> RemoveBindingsAsync(string owner, GrantSubjectKind kind, Guid subjectId, IReadOnlyCollection<string>? permissions, PermissionAction? action, CancellationToken cancellationToken)
    {
        var rows = (await ListAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false))
            .Where(c => (permissions is null || permissions.Contains(c.Permission)) && (action is null || c.Action == action))
            .ToList();
        if (rows.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await conditions.RemoveRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        await cache.InvalidateAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Result> ApplyAsync(SysPermissionCondition condition, ConditionClauseDefinition definition, CancellationToken cancellationToken)
    {
        var attribute = definition.AttributeName.Trim();
        var value = definition.Operator is ConditionOperator.IsNull or ConditionOperator.IsNotNull ? null : definition.Value?.Trim();
        var compiled = compiler.Compile(new ConditionSet([new ConditionGroup([new ConditionClause(attribute, definition.Operator, value, definition.IsNegated)])]));
        if (compiled.Errors.Count > 0)
        {
            return compiled.Errors[0].Error;
        }

        // 同一绑定的组数与每组条数（不含自己；停用的也占名额，4.x 同）
        var siblings = (await ListAsync(condition.OwnerTenantId, condition.SubjectKind, condition.SubjectId, cancellationToken).ConfigureAwait(false))
            .Where(c => c.Id != condition.Id && c.Action == condition.Action && string.Equals(c.Permission, condition.Permission, StringComparison.Ordinal))
            .ToList();
        if (siblings.Select(static c => c.GroupNo).Append(definition.GroupNo).Distinct().Count() > SysPermissionCondition.MaxGroups)
        {
            return Error.Validation(AccessErrors.ConditionGroupsExceeded, AccessMessages.ConditionGroupsExceeded, MessageArguments.Of(("max", SysPermissionCondition.MaxGroups)));
        }

        if (siblings.Count(c => c.GroupNo == definition.GroupNo) + 1 > SysPermissionCondition.MaxClausesPerGroup)
        {
            return Error.Validation(AccessErrors.ConditionClausesExceeded, AccessMessages.ConditionClausesExceeded, MessageArguments.Of(("max", SysPermissionCondition.MaxClausesPerGroup)));
        }

        condition.GroupNo = definition.GroupNo;
        condition.AttributeName = attribute;
        condition.Operator = definition.Operator;
        condition.IsNegated = definition.IsNegated;
        condition.Value = value;
        condition.Description = string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim();
        condition.Status = definition.Status;
        return Result.Success();
    }

    private async Task<Result> CheckBindingAsync(string owner, ConditionBinding binding, CancellationToken cancellationToken)
    {
        var descriptor = catalog.Find(binding.Permission);
        if (descriptor is null)
        {
            return Error.Validation(AccessErrors.PermissionUnknown, AccessMessages.PermissionUnknown, MessageArguments.Of(("code", binding.Permission)));
        }

        var bindings = await BindingsAsync(owner, binding.Kind, binding.SubjectId, cancellationToken).ConfigureAwait(false);
        if (!bindings.TryGetValue(out var current))
        {
            return bindings.Error!;
        }

        return current.Any(b => b.Action == binding.Action && string.Equals(b.Permission, binding.Permission, StringComparison.Ordinal))
            ? Result.Success()
            : Error.Validation(AccessErrors.ConditionBindingMissing, AccessMessages.ConditionBindingMissing, MessageArguments.Of(("permission", binding.Permission), ("action", binding.Action.ToString())));
    }

    // 角色须在这一侧、启用、不是内置角色；用户须在这一侧。返回角色（用户主体时没有）
    private async Task<Result<Maybe<SysRole>>> CheckSubjectAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
    {
        if (kind == GrantSubjectKind.Role)
        {
            if (!(await roles.FindAsync(subjectId, owner, cancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
            {
                return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", subjectId)));
            }

            return role.IsBuiltIn || role.Status != RoleStatus.Active
                ? Error.Validation(AccessErrors.ConditionSubjectInvalid, AccessMessages.ConditionSubjectInvalid, MessageArguments.Of(("name", role.Name)))
                : Result.Success(Maybe.Some(role));
        }

        return (await users.FindAsync(subjectId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) && string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal)
            ? Result.Success(Maybe.None<SysRole>())
            : Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", subjectId)));
    }
}
