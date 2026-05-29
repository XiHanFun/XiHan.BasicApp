#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogApplicationMapper
// Guid:3c14eb10-75db-49b3-9e7f-c5a3af7299a6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 操作日志应用层映射器
/// </summary>
public static class OperationLogApplicationMapper
{
    /// <summary>
    /// 映射操作日志列表项
    /// </summary>
    /// <param name="operationLog">操作日志实体</param>
    /// <returns>操作日志列表项 DTO</returns>
    public static OperationLogListItemDto ToListItemDto(SysOperationLog operationLog)
    {
        ArgumentNullException.ThrowIfNull(operationLog);

        return new OperationLogListItemDto
        {
            BasicId = operationLog.BasicId,
            UserId = operationLog.UserId,
            UserName = operationLog.UserName,
            SessionId = operationLog.UserSessionId,
            TraceId = operationLog.TraceId,
            OperationType = operationLog.OperationType,
            Module = operationLog.Module,
            Function = operationLog.Function,
            Title = operationLog.Title,
            Description = operationLog.Description,
            Method = operationLog.Method,
            RequestUrl = operationLog.RequestUrl,
            ExecutionTime = operationLog.ExecutionTime,
            OperationIp = operationLog.OperationIp,
            OperationLocation = operationLog.OperationLocation,
            Browser = operationLog.Browser,
            Os = operationLog.Os,
            UserAgent = operationLog.UserAgent,
            Result = operationLog.Result,
            ErrorMessage = operationLog.ErrorMessage,
            OperationTime = operationLog.OperationTime,
            CreatedTime = operationLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射操作日志详情
    /// </summary>
    /// <param name="operationLog">操作日志实体</param>
    /// <returns>操作日志详情 DTO</returns>
    public static OperationLogDetailDto ToDetailDto(SysOperationLog operationLog)
    {
        ArgumentNullException.ThrowIfNull(operationLog);

        var item = ToListItemDto(operationLog);
        return new OperationLogDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            SessionId = item.SessionId,
            TraceId = item.TraceId,
            OperationType = item.OperationType,
            Module = item.Module,
            Function = item.Function,
            Title = item.Title,
            Description = item.Description,
            Method = item.Method,
            RequestUrl = item.RequestUrl,
            ExecutionTime = item.ExecutionTime,
            OperationIp = item.OperationIp,
            OperationLocation = item.OperationLocation,
            Browser = item.Browser,
            Os = item.Os,
            UserAgent = item.UserAgent,
            Result = item.Result,
            ErrorMessage = item.ErrorMessage,
            OperationTime = item.OperationTime,
            CreatedTime = item.CreatedTime,
            CreatedId = operationLog.CreatedId,
            CreatedBy = operationLog.CreatedBy
        };
    }
}
