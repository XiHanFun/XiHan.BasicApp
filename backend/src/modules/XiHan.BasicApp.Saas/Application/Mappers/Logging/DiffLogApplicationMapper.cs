#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DiffLogApplicationMapper
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
/// 差异日志应用层映射器
/// </summary>
public static class DiffLogApplicationMapper
{
    /// <summary>
    /// 映射差异日志列表项
    /// </summary>
    /// <param name="diffLog">差异日志实体</param>
    /// <returns>差异日志列表项 DTO</returns>
    public static DiffLogListItemDto ToListItemDto(SysDiffLog diffLog)
    {
        ArgumentNullException.ThrowIfNull(diffLog);

        return new DiffLogListItemDto
        {
            BasicId = diffLog.BasicId,
            UserId = diffLog.UserId,
            UserName = diffLog.UserName,
            SessionId = diffLog.SessionId,
            RequestId = diffLog.RequestId,
            TraceId = diffLog.TraceId,
            AuditType = diffLog.AuditType,
            OperationType = diffLog.OperationType,
            EntityType = diffLog.EntityType,
            EntityId = diffLog.EntityId,
            EntityName = diffLog.EntityName,
            TableName = diffLog.TableName,
            PrimaryKey = diffLog.PrimaryKey,
            PrimaryKeyValue = diffLog.PrimaryKeyValue,
            Description = diffLog.Description,
            ChangeDescription = diffLog.ChangeDescription,
            OperationIp = diffLog.OperationIp,
            IsSuccess = diffLog.IsSuccess,
            ExceptionMessage = diffLog.ExceptionMessage,
            RiskLevel = diffLog.RiskLevel,
            ExecutionTime = diffLog.ExecutionTime,
            AuditTime = diffLog.AuditTime,
            BeforeData = diffLog.BeforeData,
            AfterData = diffLog.AfterData,
            ChangedFields = diffLog.ChangedFields,
            ExceptionStackTrace = diffLog.ExceptionStackTrace,
            ExtendData = diffLog.ExtendData,
            Remark = diffLog.Remark,
            CreatedTime = diffLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射差异日志详情
    /// </summary>
    /// <param name="diffLog">差异日志实体</param>
    /// <returns>差异日志详情 DTO</returns>
    public static DiffLogDetailDto ToDetailDto(SysDiffLog diffLog)
    {
        ArgumentNullException.ThrowIfNull(diffLog);

        var item = ToListItemDto(diffLog);
        return new DiffLogDetailDto
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
            Description = item.Description,
            ChangeDescription = item.ChangeDescription,
            OperationIp = item.OperationIp,
            IsSuccess = item.IsSuccess,
            ExceptionMessage = item.ExceptionMessage,
            RiskLevel = item.RiskLevel,
            ExecutionTime = item.ExecutionTime,
            AuditTime = item.AuditTime,
            BeforeData = item.BeforeData,
            AfterData = item.AfterData,
            ChangedFields = item.ChangedFields,
            ExceptionStackTrace = item.ExceptionStackTrace,
            ExtendData = item.ExtendData,
            Remark = item.Remark,
            CreatedTime = item.CreatedTime,
            CreatedId = diffLog.CreatedId,
            CreatedBy = diffLog.CreatedBy
        };
    }
}
