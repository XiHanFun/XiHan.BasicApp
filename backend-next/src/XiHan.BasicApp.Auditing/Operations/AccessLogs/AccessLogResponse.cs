// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;

namespace XiHan.BasicApp.Auditing.Operations.AccessLogs;

/// <summary>
/// 一条访问日志。
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
/// <param name="Result">访问结果。</param>
/// <param name="StatusCode">HTTP 状态码。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="Device">设备类型。</param>
/// <param name="Referer">请求来源。</param>
public sealed record AccessLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string OperationId,
    string? Protocol,
    string? Method,
    string? Path,
    AccessResult Result,
    int? StatusCode,
    string? ErrorCode,
    string? ErrorMessage,
    long DurationMs,
    string? Browser,
    string? Os,
    string? Device,
    string? Referer)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static AccessLogResponse From(SysAccessLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new AccessLogResponse(
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
            log.Result,
            log.StatusCode,
            log.ErrorCode,
            log.ErrorMessage,
            log.DurationMs,
            log.Browser,
            log.Os,
            log.Device,
            log.Referer);
    }
}

/// <summary>
/// <c>access-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetAccessLogRequest(Guid Id);
