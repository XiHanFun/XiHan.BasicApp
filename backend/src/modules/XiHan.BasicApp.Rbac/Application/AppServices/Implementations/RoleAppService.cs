#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleAppService
// Guid:e4cb6f6f-d53d-49fb-bfdd-6e57be306491
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Caching.Events;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.UseCases.Commands;
using XiHan.BasicApp.Rbac.Application.UseCases.Queries;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 角色应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class RoleAppService
    : CrudApplicationServiceBase<SysRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, BasicAppPRDto>,
        IRoleAppService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IRoleManager _roleManager;
    private readonly ILocalEventBus _localEventBus;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="menuRepository"></param>
    /// <param name="departmentRepository"></param>
    /// <param name="roleManager"></param>
    /// <param name="localEventBus"></param>
    /// <param name="unitOfWorkManager"></param>
    public RoleAppService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        IDepartmentRepository departmentRepository,
        IRoleManager roleManager,
        ILocalEventBus localEventBus,
        IUnitOfWorkManager unitOfWorkManager)
        : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _departmentRepository = departmentRepository;
        _roleManager = roleManager;
        _localEventBus = localEventBus;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<RoleDto?> GetByCodeAsync(RoleByCodeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var role = await _roleRepository.GetByRoleCodeAsync(query.RoleCode, query.TenantId);
        return role?.Adapt<RoleDto>();
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

        var relations = await _roleRepository.GetRolePermissionsAsync(roleId, tenantId);
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

        var relations = await _roleRepository.GetRoleMenusAsync(roleId, tenantId);
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
    /// 获取角色自定义数据范围部门ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            return [];
        }

        return await _roleRepository.GetCustomDataScopeDepartmentIdsAsync(roleId, tenantId);
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

    /// <summary>
    /// 分配角色自定义数据范围
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task AssignDataScopeAsync(AssignRoleDataScopeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RoleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(command.RoleId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(command.RoleId)
                   ?? throw new KeyNotFoundException($"未找到角色: {command.RoleId}");

        var departmentIds = command.DepartmentIds.Where(id => id > 0).Distinct().ToArray();
        if (departmentIds.Length > 0)
        {
            var departments = await _departmentRepository.GetByIdsAsync(departmentIds);
            if (departments.Count != departmentIds.Length)
            {
                throw new InvalidOperationException("存在无效部门 ID");
            }
        }

        await _roleRepository.ReplaceCustomDataScopeDepartmentIdsAsync(
            command.RoleId,
            departmentIds,
            command.TenantId ?? role.TenantId);

        if (role.DataScope != DataPermissionScope.Custom)
        {
            role.DataScope = DataPermissionScope.Custom;
        }

        role.MarkDataScopeChanged(departmentIds);
        await _roleRepository.UpdateAsync(role);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<RoleDto> CreateAsync(RoleCreateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _roleManager.EnsureRoleCodeUniqueAsync(input.RoleCode, null, input.TenantId);

        var role = new SysRole
        {
            TenantId = input.TenantId,
            RoleCode = input.RoleCode.Trim(),
            RoleName = input.RoleName.Trim(),
            RoleDescription = input.RoleDescription,
            RoleType = input.RoleType,
            DataScope = input.DataScope,
            Sort = input.Sort,
            Status = YesOrNo.Yes
        };

        var created = await _roleRepository.AddAsync(role);
        await uow.CompleteAsync();
        return created.Adapt<RoleDto>()!;
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<RoleDto> UpdateAsync(long id, RoleUpdateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(id)
                   ?? throw new KeyNotFoundException($"未找到角色: {id}");

        role.RoleName = input.RoleName;
        role.RoleDescription = input.RoleDescription;
        role.DataScope = input.DataScope;
        role.Sort = input.Sort;
        role.Remark = input.Remark;

        if (input.Status == YesOrNo.Yes)
        {
            role.Enable();
        }
        else
        {
            role.Disable();
        }

        var updated = await _roleRepository.UpdateAsync(role);
        await PublishAuthorizationChangedEventAsync(updated.TenantId, AuthorizationChangeType.All);
        await uow.CompleteAsync();
        return updated.Adapt<RoleDto>()!;
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public override async Task<bool> DeleteAsync(long roleId)
    {
        if (roleId <= 0)
        {
            return false;
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role is null)
        {
            return false;
        }

        await _roleRepository.DeleteAsync(role);
        await PublishAuthorizationChangedEventAsync(role.TenantId, AuthorizationChangeType.All);
        await uow.CompleteAsync();
        return true;
    }

    private Task PublishAuthorizationChangedEventAsync(long? tenantId, AuthorizationChangeType changeType)
    {
        return _localEventBus.PublishAsync(new RbacAuthorizationChangedEvent(tenantId, changeType));
    }

}
