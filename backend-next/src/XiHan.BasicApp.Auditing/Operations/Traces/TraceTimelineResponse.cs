// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing.Operations.Traces;

/// <summary>
/// 链路时间线：各类日志按发生时间正序合并。
/// </summary>
/// <param name="Items">时间线上的条目。</param>
/// <param name="Counts">各类日志取到的条数（截断前）。</param>
/// <param name="Truncated">是否有一类日志超出了每类上限而被截断。</param>
/// <param name="Total">条目总数。</param>
public sealed record TraceTimelineResponse(IReadOnlyList<TraceTimelineItem> Items, IReadOnlyList<TraceTypeCount> Counts, bool Truncated, int Total);

/// <summary>
/// 一类日志的条数。
/// </summary>
/// <param name="LogType">日志类型。</param>
/// <param name="Count">条数。</param>
public sealed record TraceTypeCount(TraceLogType LogType, int Count);

/// <summary>
/// 时间线上的一条。
/// </summary>
/// <remarks>
/// 各类日志投影到同一组字段，详情按 <see cref="LogType"/> 与 <see cref="Id"/> 去对应的 <c>get</c> 操作取。
/// </remarks>
/// <param name="LogType">日志类型。</param>
/// <param name="Id">日志主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="Title">标题（操作 ID、事件、异常类型、实体类型等）。</param>
/// <param name="Summary">摘要（错误码、路径、消息等）。</param>
/// <param name="Status">状态。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="Method">请求方法。</param>
/// <param name="Path">请求路径。</param>
/// <param name="StatusCode">HTTP 状态码。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
public sealed record TraceTimelineItem(
    TraceLogType LogType,
    Guid Id,
    DateTimeOffset OccurredAt,
    string Title,
    string? Summary,
    TraceItemStatus Status,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? Method,
    string? Path,
    int? StatusCode,
    long? DurationMs);

/// <summary>
/// 时间线条目的状态。
/// </summary>
public enum TraceItemStatus
{
    /// <summary>
    /// 成功。
    /// </summary>
    Success = 0,

    /// <summary>
    /// 中性的记录（数据变更、权限变更、只记录的命中）。
    /// </summary>
    Info = 1,

    /// <summary>
    /// 需要留意（越权、登录失败、被拦下的命中）。
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 失败（服务端故障、异常、操作失败）。
    /// </summary>
    Failure = 3,
}
