// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 本实例的标识（单例，每次启动不同）。
/// </summary>
/// <param name="Id">标识。</param>
internal sealed record RealtimeNode(string Id)
{
    public static RealtimeNode Create() => new(Guid.NewGuid().ToString("N"));
}

/// <summary>
/// 移出与断开连接（单例）。
/// </summary>
/// <remarks>
/// 先把连接移出全部组（之后的推送不再送到它），宽限之后再断开：先发出的 <c>ForceLogout</c> 还在连接的发送缓冲里（长轮询要等下一次轮询取走），立即断开会把它丢掉。
/// 移出组走 Hub 的组管理，配了 Redis 时经背板对别的实例上的连接同样生效；断开只能断本实例上的连接。
/// </remarks>
/// <param name="registry">本实例的连接登记。</param>
/// <param name="hubs">Hub 登记表。</param>
/// <param name="services">根服务（取 Hub 的发送面）。</param>
internal sealed class ConnectionCloser(ConnectionRegistry registry, RealtimeHubCatalog hubs, IServiceProvider services)
{
    /// <summary>
    /// 断开前的宽限。
    /// </summary>
    private static readonly TimeSpan _disconnectGrace = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 把一条连接移出若干组。
    /// </summary>
    public async Task RemoveFromGroupsAsync(string hub, string connectionId, IEnumerable<string> groups)
    {
        var context = hubs.ContextOf(hub, services);
        foreach (var group in groups)
        {
            await context.Groups.RemoveFromGroupAsync(connectionId, group, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 移出并断开一批会话在本实例上的连接。
    /// </summary>
    public async Task CloseLocalAsync(IReadOnlyCollection<Guid> sessionIds)
    {
        var connections = registry.OfSessions(sessionIds);
        foreach (var connection in connections)
        {
            await RemoveFromGroupsAsync(connection.Hub, connection.ConnectionId, connection.AllGroups).ConfigureAwait(false);
        }

        if (connections.Count > 0)
        {
            _ = AbortLaterAsync(connections);
        }
    }

    private static async Task AbortLaterAsync(IReadOnlyList<RealtimeConnection> connections)
    {
        await Task.Delay(_disconnectGrace).ConfigureAwait(false);
        foreach (var connection in connections)
        {
            connection.Caller.Abort();
        }
    }
}
