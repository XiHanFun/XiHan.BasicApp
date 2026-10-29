// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.Traces;

/// <summary>
/// 链路时间线。
/// </summary>
/// <remarks>
/// <c>GET /api/traces:timeline?dimension=&amp;value=…</c>（只读，ADR-0041）。按 trace-id、用户、登录名、会话或客户端地址聚合当前一侧的各类日志。
/// </remarks>
/// <param name="queries">链路查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.TracesRead, ReadOnly = true)]
public sealed class TraceTimeline(TraceQueries queries) : IOperation<TraceTimelineRequest, TraceTimelineResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "traces.timeline";

    /// <inheritdoc />
    public ValueTask<Result<TraceTimelineResponse>> HandleAsync(TraceTimelineRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.TimelineAsync(request, context);
    }
}
