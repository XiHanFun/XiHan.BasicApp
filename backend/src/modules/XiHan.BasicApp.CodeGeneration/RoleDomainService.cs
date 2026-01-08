#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDomainService
// Guid:b2c3d4e5-f6a7-4b5c-8d9e-1f2a3b4c5d6e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 角色领域服务
/// 处理角色相关的跨聚合业务逻辑（角色继承、权限聚合等）
/// </summary>
public class RoleDomainService : DomainService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleDomainService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 分配权限给角色（跨聚合操作）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AssignPermissionsToRoleAsync(long roleId, List<long> permissionIds, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(AssignPermissionsToRoleAsync), new { roleId, permissionIds });

        // 验证角色存在
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"角色 {roleId} 不存在");
        }

        // 验证权限存在
        var permissions = await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);
        if (permissions.Count != permissionIds.Count)
        {
            throw new InvalidOperationException("部分权限不存在");
        }

        Logger.LogInformation("角色 {RoleId} 权限分配验证通过，待分配权限: {PermissionCount}", roleId, permissionIds.Count);
        return true;
    }

    /// <summary>
    /// 分配菜单给角色（跨聚合操作）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AssignMenusToRoleAsync(long roleId, List<long> menuIds, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(AssignMenusToRoleAsync), new { roleId, menuIds });

        // 验证角色存在
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"角色 {roleId} 不存在");
        }

        // 验证菜单存在（批量查询）
        var menus = new List<SysMenu>();
        foreach (var menuId in menuIds)
        {
            var menu = await _menuRepository.GetByIdAsync(menuId, cancellationToken);
            if (menu == null)
            {
                throw new InvalidOperationException($"菜单 {menuId} 不存在");
            }
            menus.Add(menu);
        }

        Logger.LogInformation("角色 {RoleId} 菜单分配验证通过，待分配菜单: {MenuCount}", roleId, menuIds.Count);
        return true;
    }

    /// <summary>
    /// 获取角色的所有权限（包括继承的权限）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    public async Task<List<SysPermission>> GetRolePermissionsIncludingInheritedAsync(long roleId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetRolePermissionsIncludingInheritedAsync), new { roleId });

        var allPermissions = new HashSet<SysPermission>();

        // 获取当前角色的权限
        var directPermissions = await _permissionRepository.GetByRoleIdAsync(roleId, cancellationToken);
        foreach (var permission in directPermissions)
        {
            allPermissions.Add(permission);
        }

        // 获取父角色的权限（角色继承）
        var parentRoles = await _roleRepository.GetParentRolesAsync(roleId, cancellationToken);
        foreach (var parentRole in parentRoles)
        {
            var parentPermissions = await _permissionRepository.GetByRoleIdAsync(parentRole.BasicId, cancellationToken);
            foreach (var permission in parentPermissions)
            {
                allPermissions.Add(permission);
            }
        }

        Logger.LogInformation("角色 {RoleId} 共有权限(含继承): {PermissionCount}", roleId, allPermissions.Count);
        return allPermissions.ToList();
    }

    /// <summary>
    /// 获取角色的所有菜单（包括继承的菜单）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    public async Task<List<SysMenu>> GetRoleMenusIncludingInheritedAsync(long roleId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetRoleMenusIncludingInheritedAsync), new { roleId });

        var allMenus = new HashSet<SysMenu>();

        // 获取当前角色的菜单
        var directMenus = await _menuRepository.GetByRoleIdAsync(roleId, cancellationToken);
        foreach (var menu in directMenus)
        {
            allMenus.Add(menu);
        }

        // 获取父角色的菜单（角色继承）
        var parentRoles = await _roleRepository.GetParentRolesAsync(roleId, cancellationToken);
        foreach (var parentRole in parentRoles)
        {
            var parentMenus = await _menuRepository.GetByRoleIdAsync(parentRole.BasicId, cancellationToken);
            foreach (var menu in parentMenus)
            {
                allMenus.Add(menu);
            }
        }

        Logger.LogInformation("角色 {RoleId} 共有菜单(含继承): {MenuCount}", roleId, allMenus.Count);
        return allMenus.ToList();
    }

    /// <summary>
    /// 检查角色编码唯一性
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeRoleId">排除的角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsRoleCodeUniqueAsync(string roleCode, long? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _roleRepository.ExistsByRoleCodeAsync(roleCode, excludeRoleId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 验证角色删除前置条件
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanDeleteRoleAsync), new { roleId });

        // 检查是否有用户正在使用该角色
        var users = await _roleRepository.GetUsersByRoleIdAsync(roleId, cancellationToken);
        if (users.Count > 0)
        {
            throw new InvalidOperationException($"角色正在被 {users.Count} 个用户使用，无法删除");
        }

        // 检查是否有子角色（角色继承）
        var childRoles = await _roleRepository.GetChildRolesAsync(roleId, cancellationToken);
        if (childRoles.Count > 0)
        {
            throw new InvalidOperationException($"角色有 {childRoles.Count} 个子角色，无法删除");
        }

        Logger.LogInformation("角色 {RoleId} 可以删除", roleId);
        return true;
    }

    /// <summary>
    /// 验证角色继承关系是否合法（避免循环继承）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以设置父角色</returns>
    public async Task<bool> CanSetParentRoleAsync(long roleId, long parentRoleId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanSetParentRoleAsync), new { roleId, parentRoleId });

        // 不能将自己设为父角色
        if (roleId == parentRoleId)
        {
            throw new InvalidOperationException("不能将角色设置为自己的父角色");
        }

        // 检查是否会形成循环继承
        var wouldCreateCycle = await _roleRepository.WouldCreateCycleAsync(roleId, parentRoleId, cancellationToken);
        if (wouldCreateCycle)
        {
            throw new InvalidOperationException("设置该父角色会形成循环继承关系");
        }

        Logger.LogInformation("角色 {RoleId} 可以设置父角色 {ParentRoleId}", roleId, parentRoleId);
        return true;
    }

    /// <summary>
    /// 检查角色编码格式是否合法
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns>是否合法</returns>
    public bool IsValidRoleCode(string roleCode)
    {
        // 业务规则：角色编码不能为空，长度在2-50之间，只能包含字母、数字、下划线
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return false;
        }

        if (roleCode.Length is < 2 or > 50)
        {
            return false;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(roleCode, @"^[a-zA-Z0-9_]+$");
    }
}
