#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleManagementService
// Guid:d816b0be-9b68-4555-b744-0e0bfebc47bb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.DomainServices;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.DomainServices.Implementations;

/// <summary>
/// 角色管理领域服务实现
/// </summary>
public class RoleManagementService : DomainService, IRoleManagementService
{
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleManagementService(ISysRoleRepository roleRepository, ISqlSugarDbContext dbContext)
    {
        _roleRepository = roleRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    public async Task AssignRolesToUserAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        var userRoles = roleIds.Select(roleId => new SysUserRole
        {
            UserId = userId,
            RoleId = roleId,
            Status = YesOrNo.Yes,
            CreatedTime = DateTimeOffset.Now
        }).ToList();

        await _dbContext.GetClient().Insertable(userRoles)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    public async Task RemoveRolesFromUserAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserRole>()
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    public async Task AssignPermissionsToRoleAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default)
    {
        var rolePermissions = permissionIds.Select(permissionId => new SysRolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            Status = YesOrNo.Yes,
            CreatedTime = DateTimeOffset.Now
        }).ToList();

        await _dbContext.GetClient().Insertable(rolePermissions)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 移除角色的权限
    /// </summary>
    public async Task RemovePermissionsFromRoleAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId && permissionIds.Contains(rp.PermissionId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 为角色分配菜单
    /// </summary>
    public async Task AssignMenusToRoleAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken = default)
    {
        var roleMenus = menuIds.Select(menuId => new SysRoleMenu
        {
            RoleId = roleId,
            MenuId = menuId,
            Status = YesOrNo.Yes,
            CreatedTime = DateTimeOffset.Now
        }).ToList();

        await _dbContext.GetClient().Insertable(roleMenus)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 移除角色的菜单
    /// </summary>
    public async Task RemoveMenusFromRoleAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId && menuIds.Contains(rm.MenuId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 检查用户是否拥有指定角色
    /// </summary>
    public async Task<bool> UserHasRoleAsync(long userId, string roleCode, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
        return roles.Any(r => r.RoleCode == roleCode && r.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 检查用户是否拥有多个角色中的任意一个
    /// </summary>
    public async Task<bool> UserHasAnyRoleAsync(long userId, IEnumerable<string> roleCodes, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
        var userRoleCodes = roles.Where(r => r.Status == YesOrNo.Yes).Select(r => r.RoleCode).ToHashSet();
        return roleCodes.Any(code => userRoleCodes.Contains(code));
    }

    /// <summary>
    /// 验证角色是否可以被删除
    /// </summary>
    public async Task<bool> CanDeleteRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        // 检查角色是否被用户使用
        var userCount = await _dbContext.GetClient().Queryable<SysUserRole>()
            .Where(ur => ur.RoleId == roleId)
            .CountAsync(cancellationToken);

        return userCount == 0;
    }

    /// <summary>
    /// 检查角色编码是否重复
    /// </summary>
    public async Task<bool> IsRoleCodeDuplicateAsync(string roleCode, long? tenantId = null, long? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.IsRoleCodeExistsAsync(roleCode, excludeRoleId, tenantId, cancellationToken);
    }
}
