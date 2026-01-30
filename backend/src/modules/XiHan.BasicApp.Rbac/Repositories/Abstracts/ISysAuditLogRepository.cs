#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysAuditLogRepository
// Guid:d0e1f2a3-b4c5-6789-0123-456789d45678
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 审计日志仓储接口
/// </summary>
/// <remarks>
/// 覆盖实体：SysAuditLog + SysOperationLog
/// </remarks>
public interface ISysAuditLogRepository : IRepositoryBase<SysAuditLog, long>
{
    // ========== 审计日志 ==========

    /// <summary>
    /// 添加审计日志
    /// </summary>
    Task<SysAuditLog> AddAuditLogAsync(SysAuditLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加审计日志
    /// </summary>
    Task AddAuditLogsAsync(IEnumerable<SysAuditLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取实体审计历史
    /// </summary>
    Task<List<SysAuditLog>> GetEntityAuditHistoryAsync(string entityName, long entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户审计日志
    /// </summary>
    Task<List<SysAuditLog>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    // ========== 操作日志 ==========

    /// <summary>
    /// 添加操作日志
    /// </summary>
    Task<SysOperationLog> AddOperationLogAsync(SysOperationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加操作日志
    /// </summary>
    Task AddOperationLogsAsync(IEnumerable<SysOperationLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    Task<List<SysOperationLog>> GetOperationLogsByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取操作统计
    /// </summary>
    Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
