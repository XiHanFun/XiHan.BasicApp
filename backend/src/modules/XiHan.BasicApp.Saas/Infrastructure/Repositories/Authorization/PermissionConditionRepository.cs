// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限条件仓储实现
/// </summary>
public sealed class PermissionConditionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPermissionCondition>(clientResolver), IPermissionConditionRepository
{
    /// <summary>
    /// 获取角色或用户授权关联的有效 ABAC 条件
    /// </summary>
    public async Task<IReadOnlyList<SysPermissionCondition>> GetValidByAuthorizationIdsAsync(
        IEnumerable<long> rolePermissionIds,
        IEnumerable<long> userPermissionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rolePermissionIds);
        ArgumentNullException.ThrowIfNull(userPermissionIds);

        var rolePermissionIdArray = rolePermissionIds.Distinct().ToArray();
        var userPermissionIdArray = userPermissionIds.Distinct().ToArray();
        if (rolePermissionIdArray.Length == 0 && userPermissionIdArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(condition =>
                (condition.RolePermissionId != null && rolePermissionIdArray.Contains(condition.RolePermissionId.Value)) ||
                (condition.UserPermissionId != null && userPermissionIdArray.Contains(condition.UserPermissionId.Value)))
            .Where(condition => condition.Status == ValidityStatus.Valid)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 盘点用：全部条件（含停用的，不含已删的）
    /// </summary>
    public async Task<IReadOnlyList<SysPermissionCondition>> GetForInventoryAsync(bool allTenants, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queryable = allTenants ? CreateNoTenantQueryable() : CreateQueryable();
        return await queryable
            .OrderBy(condition => condition.TenantId)
            .OrderBy(condition => condition.ConditionGroup)
            .ToListAsync(cancellationToken);
    }
}
