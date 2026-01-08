#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRepository
// Guid:a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : SqlSugarAggregateRepository<SysUser, long>, IUserRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户名查询用户
    /// </summary>
    public async Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUser>()
            .FirstAsync(u => u.UserName == userName, cancellationToken);
    }

    /// <summary>
    /// 根据邮箱查询用户
    /// </summary>
    public async Task<SysUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUser>()
            .FirstAsync(u => u.Email == email, cancellationToken);
    }

    /// <summary>
    /// 根据手机号查询用户
    /// </summary>
    public async Task<SysUser?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUser>()
            .FirstAsync(u => u.Phone == phone, cancellationToken);
    }

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    public async Task<bool> ExistsByUserNameAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysUser>()
            .Where(u => u.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysUser>()
            .Where(u => u.Email == email);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    public async Task<bool> ExistsByPhoneAsync(string phone, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysUser>()
            .Where(u => u.Phone == phone);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户及其角色
    /// </summary>
    public async Task<SysUser?> GetWithRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _dbClient.Queryable<SysUser>()
            .FirstAsync(u => u.BasicId == userId, cancellationToken);

        if (user != null)
        {
            // 获取用户的角色ID列表
            var roleIds = await _dbClient.Queryable<SysUserRole>()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(cancellationToken);

            // 可以在这里通过导航属性或动态属性存储角色信息
            // 这里只返回用户，角色可以通过其他服务层方法获取
        }

        return user;
    }

    /// <summary>
    /// 获取用户及其权限
    /// </summary>
    public async Task<SysUser?> GetWithPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _dbClient.Queryable<SysUser>()
            .FirstAsync(u => u.BasicId == userId, cancellationToken);

        return user;
    }

    /// <summary>
    /// 获取租户下的所有用户
    /// </summary>
    public async Task<List<SysUser>> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUser>()
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    public async Task<bool> UpdateLastLoginAsync(long userId, string loginIp, DateTimeOffset loginTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysUser>()
            .SetColumns(u => new SysUser
            {
                LastLoginIp = loginIp,
                LastLoginTime = loginTime
            })
            .Where(u => u.BasicId == userId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 获取用户的部门ID列表
    /// </summary>
    public async Task<List<long>> GetUserDepartmentIdsAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId)
            .Select(ud => ud.DepartmentId)
            .ToListAsync(cancellationToken);
    }
}
