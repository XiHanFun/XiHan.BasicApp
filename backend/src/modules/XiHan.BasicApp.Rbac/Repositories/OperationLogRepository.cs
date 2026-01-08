#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogRepository
// Guid:b4c5d6e7-f8a9-4b5c-0d1e-3f4a5b6c7d8e
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
/// 操作日志仓储实现
/// </summary>
public class OperationLogRepository : SqlSugarRepositoryBase<SysOperationLog, long>, IOperationLogRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户ID获取操作日志
    /// </summary>
    public async Task<List<SysOperationLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOperationLog>()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据操作类型获取操作日志
    /// </summary>
    public async Task<List<SysOperationLog>> GetByOperationTypeAsync(OperationType operationType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOperationLog>()
            .Where(l => l.OperationType == operationType)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的操作日志
    /// </summary>
    public async Task<List<SysOperationLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOperationLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量插入操作日志
    /// </summary>
    public async Task<bool> BatchInsertAsync(List<SysOperationLog> logs, CancellationToken cancellationToken = default)
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

        return await _dbClient.Deleteable<SysOperationLog>()
            .Where(l => l.CreatedTime < beforeTime)
            .ExecuteCommandAsync(cancellationToken);
    }
}
