#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ApiLogRepository
// Guid:d6e7f8a9-b0c1-4d5e-2f3a-5b6c7d8e9f0a
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
/// API日志仓储实现
/// </summary>
public class ApiLogRepository : SqlSugarRepositoryBase<SysApiLog, long>, IApiLogRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户ID获取API日志
    /// </summary>
    public async Task<List<SysApiLog>> GetByUserIdAsync(long? userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysApiLog>()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据API路径获取日志
    /// </summary>
    public async Task<List<SysApiLog>> GetByApiPathAsync(string apiPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysApiLog>()
            .Where(l => l.ApiPath == apiPath)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据HTTP状态码获取日志
    /// </summary>
    public async Task<List<SysApiLog>> GetByStatusCodeAsync(int statusCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysApiLog>()
            .Where(l => l.StatusCode == statusCode)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的API日志
    /// </summary>
    public async Task<List<SysApiLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysApiLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取慢查询日志（执行时间超过指定毫秒数）
    /// </summary>
    public async Task<List<SysApiLog>> GetSlowLogsAsync(long minExecutionTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysApiLog>()
            .Where(l => l.ExecutionTime >= minExecutionTime)
            .OrderBy(l => l.ExecutionTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量插入API日志
    /// </summary>
    public async Task<bool> BatchInsertAsync(List<SysApiLog> logs, CancellationToken cancellationToken = default)
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

        return await _dbClient.Deleteable<SysApiLog>()
            .Where(l => l.CreatedTime < beforeTime)
            .ExecuteCommandAsync(cancellationToken);
    }
}
