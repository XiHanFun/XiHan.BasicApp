#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRelationRepository
// Guid:9a6e3dca-5764-494f-a7b7-bf6046d03318
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 14:26:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 用户关系仓储实现（用户-角色/权限/部门）
/// </summary>
public class UserRelationRepository : SqlSugarRepositoryBase<SysUserRole, long>, IUserRelationRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    public UserRelationRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider)
        : base(clientProvider, currentTenant, serviceProvider)
    {
    }

    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysUserRole>> GetUserRolesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(mapping => mapping.TenantId == tenantId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户直授权限关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysUserPermission>> GetUserPermissionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(mapping => mapping.TenantId == tenantId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysUserDepartment>> GetUserDepartmentsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysUserDepartment>()
            .Where(mapping => mapping.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(mapping => mapping.TenantId == tenantId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 替换用户角色关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleIds"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReplaceUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedTenantId = tenantId ?? CurrentTenantId;

        var deleteable = DbClient.Deleteable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId);

        if (resolvedTenantId.HasValue)
        {
            deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }

        await deleteable.ExecuteCommandAsync(cancellationToken);

        var distinctRoleIds = roleIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctRoleIds.Length == 0)
        {
            return;
        }

        var mappings = distinctRoleIds.Select(roleId => new SysUserRole
        {
            TenantId = resolvedTenantId,
            UserId = userId,
            RoleId = roleId,
            Status = YesOrNo.Yes
        }).ToArray();

        foreach (var mapping in mappings)
        {
            TrySetTenantId(mapping);
        }

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 替换用户直授权限关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="permissionIds"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReplaceUserPermissionsAsync(long userId, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedTenantId = tenantId ?? CurrentTenantId;

        var deleteable = DbClient.Deleteable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId);

        if (resolvedTenantId.HasValue)
        {
            deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }

        await deleteable.ExecuteCommandAsync(cancellationToken);

        var distinctPermissionIds = permissionIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctPermissionIds.Length == 0)
        {
            return;
        }

        var mappings = distinctPermissionIds.Select(permissionId => new SysUserPermission
        {
            TenantId = resolvedTenantId,
            UserId = userId,
            PermissionId = permissionId,
            PermissionAction = PermissionAction.Grant,
            Status = YesOrNo.Yes
        }).ToArray();

        foreach (var mapping in mappings)
        {
            TrySetTenantId(mapping);
        }

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 替换用户部门关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="departmentIds"></param>
    /// <param name="mainDepartmentId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReplaceUserDepartmentsAsync(
        long userId,
        IReadOnlyCollection<long> departmentIds,
        long? mainDepartmentId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedTenantId = tenantId ?? CurrentTenantId;

        var deleteable = DbClient.Deleteable<SysUserDepartment>()
            .Where(mapping => mapping.UserId == userId);

        if (resolvedTenantId.HasValue)
        {
            deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId.Value);
        }

        await deleteable.ExecuteCommandAsync(cancellationToken);

        var distinctDepartmentIds = departmentIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctDepartmentIds.Length == 0)
        {
            return;
        }

        var mappings = distinctDepartmentIds.Select(departmentId => new SysUserDepartment
        {
            TenantId = resolvedTenantId,
            UserId = userId,
            DepartmentId = departmentId,
            IsMain = mainDepartmentId.HasValue && mainDepartmentId.Value == departmentId,
            Status = YesOrNo.Yes
        }).ToArray();

        foreach (var mapping in mappings)
        {
            TrySetTenantId(mapping);
        }

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }
}
