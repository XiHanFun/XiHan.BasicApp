// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 给 Identity 的登录 / 刷新提供用户的角色 ID：取该侧此刻在有效期内的指派，只要启用的角色。
/// 签发之后才生效的指派要等下次刷新令牌才进令牌；到期的由权限过滤器立即拿掉。
/// </summary>
/// <param name="userRoles">指派仓储。</param>
/// <param name="roles">角色仓储。</param>
/// <param name="clock">时间源。</param>
public sealed class UserRoleResolver(IRepository<SysUserRole, Guid> userRoles, IRepository<SysRole, Guid> roles, TimeProvider clock) : IUserRoleResolver
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ResolveAsync(Guid userId, string? tenantId, CancellationToken cancellationToken)
    {
        var owner = tenantId ?? TenantIds.Platform;
        var now = clock.GetUtcNow();
        var assignments = (await userRoles.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(a => a.UserId == userId && a.OwnerTenantId == owner)), cancellationToken).ConfigureAwait(false))
            .Where(a => a.IsEffectiveAt(now))
            .ToList();
        if (assignments.Count == 0)
        {
            return [];
        }

        var found = await roles.FindManyAsync(assignments.Select(static a => a.RoleId).Distinct().ToList(), cancellationToken).ConfigureAwait(false);
        return found.Where(r => r.Status == RoleStatus.Active).OrderBy(static r => r.Order).ThenBy(static r => r.Code, StringComparer.Ordinal).Select(static r => r.Id.ToString("D")).ToList();
    }
}
