// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 在线状态（Realtime 实现）：哪些会话此刻持有实时连接
/// </summary>
/// <remarks>
/// 任一 Hub 上的连接都算在线。配了 Redis 时汇总各实例登记在共享缓存里的连接（按实例心跳续期，停了心跳的实例随之过期），没配只看本实例。没装 Realtime 即没有实现
/// </remarks>
public interface IRealtimePresence
{
    /// <summary>
    /// 一批会话里此刻持有实时连接的
    /// </summary>
    /// <param name="sessionIds">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>持有连接的会话 ID</returns>
    ValueTask<IReadOnlySet<Guid>> ConnectedSessionsAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken);

    /// <summary>
    /// 某一侧持有实时连接的去重用户数
    /// </summary>
    /// <param name="side">侧（租户 ID，平台为 <see cref="TenantIds.Platform"/>）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数</returns>
    ValueTask<int> ConnectedUsersAsync(string side, CancellationToken cancellationToken);

    /// <summary>
    /// 一批用户里此刻在某一侧持有实时连接的
    /// </summary>
    /// <param name="side">侧（租户 ID，平台为 <see cref="TenantIds.Platform"/>）</param>
    /// <param name="userIds">用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>持有连接的用户 ID</returns>
    ValueTask<IReadOnlySet<Guid>> ConnectedUsersAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
