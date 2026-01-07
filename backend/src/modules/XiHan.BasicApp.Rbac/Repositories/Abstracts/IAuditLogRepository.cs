#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuditLogRepository
// Guid:a9b0c1d2-e3f4-4a5b-5c6d-8e9f0a1b2c3d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 审计日志仓储接口
/// </summary>
public interface IAuditLogRepository : IRepositoryBase<SysAuditLog, long>
{
    /// <summary>
    /// 根据审计ID获取审计日志
    /// </summary>
    /// <param name="auditId">审计ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByAuditIdAsync(long auditId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据实体类型和实体ID获取审计日志
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据操作用户获取审计日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据操作类型获取审计日志
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByOperationTypeAsync(string operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的审计日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入审计日志
    /// </summary>
    /// <param name="logs">审计日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchInsertAsync(List<SysAuditLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理指定时间之前的日志
    /// </summary>
    /// <param name="beforeTime">截止时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanLogsBeforeAsync(DateTimeOffset beforeTime, CancellationToken cancellationToken = default);
}
