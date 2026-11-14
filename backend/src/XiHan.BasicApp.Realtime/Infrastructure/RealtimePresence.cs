// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Realtime;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// <see cref="IRealtimePresence"/> 的实现（单例）
/// </summary>
/// <remarks>
/// 本实例的连接直接查登记表；配了 Redis（有 <paramref name="board"/>）时再并上别的实例登记在共享缓存里的连接
/// </remarks>
/// <param name="registry">本实例的连接登记</param>
/// <param name="board">共享的在线登记板；不配 Redis 时为空</param>
internal sealed class RealtimePresence(ConnectionRegistry registry, PresenceBoard? board) : IRealtimePresence
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlySet<Guid>> ConnectedSessionsAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);
        var wanted = sessionIds.ToHashSet();
        var online = registry.All.Select(static c => c.SessionId).Where(wanted.Contains).ToHashSet();
        if (board is not null && online.Count < wanted.Count)
        {
            online.UnionWith((await board.OthersAsync(cancellationToken).ConfigureAwait(false)).Select(static e => e.SessionId).Where(wanted.Contains));
        }

        return online;
    }

    /// <inheritdoc />
    public async ValueTask<int> ConnectedUsersAsync(string side, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        var users = registry.All.Where(c => string.Equals(c.Side, side, StringComparison.Ordinal)).Select(static c => c.UserId).ToHashSet();
        if (board is not null)
        {
            users.UnionWith((await board.OthersAsync(cancellationToken).ConfigureAwait(false)).Where(e => string.Equals(e.Side, side, StringComparison.Ordinal)).Select(static e => e.UserId));
        }

        return users.Count;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlySet<Guid>> ConnectedUsersAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        ArgumentNullException.ThrowIfNull(userIds);
        var online = registry.OnlineIn(side, userIds);
        if (board is not null && online.Count < userIds.Count)
        {
            var wanted = userIds.ToHashSet();
            online.UnionWith((await board.OthersAsync(cancellationToken).ConfigureAwait(false)).Where(e => string.Equals(e.Side, side, StringComparison.Ordinal) && wanted.Contains(e.UserId)).Select(static e => e.UserId));
        }

        return online;
    }
}
