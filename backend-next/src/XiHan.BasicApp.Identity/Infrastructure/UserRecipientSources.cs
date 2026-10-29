// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 一侧此刻正常的账号。
/// </summary>
/// <remarks>
/// 平台是注册地为 <c>platform</c> 的账号；租户是注册地在该租户的用户加租户成员（成员由 Tenancy 的 <see cref="ITenantMembershipResolver"/> 给出）。
/// </remarks>
internal static class SideAccounts
{
    public static async Task<HashSet<Guid>> ActiveAsync(InvocationContext context, IEnumerable<ITenantMembershipResolver> memberships, IReadOnlyCollection<Guid>? within)
    {
        var tenantId = context.GetSecurity().Tenant?.Id;
        var home = tenantId ?? TenantIds.Platform;
        var members = new List<Guid>();
        if (tenantId is not null)
        {
            foreach (var membership in memberships)
            {
                members.AddRange(await membership.MembersOfAsync(tenantId, context.CancellationToken).ConfigureAwait(false));
            }
        }

        var query = TenantScopedQuery.For<SysUser>(context).Where(u => u.Status == UserStatus.Active && (u.HomeTenantId == home || members.Contains(u.Id)));
        if (within is not null)
        {
            var ids = within.Distinct().ToList();
            if (ids.Count == 0)
            {
                return [];
            }

            query = query.Where(u => ids.Contains(u.Id));
        }

        return (await query.Select(static u => u.Id).ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToHashSet();
    }
}

/// <summary>
/// <see cref="RecipientKind.All"/>：当前一侧的全部正常账号。
/// </summary>
/// <param name="memberships">租户成员（没装 Tenancy 即没有）。</param>
public sealed class AllUsersRecipientSource(IEnumerable<ITenantMembershipResolver> memberships) : IRecipientSource
{
    /// <inheritdoc />
    public RecipientKind Kind => RecipientKind.All;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> ResolveAsync(InvocationContext context, IReadOnlyCollection<Guid> targetIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await SideAccounts.ActiveAsync(context, memberships, within: null).ConfigureAwait(false);
    }
}

/// <summary>
/// <see cref="RecipientKind.User"/> 的接收人来源。
/// </summary>
/// <remarks>
/// 指定用户里属于当前一侧、此刻正常的（不存在、停用、锁定与别的租户的用户都滤掉）。
/// </remarks>
/// <param name="memberships">租户成员（没装 Tenancy 即没有）。</param>
public sealed class UserRecipientSource(IEnumerable<ITenantMembershipResolver> memberships) : IRecipientSource
{
    /// <inheritdoc />
    public RecipientKind Kind => RecipientKind.User;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> ResolveAsync(InvocationContext context, IReadOnlyCollection<Guid> targetIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(targetIds);
        return await SideAccounts.ActiveAsync(context, memberships, targetIds).ConfigureAwait(false);
    }
}
