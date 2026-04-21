#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuRepository
// Guid:50645114-3d16-496e-a535-63c1756d8fb8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:53:17
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 菜单仓储实现
/// </summary>
public class MenuRepository : SqlSugarAuditedRepository<SysMenu, long>, IMenuRepository
{
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleHierarchyRepository"></param>
    /// <param name="clientResolver"></param>
    public MenuRepository(
        IRoleHierarchyRepository roleHierarchyRepository,
        ISqlSugarClientResolver clientResolver)
        : base(clientResolver)
    {
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <summary>
    /// 获取所有菜单
    /// </summary>
    public new async Task<IReadOnlyList<SysMenu>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await CreateQueryable()
            .OrderBy(menu => menu.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysMenu?> GetByMenuCodeAsync(string menuCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuCode);
        var query = SaasTenantQueryHelper
            .ApplyTenantFilter(CreateQueryable().Where(menu => menu.MenuCode == menuCode), tenantId, includePlatform: true)
            .OrderByDescending(menu => menu.TenantId);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色ID获取菜单（通过 SysRolePermission + SysMenu.PermissionId 推导）
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysMenu>> GetRoleMenusAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var permissionQuery = CreateQueryable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId && rp.Status == YesOrNo.Yes);

        permissionQuery = SaasTenantQueryHelper.ApplyTenantFilter(permissionQuery, tenantId);

        var permissionIds = await permissionQuery
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var query = CreateQueryable()
            .Where(menu => menu.PermissionId.HasValue
                && permissionIds.Contains(menu.PermissionId.Value)
                && menu.Status == YesOrNo.Yes);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId, includePlatform: true);

        return await query.OrderBy(menu => menu.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取菜单（通过用户角色→权限→菜单.PermissionId 推导）
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysMenu>> GetUserMenusAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var roleQuery = CreateQueryable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes);

        roleQuery = SaasTenantQueryHelper.ApplyTenantFilter(roleQuery, tenantId);

        var roleIds = await roleQuery
            .Select(mapping => mapping.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var inheritedRoleIds = await _roleHierarchyRepository.GetInheritedRoleIdsAsync(
            roleIds,
            tenantId,
            cancellationToken);

        if (inheritedRoleIds.Count == 0)
        {
            return [];
        }

        var rolePermissionQuery = CreateQueryable<SysRolePermission>()
            .Where(rp => inheritedRoleIds.Contains(rp.RoleId) && rp.Status == YesOrNo.Yes);

        rolePermissionQuery = SaasTenantQueryHelper.ApplyTenantFilter(rolePermissionQuery, tenantId);

        var permissionIds = await rolePermissionQuery
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var query = CreateQueryable()
            .Where(menu => menu.PermissionId.HasValue
                && permissionIds.Contains(menu.PermissionId.Value)
                && menu.Status == YesOrNo.Yes);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId, includePlatform: true);

        return await query.OrderBy(menu => menu.Sort)
            .ToListAsync(cancellationToken);
    }
}
