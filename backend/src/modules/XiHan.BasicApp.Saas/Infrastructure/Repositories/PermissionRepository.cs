#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRepository
// Guid:1eef8771-7cd0-485a-9c96-9e412e20c9ac
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:52:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public class PermissionRepository : SqlSugarAuditedRepository<SysPermission, long>, IPermissionRepository
{
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleHierarchyRepository"></param>
    /// <param name="clientResolver"></param>
    public PermissionRepository(
        IRoleHierarchyRepository roleHierarchyRepository,
        ISqlSugarClientResolver clientResolver)
        : base(clientResolver)
    {
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var query = CreateQueryable().Where(permission => permission.PermissionCode == permissionCode);

        query = tenantId.HasValue
            ? query.Where(permission => permission.TenantId == tenantId.Value)
            : query.Where(permission => permission.TenantId == 0);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysPermission>> GetUserPermissionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = tenantId;
        var roleQuery = CreateQueryable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes);

        roleQuery = resolvedTenantId.HasValue
            ? roleQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value)
            : roleQuery.Where(mapping => mapping.TenantId == 0);

        var directRoleIds = await roleQuery
            .Select(mapping => mapping.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var inheritedRoleIds = await _roleHierarchyRepository.GetInheritedRoleIdsAsync(
            directRoleIds,
            resolvedTenantId,
            cancellationToken);

        var rolePermissionIds = new List<long>();
        if (inheritedRoleIds.Count > 0)
        {
            var rolePermissionQuery = CreateQueryable<SysRolePermission>()
                .Where(mapping =>
                    inheritedRoleIds.Contains(mapping.RoleId)
                    && mapping.Status == YesOrNo.Yes);

            rolePermissionQuery = resolvedTenantId.HasValue
                ? rolePermissionQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value)
                : rolePermissionQuery.Where(mapping => mapping.TenantId == 0);

            rolePermissionIds = await rolePermissionQuery
                .Select(mapping => mapping.PermissionId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        var directPermissionQuery = CreateQueryable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes);

        directPermissionQuery = resolvedTenantId.HasValue
            ? directPermissionQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value)
            : directPermissionQuery.Where(mapping => mapping.TenantId == 0);

        // 获取用户直接授权的权限列表，包括授权和拒绝
        var allUserPermissions = await directPermissionQuery
            .Select(mapping => new { mapping.PermissionId, mapping.PermissionAction, mapping.EffectiveTime, mapping.ExpirationTime })
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var validUserPermissions = allUserPermissions.Where(x =>
            (!x.EffectiveTime.HasValue || x.EffectiveTime <= now) &&
            (!x.ExpirationTime.HasValue || x.ExpirationTime > now));

        // 将授权和拒绝的权限ID分别存储在两个HashSet中，方便后续的权限计算
        var grantIds = validUserPermissions
            .Where(x => x.PermissionAction == PermissionAction.Grant)
            .Select(x => x.PermissionId)
            .ToHashSet();

        var denyIds = validUserPermissions
            .Where(x => x.PermissionAction == PermissionAction.Deny)
            .Select(x => x.PermissionId)
            .ToHashSet();

        var permissionIdSet = rolePermissionIds.ToHashSet();
        permissionIdSet.UnionWith(grantIds);
        permissionIdSet.ExceptWith(denyIds);

        if (permissionIdSet.Count == 0)
        {
            return [];
        }

        var query = CreateQueryable()
            .Where(permission => permissionIdSet.Contains(permission.BasicId) && permission.Status == YesOrNo.Yes);

        query = resolvedTenantId.HasValue
            ? query.Where(permission => permission.TenantId == resolvedTenantId.Value)
            : query.Where(permission => permission.TenantId == 0);

        return await query.OrderBy(permission => permission.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysPermission>> GetRolePermissionsAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = tenantId;
        var mappingQuery = CreateQueryable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == roleId && mapping.Status == YesOrNo.Yes);

        mappingQuery = resolvedTenantId.HasValue
            ? mappingQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value)
            : mappingQuery.Where(mapping => mapping.TenantId == 0);

        var permissionIds = await mappingQuery
            .Select(mapping => mapping.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var query = CreateQueryable()
            .Where(permission => permissionIds.Contains(permission.BasicId) && permission.Status == YesOrNo.Yes);

        query = resolvedTenantId.HasValue
            ? query.Where(permission => permission.TenantId == resolvedTenantId.Value)
            : query.Where(permission => permission.TenantId == 0);

        return await query.OrderBy(permission => permission.Sort)
            .ToListAsync(cancellationToken);
    }
}
