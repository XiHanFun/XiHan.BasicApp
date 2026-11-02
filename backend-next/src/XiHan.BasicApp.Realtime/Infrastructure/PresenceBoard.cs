// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 共享缓存里一条连接的登记。
/// </summary>
/// <param name="ConnectionId">连接 ID。</param>
/// <param name="Hub">Hub 路径。</param>
/// <param name="SessionId">会话。</param>
/// <param name="UserId">用户。</param>
/// <param name="Side">侧。</param>
/// <param name="Groups">Hub 另外让它加入的组。</param>
internal sealed record PresenceEntry(string ConnectionId, string Hub, Guid SessionId, Guid UserId, string Side, IReadOnlyList<string> Groups);

/// <summary>
/// 一个实例的连接快照。
/// </summary>
/// <param name="Connections">连接。</param>
internal sealed record PresenceSnapshot(IReadOnlyList<PresenceEntry> Connections);

/// <summary>
/// 实例名册里的一项。
/// </summary>
/// <param name="Id">实例标识。</param>
/// <param name="ExpiresAt">心跳续到的时刻。</param>
internal sealed record PresenceNode(string Id, DateTimeOffset ExpiresAt);

/// <summary>
/// 实例名册。
/// </summary>
/// <param name="Nodes">实例。</param>
internal sealed record PresenceNodes(IReadOnlyList<PresenceNode> Nodes);

/// <summary>
/// 配了 Redis 时的在线登记板（单例）。
/// </summary>
/// <remarks>
/// 每个实例把本实例的连接快照写在自己的键下（<c>realtime:presence:node:&lt;实例&gt;</c>，只有它自己写），连接变化即重写，并按心跳续期；
/// 实例名册（<c>realtime:presence:nodes</c>）记各实例与续到的时刻，读在线状态时汇总名册里未过期实例的快照。
/// 名册是读改写：并发写丢掉的一项在那个实例的下一次心跳补回；停了心跳的实例在 <see cref="Lifetime"/> 之后从汇总里消失。
/// 写缓存失败只记日志，在线状态等下一次心跳修复，不影响连接本身。
/// </remarks>
/// <param name="node">本实例的标识。</param>
/// <param name="cache">共享缓存。</param>
/// <param name="registry">本实例的连接登记。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
internal sealed partial class PresenceBoard(RealtimeNode node, ISharedCache cache, ConnectionRegistry registry, TimeProvider clock, ILogger<PresenceBoard> logger) : IDisposable
{
    /// <summary>
    /// 心跳间隔。
    /// </summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 快照与名册项的存活时间（三次心跳）。
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(45);

    private static readonly CacheKey _nodesKey = new("realtime:presence:nodes");
    private static readonly TimeSpan _nodesLifetime = TimeSpan.FromHours(1);
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// 本实例的标识（每次启动不同）。
    /// </summary>
    public string NodeId => node.Id;

    /// <summary>
    /// 把本实例此刻的连接写进共享缓存。
    /// </summary>
    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshot = new PresenceSnapshot([.. registry.All.Select(static c => new PresenceEntry(c.ConnectionId, c.Hub, c.SessionId, c.UserId, c.Side, [.. c.ExtraGroups]))]);
            await cache.SetAsync(NodeKey(NodeId), JsonSerializer.Serialize(snapshot, RealtimeJsonContext.Default.PresenceSnapshot), CacheEntryOptions.Expires(Lifetime), cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // 在线登记尽力而为：写失败等下一次心跳修复
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPublishFailed(logger, exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 心跳：重写快照并把本实例续进名册。
    /// </summary>
    public async Task HeartbeatAsync(CancellationToken cancellationToken)
    {
        await PublishAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = clock.GetUtcNow();
            var nodes = (await ReadNodesAsync(cancellationToken).ConfigureAwait(false)).Where(n => n.ExpiresAt > now && !string.Equals(n.Id, NodeId, StringComparison.Ordinal)).Append(new PresenceNode(NodeId, now + Lifetime)).ToList();
            await WriteNodesAsync(nodes, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // 同上：名册等下一次心跳修复
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPublishFailed(logger, exception);
        }
    }

    /// <summary>
    /// 停机时把本实例从名册与缓存里拿掉。
    /// </summary>
    public async Task LeaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var nodes = (await ReadNodesAsync(cancellationToken).ConfigureAwait(false)).Where(n => !string.Equals(n.Id, NodeId, StringComparison.Ordinal)).ToList();
            await WriteNodesAsync(nodes, cancellationToken).ConfigureAwait(false);
            await cache.RemoveAsync(NodeKey(NodeId), cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // 停机尽力而为：没拿掉的条目随存活时间过期
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPublishFailed(logger, exception);
        }
    }

    /// <summary>
    /// 别的实例此刻登记的连接（不含本实例）。
    /// </summary>
    public async Task<IReadOnlyList<PresenceEntry>> OthersAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var keys = (await ReadNodesAsync(cancellationToken).ConfigureAwait(false))
            .Where(n => n.ExpiresAt > now && !string.Equals(n.Id, NodeId, StringComparison.Ordinal))
            .Select(static n => NodeKey(n.Id))
            .ToList();
        if (keys.Count == 0)
        {
            return [];
        }

        var entries = new List<PresenceEntry>();
        foreach (var value in await cache.GetManyAsync<string>(keys, cancellationToken).ConfigureAwait(false))
        {
            if (value.TryGetValue(out var json) && JsonSerializer.Deserialize(json, RealtimeJsonContext.Default.PresenceSnapshot) is { } snapshot)
            {
                entries.AddRange(snapshot.Connections);
            }
        }

        return entries;
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private static CacheKey NodeKey(string nodeId) => new($"realtime:presence:node:{nodeId}");

    private async Task<IReadOnlyList<PresenceNode>> ReadNodesAsync(CancellationToken cancellationToken)
    {
        var stored = await cache.GetAsync<string>(_nodesKey, cancellationToken).ConfigureAwait(false);
        return stored.TryGetValue(out var json) && JsonSerializer.Deserialize(json, RealtimeJsonContext.Default.PresenceNodes) is { } nodes ? nodes.Nodes : [];
    }

    private ValueTask WriteNodesAsync(IReadOnlyList<PresenceNode> nodes, CancellationToken cancellationToken)
        => cache.SetAsync(_nodesKey, JsonSerializer.Serialize(new PresenceNodes(nodes), RealtimeJsonContext.Default.PresenceNodes), CacheEntryOptions.Expires(_nodesLifetime), cancellationToken);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Writing realtime presence to the shared cache failed; it is repaired by the next heartbeat.")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception);
}
