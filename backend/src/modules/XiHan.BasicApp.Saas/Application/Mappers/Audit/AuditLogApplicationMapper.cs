#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditLogApplicationMapper
// Guid:e1451dc3-0eac-4e70-9dfb-1d463ddc6438
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 审计日志应用层映射器
/// </summary>
public static class AuditLogApplicationMapper
{
    /// <summary>
    /// 映射审计日志列表项
    /// </summary>
    /// <param name="auditLog">审计日志实体</param>
    /// <returns>审计日志列表项 DTO</returns>
    public static AuditLogListItemDto ToListItemDto(SysAuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        return new AuditLogListItemDto
        {
            BasicId = auditLog.BasicId,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            SessionId = auditLog.SessionId,
            RequestId = auditLog.RequestId,
            TraceId = auditLog.TraceId,
            AuditType = auditLog.AuditType,
            OperationType = auditLog.OperationType,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            EntityName = auditLog.EntityName,
            TableName = auditLog.TableName,
            PrimaryKey = auditLog.PrimaryKey,
            PrimaryKeyValue = auditLog.PrimaryKeyValue,
            IsSuccess = auditLog.IsSuccess,
            RiskLevel = auditLog.RiskLevel,
            ExecutionTime = auditLog.ExecutionTime,
            AuditTime = auditLog.AuditTime,
            HasOperationContext = !string.IsNullOrWhiteSpace(auditLog.OperationIp),
            HasAuditText = !string.IsNullOrWhiteSpace(auditLog.Description),
            HasChangeSummary = !string.IsNullOrWhiteSpace(auditLog.ChangeDescription),
            HasBeforeSnapshot = !string.IsNullOrWhiteSpace(auditLog.BeforeData),
            HasAfterSnapshot = !string.IsNullOrWhiteSpace(auditLog.AfterData),
            HasFieldChanges = !string.IsNullOrWhiteSpace(auditLog.ChangedFields),
            HasException = !string.IsNullOrWhiteSpace(auditLog.ExceptionMessage) ||
                           !string.IsNullOrWhiteSpace(auditLog.ExceptionStackTrace),
            HasExtension = !string.IsNullOrWhiteSpace(auditLog.ExtendData),
            CreatedTime = auditLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射审计日志详情
    /// </summary>
    /// <param name="auditLog">审计日志实体</param>
    /// <returns>审计日志详情 DTO</returns>
    public static AuditLogDetailDto ToDetailDto(SysAuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        var item = ToListItemDto(auditLog);
        return new AuditLogDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            SessionId = item.SessionId,
            RequestId = item.RequestId,
            TraceId = item.TraceId,
            AuditType = item.AuditType,
            OperationType = item.OperationType,
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            EntityName = item.EntityName,
            TableName = item.TableName,
            PrimaryKey = item.PrimaryKey,
            PrimaryKeyValue = item.PrimaryKeyValue,
            IsSuccess = item.IsSuccess,
            RiskLevel = item.RiskLevel,
            ExecutionTime = item.ExecutionTime,
            AuditTime = item.AuditTime,
            HasOperationContext = item.HasOperationContext,
            HasAuditText = item.HasAuditText,
            HasChangeSummary = item.HasChangeSummary,
            HasBeforeSnapshot = item.HasBeforeSnapshot,
            HasAfterSnapshot = item.HasAfterSnapshot,
            HasFieldChanges = item.HasFieldChanges,
            HasException = item.HasException,
            HasExtension = item.HasExtension,
            CreatedTime = item.CreatedTime,
            CreatedId = auditLog.CreatedId,
            CreatedBy = auditLog.CreatedBy
        };
    }
}
