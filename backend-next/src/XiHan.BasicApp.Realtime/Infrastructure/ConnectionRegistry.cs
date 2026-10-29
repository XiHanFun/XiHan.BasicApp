// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using XiHan.BasicApp.Contracts.Realtime;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 一条实时连接。
/// </summary>
/// <remarks>
/// 连接 ID、访问令牌里的会话与用户、连接时所在的侧，以及用来断开它的调用方上下文。
/// </remarks>
/// <param name="ConnectionId">SignalR 连接 ID。</param>
/// <param name="SessionId">会话（令牌的 <c>sid</c>）。</param>
/// <param name="UserId">用户（令牌的 <c>sub</c>）。</param>
/// <param name="Side">侧（令牌的 <c>tenant</c>，平台账号为 <c>platform</c>）。</param>
/// <param name="Caller">调用方上下文。</param>
internal sealed record RealtimeConnection(string ConnectionId, Guid SessionId, Guid UserId, string Side, HubCallerContext Caller);

/// <summary>
/// 本实例的连接登记（单例）。
/// </summary>
/// <remarks>
/// 在线状态与按会话断开都查它。进程内的表，多实例部署要换成共享登记并接 SignalR 背板（方案 9.4）。
/// </remarks>
internal sealed class ConnectionRegistry : IRealtimePresence
{
    private readonly ConcurrentDictionary<string, RealtimeConnection> _connections = new(StringComparer.Ordinal);

    public void Add(RealtimeConnection connection) => _connections[connection.ConnectionId] = connection;

    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);

    /// <summary>
    /// 一批会话上的连接。
    /// </summary>
    public IReadOnlyList<RealtimeConnection> OfSessions(IReadOnlyCollection<Guid> sessionIds)
    {
        var wanted = sessionIds.ToHashSet();
        return _connections.Values.Where(c => wanted.Contains(c.SessionId)).ToList();
    }

    /// <inheritdoc />
    public IReadOnlySet<Guid> ConnectedSessions(IEnumerable<Guid> sessionIds)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);
        var wanted = sessionIds.ToHashSet();
        return _connections.Values.Select(static c => c.SessionId).Where(wanted.Contains).ToHashSet();
    }

    /// <inheritdoc />
    public int ConnectedUsers(string side)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return _connections.Values.Where(c => string.Equals(c.Side, side, StringComparison.Ordinal)).Select(static c => c.UserId).Distinct().Count();
    }
}
