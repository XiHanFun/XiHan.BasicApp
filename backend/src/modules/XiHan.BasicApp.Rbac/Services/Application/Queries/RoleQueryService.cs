#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleQueryService
// Guid:c4d5e6f7-a8b9-4c5d-1e2f-3a4b5c6d7e8f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 角色查询服务（处理角色的读操作 - CQRS）
/// </summary>
public class RoleQueryService : ApplicationServiceBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly RoleDomainService _roleDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleQueryService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        RoleDomainService roleDomainService)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _roleDomainService = roleDomainService;
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>角色DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns>角色DTO</returns>
    public async Task<RbacDtoBase?> GetByRoleCodeAsync(string roleCode)
    {
        var role = await _roleRepository.GetByRoleCodeAsync(roleCode);
        return role?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取角色的权限列表（包括继承的权限）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>权限DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetRolePermissionsAsync(long roleId)
    {
        var permissions = await _roleDomainService.GetRolePermissionsIncludingInheritedAsync(roleId);
        return permissions.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色的菜单列表（包括继承的菜单）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>菜单DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetRoleMenusAsync(long roleId)
    {
        var menus = await _roleDomainService.GetRoleMenusIncludingInheritedAsync(roleId);
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色的用户列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>用户DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetRoleUsersAsync(long roleId)
    {
        var users = await _roleRepository.GetUsersByRoleIdAsync(roleId);
        return users.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色的父角色列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetParentRolesAsync(long roleId)
    {
        var roles = await _roleRepository.GetParentRolesAsync(roleId);
        return roles.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色的子角色列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>角色DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetChildRolesAsync(long roleId)
    {
        var roles = await _roleRepository.GetChildRolesAsync(roleId);
        return roles.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _roleRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
