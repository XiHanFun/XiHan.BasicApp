#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysExceptionLogRepository
// Guid:e9f0a1b2-c3d4-5678-9012-345678e34567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 异常日志仓储实现
/// </summary>
public class SysExceptionLogRepository : SqlSugarRepositoryBase<SysExceptionLog, long>, ISysExceptionLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysExceptionLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 添加异常日志
    /// </summary>
    public async Task<SysExceptionLog> AddExceptionLogAsync(SysExceptionLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 批量添加异常日志
    /// </summary>
    public async Task AddExceptionLogsAsync(IEnumerable<SysExceptionLog> logs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(logs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取最近异常日志
    /// </summary>
    public async Task<List<SysExceptionLog>> GetRecentExceptionsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysExceptionLog>()
            .OrderByDescending(log => log.ExceptionTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取异常类型统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetExceptionTypeStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.GetClient().Queryable<SysExceptionLog>()
            .Where(log => log.ExceptionTime >= startTime && log.ExceptionTime <= endTime)
            .GroupBy(log => log.ExceptionType)
            .Select(g => new
            {
                ExceptionType = g.ExceptionType,
                Count = SqlFunc.AggregateCount(g.ExceptionType)
            })
            .ToListAsync(cancellationToken);

        return logs.ToDictionary(x => x.ExceptionType ?? "Unknown", x => x.Count);
    }
}
