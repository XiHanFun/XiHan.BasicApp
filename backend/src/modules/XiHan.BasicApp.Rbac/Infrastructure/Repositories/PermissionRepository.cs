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

using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public class PermissionRepository : SqlSugarAggregateRepository<SysPermission, long>, IPermissionRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public PermissionRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
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
        var query = CreateTenantQueryable().Where(permission => permission.PermissionCode == permissionCode);

        if (tenantId.HasValue)
        {
            query = query.Where(permission => permission.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(permission => permission.TenantId == null);
        }

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
        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var rolePermissionQuery = DbClient.Queryable<SysUserRole, SysRolePermission>(
            (userRole, rolePermission) => userRole.RoleId == rolePermission.RoleId)
            .Where((userRole, rolePermission) =>
                userRole.UserId == userId &&
                userRole.Status == YesOrNo.Yes &&
                rolePermission.Status == YesOrNo.Yes);

        if (resolvedTenantId.HasValue)
        {
            rolePermissionQuery = rolePermissionQuery.Where((userRole, rolePermission) =>
                userRole.TenantId == resolvedTenantId.Value && rolePermission.TenantId == resolvedTenantId.Value);
        }
        else
        {
            rolePermissionQuery = rolePermissionQuery.Where((userRole, rolePermission) =>
                userRole.TenantId == null && rolePermission.TenantId == null);
        }

        var rolePermissionIds = await rolePermissionQuery
            .Select((userRole, rolePermission) => rolePermission.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var directPermissionQuery = CreateTenantQueryable<SysUserPermission>()
            .Where(mapping =>
                mapping.UserId == userId
                && mapping.Status == YesOrNo.Yes
                && (!mapping.EffectiveTime.HasValue || mapping.EffectiveTime <= now)
                && (!mapping.ExpirationTime.HasValue || mapping.ExpirationTime > now));

        if (resolvedTenantId.HasValue)
        {
            directPermissionQuery = directPermissionQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }
        else
        {
            directPermissionQuery = directPermissionQuery.Where(mapping => mapping.TenantId == null);
        }

        var directPermissions = await directPermissionQuery.ToListAsync(cancellationToken);

        var grantIds = directPermissions
            .Where(mapping => mapping.PermissionAction == PermissionAction.Grant)
            .Select(mapping => mapping.PermissionId)
            .ToHashSet();
        var denyIds = directPermissions
            .Where(mapping => mapping.PermissionAction == PermissionAction.Deny)
            .Select(mapping => mapping.PermissionId)
            .ToHashSet();

        var permissionIdSet = rolePermissionIds.ToHashSet();
        permissionIdSet.UnionWith(grantIds);
        permissionIdSet.ExceptWith(denyIds);

        if (permissionIdSet.Count == 0)
        {
            return [];
        }

        var query = CreateTenantQueryable()
            .Where(permission => permissionIdSet.Contains(permission.BasicId) && permission.Status == YesOrNo.Yes);

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(permission => permission.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(permission => permission.TenantId == null);
        }

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
        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var mappingQuery = CreateTenantQueryable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == roleId && mapping.Status == YesOrNo.Yes);

        if (resolvedTenantId.HasValue)
        {
            mappingQuery = mappingQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }
        else
        {
            mappingQuery = mappingQuery.Where(mapping => mapping.TenantId == null);
        }

        var permissionIds = await mappingQuery
            .Select(mapping => mapping.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var query = CreateTenantQueryable()
            .Where(permission => permissionIds.Contains(permission.BasicId) && permission.Status == YesOrNo.Yes);

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(permission => permission.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(permission => permission.TenantId == null);
        }

        return await query.OrderBy(permission => permission.Sort)
            .ToListAsync(cancellationToken);
    }
}
