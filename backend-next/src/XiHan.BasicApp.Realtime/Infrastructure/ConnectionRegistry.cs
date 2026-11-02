// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 一条实时连接。
/// </summary>
/// <remarks>
/// 连接 ID、所在 Hub、访问令牌里的会话与用户、连接时所在的侧，用来断开它的调用方上下文，以及 Hub 自己让它加入的组（如聊天的会话组）。
/// </remarks>
/// <param name="ConnectionId">SignalR 连接 ID。</param>
/// <param name="Hub">Hub 路径。</param>
/// <param name="SessionId">会话（令牌的 <c>sid</c>）。</param>
/// <param name="UserId">用户（令牌的 <c>sub</c>）。</param>
/// <param name="Side">侧（令牌的 <c>tenant</c>，平台账号为 <c>platform</c>）。</param>
/// <param name="Caller">调用方上下文。</param>
public sealed record RealtimeConnection(string ConnectionId, string Hub, Guid SessionId, Guid UserId, string Side, HubCallerContext Caller)
{
    private readonly ConcurrentDictionary<string, byte> _groups = new(StringComparer.Ordinal);

    /// <summary>
    /// Hub 另外让它加入的组。
    /// </summary>
    public IReadOnlyCollection<string> ExtraGroups => [.. _groups.Keys];

    /// <summary>
    /// 它所在的全部组：会话、用户、用户在当前侧、当前侧，再加 Hub 另外加入的组。
    /// </summary>
    public IReadOnlyList<string> AllGroups => [.. RealtimeGroups.Of(SessionId, UserId, Side), .. _groups.Keys];

    internal bool AddGroup(string group) => _groups.TryAdd(group, 0);

    internal bool RemoveGroup(string group) => _groups.TryRemove(group, out _);
}

/// <summary>
/// 本实例的连接登记（单例）。
/// </summary>
/// <remarks>
/// 断开本实例上的连接都查它；在线状态不配 Redis 时直接查它，配了 Redis 时由 <see cref="PresenceBoard"/> 把它写进共享缓存。变化时触发 <see cref="Changed"/>。
/// </remarks>
internal sealed class ConnectionRegistry
{
    private readonly ConcurrentDictionary<string, RealtimeConnection> _connections = new(StringComparer.Ordinal);

    /// <summary>
    /// 登记有变化（连接、断开、加组、退组）。
    /// </summary>
    public event Func<CancellationToken, Task>? Changed;

    /// <summary>
    /// 本实例的全部连接。
    /// </summary>
    public IReadOnlyList<RealtimeConnection> All => [.. _connections.Values];

    public Task AddAsync(RealtimeConnection connection, CancellationToken cancellationToken)
    {
        _connections[connection.ConnectionId] = connection;
        return NotifyAsync(cancellationToken);
    }

    public Task RemoveAsync(string connectionId, CancellationToken cancellationToken)
        => _connections.TryRemove(connectionId, out _) ? NotifyAsync(cancellationToken) : Task.CompletedTask;

    public Task AddGroupAsync(string connectionId, string group, CancellationToken cancellationToken)
        => _connections.TryGetValue(connectionId, out var connection) && connection.AddGroup(group) ? NotifyAsync(cancellationToken) : Task.CompletedTask;

    public Task RemoveGroupAsync(string connectionId, string group, CancellationToken cancellationToken)
        => _connections.TryGetValue(connectionId, out var connection) && connection.RemoveGroup(group) ? NotifyAsync(cancellationToken) : Task.CompletedTask;

    /// <summary>
    /// 一批会话在本实例上的连接。
    /// </summary>
    public IReadOnlyList<RealtimeConnection> OfSessions(IReadOnlyCollection<Guid> sessionIds)
    {
        var wanted = sessionIds.ToHashSet();
        return [.. _connections.Values.Where(c => wanted.Contains(c.SessionId))];
    }

    private Task NotifyAsync(CancellationToken cancellationToken) => Changed?.Invoke(cancellationToken) ?? Task.CompletedTask;
}
