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
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public class PermissionRepository : SqlSugarAggregateRepository<SysPermission, long>, IPermissionRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public PermissionRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
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
        var resolvedTenantId = tenantId;
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

        var directPermissionQuery = CreateTenantQueryable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes);

        if (resolvedTenantId.HasValue)
        {
            directPermissionQuery = directPermissionQuery.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }
        else
        {
            directPermissionQuery = directPermissionQuery.Where(mapping => mapping.TenantId == null);
        }

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
        var resolvedTenantId = tenantId;
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
