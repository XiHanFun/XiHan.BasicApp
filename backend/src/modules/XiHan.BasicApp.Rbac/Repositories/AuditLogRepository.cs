#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditLogRepository
// Guid:c828152c-d6e9-4396-addb-b479254bad92
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
/// 审计日志仓储实现
/// </summary>
public class AuditLogRepository : SqlSugarRepositoryBase<SysAuditLog, long>, IAuditLogRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据用户ID获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据审计类型获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByAuditTypeAsync(string auditType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.AuditType == auditType)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据操作类型获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByOperationTypeAsync(OperationType operationType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.OperationType == operationType)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据实体类型获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByEntityTypeAsync(string entityType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.EntityType == entityType)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据实体ID获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByEntityIdAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据表名获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.TableName == tableName)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据部门ID获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByDepartmentIdAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.DepartmentId == departmentId)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据风险等级获取审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByRiskLevelAsync(int riskLevel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.RiskLevel >= riskLevel)
            .OrderBy(l => l.RiskLevel, OrderByType.Desc)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取失败的审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetFailedLogsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.IsSuccess == false)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的审计日志
    /// </summary>
    public async Task<List<SysAuditLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysAuditLog>()
            .Where(l => l.CreatedTime >= startTime && l.CreatedTime <= endTime)
            .OrderBy(l => l.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量插入审计日志
    /// </summary>
    public async Task<bool> BatchInsertAsync(List<SysAuditLog> logs, CancellationToken cancellationToken = default)
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

        return await _dbClient.Deleteable<SysAuditLog>()
            .Where(l => l.CreatedTime < beforeTime)
            .ExecuteCommandAsync(cancellationToken);
    }
}
