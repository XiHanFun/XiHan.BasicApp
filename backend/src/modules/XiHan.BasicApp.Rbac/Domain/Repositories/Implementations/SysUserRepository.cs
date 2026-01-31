#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRepository
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统用户仓储实现
/// </summary>
public class SysUserRepository : SqlSugarAggregateRepository<SysUser, long>, ISysUserRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    public async Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.UserName == userName);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    public async Task<SysUser?> GetByEmailAsync(string email, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.Email == email);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    public async Task<SysUser?> GetByPhoneAsync(string phone, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.Phone == phone);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    public async Task<bool> IsUserNameExistsAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeUserId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取租户下的用户列表
    /// </summary>
    public async Task<List<SysUser>> GetUsersByTenantAsync(long tenantId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.TenantId == tenantId);

        if (isActive.HasValue)
        {
            query = query.Where(u => u.Status == (isActive.Value ? YesOrNo.Yes : YesOrNo.No));
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存用户
    /// </summary>
    public async Task<SysUser> SaveAsync(SysUser user, CancellationToken cancellationToken = default)
    {
        if (user.IsTransient())
        {
            return await AddAsync(user, cancellationToken);
        }
        else
        {
            return await UpdateAsync(user, cancellationToken);
        }
    }

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    public async Task UpdateLastLoginInfoAsync(long userId, string loginIp, DateTimeOffset loginTime, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysUser>()
            .SetColumns(u => new SysUser
            {
                LastLoginIp = loginIp,
                LastLoginTime = loginTime
            })
            .Where(u => u.BasicId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    public async Task EnableUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysUser>()
            .SetColumns(u => u.Status == YesOrNo.Yes)
            .Where(u => u.BasicId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    public async Task DisableUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysUser>()
            .SetColumns(u => u.Status == YesOrNo.No)
            .Where(u => u.BasicId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
