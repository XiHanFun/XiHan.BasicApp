#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewLogRepository
// Guid:b828152c-d6e9-4396-addb-b479254bad91
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
/// 审查日志仓储实现
/// </summary>
public class ReviewLogRepository : SqlSugarRepositoryBase<SysReviewLog, long>, IReviewLogRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据审查ID获取审查日志
    /// </summary>
    public async Task<List<SysReviewLog>> GetByReviewIdAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReviewLog>()
            .Where(l => l.ReviewId == reviewId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据审查人ID获取审查日志
    /// </summary>
    public async Task<List<SysReviewLog>> GetByReviewerIdAsync(long reviewerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReviewLog>()
            .Where(l => l.ReviewerId == reviewerId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据审查结果获取审查日志
    /// </summary>
    public async Task<List<SysReviewLog>> GetByReviewResultAsync(AuditResult reviewResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReviewLog>()
            .Where(l => l.ReviewResult == reviewResult)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据审查类型获取审查日志
    /// </summary>
    public async Task<List<SysReviewLog>> GetByReviewTypeAsync(string reviewType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReviewLog>()
            .Where(l => l.ReviewType == reviewType)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的审查日志
    /// </summary>
    public async Task<List<SysReviewLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysReviewLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量插入审查日志
    /// </summary>
    public async Task<bool> BatchInsertAsync(List<SysReviewLog> logs, CancellationToken cancellationToken = default)
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

        return await _dbClient.Deleteable<SysReviewLog>()
            .Where(l => l.CreatedTime < beforeTime)
            .ExecuteCommandAsync(cancellationToken);
    }
}
