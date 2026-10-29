// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Users;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// <see cref="RecipientKind.Role"/>：当前一侧持有这些角色的用户——角色须启用、指派此刻在有效期内（只看直接指派，上级角色不连带下级的成员）。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="clock">时钟。</param>
public sealed class RoleRecipientSource(SqlSugarUnitOfWork unitOfWork, TimeProvider clock) : IRecipientSource
{
    /// <inheritdoc />
    public RecipientKind Kind => RecipientKind.Role;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> ResolveAsync(InvocationContext context, IReadOnlyCollection<Guid> targetIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(targetIds);
        var ids = targetIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var side = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        var active = await unitOfWork.ConnectionFor(typeof(SysRole)).Queryable<SysRole>()
            .Where(r => ids.Contains(r.Id) && r.Status == RoleStatus.Active)
            .Select(static r => r.Id)
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        if (active.Count == 0)
        {
            return [];
        }

        var assignments = await unitOfWork.ConnectionFor(typeof(SysUserRole)).Queryable<SysUserRole>()
            .Where(a => a.OwnerTenantId == side && active.Contains(a.RoleId))
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return assignments.Where(a => a.IsEffectiveAt(now)).Select(static a => a.UserId).ToHashSet();
    }
}
