// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 角色域服务（作用域）：作用侧解析（当前租户或平台）、按侧找角色、角色码唯一、角色对应的授予主体与授予行的租户列。
/// </summary>
/// <param name="roles">角色仓储。</param>
public sealed class RoleService(IRepository<SysRole, Guid> roles)
{
    /// <summary>
    /// 当前主体的作用侧：租户主体是它的租户，平台主体是 <see cref="TenantIds.Platform"/>。
    /// </summary>
    public static string OwnerOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
    }

    /// <summary>
    /// 是否平台侧。
    /// </summary>
    public static bool IsPlatform(string owner) => string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal);

    /// <summary>
    /// 角色的授予主体：<c>role:&lt;ID&gt;</c>，与访问令牌里的 <c>role</c> 声明一致。
    /// </summary>
    public static GrantSubject SubjectOf(SysRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return GrantSubject.ForRole(role.Id.ToString("D"));
    }

    /// <summary>
    /// 角色授予行的租户列：平台角色是全局行（<see langword="null"/>），租户角色是该租户。
    /// </summary>
    public static string? GrantTenantOf(SysRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return IsPlatform(role.OwnerTenantId) ? null : role.OwnerTenantId;
    }

    /// <summary>
    /// 按 ID 在作用侧内找角色；别的侧的角色视为不存在。
    /// </summary>
    public async Task<Maybe<SysRole>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await roles.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var role) && string.Equals(role.OwnerTenantId, owner, StringComparison.Ordinal) ? Maybe.Some(role) : Maybe.None<SysRole>();
    }

    /// <summary>
    /// 作用侧内按角色码找角色。
    /// </summary>
    public async Task<Maybe<SysRole>> FindByCodeAsync(string owner, string code, CancellationToken cancellationToken)
    {
        var rows = await roles.ListAsync(EntityQuery.All<SysRole>().Where(new Specification<SysRole>(r => r.OwnerTenantId == owner && r.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysRole>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 作用侧内按 ID 批量取角色（只取本侧的）。
    /// </summary>
    public async Task<IReadOnlyList<SysRole>> FindManyAsync(IReadOnlyList<Guid> ids, string owner, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await roles.FindManyAsync(ids, cancellationToken).ConfigureAwait(false);
        return rows.Where(r => string.Equals(r.OwnerTenantId, owner, StringComparison.Ordinal)).ToList();
    }
}
