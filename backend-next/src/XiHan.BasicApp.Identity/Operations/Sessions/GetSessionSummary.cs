// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Sessions;

/// <summary>
/// 在线概况：<c>GET /api/sessions:summary</c>。当前一侧的有效会话数、有会话的用户数（同列表一样按数据范围收窄）与持有实时连接的用户数。
/// </summary>
/// <param name="queries">会话查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = IdentityPermissions.SessionsRead)]
public sealed class GetSessionSummary(SessionQueries queries) : IOperation<SessionSummaryRequest, SessionSummaryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sessions.summary";

    /// <inheritdoc />
    public async ValueTask<Result<SessionSummaryResponse>> HandleAsync(SessionSummaryRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await queries.SummaryAsync(context).ConfigureAwait(false);
    }
}
