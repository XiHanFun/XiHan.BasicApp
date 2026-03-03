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

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

/// <summary>
/// 权限规则领域服务实现
/// </summary>
public class PermissionDomainService : IPermissionDomainService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationDomainService _organizationDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionRepository">权限仓储</param>
    /// <param name="roleRepository">角色仓储</param>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="organizationDomainService">组织架构领域服务</param>
    public PermissionDomainService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IOrganizationDomainService organizationDomainService)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _organizationDomainService = organizationDomainService;
    }

    /// <summary>
    /// 获取用户最终权限编码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission.PermissionCode))
            .Select(permission => permission.PermissionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 判断用户是否具备某权限
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="permissionCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var codes = await GetUserPermissionCodesAsync(userId, tenantId, cancellationToken);
        return codes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算用户数据范围部门ID
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var activeRoles = await GetActiveUserRolesAsync(userId, tenantId, cancellationToken);
        if (activeRoles.Count == 0)
        {
            return [];
        }

        if (activeRoles.Any(role => role.DataScope == DataPermissionScope.All))
        {
            // 空集合表示不限部门（全量数据）
            return [];
        }

        var hasDepartmentOnly = activeRoles.Any(role => role.DataScope == DataPermissionScope.DepartmentOnly);
        var hasDepartmentAndChildren = activeRoles.Any(role => role.DataScope == DataPermissionScope.DepartmentAndChildren);
        var customRoles = activeRoles
            .Where(role => role.DataScope == DataPermissionScope.Custom)
            .ToArray();

        if (!hasDepartmentOnly && !hasDepartmentAndChildren && customRoles.Length == 0)
        {
            // SelfOnly 等不基于部门范围控制的策略，在部门维度不做过滤。
            return [];
        }

        var scopeDepartmentIds = new HashSet<long>();

        if (hasDepartmentOnly)
        {
            var ownDepartments = await _organizationDomainService.GetUserDepartmentScopeIdsAsync(
                userId,
                includeChildren: false,
                tenantId,
                cancellationToken);
            scopeDepartmentIds.UnionWith(ownDepartments);
        }

        if (hasDepartmentAndChildren)
        {
            var ownAndChildrenDepartments = await _organizationDomainService.GetUserDepartmentScopeIdsAsync(
                userId,
                includeChildren: true,
                tenantId,
                cancellationToken);
            scopeDepartmentIds.UnionWith(ownAndChildrenDepartments);
        }

        foreach (var customRole in customRoles)
        {
            var customDepartmentIds = await _roleRepository.GetCustomDataScopeDepartmentIdsAsync(
                customRole.BasicId,
                tenantId ?? customRole.TenantId,
                cancellationToken);
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

        var roles = await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);
        return roles
            .Where(role => role.Status == YesOrNo.Yes)
            .Where(role => !tenantId.HasValue || role.TenantId == tenantId.Value)
            .ToArray();
    }
}
