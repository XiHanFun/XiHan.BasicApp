// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 角色的授予：读写框架的 <c>PermissionGrantRecord</c>（主体 <c>role:&lt;ID&gt;</c>，租户列按角色所属侧），授予 / 撤销后框架存储自行失效缓存。
/// </summary>
/// <param name="store">框架授予存储。</param>
public sealed class RoleGrants(PermissionGrantRecordStore store)
{
    /// <summary>
    /// 角色当前的全部权限码。
    /// </summary>
    public async Task<IReadOnlyList<string>> ListAsync(SysRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        var rows = await store.ListAsync(RoleService.SubjectOf(role), tenantId: null, skip: 0, take: EntityQuery.DefaultMaxTake, cancellationToken).ConfigureAwait(false);
        return rows.Select(static r => r.Permission).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 给角色授予权限；逐项结果与入参同序。
    /// </summary>
    public ValueTask<IReadOnlyList<PermissionGrantResult>> GrantAsync(SysRole role, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permissions);
        return store.GrantAsync(permissions.Select(p => new PermissionGrant(RoleService.SubjectOf(role), p, RoleService.GrantTenantOf(role))).ToList(), cancellationToken);
    }

    /// <summary>
    /// 撤销角色的权限；逐项结果与入参同序（没有的授予是 NotFound）。
    /// </summary>
    public ValueTask<IReadOnlyList<PermissionGrantResult>> RevokeAsync(SysRole role, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permissions);
        return store.RevokeAsync(permissions.Select(p => new PermissionGrant(RoleService.SubjectOf(role), p, RoleService.GrantTenantOf(role))).ToList(), cancellationToken);
    }

    /// <summary>
    /// 撤销角色的全部授予（删除角色时）。
    /// </summary>
    public async Task<Result> RevokeAllAsync(SysRole role, CancellationToken cancellationToken)
    {
        var current = await ListAsync(role, cancellationToken).ConfigureAwait(false);
        if (current.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await RevokeAsync(role, current, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.Outcome.IsFailure)
            {
                return outcome.Outcome;
            }
        }

        return Result.Success();
    }
}
