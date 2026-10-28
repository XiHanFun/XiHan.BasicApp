// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing.Operations.OperationLogs;

/// <summary>
/// 操作日志的列表项：框架操作审计的一条记录（<c>AuditRecord</c>），加上本包中间件补充的登录名、会话与耗时。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="OperationId">操作 ID（审计动作）。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息（有消息键的按调用方语言渲染）。</param>
/// <param name="Resource">作用的资源名（<c>xrn:</c>）。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
public sealed record OperationLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string OperationId,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    string? Resource,
    long? DurationMs);

/// <summary>
/// 操作日志详情：列表项的字段加原始细节。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="OperationId">操作 ID（审计动作）。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="Resource">作用的资源名。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="Sequence">写入序号。</param>
/// <param name="Details">细节（JSON 对象文本）。</param>
public sealed record OperationLogDetailResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string OperationId,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    string? Resource,
    long? DurationMs,
    long Sequence,
    string? Details);

/// <summary>
/// <c>operation-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetOperationLogRequest(Guid Id);
