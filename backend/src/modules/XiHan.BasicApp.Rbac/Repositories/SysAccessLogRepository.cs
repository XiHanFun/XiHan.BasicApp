#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAccessLogRepository
// Guid:c7d8e9f0-a1b2-3456-7890-123456c12345
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
/// 访问与安全日志仓储实现
/// </summary>
public class SysAccessLogRepository : SqlSugarRepositoryBase<SysAccessLog, long>, ISysAccessLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysAccessLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // ========== 访问日志 ==========

    /// <summary>
    /// 批量添加访问日志
    /// </summary>
    public async Task AddAccessLogsAsync(IEnumerable<SysAccessLog> logs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(logs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户访问日志
    /// </summary>
    public async Task<List<SysAccessLog>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysAccessLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    // ========== 登录日志 ==========

    /// <summary>
    /// 添加登录日志
    /// </summary>
    public async Task<SysLoginLog> AddLoginLogAsync(SysLoginLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 获取用户登录日志
    /// </summary>
    public async Task<List<SysLoginLog>> GetLoginLogsByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysLoginLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.LoginTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取最近登录日志
    /// </summary>
    public async Task<SysLoginLog?> GetLastLoginLogAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysLoginLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.LoginTime)
            .FirstAsync(cancellationToken);
    }

    // ========== API日志 ==========

    /// <summary>
    /// 添加API日志
    /// </summary>
    public async Task<SysApiLog> AddApiLogAsync(SysApiLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 批量添加API日志
    /// </summary>
    public async Task AddApiLogsAsync(IEnumerable<SysApiLog> logs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(logs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取API调用统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetApiCallStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.GetClient().Queryable<SysApiLog>()
            .Where(log => log.RequestTime >= startTime && log.RequestTime <= endTime)
            .GroupBy(log => log.ApiPath)
            .Select(g => new
            {
                ApiPath = g.ApiPath,
                Count = SqlFunc.AggregateCount(g.ApiPath)
            })
            .ToListAsync(cancellationToken);

        return logs.ToDictionary(x => x.ApiPath, x => x.Count);
    }
}
