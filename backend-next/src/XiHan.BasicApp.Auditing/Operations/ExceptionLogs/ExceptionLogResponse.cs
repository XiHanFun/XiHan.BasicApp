// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;

namespace XiHan.BasicApp.Auditing.Operations.ExceptionLogs;

/// <summary>
/// 异常日志的列表项（不含堆栈）。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Protocol">调用协议。</param>
/// <param name="Method">请求方法。</param>
/// <param name="Path">请求路径。</param>
/// <param name="ExceptionType">异常类型。</param>
/// <param name="Message">异常消息。</param>
/// <param name="Source">异常源。</param>
/// <param name="Location">发生位置。</param>
/// <param name="MachineName">服务器主机名。</param>
/// <param name="EnvironmentName">运行环境。</param>
/// <param name="AppVersion">应用版本。</param>
public sealed record ExceptionLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string? OperationId,
    string? Protocol,
    string? Method,
    string? Path,
    string ExceptionType,
    string Message,
    string? Source,
    string? Location,
    string? MachineName,
    string? EnvironmentName,
    string? AppVersion)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static ExceptionLogResponse From(SysExceptionLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new ExceptionLogResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.OperationId,
            log.Protocol,
            log.Method,
            log.Path,
            log.ExceptionType,
            log.Message,
            log.Source,
            log.Location,
            log.MachineName,
            log.EnvironmentName,
            log.AppVersion);
    }
}

/// <summary>
/// 异常日志详情：列表项的字段加完整堆栈。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Protocol">调用协议。</param>
/// <param name="Method">请求方法。</param>
/// <param name="Path">请求路径。</param>
/// <param name="ExceptionType">异常类型。</param>
/// <param name="Message">异常消息。</param>
/// <param name="Source">异常源。</param>
/// <param name="Location">发生位置。</param>
/// <param name="MachineName">服务器主机名。</param>
/// <param name="EnvironmentName">运行环境。</param>
/// <param name="AppVersion">应用版本。</param>
/// <param name="StackTrace">异常堆栈（含内部异常）。</param>
public sealed record ExceptionLogDetailResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string? OperationId,
    string? Protocol,
    string? Method,
    string? Path,
    string ExceptionType,
    string Message,
    string? Source,
    string? Location,
    string? MachineName,
    string? EnvironmentName,
    string? AppVersion,
    string? StackTrace)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static ExceptionLogDetailResponse From(SysExceptionLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new ExceptionLogDetailResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.OperationId,
            log.Protocol,
            log.Method,
            log.Path,
            log.ExceptionType,
            log.Message,
            log.Source,
            log.Location,
            log.MachineName,
            log.EnvironmentName,
            log.AppVersion,
            log.StackTrace);
    }
}

/// <summary>
/// <c>exception-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetExceptionLogRequest(Guid Id);
