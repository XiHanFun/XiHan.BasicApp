#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLogRepository
// Guid:b3b2c3d4-e5f6-7890-abcd-ef1234567892
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.LoginLogs;

/// <summary>
/// 系统登录日志仓储实现
/// </summary>
public class SysLoginLogRepository : SqlSugarRepositoryBase<SysLoginLog, long>, ISysLoginLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysLoginLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取登录日志列表
    /// </summary>
    public async Task<List<SysLoginLog>> GetByUserIdAsync(long userId)
    {
        var result = await GetListAsync(log => log.UserId == userId);
        return [.. result.OrderByDescending(log => log.LoginTime)];
    }

    /// <summary>
    /// 根据用户名获取登录日志列表
    /// </summary>
    public async Task<List<SysLoginLog>> GetByUserNameAsync(string userName)
    {
        var result = await GetListAsync(log => log.UserName == userName);
        return [.. result.OrderByDescending(log => log.LoginTime)];
    }

    /// <summary>
    /// 根据时间范围获取登录日志列表
    /// </summary>
    public async Task<List<SysLoginLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var result = await GetListAsync(log => log.LoginTime >= startTime && log.LoginTime <= endTime);
        return [.. result.OrderByDescending(log => log.LoginTime)];
    }

    /// <summary>
    /// 获取最近的登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetRecentLoginLogsAsync(long userId, int count = 10)
    {
        return await _dbContext.GetClient()
            .Queryable<SysLoginLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.LoginTime)
            .Take(count)
            .ToListAsync();
    }
}
