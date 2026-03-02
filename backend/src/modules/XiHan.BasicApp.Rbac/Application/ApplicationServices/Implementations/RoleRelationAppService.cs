#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRelationAppService
// Guid:8f5592e0-1286-46c4-89f2-5532588bc86f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 角色关系应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class RoleRelationAppService : ApplicationServiceBase, IRoleRelationAppService
{
    private readonly IRoleRelationRepository _roleRelationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IRoleManager _roleManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRelationRepository"></param>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="menuRepository"></param>
    /// <param name="roleManager"></param>
    /// <param name="unitOfWorkManager"></param>
    public RoleRelationAppService(
        IRoleRelationRepository roleRelationRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        IRoleManager roleManager,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _roleRelationRepository = roleRelationRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _roleManager = roleManager;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<RolePermissionRelationDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            return [];
        }

        var relations = await _roleRelationRepository.GetRolePermissionsAsync(roleId, tenantId);
        return relations.Select(relation => new RolePermissionRelationDto
        {
            BasicId = relation.BasicId,
            TenantId = relation.TenantId,
            RoleId = relation.RoleId,
            PermissionId = relation.PermissionId,
            Status = relation.Status
        }).ToArray();
    }

    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<RoleMenuRelationDto>> GetRoleMenusAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            return [];
        }

        var relations = await _roleRelationRepository.GetRoleMenusAsync(roleId, tenantId);
        return relations.Select(relation => new RoleMenuRelationDto
        {
            BasicId = relation.BasicId,
            TenantId = relation.TenantId,
            RoleId = relation.RoleId,
            MenuId = relation.MenuId,
            Status = relation.Status
        }).ToArray();
    }

    /// <summary>
    /// 分配角色权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task AssignPermissionsAsync(AssignRolePermissionsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RoleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(command.RoleId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(command.RoleId)
                   ?? throw new KeyNotFoundException($"未找到角色: {command.RoleId}");

        var permissionIds = command.PermissionIds.Where(id => id > 0).Distinct().ToArray();
        if (permissionIds.Length > 0)
        {
            var permissions = await _permissionRepository.GetByIdsAsync(permissionIds);
            if (permissions.Count != permissionIds.Length)
            {
                throw new InvalidOperationException("存在无效权限 ID");
            }
        }

        await _roleManager.AssignPermissionsAsync(
            role,
            permissionIds,
            command.TenantId ?? role.TenantId);

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task AssignMenusAsync(AssignRoleMenusCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RoleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(command.RoleId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(command.RoleId)
                   ?? throw new KeyNotFoundException($"未找到角色: {command.RoleId}");

        var menuIds = command.MenuIds.Where(id => id > 0).Distinct().ToArray();
        if (menuIds.Length > 0)
        {
            var menus = await _menuRepository.GetByIdsAsync(menuIds);
            if (menus.Count != menuIds.Length)
            {
                throw new InvalidOperationException("存在无效菜单 ID");
            }
        }

        await _roleManager.AssignMenusAsync(
            command.RoleId,
            menuIds,
            command.TenantId ?? role.TenantId);

        await uow.CompleteAsync();
    }
}
