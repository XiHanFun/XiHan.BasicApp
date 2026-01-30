#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleRelationRepository
// Guid:b6c7d8e9-f0a1-2345-6789-012345b01234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 角色关系映射仓储实现
/// </summary>
public class SysRoleRelationRepository : ISysRoleRelationRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRoleRelationRepository(ISqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ========== 角色菜单关系 ==========

    /// <summary>
    /// 批量添加角色菜单
    /// </summary>
    public async Task AddRoleMenusAsync(IEnumerable<SysRoleMenu> roleMenus, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(roleMenus.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除角色菜单
    /// </summary>
    public async Task DeleteRoleMenusAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId && menuIds.Contains(rm.MenuId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除角色所有菜单
    /// </summary>
    public async Task DeleteRoleAllMenusAsync(long roleId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色菜单列表
    /// </summary>
    public async Task<List<SysRoleMenu>> GetRoleMenusAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId && rm.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    // ========== 角色权限关系 ==========

    /// <summary>
    /// 批量添加角色权限
    /// </summary>
    public async Task AddRolePermissionsAsync(IEnumerable<SysRolePermission> rolePermissions, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(rolePermissions.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除角色权限
    /// </summary>
    public async Task DeleteRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId && permissionIds.Contains(rp.PermissionId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除角色所有权限
    /// </summary>
    public async Task DeleteRoleAllPermissionsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色权限列表
    /// </summary>
    public async Task<List<SysRolePermission>> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId && rp.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }
}
