#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurityRepository
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统用户安全仓储实现
/// </summary>
public class SysUserSecurityRepository : SqlSugarReadOnlyRepository<SysUserSecurity, long>, ISysUserSecurityRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserSecurityRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    public async Task<SysUserSecurity?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUserSecurity>()
            .Where(us => us.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 保存用户安全信息
    /// </summary>
    public async Task<SysUserSecurity> SaveAsync(SysUserSecurity userSecurity, CancellationToken cancellationToken = default)
    {
        if (userSecurity.BasicId == 0)
        {
            // 新增
            await _dbContext.GetClient().Insertable(userSecurity).ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 更新
            await _dbContext.GetClient().Updateable(userSecurity).ExecuteCommandAsync(cancellationToken);
        }

        return userSecurity;
    }

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    public async Task IncrementFailedLoginAttemptsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var userSecurity = await GetByUserIdAsync(userId, cancellationToken);
        if (userSecurity == null)
        {
            // 如果不存在，创建新的安全记录
            userSecurity = new SysUserSecurity
            {
                UserId = userId,
                FailedLoginAttempts = 1,
                LastFailedLoginTime = DateTimeOffset.Now,
                CreatedTime = DateTimeOffset.Now
            };
            await SaveAsync(userSecurity, cancellationToken);
        }
        else
        {
            userSecurity.FailedLoginAttempts++;
            userSecurity.LastFailedLoginTime = DateTimeOffset.Now;
            userSecurity.ModifiedTime = DateTimeOffset.Now;
            await SaveAsync(userSecurity, cancellationToken);
        }
    }

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    public async Task ResetFailedLoginAttemptsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var userSecurity = await GetByUserIdAsync(userId, cancellationToken);
        if (userSecurity != null)
        {
            userSecurity.FailedLoginAttempts = 0;
            userSecurity.LastFailedLoginTime = null;
            userSecurity.ModifiedTime = DateTimeOffset.Now;
            await SaveAsync(userSecurity, cancellationToken);
        }
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    public async Task LockUserAsync(long userId, DateTimeOffset lockoutEndTime, CancellationToken cancellationToken = default)
    {
        var userSecurity = await GetByUserIdAsync(userId, cancellationToken);
        if (userSecurity == null)
        {
            userSecurity = new SysUserSecurity
            {
                UserId = userId,
                IsLocked = true,
                LockoutTime = DateTimeOffset.Now,
                LockoutEndTime = lockoutEndTime,
                CreatedTime = DateTimeOffset.Now
            };
            await SaveAsync(userSecurity, cancellationToken);
        }
        else
        {
            userSecurity.IsLocked = true;
            userSecurity.LockoutTime = DateTimeOffset.Now;
            userSecurity.LockoutEndTime = lockoutEndTime;
            userSecurity.ModifiedTime = DateTimeOffset.Now;
            await SaveAsync(userSecurity, cancellationToken);
        }
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    public async Task UnlockUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var userSecurity = await GetByUserIdAsync(userId, cancellationToken);
        if (userSecurity != null)
        {
            userSecurity.IsLocked = false;
            userSecurity.LockoutTime = null;
            userSecurity.LockoutEndTime = null;
            userSecurity.FailedLoginAttempts = 0;
            userSecurity.LastFailedLoginTime = null;
            userSecurity.ModifiedTime = DateTimeOffset.Now;
            await SaveAsync(userSecurity, cancellationToken);
        }
    }
}
