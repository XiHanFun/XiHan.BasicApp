// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 角色权限仓储实现
/// </summary>
public sealed class RolePermissionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysRolePermission>(clientResolver), IRolePermissionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysRolePermission>> GetValidByRoleIdsAsync(IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var roleIdArray = roleIds.Distinct().ToArray();
        if (roleIdArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => roleIdArray.Contains(permission.RoleId))
            .Where(permission => permission.Status == ValidityStatus.Valid)
            .Where(permission => permission.EffectiveTime == null || permission.EffectiveTime <= now)
            .Where(permission => permission.ExpirationTime == null || permission.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }
}
