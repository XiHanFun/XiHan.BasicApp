// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Sessions;

/// <summary>
/// 分页列出在线用户：<c>POST /api/sessions:list</c>。一行一个有效会话，带设备、浏览器、遮掩过的 IP、登录地点、登录与最后活动时间、是否持有实时连接；
/// 只看当前一侧，租户一侧再按数据范围收窄。
/// </summary>
/// <param name="queries">会话查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.SessionsRead)]
public sealed class ListSessions(SessionQueries queries) : IOperation<PageRequest, PageResult<OnlineSessionResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "sessions.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<OnlineSessionResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
