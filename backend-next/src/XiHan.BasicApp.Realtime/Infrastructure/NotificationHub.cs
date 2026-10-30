// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 通知 Hub（<c>/hubs/notification</c>）。
/// </summary>
/// <remarks>
/// 只推不收，客户端没有可调用的方法。连接经框架的认证方案 <c>XiHan</c> 认证，会话仍有效由 <see cref="SessionAuthenticationFilter"/> 在认证时核对；
/// 连上即加入会话 / 用户 / 侧的组，再登记连接（在线状态显示为在线时一定已经收得到推送）。
/// </remarks>
/// <param name="registry">连接登记。</param>
internal sealed class NotificationHub(ConnectionRegistry registry) : Hub
{
    // Identity 签发的访问令牌声明（方案 5.8），按原始类型名读
    private const string SubjectClaim = "sub";
    private const string SessionClaim = "sid";
    private const string TenantClaim = "tenant";

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (!Guid.TryParse(user?.FindFirstValue(SubjectClaim), out var userId)
            || !Guid.TryParse(user?.FindFirstValue(SessionClaim), out var sessionId))
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
