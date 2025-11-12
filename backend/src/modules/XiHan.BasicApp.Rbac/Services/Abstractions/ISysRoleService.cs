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

using XiHan.BasicApp.Rbac.Dtos.Roles;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Abstractions;

/// <summary>
/// 角色服务接口
/// </summary>
public interface ISysRoleService : ICrudApplicationService<RoleDto, RbacIdType, CreateRoleDto, UpdateRoleDto>
{
    /// <summary>
    /// 获取角色详情
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns></returns>
    Task<RoleDetailDto?> GetDetailAsync(RbacIdType id);

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
    Task<List<RbacIdType>> GetRoleMenuIdsAsync(RbacIdType roleId);

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<RbacIdType>> GetRolePermissionIdsAsync(RbacIdType roleId);
}
