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
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 角色应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("role:read")]
public class RoleAppService
    : CrudApplicationServiceBase<SysRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, BasicAppPRDto>,
        IRoleAppService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;
    private readonly IRoleManager _roleManager;
    private readonly IRoleQueryService _queryService;
    private readonly ILocalEventBus _localEventBus;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="departmentRepository"></param>
    /// <param name="roleHierarchyRepository"></param>
    /// <param name="roleManager"></param>
    /// <param name="queryService"></param>
    /// <param name="localEventBus"></param>
    /// <param name="unitOfWorkManager"></param>
    public RoleAppService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IDepartmentRepository departmentRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IRoleManager roleManager,
        IRoleQueryService queryService,
        ILocalEventBus localEventBus,
        IUnitOfWorkManager unitOfWorkManager)
        : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _departmentRepository = departmentRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _roleManager = roleManager;
        _queryService = queryService;
        _localEventBus = localEventBus;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    [PermissionAuthorize("role:read")]
    public override async Task<RoleDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:read")]
    public async Task<RoleDto?> GetByCodeAsync(RoleByCodeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _queryService.GetByCodeAsync(query.RoleCode, query.TenantId);
    }

    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:read")]
    public async Task<IReadOnlyList<RolePermissionRelationDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
        }

        return await _queryService.GetRolePermissionsAsync(roleId, tenantId);
    }

    /// <summary>
    /// 获取角色自定义数据范围部门ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:read")]
    public async Task<IReadOnlyCollection<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
        }

        return await _queryService.GetRoleDataScopeDepartmentIdsAsync(roleId, tenantId);
    }

    /// <summary>
    /// 获取角色直接父角色ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:read")]
    public async Task<IReadOnlyCollection<long>> GetRoleParentRoleIdsAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
        }

        return await _queryService.GetRoleParentRoleIdsAsync(roleId, tenantId);
    }

    /// <summary>
    /// 分配角色权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:update")]
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
                throw new BusinessException(message: "存在无效权限 ID");
            }
        }

        var resolvedTenantId = command.TenantId ?? role.TenantId;
        await _roleManager.AssignPermissionsAsync(
            role,
            permissionIds,
            resolvedTenantId);

        await uow.CompleteAsync();
        await PublishAuthorizationChangedEventAsync(resolvedTenantId, AuthorizationChangeType.Permission);
    }

    /// <summary>
    /// 分配角色自定义数据范围
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:update")]
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
                throw new BusinessException(message: "存在无效部门 ID");
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
        await PublishAuthorizationChangedEventAsync(command.TenantId ?? role.TenantId, AuthorizationChangeType.DataScope);
    }

    /// <summary>
    /// 分配角色继承关系
    /// </summary>
    /// <param name="command"></param>
    [PermissionAuthorize("role:update")]
    public async Task AssignInheritanceAsync(AssignRoleInheritanceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RoleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(command.RoleId));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(command.RoleId)
                   ?? throw new KeyNotFoundException($"未找到角色: {command.RoleId}");

        var parentRoleIds = command.ParentRoleIds.Where(id => id > 0).Distinct().ToArray();
        if (parentRoleIds.Length > 0)
        {
            var parentRoles = await _roleRepository.GetByIdsAsync(parentRoleIds);
            if (parentRoles.Count != parentRoleIds.Length)
            {
                throw new BusinessException(message: "存在无效父角色 ID");
            }
        }

        await _roleHierarchyRepository.ReplaceDirectParentsAsync(
            command.RoleId,
            parentRoleIds,
            command.TenantId ?? role.TenantId);

        await uow.CompleteAsync();
        await PublishAuthorizationChangedEventAsync(command.TenantId ?? role.TenantId, AuthorizationChangeType.All);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:create")]
    public override async Task<RoleDto> CreateAsync(RoleCreateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _roleManager.EnsureRoleCodeUniqueAsync(input.RoleCode, null, input.TenantId);

        var role = new SysRole
        {
            TenantId = input.TenantId ?? 0,
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
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("role:update")]
    public override async Task<RoleDto> UpdateAsync(RoleUpdateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(input.BasicId)
                   ?? throw new KeyNotFoundException($"未找到角色: {input.BasicId}");

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
    [PermissionAuthorize("role:delete")]
    public override async Task<bool> DeleteAsync(long roleId)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
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
