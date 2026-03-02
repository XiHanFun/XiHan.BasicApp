#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRelationRepository
// Guid:3b6a59f1-298d-4bd6-8752-08fa58928e9e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 14:26:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 角色关系仓储实现（角色-权限/菜单）
/// </summary>
public class RoleRelationRepository : SqlSugarRepositoryBase<SysRolePermission, long>, IRoleRelationRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    public RoleRelationRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider)
        : base(clientProvider, currentTenant, serviceProvider)
    {
    }

    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysRolePermission>> GetRolePermissionsAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == roleId);

        if (tenantId.HasValue)
        {
            query = query.Where(mapping => mapping.TenantId == tenantId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysRoleMenu>> GetRoleMenusAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysRoleMenu>()
            .Where(mapping => mapping.RoleId == roleId);

        if (tenantId.HasValue)
        {
            query = query.Where(mapping => mapping.TenantId == tenantId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 替换角色权限关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="permissionIds"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReplaceRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedTenantId = tenantId ?? CurrentTenantId;

        var deleteable = DbClient.Deleteable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == roleId);

        if (resolvedTenantId.HasValue)
        {
            deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }

        await deleteable.ExecuteCommandAsync(cancellationToken);

        var distinctPermissionIds = permissionIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctPermissionIds.Length == 0)
        {
            return;
        }

        var mappings = distinctPermissionIds.Select(permissionId => new SysRolePermission
        {
            TenantId = resolvedTenantId,
            RoleId = roleId,
            PermissionId = permissionId,
            Status = YesOrNo.Yes
        }).ToArray();

        foreach (var mapping in mappings)
        {
            TrySetTenantId(mapping);
        }

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 替换角色菜单关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="menuIds"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReplaceRoleMenusAsync(long roleId, IReadOnlyCollection<long> menuIds, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedTenantId = tenantId ?? CurrentTenantId;

        var deleteable = DbClient.Deleteable<SysRoleMenu>()
            .Where(mapping => mapping.RoleId == roleId);

        if (resolvedTenantId.HasValue)
        {
            deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }

        await deleteable.ExecuteCommandAsync(cancellationToken);

        var distinctMenuIds = menuIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctMenuIds.Length == 0)
        {
            return;
        }

        var mappings = distinctMenuIds.Select(menuId => new SysRoleMenu
        {
            TenantId = resolvedTenantId,
            RoleId = roleId,
            MenuId = menuId,
            Status = YesOrNo.Yes
        }).ToArray();

        foreach (var mapping in mappings)
        {
            TrySetTenantId(mapping);
        }

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }
}
