#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogRepository
// Guid:e7f8a9b0-c1d2-4e5f-3a4b-6c7d8e9f0a1b
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

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 访问日志仓储实现
/// </summary>
public class AccessLogRepository : SqlSugarRepositoryBase<SysAccessLog, long>, IAccessLogRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户ID获取访问日志
    /// </summary>
    public async Task<List<SysAccessLog>> GetByUserIdAsync(long? userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAccessLog>()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据访问路径获取日志
    /// </summary>
    public async Task<List<SysAccessLog>> GetByResourcePathAsync(string resourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAccessLog>()
            .Where(l => l.ResourcePath == resourcePath)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据IP地址获取访问日志
    /// </summary>
    public async Task<List<SysAccessLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAccessLog>()
            .Where(l => l.AccessIp == ipAddress)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的访问日志
    /// </summary>
    public async Task<List<SysAccessLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAccessLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 统计访问量
    /// </summary>
    public async Task<int> CountAccessAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAccessLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 批量插入访问日志
    /// </summary>
    public async Task<bool> BatchInsertAsync(List<SysAccessLog> logs, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (logs == null || logs.Count == 0)
        {
            return false;
        }

        var affectedRows = await _dbClient.Insertable(logs)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    public async Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Deleteable<SysAccessLog>()
            .Where(l => l.CreatedTime < beforeTime)
            .ExecuteCommandAsync(cancellationToken);
    }
}
