// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// <see cref="IRoleMemberDirectory"/> 的实现。
/// </summary>
/// <remarks>
/// 角色与指派是平台库实体，按调用所在的一侧（<see cref="RoleService.OwnerOf"/>）读；指派的有效期按此刻判定（与 <see cref="RoleRecipientSource"/> 同口径）。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="clock">时钟。</param>
public sealed class RoleMemberDirectory(SqlSugarUnitOfWork unitOfWork, TimeProvider clock) : IRoleMemberDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> MembersAsync(InvocationContext context, IReadOnlyCollection<string> roleCodes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(roleCodes);
        var codes = roleCodes.Where(static c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToList();
        if (codes.Count == 0)
        {
            return [];
        }

        var owner = RoleService.OwnerOf(context);
        var roles = await unitOfWork.ConnectionFor(typeof(SysRole)).Queryable<SysRole>()
            .Where(r => r.OwnerTenantId == owner && codes.Contains(r.Code) && r.Status == RoleStatus.Active && !r.IsDeleted)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (roles.Count == 0)
        {
            return [];
        }

        var roleIds = roles.Select(static r => r.Id).ToList();
        var assignments = await unitOfWork.ConnectionFor(typeof(SysUserRole)).Queryable<SysUserRole>()
            .Where(a => a.OwnerTenantId == owner && roleIds.Contains(a.RoleId))
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return codes
            .SelectMany(code => roles.Where(r => r.Code == code))
            .SelectMany(r => assignments.Where(a => a.RoleId == r.Id && a.IsEffectiveAt(now)).Select(static a => a.UserId))
            .Distinct()
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleOption>> RolesAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var roles = await unitOfWork.ConnectionFor(typeof(SysRole)).Queryable<SysRole>()
            .Where(r => r.OwnerTenantId == owner && r.Status == RoleStatus.Active && !r.IsDeleted)
            .OrderBy(static r => r.Order)
            .OrderBy(static r => r.Code)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return roles.Select(static r => new RoleOption(r.Id, r.Code, r.Name)).ToList();
    }
}
