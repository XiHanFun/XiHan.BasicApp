// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 找一侧的用户
/// </summary>
/// <remarks>
/// 平台一侧是注册在平台的账号，租户一侧是注册在该租户的账号与生效中的普通成员（外部成员，运维支持成员不算，见 <see cref="ITenantMembershipResolver"/>）
/// 给本租户配角色、部门、数据范围与直授都按它认人；账号本身（资料、密码、状态、删除）只有注册地能动，那些操作仍按注册地判定
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="memberships">成员关系（没装 Tenancy 即没有外部成员）</param>
public sealed class SideUsers(IRepository<SysUser, Guid> users, IEnumerable<ITenantMembershipResolver> memberships)
{
    /// <summary>
    /// 这一侧的一个用户
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="side">一侧：平台为 <c>platform</c>，否则是租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户；不存在或不在这一侧为空</returns>
    public async Task<Maybe<SysUser>> FindAsync(Guid userId, string side, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        if (!(await users.FindAsync(userId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Maybe.None<SysUser>();
        }

        if (string.Equals(user.HomeTenantId, side, StringComparison.Ordinal))
        {
            return Maybe.Some(user);
        }

        if (!string.Equals(side, TenantIds.Platform, StringComparison.Ordinal))
        {
            foreach (var resolver in memberships)
            {
                if (await resolver.IsMemberAsync(side, user.Id, cancellationToken).ConfigureAwait(false))
                {
                    return Maybe.Some(user);
                }
            }
        }

        return Maybe.None<SysUser>();
    }

    /// <summary>
    /// 这一批里在这一侧的用户
    /// </summary>
    /// <param name="userIds">用户 ID</param>
    /// <param name="side">一侧：平台为 <c>platform</c>，否则是租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>在这一侧的用户；不存在或不在这一侧的不在结果里</returns>
    public async Task<IReadOnlyList<SysUser>> FindManyAsync(IReadOnlyCollection<Guid> userIds, string side, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        var found = await users.FindManyAsync(userIds.Distinct().ToList(), cancellationToken).ConfigureAwait(false);
        var home = found.Where(u => string.Equals(u.HomeTenantId, side, StringComparison.Ordinal)).ToList();
        if (home.Count == found.Count || string.Equals(side, TenantIds.Platform, StringComparison.Ordinal))
        {
            return home;
        }

        var members = await MembersOfAsync(side, cancellationToken).ConfigureAwait(false);
        return found.Where(u => string.Equals(u.HomeTenantId, side, StringComparison.Ordinal) || members.Contains(u.Id)).ToList();
    }

    /// <summary>
    /// 租户的外部成员
    /// </summary>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生效中的普通成员的用户 ID</returns>
    public async Task<IReadOnlySet<Guid>> MembersOfAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var members = new HashSet<Guid>();
        foreach (var resolver in memberships)
        {
            members.UnionWith(await resolver.MembersOfAsync(tenantId, cancellationToken).ConfigureAwait(false));
        }

        return members;
    }
}
