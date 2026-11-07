// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization.Metadata;

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 实时推送（Realtime 实现）
/// </summary>
/// <remarks>
/// 按 Hub 路径（<see cref="RealtimeHubs"/>）推送到登记过的 Hub
/// <see cref="SendAsync{T}"/> 在调用管道里（操作、按租户执行的后台调用）先记下，调用成功且事务提交之后才发出，失败或回滚即丢弃；管道外（后台循环）立即发出
/// 不落库的瞬时事件（正在输入、助手的流式增量）用 <see cref="SendNowAsync{T}"/>，只能在事务之外调用
/// 配了 Redis 时经 SignalR 背板送到各实例的连接，没配只在本实例内
/// 推送不保证送达：接收者此刻没有连接即略过，持久的状态（收件箱、消息、任务进度）由各包自己的表承担。没装 Realtime 即没有推送
/// </remarks>
public interface IRealtimePush
{
    /// <summary>
    /// 推送一条消息，调用提交之后发出
    /// </summary>
    /// <remarks>
    /// 载荷按 <paramref name="payloadInfo"/> 序列化（各包的源生成上下文，枚举按名称），记下时就序列化，不随之后的修改变化
    /// </remarks>
    /// <typeparam name="T">载荷类型</typeparam>
    /// <param name="hub">Hub 路径（<see cref="RealtimeHubs"/>）；没登记的 Hub 即抛出</param>
    /// <param name="audience">受众；为空即不推送</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="payload">载荷</param>
    /// <param name="payloadInfo">载荷的序列化元数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask SendAsync<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 立即推送一条瞬时消息
    /// </summary>
    /// <remarks>
    /// 不等提交、不随回滚撤回，只给不落库的瞬时事件用；在开着事务的调用里调用即抛出 <see cref="InvalidOperationException"/>（要随数据一起生效的推送用 <see cref="SendAsync{T}"/>）
    /// </remarks>
    /// <typeparam name="T">载荷类型</typeparam>
    /// <param name="hub">Hub 路径（<see cref="RealtimeHubs"/>）；没登记的 Hub 即抛出</param>
    /// <param name="audience">受众；为空即不推送</param>
    /// <param name="method">客户端方法名</param>
    /// <param name="payload">载荷</param>
    /// <param name="payloadInfo">载荷的序列化元数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask SendNowAsync<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 断开指定会话在全部 Hub 上的连接
    /// </summary>
    /// <remarks>
    /// 用在会话吊销之后，客户端已先收到 <see cref="RealtimeMethods.ForceLogout"/>；发出时机同 <see cref="SendAsync{T}"/>
    /// 先把这些连接移出全部组（配了 Redis 时经背板跨实例生效，之后的推送不再送到），再经消息代理广播、由连接所在的实例断开；
    /// 没配 RabbitMQ 时广播只到本实例，别的实例上的连接只被移出组，等客户端收到 <see cref="RealtimeMethods.ForceLogout"/> 自行断开或令牌过期
    /// </remarks>
    /// <param name="sessionIds">会话 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask DisconnectAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken);
}
