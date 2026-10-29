// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 通知 Hub（<c>/hubs/notification</c>）：只推不收，客户端没有可调用的方法。连接经 ASP.NET Core 的 JWT 认证后，
/// 再按令牌的 <c>sid</c> 经 <see cref="ISessionStatus"/> 核对会话仍有效（Hub 绕过内核管道，HTTP 调用的会话门禁管不到这里），无效即断开；
/// 有效则加入会话 / 用户 / 侧的组，再登记连接（在线状态显示为在线时一定已经收得到推送）。
/// </summary>
/// <param name="registry">连接登记。</param>
/// <param name="sessions">会话状态。</param>
internal sealed class NotificationHub(ConnectionRegistry registry, ISessionStatus sessions) : Hub
{
    // Identity 签发的访问令牌声明（方案 5.8）；JWT 认证关掉了入站声明映射，按原始类型名读
    private const string SubjectClaim = "sub";
    private const string SessionClaim = "sid";
    private const string TenantClaim = "tenant";

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (!Guid.TryParse(user?.FindFirstValue(SubjectClaim), out var userId)
            || !Guid.TryParse(user?.FindFirstValue(SessionClaim), out var sessionId)
            || !await sessions.IsActiveAsync(sessionId, Context.ConnectionAborted).ConfigureAwait(false))
        {
            Context.Abort();
            return;
        }

        var side = user?.FindFirstValue(TenantClaim) is { Length: > 0 } tenant ? tenant : TenantIds.Platform;
        foreach (var group in RealtimeGroups.Of(sessionId, userId, side))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);
        }

        registry.Add(new RealtimeConnection(Context.ConnectionId, sessionId, userId, side, Context));

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // 组成员随连接一起由 SignalR 清掉，登记表自己清
        registry.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
