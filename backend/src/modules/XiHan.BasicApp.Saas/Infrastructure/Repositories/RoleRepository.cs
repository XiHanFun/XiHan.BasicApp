#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRepository
// Guid:ca2dd776-5f0c-4617-9009-5bd860aa0943
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:52:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 角色仓储实现
/// </summary>
public class RoleRepository : SqlSugarAggregateRepository<SysRole, long>, IRoleRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public RoleRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var query = CreateTenantQueryable().Where(role => role.RoleCode == roleCode);

        query = tenantId.HasValue ? query.Where(role => role.TenantId == tenantId.Value) : query.Where(role => role.TenantId == 0);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 判断角色编码是否存在
    /// </summary>
    /// <param name="roleCode"></param>
    /// <param name="excludeRoleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> IsRoleCodeExistsAsync(
        string roleCode,
        long? excludeRoleId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var query = CreateTenantQueryable().Where(role => role.RoleCode == roleCode);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(role => role.BasicId != excludeRoleId.Value);
        }

        query = tenantId.HasValue ? query.Where(role => role.TenantId == tenantId.Value) : query.Where(role => role.TenantId == 0);

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色自定义数据范围部门ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetCustomDataScopeDepartmentIdsAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            return [];
        }

        var query = CreateTenantQueryable<SysRoleDataScope>()
            .Where(scope => scope.RoleId == roleId && scope.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(scope => scope.TenantId == tenantId.Value);
        }

        return await query
            .Select(scope => scope.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 替换角色自定义数据范围部门ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="departmentIds"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReplaceCustomDataScopeDepartmentIdsAsync(
        long roleId,
        IReadOnlyCollection<long> departmentIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var resolvedTenantId = tenantId;

        var deleteable = DbClient.Deleteable<SysRoleDataScope>()
            .Where(scope => scope.RoleId == roleId);

        if (resolvedTenantId.HasValue)
        {
            deleteable = deleteable.Where(scope => scope.TenantId == resolvedTenantId.Value);
        }

        await deleteable.ExecuteCommandAsync(cancellationToken);

        var distinctDepartmentIds = departmentIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctDepartmentIds.Length == 0)
        {
            return;
        }

        var scopes = distinctDepartmentIds.Select(departmentId => new SysRoleDataScope
        {
            TenantId = resolvedTenantId,
            RoleId = roleId,
            DepartmentId = departmentId,
            Status = YesOrNo.Yes
        }).ToArray();

        await DbClient.Insertable(scopes).ExecuteCommandAsync(cancellationToken);
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
        var resolvedTenantId = tenantId;

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
        var resolvedTenantId = tenantId;

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

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }
}
