// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 一个会话的起止。
/// </summary>
/// <param name="SessionId">会话 ID。</param>
/// <param name="StartedAt">签发（登录）时间。</param>
/// <param name="LastActiveAt">最近一次刷新（没有即签发时间）。</param>
/// <param name="EndedAt">登出或吊销的时间；仍有效为空。</param>
public sealed record SessionSpan(Guid SessionId, DateTimeOffset StartedAt, DateTimeOffset LastActiveAt, DateTimeOffset? EndedAt);

/// <summary>
/// 用户的会话时段（Identity 实现）。
/// </summary>
/// <remarks>
/// 个人中心按它算在线时长（4.x 同口径）。模仿会话不算本人的。
/// </remarks>
public interface ISessionHistory
{
    /// <summary>
    /// 列出某人在某个时刻之后还可能活动过的会话。
    /// </summary>
    /// <remarks>
    /// 即那之后才结束、或刷新令牌那之后才到期的会话。
    /// </remarks>
    /// <param name="userId">用户 ID。</param>
    /// <param name="since">起点。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    ValueTask<IReadOnlyList<SessionSpan>> ListAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);
}
