#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDomainService
// Guid:4d655fdd-ed66-4096-8f34-d77f81a0390d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 权限规则领域服务实现
/// </summary>
public class PermissionDomainService : IPermissionDomainService, IScopedDependency
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationDomainService _organizationDomainService;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionDomainService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IUserRepository userRepository,
        IOrganizationDomainService organizationDomainService,
        ILocalEventBus localEventBus)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _userRepository = userRepository;
        _organizationDomainService = organizationDomainService;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysPermission> CreateAsync(SysPermission permission)
    {
        var created = await _permissionRepository.AddAsync(permission);
        await _localEventBus.PublishAsync(new PermissionChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysPermission> UpdateAsync(SysPermission permission)
    {
        var updated = await _permissionRepository.UpdateAsync(permission);
        await _localEventBus.PublishAsync(new PermissionChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            return false;
        }

        var result = await _permissionRepository.DeleteAsync(permission);
        if (result)
        {
            await _localEventBus.PublishAsync(new PermissionChangedDomainEvent(id));
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission.PermissionCode))
            .Select(permission => permission.PermissionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var codes = await GetUserPermissionCodesAsync(userId, tenantId, cancellationToken);
        return codes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var activeRoles = await GetActiveUserRolesAsync(userId, tenantId, cancellationToken);
        if (activeRoles.Count == 0)
        {
            return [];
        }

        if (activeRoles.Any(role => role.DataScope == DataPermissionScope.All))
        {
            return [];
        }

        var hasDepartmentOnly = activeRoles.Any(role => role.DataScope == DataPermissionScope.DepartmentOnly);
        var hasDepartmentAndChildren = activeRoles.Any(role => role.DataScope == DataPermissionScope.DepartmentAndChildren);
        var customRoles = activeRoles
            .Where(role => role.DataScope == DataPermissionScope.Custom)
            .ToArray();

        if (!hasDepartmentOnly && !hasDepartmentAndChildren && customRoles.Length == 0)
        {
            return [];
        }

        var scopeDepartmentIds = new HashSet<long>();

        if (hasDepartmentOnly)
        {
            var ownDepartments = await _organizationDomainService.GetUserDepartmentScopeIdsAsync(
                userId, includeChildren: false, tenantId, cancellationToken);
            scopeDepartmentIds.UnionWith(ownDepartments);
        }

        if (hasDepartmentAndChildren)
        {
            var ownAndChildrenDepartments = await _organizationDomainService.GetUserDepartmentScopeIdsAsync(
                userId, includeChildren: true, tenantId, cancellationToken);
            scopeDepartmentIds.UnionWith(ownAndChildrenDepartments);
        }

        foreach (var customRole in customRoles)
        {
            var customDepartmentIds = await _roleRepository.GetCustomDataScopeDepartmentIdsAsync(
                customRole.BasicId, tenantId ?? customRole.TenantId, cancellationToken);
            scopeDepartmentIds.UnionWith(customDepartmentIds);
        }

        return scopeDepartmentIds.ToArray();
    }

    private async Task<IReadOnlyCollection<SysRole>> GetActiveUserRolesAsync(long userId, long? tenantId, CancellationToken cancellationToken)
    {
        var roleRelations = await _userRepository.GetUserRolesAsync(userId, tenantId, cancellationToken);
        var roleIds = roleRelations
            .Where(relation => relation.Status == YesOrNo.Yes)
            .Select(relation => relation.RoleId)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
        {
            return [];
        }

        var inheritedRoleIds = await _roleHierarchyRepository.GetInheritedRoleIdsAsync(
            roleIds,
            tenantId,
            cancellationToken);
        if (inheritedRoleIds.Count == 0)
        {
            return [];
        }

        var roles = await _roleRepository.GetByIdsAsync([.. inheritedRoleIds], cancellationToken);
        return roles
            .Where(role => role.Status == YesOrNo.Yes)
            .Where(role => !tenantId.HasValue || role.TenantId == tenantId.Value)
            .ToArray();
    }
}
