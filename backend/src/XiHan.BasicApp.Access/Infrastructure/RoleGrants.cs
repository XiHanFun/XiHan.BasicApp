// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 角色的授予
/// </summary>
/// <remarks>
/// 读写框架的 <c>PermissionGrantRecord</c>（主体 <c>role:&lt;ID&gt;</c>，租户列按角色所属侧），授予 / 撤销后框架存储自行失效缓存
/// </remarks>
/// <param name="store">框架授予存储</param>
public sealed class RoleGrants(PermissionGrantRecordStore store)
{
    /// <summary>
    /// 角色当前的全部权限码
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> ListAsync(SysRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        var listed = await store.ListAsync(new PermissionGrantQuery { Subject = RoleService.SubjectOf(role), Take = PermissionGrantQuery.MaxTake }, cancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var page))
        {
            return listed.Error!;
        }

        return page.Items.Select(static g => g.Permission).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 给角色授予权限
    /// </summary>
    /// <returns>逐项结果与入参同序</returns>
    public async ValueTask<IReadOnlyList<PermissionOutcome>> GrantAsync(SysRole role, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permissions);
        var grants = permissions.Select(p => new PermissionGrant(RoleService.SubjectOf(role), p, RoleService.GrantTenantOf(role))).ToList();
        return PermissionOutcome.Zip(grants, await store.GrantAsync(grants, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 撤销角色的权限
    /// </summary>
    /// <returns>逐项结果与入参同序（没有的授予是 NotFound）</returns>
    public async ValueTask<IReadOnlyList<PermissionOutcome>> RevokeAsync(SysRole role, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permissions);
        var grants = permissions.Select(p => new PermissionGrant(RoleService.SubjectOf(role), p, RoleService.GrantTenantOf(role))).ToList();
        return PermissionOutcome.Zip(grants, await store.RevokeAsync(grants, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 撤销角色的全部授予（删除角色时）
    /// </summary>
    public async Task<Result> RevokeAllAsync(SysRole role, CancellationToken cancellationToken)
    {
        var listed = await ListAsync(role, cancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var current))
        {
            return listed.Error!;
        }

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
