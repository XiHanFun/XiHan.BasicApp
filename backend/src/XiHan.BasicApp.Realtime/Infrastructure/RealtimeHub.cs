// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 实时 Hub 的基类
/// </summary>
/// <remarks>
/// 各包的 Hub 继承它并经 <see cref="RealtimeHubServiceCollectionExtensions.AddRealtimeHub{THub}"/> 登记，由宿主统一映射（绕过内核管道，经框架的认证方案 <c>XiHan</c> 认证，
/// 会话仍有效由 <see cref="SessionAuthenticationFilter"/> 在认证时核对）。连上即加入会话 / 用户 / 侧的组，再登记连接（在线状态显示为在线时一定已经收得到推送）；
/// 派生的 Hub 要让连接加入别的组时用 <see cref="JoinGroupAsync"/>，这样吊销会话时它们一并被移出
/// </remarks>
/// <param name="realtime">实时服务</param>
public abstract class RealtimeHub(RealtimeHubServices realtime) : Hub
{
    // Identity 签发的访问令牌声明（方案 5.8），按原始类型名读
    private const string SubjectClaim = "sub";
    private const string SessionClaim = "sid";
    private const string TenantClaim = "tenant";
    private const string ConnectionItem = "xihan.basicapp.realtime.connection";

    /// <summary>
    /// 当前连接（连上之后才有）
    /// </summary>
    protected RealtimeConnection? Connection => Context.Items.TryGetValue(ConnectionItem, out var value) ? value as RealtimeConnection : null;

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

        var connection = new RealtimeConnection(Context.ConnectionId, realtime.HubPathOf(GetType()), sessionId, userId, side, Context);
        Context.Items[ConnectionItem] = connection;
        await realtime.Registry.AddAsync(connection, Context.ConnectionAborted).ConfigureAwait(false);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // 组成员随连接一起由 SignalR 清掉，登记表自己清
        await realtime.Registry.RemoveAsync(Context.ConnectionId, CancellationToken.None).ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    /// <summary>
    /// 让当前连接加入一个组并记进登记
    /// </summary>
    /// <param name="group">组名</param>
    protected async Task JoinGroupAsync(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);
        await realtime.Registry.AddGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// 让当前连接退出一个组
    /// </summary>
    /// <param name="group">组名</param>
    protected async Task LeaveGroupAsync(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);
        await realtime.Registry.RemoveGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted).ConfigureAwait(false);
    }
}

/// <summary>
/// 实时 Hub 用到的服务
/// </summary>
/// <remarks>
/// 派生的 Hub 经构造注入后交给 <see cref="RealtimeHub"/>
/// </remarks>
public sealed class RealtimeHubServices
{
    internal RealtimeHubServices(ConnectionRegistry registry, RealtimeHubCatalog catalog)
    {
        Registry = registry;
        Catalog = catalog;
    }

    internal ConnectionRegistry Registry { get; }

    internal RealtimeHubCatalog Catalog { get; }

    internal string HubPathOf(Type hubType) => Catalog.PathOf(hubType);
}

/// <summary>
/// 通知 Hub（<c>/hubs/notification</c>）
/// </summary>
/// <remarks>
/// 只推不收，客户端没有可调用的方法
/// </remarks>
/// <param name="realtime">实时服务</param>
internal sealed class NotificationHub(RealtimeHubServices realtime) : RealtimeHub(realtime);
