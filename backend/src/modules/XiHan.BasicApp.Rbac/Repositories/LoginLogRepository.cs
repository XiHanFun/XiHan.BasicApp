#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogRepository
// Guid:c5d6e7f8-a9b0-4c5d-1e2f-4a5b6c7d8e9f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 登录日志仓储实现
/// </summary>
public class LoginLogRepository : SqlSugarRepositoryBase<SysLoginLog, long>, ILoginLogRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoginLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户ID获取登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysLoginLog>()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据登录状态获取登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetByLoginResultAsync(LoginResult loginResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysLoginLog>()
            .Where(l => l.Result == loginResult)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户最后一次成功登录记录
    /// </summary>
    public async Task<SysLoginLog?> GetLastSuccessLoginAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysLoginLog>()
            .Where(l => l.UserId == userId && l.Result == LoginResult.Success)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户连续登录失败次数
    /// </summary>
    public async Task<int> GetContinuousFailureCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 获取最后一次成功登录时间
        var lastSuccess = await GetLastSuccessLoginAsync(userId, cancellationToken);
        var lastSuccessTime = lastSuccess?.CreatedTime ?? DateTimeOffset.MinValue;

        // 统计最后一次成功登录之后的失败次数
        return await _dbClient.Queryable<SysLoginLog>()
            .Where(l => l.UserId == userId
                && l.Result == LoginResult.Failed
                && l.CreatedTime > lastSuccessTime)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定IP的登录记录
    /// </summary>
    public async Task<List<SysLoginLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysLoginLog>()
            .Where(l => l.LoginIp == ipAddress)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysLoginLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    public async Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Deleteable<SysLoginLog>()
            .Where(l => l.CreatedTime < beforeTime)
            .ExecuteCommandAsync(cancellationToken);
    }
}
