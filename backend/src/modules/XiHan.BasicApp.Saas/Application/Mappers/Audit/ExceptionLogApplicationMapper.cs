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
            ExceptionSource = exceptionLog.ExceptionSource,
            ExceptionLocation = exceptionLog.ExceptionLocation,
            SeverityLevel = exceptionLog.SeverityLevel,
            RequestPath = exceptionLog.RequestPath,
            RequestMethod = exceptionLog.RequestMethod,
            StatusCode = exceptionLog.StatusCode,
            DeviceType = exceptionLog.DeviceType,
            ApplicationName = exceptionLog.ApplicationName,
            ApplicationVersion = exceptionLog.ApplicationVersion,
            EnvironmentName = exceptionLog.EnvironmentName,
            ExceptionTime = exceptionLog.ExceptionTime,
            IsHandled = exceptionLog.IsHandled,
            HandledTime = exceptionLog.HandledTime,
            HandledBy = exceptionLog.HandledBy,
            ErrorCode = exceptionLog.ErrorCode,
            HasExceptionText = !string.IsNullOrWhiteSpace(exceptionLog.ExceptionMessage),
            HasStack = !string.IsNullOrWhiteSpace(exceptionLog.ExceptionStackTrace),
            HasRequestPayload = HasRequestPayload(exceptionLog),
            HasHeaders = !string.IsNullOrWhiteSpace(exceptionLog.RequestHeaders),
            HasOperationContext = HasOperationContext(exceptionLog),
            HasDeviceContext = HasDeviceContext(exceptionLog),
            HasRuntimeContext = HasRuntimeContext(exceptionLog),
            HasHandlingNote = !string.IsNullOrWhiteSpace(exceptionLog.HandledRemark),
            HasExtension = !string.IsNullOrWhiteSpace(exceptionLog.ExtendData),
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
            ExceptionSource = item.ExceptionSource,
            ExceptionLocation = item.ExceptionLocation,
            SeverityLevel = item.SeverityLevel,
            RequestPath = item.RequestPath,
            RequestMethod = item.RequestMethod,
            StatusCode = item.StatusCode,
            DeviceType = item.DeviceType,
            ApplicationName = item.ApplicationName,
            ApplicationVersion = item.ApplicationVersion,
            EnvironmentName = item.EnvironmentName,
            ExceptionTime = item.ExceptionTime,
            IsHandled = item.IsHandled,
            HandledTime = item.HandledTime,
            HandledBy = item.HandledBy,
            ErrorCode = item.ErrorCode,
            HasExceptionText = item.HasExceptionText,
            HasStack = item.HasStack,
            HasRequestPayload = item.HasRequestPayload,
            HasHeaders = item.HasHeaders,
            HasOperationContext = item.HasOperationContext,
            HasDeviceContext = item.HasDeviceContext,
            HasRuntimeContext = item.HasRuntimeContext,
            HasHandlingNote = item.HasHandlingNote,
            HasExtension = item.HasExtension,
            CreatedTime = item.CreatedTime,
            CreatedId = exceptionLog.CreatedId,
            CreatedBy = exceptionLog.CreatedBy
        };
    }

    /// <summary>
    /// 判断是否存在请求载荷
    /// </summary>
    private static bool HasRequestPayload(SysExceptionLog exceptionLog)
    {
        return !string.IsNullOrWhiteSpace(exceptionLog.RequestParams) ||
               !string.IsNullOrWhiteSpace(exceptionLog.RequestBody);
    }

    /// <summary>
    /// 判断是否存在操作上下文
    /// </summary>
    private static bool HasOperationContext(SysExceptionLog exceptionLog)
    {
        return !string.IsNullOrWhiteSpace(exceptionLog.OperationIp) ||
               !string.IsNullOrWhiteSpace(exceptionLog.OperationLocation) ||
               !string.IsNullOrWhiteSpace(exceptionLog.UserAgent) ||
               !string.IsNullOrWhiteSpace(exceptionLog.Browser) ||
               !string.IsNullOrWhiteSpace(exceptionLog.Os);
    }

    /// <summary>
    /// 判断是否存在设备上下文
    /// </summary>
    private static bool HasDeviceContext(SysExceptionLog exceptionLog)
    {
        return exceptionLog.DeviceType != DeviceType.Unknown ||
               !string.IsNullOrWhiteSpace(exceptionLog.DeviceInfo);
    }

    /// <summary>
    /// 判断是否存在运行时上下文
    /// </summary>
    private static bool HasRuntimeContext(SysExceptionLog exceptionLog)
    {
        return !string.IsNullOrWhiteSpace(exceptionLog.ServerHostName) ||
               exceptionLog.ThreadId > 0 ||
               exceptionLog.ProcessId > 0;
    }
}
