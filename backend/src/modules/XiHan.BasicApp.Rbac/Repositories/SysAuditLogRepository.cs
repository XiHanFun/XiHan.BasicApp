#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLogRepository
// Guid:d8e9f0a1-b2c3-4567-8901-234567d23456
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
/// 审计日志仓储实现
/// </summary>
public class SysAuditLogRepository : SqlSugarRepositoryBase<SysAuditLog, long>, ISysAuditLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysAuditLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // ========== 审计日志 ==========

    /// <summary>
    /// 添加审计日志
    /// </summary>
    public async Task<SysAuditLog> AddAuditLogAsync(SysAuditLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 批量添加审计日志
    /// </summary>
    public async Task AddAuditLogsAsync(IEnumerable<SysAuditLog> logs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(logs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取实体审计历史
    /// </summary>
    public async Task<List<SysAuditLog>> GetEntityAuditHistoryAsync(string entityName, long entityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysAuditLog>()
            .Where(log => log.EntityName == entityName && log.EntityId == entityId.ToString())
            .OrderByDescending(log => log.CreatedTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysAuditLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    // ========== 操作日志 ==========

    /// <summary>
    /// 添加操作日志
    /// </summary>
    public async Task<SysOperationLog> AddOperationLogAsync(SysOperationLog log, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(log).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 批量添加操作日志
    /// </summary>
    public async Task AddOperationLogsAsync(IEnumerable<SysOperationLog> logs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(logs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    public async Task<List<SysOperationLog>> GetOperationLogsByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysOperationLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.OperationTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取操作类型统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.GetClient().Queryable<SysOperationLog>()
            .Where(log => log.OperationTime >= startTime && log.OperationTime <= endTime)
            .GroupBy(log => log.OperationType)
            .Select(g => new
            {
                OperationType = g.OperationType.ToString(),
                Count = SqlFunc.AggregateCount(g.OperationType)
            })
            .ToListAsync(cancellationToken);

        return logs.ToDictionary(x => x.OperationType, x => x.Count);
    }
}
