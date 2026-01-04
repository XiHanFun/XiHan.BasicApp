#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRoleService
// Guid:fa2b3c4d-5e6f-7890-abcd-ef12345678a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Services.Roles.Dtos;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Application.Services.Abstracts;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Roles;

/// <summary>
/// 系统角色服务接口
/// </summary>
public interface ISysRoleService : ICrudApplicationService<RoleDto, long, CreateRoleDto, UpdateRoleDto>
{
    /// <summary>
    /// 获取角色详情
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns></returns>
    Task<RoleDetailDto?> GetDetailAsync(long id);

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns></returns>
    Task<RoleDto?> GetByRoleCodeAsync(string roleCode);

    /// <summary>
    /// 分配菜单
    /// </summary>
    /// <param name="input">分配菜单DTO</param>
    /// <returns></returns>
    Task<bool> AssignMenusAsync(AssignRoleMenusDto input);

    /// <summary>
    /// 分配权限
    /// </summary>
    /// <param name="input">分配权限DTO</param>
    /// <returns></returns>
    Task<bool> AssignPermissionsAsync(AssignRolePermissionsDto input);

    /// <summary>
    /// 获取角色的菜单ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetRoleMenuIdsAsync(long roleId);

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetRolePermissionIdsAsync(long roleId);

    /// <summary>
    /// 设置父角色（建立继承关系）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="parentRoleId">父角色ID，null表示移除继承关系</param>
    /// <returns></returns>
    Task<bool> SetParentRoleAsync(long roleId, long? parentRoleId);

    /// <summary>
    /// 获取角色的所有权限（包括继承的权限）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="includeInherited">是否包括继承的权限，默认true</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetRolePermissionsWithInheritanceAsync(long roleId, bool includeInherited = true);

    /// <summary>
    /// 获取角色的所有菜单（包括继承的菜单）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="includeInherited">是否包括继承的菜单，默认true</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetRoleMenusWithInheritanceAsync(long roleId, bool includeInherited = true);

    /// <summary>
    /// 获取用户的所有权限（包括角色继承的权限）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetUserPermissionsAsync(long userId);

    /// <summary>
    /// 获取用户的所有菜单（包括角色继承的菜单）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetUserMenusAsync(long userId);

    /// <summary>
    /// 检查用户是否拥有指定权限（考虑继承和直接授予/禁用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限编码</param>
    /// <returns>是否拥有权限</returns>
    Task<bool> HasPermissionAsync(long userId, string permissionCode);

    /// <summary>
    /// 批量检查用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <returns>权限检查结果字典（key: 权限编码, value: 是否拥有）</returns>
    Task<Dictionary<string, bool>> BatchCheckPermissionsAsync(long userId, params string[] permissionCodes);

    /// <summary>
    /// 获取角色继承链（从当前角色到根角色）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色继承链</returns>
    Task<List<RoleDto>> GetRoleInheritanceChainAsync(long roleId);

    /// <summary>
    /// 获取角色树（包含子角色）
    /// </summary>
    /// <param name="parentRoleId">父角色ID，null表示获取根角色</param>
    /// <returns>角色树</returns>
    Task<List<RoleDto>> GetRoleTreeAsync(long? parentRoleId = null);
}
