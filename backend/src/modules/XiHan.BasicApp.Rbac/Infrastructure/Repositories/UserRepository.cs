#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRepository
// Guid:7dcf161c-69ea-4fa5-ab72-8c524954d825
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:51:54
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : SqlSugarAggregateRepository<SysUser, long>, IUserRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public UserRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var query = CreateTenantQueryable()
            .Where(user => user.UserName == userName);

        if (tenantId.HasValue)
        {
            query = query.Where(user => user.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(user => user.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 判断用户名是否存在
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="excludeUserId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> IsUserNameExistsAsync(
        string userName,
        long? excludeUserId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var query = CreateTenantQueryable()
            .Where(user => user.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(user => user.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(user => user.TenantId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取安全状态
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysUserSecurity?> GetSecurityByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysUserSecurity>()
            .Where(entity => entity.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(entity => entity.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 保存用户安全状态（新增或更新）
    /// </summary>
    /// <param name="security"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysUserSecurity> SaveSecurityAsync(SysUserSecurity security, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(security);
        cancellationToken.ThrowIfCancellationRequested();

        if (security.BasicId <= 0)
        {
            return await DbClient.Insertable(security).ExecuteReturnEntityAsync();
        }

        await DbClient.Updateable(security).ExecuteCommandAsync(cancellationToken);
        return security;
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
        var resolvedTenantId = tenantId;

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
        var resolvedTenantId = tenantId;

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
        var resolvedTenantId = tenantId;

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

        await DbClient.Insertable(mappings).ExecuteCommandAsync(cancellationToken);
    }
}
