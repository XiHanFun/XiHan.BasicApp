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

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 新建委托的内容。
/// </summary>
/// <param name="DelegatorId">委托人。</param>
/// <param name="DelegateeId">被委托人。</param>
/// <param name="Permission">委托的权限码（与 <paramref name="RoleId"/> 二选一）。</param>
/// <param name="RoleId">委托的角色。</param>
/// <param name="EffectiveAt">生效时间；为空即立即生效。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Reason">委托原因。</param>
public sealed record DelegationDefinition(Guid DelegatorId, Guid DelegateeId, string? Permission, Guid? RoleId, DateTimeOffset? EffectiveAt, DateTimeOffset ExpiresAt, string? Reason);

/// <summary>
/// 委托的写入规则（方案 5.9）。比 4.x 多三条：新建时委托人此刻须持有所委托的权限或角色；未到生效时间的到点自动生效（状态按时间推出）；
/// 重复只看同一委托人、被委托人与目标上未撤销、未到期的那条，撤销或到期后可以重建。委托的角色须启用且不是内置角色。
/// </summary>
/// <param name="delegations">委托仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="roles">角色查找。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="evaluator">授予全貌（委托人是否持有）。</param>
/// <param name="cache">被委托人的委托缓存（写入后失效）。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
public sealed class DelegationService(
    IRepository<SysPermissionDelegation, Guid> delegations,
    IRepository<SysUser, Guid> users,
    RoleService roles,
    PermissionCatalog catalog,
    GrantEvaluator evaluator,
    Delegations cache,
    IIdGenerator<Guid> ids,
    TimeProvider clock)
{
    /// <summary>
    /// 原因的长度上限。
    /// </summary>
    public const int MaxReasonLength = 512;

    /// <summary>
    /// 当前时间（状态按它推出）。
    /// </summary>
    public DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>
    /// 按 ID 取这一侧的委托。
    /// </summary>
    public async Task<Maybe<SysPermissionDelegation>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await delegations.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var delegation) && string.Equals(delegation.OwnerTenantId, owner, StringComparison.Ordinal) ? Maybe.Some(delegation) : Maybe.None<SysPermissionDelegation>();
    }

    /// <summary>
    /// 新建一条委托。
    /// </summary>
    public async Task<Result<SysPermissionDelegation>> CreateAsync(string owner, DelegationDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var permission = string.IsNullOrWhiteSpace(definition.Permission) ? null : definition.Permission.Trim();
        if ((permission is null) == (definition.RoleId is null))
        {
            return Error.Validation(AccessErrors.DelegationTargetInvalid, AccessMessages.DelegationTargetInvalid);
        }

        var now = clock.GetUtcNow();
        if (definition.ExpiresAt <= now || (definition.EffectiveAt is { } from && from >= definition.ExpiresAt))
        {
            return Error.Validation(AccessErrors.DelegationWindowInvalid, AccessMessages.DelegationWindowInvalid, MessageArguments.Of(("effectiveAt", definition.EffectiveAt), ("expiresAt", definition.ExpiresAt)));
        }

        if (definition.DelegatorId == definition.DelegateeId)
        {
            return Error.Validation(AccessErrors.DelegationSelf, AccessMessages.DelegationSelf);
        }

        var reason = string.IsNullOrWhiteSpace(definition.Reason) ? null : definition.Reason.Trim();
        if (reason is { Length: > MaxReasonLength })
        {
            reason = reason[..MaxReasonLength];
        }

        foreach (var userId in new[] { definition.DelegatorId, definition.DelegateeId })
        {
            if (!(await users.FindAsync(userId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || !string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal))
            {
                return Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", userId)));
            }
        }

        string target;
        if (permission is not null)
        {
            var descriptor = catalog.Find(permission);
            if (descriptor is null)
            {
                return Error.Validation(AccessErrors.PermissionUnknown, AccessMessages.PermissionUnknown, MessageArguments.Of(("code", permission)));
            }

            if (!PermissionCatalog.AllowsSide(descriptor, RoleService.IsPlatform(owner)))
            {
                return Error.Validation(AccessErrors.PermissionScopeMismatch, AccessMessages.PermissionScopeMismatch, MessageArguments.Of(("code", permission), ("side", RoleService.IsPlatform(owner) ? "platform" : "tenant")));
            }

            target = permission;
        }
        else
        {
            if (!(await roles.FindAsync(definition.RoleId!.Value, owner, cancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
            {
                return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", definition.RoleId.Value)));
            }

            if (role.IsBuiltIn || role.Status != RoleStatus.Active)
            {
                return Error.Validation(AccessErrors.DelegationRoleInvalid, AccessMessages.DelegationRoleInvalid, MessageArguments.Of(("name", role.Name)));
            }

            target = role.Name;
        }

        if (!await evaluator.HoldsAsync(owner, definition.DelegatorId, permission, definition.RoleId, cancellationToken).ConfigureAwait(false))
        {
            return Error.Validation(AccessErrors.DelegatorNotHolding, AccessMessages.DelegatorNotHolding, MessageArguments.Of(("target", target)));
        }

        var delegatorId = definition.DelegatorId;
        var delegateeId = definition.DelegateeId;
        var open = await delegations.ListAsync(
            EntityQuery.All<SysPermissionDelegation>().Where(new Specification<SysPermissionDelegation>(d => d.OwnerTenantId == owner && d.DelegatorId == delegatorId && d.DelegateeId == delegateeId && !d.RevokedAt.HasValue && d.ExpiresAt > now)),
            cancellationToken).ConfigureAwait(false);
        if (open.Any(d => string.Equals(d.Permission, permission, StringComparison.Ordinal) && d.RoleId == definition.RoleId))
        {
            return Error.Conflict(AccessErrors.DelegationTaken, AccessMessages.DelegationTaken);
        }

        var delegation = new SysPermissionDelegation
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            DelegatorId = delegatorId,
            DelegateeId = delegateeId,
            Permission = permission,
            RoleId = definition.RoleId,
            EffectiveAt = definition.EffectiveAt,
            ExpiresAt = definition.ExpiresAt,
            Reason = reason,
        };
        var added = await delegations.AddAsync(delegation, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        await cache.InvalidateAsync(owner, delegateeId, cancellationToken).ConfigureAwait(false);
        return delegation;
    }

    /// <summary>
    /// 撤销一条未撤销、未到期的委托（保留记录）。
    /// </summary>
    public async Task<Result> RevokeAsync(SysPermissionDelegation delegation, Guid? revokedBy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delegation);
        var now = clock.GetUtcNow();
        if (delegation.StatusAt(now) is DelegationStatus.Revoked or DelegationStatus.Expired)
        {
            return Error.Conflict(AccessErrors.DelegationClosed, AccessMessages.DelegationClosed, MessageArguments.Of(("id", delegation.Id)));
        }

        delegation.RevokedAt = now;
        delegation.RevokedBy = revokedBy;
        var updated = await delegations.UpdateAsync(delegation, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        await cache.InvalidateAsync(delegation.OwnerTenantId, delegation.DelegateeId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 删除一条委托记录（生效中的随之失效）。
    /// </summary>
    public async Task<Result> DeleteAsync(SysPermissionDelegation delegation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delegation);
        var removed = await delegations.RemoveAsync(delegation, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        await cache.InvalidateAsync(delegation.OwnerTenantId, delegation.DelegateeId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 删除角色时连同删掉委托这个角色的记录。
    /// </summary>
    public async Task<Result> RemoveForRoleAsync(SysRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        var roleId = role.Id;
        var rows = await delegations.ListAsync(EntityQuery.All<SysPermissionDelegation>().Where(new Specification<SysPermissionDelegation>(d => d.RoleId == roleId)), cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await delegations.RemoveRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        foreach (var delegateeId in rows.Select(static d => d.DelegateeId).Distinct())
        {
            await cache.InvalidateAsync(role.OwnerTenantId, delegateeId, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// 委托涉及的用户与角色的显示名：用户为「显示名 (@登录名)」，角色为名称。
    /// </summary>
    public async Task<(IReadOnlyDictionary<Guid, string> Users, IReadOnlyDictionary<Guid, string> Roles)> NamesAsync(IReadOnlyList<SysPermissionDelegation> rows, string owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var userIds = rows.SelectMany(static d => new[] { d.DelegatorId, d.DelegateeId }).Distinct().ToList();
        var roleIds = rows.Where(static d => d.RoleId.HasValue).Select(static d => d.RoleId!.Value).Distinct().ToList();
        var userNames = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await users.FindManyAsync(userIds, cancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id, static u => $"{u.DisplayName} (@{u.UserName})");
        var roleNames = (await roles.FindManyAsync(roleIds, owner, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id, static r => r.Name);
        return (userNames, roleNames);
    }
}
