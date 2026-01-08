#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuditLogRepository
// Guid:c728152c-d6e9-4396-addb-b479254bad82
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 审计日志仓储接口
/// </summary>
public interface IAuditLogRepository : IRepositoryBase<SysAuditLog, long>
{
    /// <summary>
    /// 根据用户ID获取审计日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审计类型获取审计日志
    /// </summary>
    /// <param name="auditType">审计类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByAuditTypeAsync(string auditType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据操作类型获取审计日志
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByOperationTypeAsync(OperationType operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据实体类型获取审计日志
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByEntityTypeAsync(string entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据实体ID获取审计日志
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByEntityIdAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据表名获取审计日志
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据部门ID获取审计日志
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByDepartmentIdAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据风险等级获取审计日志
    /// </summary>
    /// <param name="riskLevel">风险等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetByRiskLevelAsync(int riskLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取失败的审计日志
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<List<SysAuditLog>> GetFailedLogsAsync(CancellationToken cancellationToken = default);

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
