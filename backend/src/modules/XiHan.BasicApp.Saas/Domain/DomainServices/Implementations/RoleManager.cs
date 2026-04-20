#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleManager
// Guid:7392c3e6-e5f7-4e94-8dcd-f9425c3f0513
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:59:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 角色领域管理器实现
/// </summary>
public class RoleManager : IRoleManager
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleManager(
        IRoleRepository roleRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IDepartmentRepository departmentRepository)
    {
        _roleRepository = roleRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 校验角色编码唯一性
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeRoleId">排除的角色ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    /// <exception cref="InvalidOperationException">角色编码已存在</exception>
    public async Task EnsureRoleCodeUniqueAsync(
        string roleCode,
        long? excludeRoleId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var exists = await _roleRepository.IsRoleCodeExistsAsync(roleCode, excludeRoleId, tenantId, cancellationToken);
        if (exists)
        {
            throw new BusinessException(message: $"角色编码 '{roleCode}' 已存在");
        }
    }

    /// <summary>
    /// 分配角色权限
    /// </summary>
    /// <param name="role">角色</param>
    /// <param name="permissionIds">权限ID集合</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    /// <exception cref="ArgumentNullException">角色为空</exception>
    public async Task AssignPermissionsAsync(
        SysRole role,
        IReadOnlyCollection<long> permissionIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        await _roleRepository.ReplaceRolePermissionsAsync(
            role.BasicId,
            permissionIds,
            tenantId ?? role.TenantId,
            cancellationToken);

        role.MarkPermissionsChanged(permissionIds.Distinct().ToArray());
        await _roleRepository.UpdateAsync(role, cancellationToken);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task<SysRole> CreateRoleAsync(SysRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        await EnsureRoleCodeUniqueAsync(role.RoleCode, null, role.TenantId, cancellationToken);
        return await _roleRepository.AddAsync(role, cancellationToken);
    }

    /// <summary>
    /// 分配数据范围
    /// </summary>
    public async Task AssignDataScopeAsync(
        SysRole role,
        IReadOnlyCollection<long> departmentIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        var resolvedTenantId = tenantId ?? role.TenantId;

        if (departmentIds.Count > 0)
        {
            var existingDepts = await _departmentRepository.GetByIdsAsync(departmentIds, cancellationToken);
            if (existingDepts.Count != departmentIds.Count)
            {
                throw new BusinessException(message: "部分部门ID不存在");
            }
        }

        await _roleRepository.ReplaceCustomDataScopeDepartmentIdsAsync(
            role.BasicId, departmentIds, resolvedTenantId, cancellationToken);

        if (role.DataScope != DataPermissionScope.Custom && departmentIds.Count > 0)
        {
            role.DataScope = DataPermissionScope.Custom;
        }

        role.MarkDataScopeChanged(departmentIds.Distinct().ToArray());
        await _roleRepository.UpdateAsync(role, cancellationToken);
    }

    /// <summary>
    /// 分配角色继承关系
    /// </summary>
    public async Task AssignInheritanceAsync(
        SysRole role,
        IReadOnlyCollection<long> parentRoleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (parentRoleIds.Count > 0)
        {
            var existingRoles = await _roleRepository.GetByIdsAsync(parentRoleIds, cancellationToken);
            if (existingRoles.Count != parentRoleIds.Count)
            {
                throw new BusinessException(message: "部分父角色ID不存在");
            }
        }

        await _roleHierarchyRepository.ReplaceDirectParentsAsync(
            role.BasicId, parentRoleIds, cancellationToken: cancellationToken);
    }

}
