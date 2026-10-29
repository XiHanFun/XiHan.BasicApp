// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 在线状态（Realtime 实现）：哪些会话此刻持有实时连接。只看本实例的连接；多实例部署要接 SignalR 背板与共享的连接登记（方案 9.4）。没装 Realtime 即没有实现。
/// </summary>
public interface IRealtimePresence
{
    /// <summary>
    /// 一批会话里此刻持有实时连接的。
    /// </summary>
    /// <param name="sessionIds">会话 ID。</param>
    /// <returns>持有连接的会话 ID。</returns>
    IReadOnlySet<Guid> ConnectedSessions(IEnumerable<Guid> sessionIds);

    /// <summary>
    /// 某一侧持有实时连接的去重用户数。
    /// </summary>
    /// <param name="side">侧（租户 ID，平台为 <see cref="TenantIds.Platform"/>）。</param>
    /// <returns>用户数。</returns>
    int ConnectedUsers(string side);
}
