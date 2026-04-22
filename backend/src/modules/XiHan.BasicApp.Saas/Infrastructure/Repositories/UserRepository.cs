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

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : SqlSugarAggregateRepository<SysUser, long>, IUserRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientResolver"></param>
    /// <param name="unitOfWorkManager"></param>
    public UserRepository(
        ISqlSugarClientResolver clientResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientResolver, unitOfWorkManager)
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

        var query = CreateQueryable()
            .Where(user => user.UserName == userName);

        return await FirstByTenantAsync(query, tenantId, cancellationToken);
    }

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phone"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysUser?> GetByPhoneAsync(string phone, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        var query = CreateQueryable()
            .Where(user => user.Phone == phone);

        return await FirstByTenantAsync(query, tenantId, cancellationToken);
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysUser?> GetByEmailAsync(string email, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var query = CreateQueryable()
            .Where(user => user.Email == email);

        return await FirstByTenantAsync(query, tenantId, cancellationToken);
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

        var query = CreateQueryable()
            .Where(user => user.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

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
        var query = CreateQueryable<SysUserSecurity>()
            .Where(entity => entity.UserId == userId);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

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
        var query = CreateQueryable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取用户角色 ID 映射
    /// </summary>
    /// <param name="userIds"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyDictionary<long, IReadOnlyList<long>>> GetRoleIdsMapByUserIdsAsync(
        IReadOnlyCollection<long> userIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var distinctUserIds = userIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctUserIds.Length == 0)
        {
            return new Dictionary<long, IReadOnlyList<long>>();
        }

        var query = CreateQueryable<SysUserRole>()
            .Where(mapping => distinctUserIds.Contains(mapping.UserId));

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

        var mappings = await query.ToListAsync(cancellationToken);
        return mappings
            .GroupBy(mapping => mapping.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<long>)[.. group
                    .Select(mapping => mapping.RoleId)
                    .Distinct()]);
    }

    /// <summary>
    /// 根据角色ID获取关联用户ID列表
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetUserIdsByRoleIdAsync(
        long roleId,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable<SysUserRole>()
            .Where(mapping => mapping.RoleId == roleId && mapping.Status == YesOrNo.Yes);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

        return await query
            .Select(mapping => mapping.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
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
        var query = CreateQueryable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

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
        var query = CreateQueryable<SysUserDepartment>()
            .Where(mapping => mapping.UserId == userId);

        query = SaasTenantQueryHelper.ApplyTenantFilter(query, tenantId);

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
        var resolvedTenantId = SaasTenantQueryHelper.ResolveWriteTenantId(tenantId);

        var deleteable = DbClient.Deleteable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId);

        deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId);

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
        var resolvedTenantId = SaasTenantQueryHelper.ResolveWriteTenantId(tenantId);

        var deleteable = DbClient.Deleteable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId);

        deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId);

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
        var resolvedTenantId = SaasTenantQueryHelper.ResolveWriteTenantId(tenantId);

        var deleteable = DbClient.Deleteable<SysUserDepartment>()
            .Where(mapping => mapping.UserId == userId);

        deleteable = deleteable.Where(mapping => mapping.TenantId == resolvedTenantId);

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

    public async Task<SysTenantUser?> GetTenantMembershipAsync(long userId, long tenantId, CancellationToken cancellationToken = default)
    {
        return await CreateQueryable<SysTenantUser>()
            .Where(membership => membership.UserId == userId && membership.TenantId == tenantId)
            .FirstAsync(cancellationToken);
    }

    public async Task<SysTenantUser> SaveTenantMembershipAsync(SysTenantUser membership, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);
        cancellationToken.ThrowIfCancellationRequested();

        if (membership.BasicId <= 0)
        {
            return await DbClient.Insertable(membership).ExecuteReturnEntityAsync();
        }

        await DbClient.Updateable(membership).ExecuteCommandAsync(cancellationToken);
        return membership;
    }

    /// <summary>
    /// 按租户优先级获取首个用户（优先指定租户，回退到全局租户）
    /// </summary>
    /// <param name="query"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task<SysUser?> FirstByTenantAsync(
        ISugarQueryable<SysUser> query,
        long? tenantId,
        CancellationToken cancellationToken)
    {
        var resolvedTenantId = SaasTenantQueryHelper.ResolveWriteTenantId(tenantId);
        return await query.Where(user => user.TenantId == resolvedTenantId).FirstAsync(cancellationToken);
    }
}
