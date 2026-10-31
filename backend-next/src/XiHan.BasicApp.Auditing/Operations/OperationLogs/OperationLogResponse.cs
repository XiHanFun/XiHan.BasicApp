// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing.Operations.OperationLogs;

/// <summary>
/// 操作日志的列表项。
/// </summary>
/// <remarks>
/// 框架操作审计的一条记录（<c>AuditRecord</c>），加上本包中间件在写入时补充的登录名、会话、耗时与客户端信息。
/// </remarks>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="OperationId">操作 ID（审计动作）。</param>
/// <param name="Description">操作描述（操作的本地化标题，按调用方语言）。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息（有消息键的按调用方语言渲染）。</param>
/// <param name="Resource">作用的资源名（<c>xrn:</c>）。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="Device">设备类型。</param>
/// <param name="Location">归属地。</param>
public sealed record OperationLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string OperationId,
    string? Description,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    string? Resource,
    long? DurationMs,
    string? ClientIp,
    string? Browser,
    string? Os,
    string? Device,
    string? Location);

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
/// <param name="Description">操作描述。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="Resource">作用的资源名。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="Device">设备类型。</param>
/// <param name="Location">归属地。</param>
/// <param name="UserAgent">User-Agent 原文。</param>
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
    string? Description,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    string? Resource,
    long? DurationMs,
    string? ClientIp,
    string? Browser,
    string? Os,
    string? Device,
    string? Location,
    string? UserAgent,
    long Sequence,
    string? Details);

/// <summary>
/// <c>operation-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetOperationLogRequest(Guid Id);
