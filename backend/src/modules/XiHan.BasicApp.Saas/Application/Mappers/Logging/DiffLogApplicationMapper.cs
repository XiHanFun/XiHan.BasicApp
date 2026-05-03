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
            IsSuccess = diffLog.IsSuccess,
            RiskLevel = diffLog.RiskLevel,
            ExecutionTime = diffLog.ExecutionTime,
            AuditTime = diffLog.AuditTime,
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
            IsSuccess = item.IsSuccess,
            RiskLevel = item.RiskLevel,
            ExecutionTime = item.ExecutionTime,
            AuditTime = item.AuditTime,
            CreatedTime = item.CreatedTime,
            CreatedId = diffLog.CreatedId,
            CreatedBy = diffLog.CreatedBy
        };
    }
}
