#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLogRepository
// Guid:b6b2c3d4-e5f6-7890-abcd-ef1234567895
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.AuditLogs;

/// <summary>
/// 系统审核日志仓储实现
/// </summary>
public class SysAuditLogRepository : SqlSugarRepositoryBase<SysAuditLog, long>, ISysAuditLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysAuditLogRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据审核ID获取审核日志列表
    /// </summary>
    public async Task<List<SysAuditLog>> GetByAuditIdAsync(long auditId)
    {
        var result = await GetListAsync(log => log.AuditId == auditId);
        return result.OrderBy(log => log.AuditTime).ToList();
    }

    /// <summary>
    /// 根据审核用户ID获取审核日志列表
    /// </summary>
    public async Task<List<SysAuditLog>> GetByAuditorIdAsync(long auditorId)
    {
        var result = await GetListAsync(log => log.AuditorId == auditorId);
        return result.OrderByDescending(log => log.AuditTime).ToList();
    }

    /// <summary>
    /// 根据审核结果获取审核日志列表
    /// </summary>
    public async Task<List<SysAuditLog>> GetByResultAsync(AuditResult auditResult)
    {
        var result = await GetListAsync(log => log.AuditResult == auditResult);
        return result.OrderByDescending(log => log.AuditTime).ToList();
    }

    /// <summary>
    /// 根据时间范围获取审核日志列表
    /// </summary>
    public async Task<List<SysAuditLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var result = await GetListAsync(log => log.AuditTime >= startTime && log.AuditTime <= endTime);
        return result.OrderByDescending(log => log.AuditTime).ToList();
    }
}
