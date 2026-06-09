#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExceptionLogApplicationMapper
// Guid:3dd15334-b701-442c-9f84-67447c7a7b5e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 异常日志应用层映射器
/// </summary>
public static class ExceptionLogApplicationMapper
{
    /// <summary>
    /// 映射异常日志列表项
    /// </summary>
    /// <param name="exceptionLog">异常日志实体</param>
    /// <returns>异常日志列表项 DTO</returns>
    public static ExceptionLogListItemDto ToListItemDto(SysExceptionLog exceptionLog)
    {
        ArgumentNullException.ThrowIfNull(exceptionLog);

        return new ExceptionLogListItemDto
        {
            BasicId = exceptionLog.BasicId,
            UserId = exceptionLog.UserId,
            UserName = exceptionLog.UserName,
            SessionId = exceptionLog.SessionId,
            RequestId = exceptionLog.RequestId,
            TraceId = exceptionLog.TraceId,
            ExceptionType = exceptionLog.ExceptionType,
            ExceptionMessage = exceptionLog.ExceptionMessage,
            ExceptionSource = exceptionLog.ExceptionSource,
            ExceptionLocation = exceptionLog.ExceptionLocation,
            SeverityLevel = exceptionLog.SeverityLevel,
            RequestPath = exceptionLog.RequestPath,
            RequestMethod = exceptionLog.RequestMethod,
            ControllerName = exceptionLog.ControllerName,
            ActionName = exceptionLog.ActionName,
            StatusCode = exceptionLog.StatusCode,
            OperationIp = exceptionLog.OperationIp,
            OperationLocation = exceptionLog.OperationLocation,
            UserAgent = exceptionLog.UserAgent,
            Browser = exceptionLog.Browser,
            Os = exceptionLog.Os,
            DeviceType = exceptionLog.DeviceType,
            DeviceInfo = exceptionLog.DeviceInfo,
            ApplicationName = exceptionLog.ApplicationName,
            ApplicationVersion = exceptionLog.ApplicationVersion,
            EnvironmentName = exceptionLog.EnvironmentName,
            ServerHostName = exceptionLog.ServerHostName,
            ThreadId = exceptionLog.ThreadId,
            ProcessId = exceptionLog.ProcessId,
            ExceptionTime = exceptionLog.ExceptionTime,
            IsHandled = exceptionLog.IsHandled,
            HandledTime = exceptionLog.HandledTime,
            HandledBy = exceptionLog.HandledBy,
            ErrorCode = exceptionLog.ErrorCode,
            HandledRemark = exceptionLog.HandledRemark,
            ExceptionStackTrace = exceptionLog.ExceptionStackTrace,
            RequestParams = exceptionLog.RequestParams,
            RequestBody = exceptionLog.RequestBody,
            RequestHeaders = exceptionLog.RequestHeaders,
            ExtendData = exceptionLog.ExtendData,
            Remark = exceptionLog.Remark,
            CreatedTime = exceptionLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射异常日志详情
    /// </summary>
    /// <param name="exceptionLog">异常日志实体</param>
    /// <returns>异常日志详情 DTO</returns>
    public static ExceptionLogDetailDto ToDetailDto(SysExceptionLog exceptionLog)
    {
        ArgumentNullException.ThrowIfNull(exceptionLog);

        var item = ToListItemDto(exceptionLog);
        return new ExceptionLogDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            SessionId = item.SessionId,
            RequestId = item.RequestId,
            TraceId = item.TraceId,
            ExceptionType = item.ExceptionType,
            ExceptionMessage = item.ExceptionMessage,
            ExceptionSource = item.ExceptionSource,
            ExceptionLocation = item.ExceptionLocation,
            SeverityLevel = item.SeverityLevel,
            RequestPath = item.RequestPath,
            RequestMethod = item.RequestMethod,
            ControllerName = item.ControllerName,
            ActionName = item.ActionName,
            StatusCode = item.StatusCode,
            OperationIp = item.OperationIp,
            OperationLocation = item.OperationLocation,
            UserAgent = item.UserAgent,
            Browser = item.Browser,
            Os = item.Os,
            DeviceType = item.DeviceType,
            DeviceInfo = item.DeviceInfo,
            ApplicationName = item.ApplicationName,
            ApplicationVersion = item.ApplicationVersion,
            EnvironmentName = item.EnvironmentName,
            ServerHostName = item.ServerHostName,
            ThreadId = item.ThreadId,
            ProcessId = item.ProcessId,
            ExceptionTime = item.ExceptionTime,
            IsHandled = item.IsHandled,
            HandledTime = item.HandledTime,
            HandledBy = item.HandledBy,
            ErrorCode = item.ErrorCode,
            HandledRemark = item.HandledRemark,
            ExceptionStackTrace = item.ExceptionStackTrace,
            RequestParams = item.RequestParams,
            RequestBody = item.RequestBody,
            RequestHeaders = item.RequestHeaders,
            ExtendData = item.ExtendData,
            Remark = item.Remark,
            CreatedTime = item.CreatedTime,
            CreatedId = exceptionLog.CreatedId,
            CreatedBy = exceptionLog.CreatedBy
        };
    }
}
