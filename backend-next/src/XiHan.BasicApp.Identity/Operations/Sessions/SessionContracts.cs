// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Sessions;

/// <summary>
/// 在线用户列表的一行：一个有效会话（同一用户多端登录即多行）。
/// </summary>
/// <param name="Id">会话 ID。</param>
/// <param name="UserId">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Device">设备类型：Desktop、Mobile、Tablet、Bot；认不出为空。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="IpAddressMasked">登录 IP（IPv4 遮住末段、IPv6 只留前四组）。</param>
/// <param name="Location">登录地点。</param>
/// <param name="LoginAt">登录时间（会话签发）。</param>
/// <param name="LastActiveAt">最后活动时间（最近一次续签，没续签过即登录时间）。</param>
/// <param name="IsRealtimeOnline">此刻是否持有实时连接。</param>
/// <param name="IsCurrent">是否调用方自己当前的会话（不能在这里强制下线）。</param>
/// <param name="ImpersonatorUserId">模仿会话的发起人；普通会话为空。</param>
public sealed record OnlineSessionResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string DisplayName,
    string? Device,
    string? Browser,
    string? Os,
    string? IpAddressMasked,
    string? Location,
    DateTimeOffset LoginAt,
    DateTimeOffset LastActiveAt,
    bool IsRealtimeOnline,
    bool IsCurrent,
    Guid? ImpersonatorUserId);

/// <summary>
/// <c>sessions.summary</c> 的请求（没有参数）。
/// </summary>
public sealed record SessionSummaryRequest;

/// <summary>
/// 在线概况（当前一侧，不受数据范围收窄的只有实时在线用户数）。
/// </summary>
/// <param name="ActiveSessions">有效会话数。</param>
/// <param name="ActiveUsers">有有效会话的用户数。</param>
/// <param name="RealtimeOnlineUsers">持有实时连接的用户数。</param>
public sealed record SessionSummaryResponse(int ActiveSessions, int ActiveUsers, int RealtimeOnlineUsers);

/// <summary>
/// <c>sessions.revoke</c> 的请求：强制下线一批会话。
/// </summary>
/// <param name="Ids">会话 ID。</param>
/// <param name="Reason">给被下线用户看的原因（弹窗里显示），不填即客户端的缺省文案。</param>
public sealed record RevokeSessionsRequest(
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids,
    [StringLength(200)] string? Reason = null);
