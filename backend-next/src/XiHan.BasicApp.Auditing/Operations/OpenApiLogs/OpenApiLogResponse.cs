// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;

namespace XiHan.BasicApp.Auditing.Operations.OpenApiLogs;

/// <summary>
/// 开放接口日志的列表项（不含正文摘要）。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="ClientId">接入应用的客户端标识。</param>
/// <param name="AppName">接入应用名称。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Method">请求方法。</param>
/// <param name="Path">请求路径。</param>
/// <param name="SignatureAlgorithm">签名算法。</param>
/// <param name="SignatureValid">签名是否有效。</param>
/// <param name="StatusCode">HTTP 状态码。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="RequestSize">请求大小（字节）。</param>
/// <param name="ResponseSize">响应大小（字节）。</param>
public sealed record OpenApiLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string? ClientId,
    string? AppName,
    string? OperationId,
    string Method,
    string Path,
    string? SignatureAlgorithm,
    bool SignatureValid,
    int StatusCode,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    long DurationMs,
    long RequestSize,
    long ResponseSize)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static OpenApiLogResponse From(SysOpenApiLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new OpenApiLogResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.ClientId,
            log.AppName,
            log.OperationId,
            log.Method,
            log.Path,
            log.SignatureAlgorithm,
            log.SignatureValid,
            log.StatusCode,
            log.Succeeded,
            log.ErrorCode,
            log.ErrorMessage,
            log.DurationMs,
            log.RequestSize,
            log.ResponseSize);
    }
}

/// <summary>
/// 开放接口日志详情：列表项的字段加正文摘要。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="ClientId">接入应用的客户端标识。</param>
/// <param name="AppName">接入应用名称。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Method">请求方法。</param>
/// <param name="Path">请求路径。</param>
/// <param name="SignatureAlgorithm">签名算法。</param>
/// <param name="SignatureValid">签名是否有效。</param>
/// <param name="StatusCode">HTTP 状态码。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="RequestSize">请求大小（字节）。</param>
/// <param name="ResponseSize">响应大小（字节）。</param>
/// <param name="RequestBody">请求正文摘要。</param>
/// <param name="ResponseBody">响应正文摘要。</param>
public sealed record OpenApiLogDetailResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string? ClientId,
    string? AppName,
    string? OperationId,
    string Method,
    string Path,
    string? SignatureAlgorithm,
    bool SignatureValid,
    int StatusCode,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    long DurationMs,
    long RequestSize,
    long ResponseSize,
    string? RequestBody,
    string? ResponseBody)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static OpenApiLogDetailResponse From(SysOpenApiLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new OpenApiLogDetailResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.ClientId,
            log.AppName,
            log.OperationId,
            log.Method,
            log.Path,
            log.SignatureAlgorithm,
            log.SignatureValid,
            log.StatusCode,
            log.Succeeded,
            log.ErrorCode,
            log.ErrorMessage,
            log.DurationMs,
            log.RequestSize,
            log.ResponseSize,
            log.RequestBody,
            log.ResponseBody);
    }
}

/// <summary>
/// <c>open-api-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetOpenApiLogRequest(Guid Id);
