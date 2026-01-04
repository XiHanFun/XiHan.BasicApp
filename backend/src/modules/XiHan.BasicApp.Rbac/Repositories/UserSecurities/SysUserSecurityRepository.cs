#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurityRepository
// Guid:bd2b3c4d-5e6f-7890-abcd-ef12345678a0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.UserSecurities;

/// <summary>
/// 系统用户安全仓储实现
/// </summary>
public class SysUserSecurityRepository : SqlSugarRepositoryBase<SysUserSecurity, long>, ISysUserSecurityRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysUserSecurityRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    public async Task<SysUserSecurity?> GetByUserIdAsync(long userId)
    {
        return await GetFirstAsync(us => us.UserId == userId);
    }

    /// <summary>
    /// 获取锁定的用户列表
    /// </summary>
    public async Task<List<SysUserSecurity>> GetLockedUsersAsync()
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserSecurity>()
            .Where(us => us.IsLocked)
            .ToListAsync();
    }

    /// <summary>
    /// 获取密码过期的用户列表
    /// </summary>
    public async Task<List<SysUserSecurity>> GetPasswordExpiredUsersAsync()
    {
        var now = DateTimeOffset.Now;
        return await _dbContext.GetClient()
            .Queryable<SysUserSecurity>()
            .Where(us => us.PasswordExpiryTime != null && us.PasswordExpiryTime < now)
            .ToListAsync();
    }

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    public async Task<bool> IncrementFailedLoginAttemptsAsync(long userId)
    {
        var userSecurity = await GetByUserIdAsync(userId);
        if (userSecurity == null) return false;

        userSecurity.FailedLoginAttempts++;
        userSecurity.LastFailedLoginTime = DateTimeOffset.Now;
        await UpdateAsync(userSecurity);
        return true;
    }

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    public async Task<bool> ResetFailedLoginAttemptsAsync(long userId)
    {
        var userSecurity = await GetByUserIdAsync(userId);
        if (userSecurity == null) return false;

        userSecurity.FailedLoginAttempts = 0;
        userSecurity.LastFailedLoginTime = null;
        await UpdateAsync(userSecurity);
        return true;
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    public async Task<bool> LockUserAsync(long userId, DateTimeOffset? lockoutEndTime = null)
    {
        var userSecurity = await GetByUserIdAsync(userId);
        if (userSecurity == null) return false;

        userSecurity.IsLocked = true;
        userSecurity.LockoutTime = DateTimeOffset.Now;
        userSecurity.LockoutEndTime = lockoutEndTime;
        await UpdateAsync(userSecurity);
        return true;
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    public async Task<bool> UnlockUserAsync(long userId)
    {
        var userSecurity = await GetByUserIdAsync(userId);
        if (userSecurity == null) return false;

        userSecurity.IsLocked = false;
        userSecurity.LockoutTime = null;
        userSecurity.LockoutEndTime = null;
        userSecurity.FailedLoginAttempts = 0;
        await UpdateAsync(userSecurity);
        return true;
    }

    /// <summary>
    /// 更新安全戳
    /// </summary>
    public async Task<bool> UpdateSecurityStampAsync(long userId)
    {
        var userSecurity = await GetByUserIdAsync(userId);
        if (userSecurity == null) return false;

        userSecurity.SecurityStamp = Guid.NewGuid().ToString("N");
        await UpdateAsync(userSecurity);
        return true;
    }
}

