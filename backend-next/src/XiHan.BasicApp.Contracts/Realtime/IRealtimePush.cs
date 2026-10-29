// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization.Metadata;

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 实时推送（Realtime 实现）。
/// </summary>
/// <remarks>
/// 经 SignalR 的 <see cref="RealtimeHubs.Notification"/> 推送。
/// 在调用管道里（操作、按租户执行的后台调用）先记下，调用成功且事务提交之后才发出，失败或回滚即丢弃；管道外（后台循环）立即发出。
/// 推送不保证送达：接收者此刻没有连接即略过，持久的状态（收件箱、任务进度）由各包自己的表承担。没装 Realtime 即没有推送。
/// </remarks>
public interface IRealtimePush
{
    /// <summary>
    /// 推送一条消息。
    /// </summary>
    /// <remarks>
    /// 载荷按 <paramref name="payloadInfo"/> 序列化（各包的源生成上下文，枚举按名称）。
    /// </remarks>
    /// <typeparam name="T">载荷类型。</typeparam>
    /// <param name="audience">受众；为空即不推送。</param>
    /// <param name="method">客户端方法名（<see cref="RealtimeMethods"/>）。</param>
    /// <param name="payload">载荷。</param>
    /// <param name="payloadInfo">载荷的序列化元数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    ValueTask SendAsync<T>(RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 断开指定会话上的连接。
    /// </summary>
    /// <remarks>
    /// 用在会话吊销之后，客户端已先收到 <see cref="RealtimeMethods.ForceLogout"/>；发出时机同 <see cref="SendAsync{T}"/>。
    /// </remarks>
    /// <param name="sessionIds">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    ValueTask DisconnectAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken);
}
