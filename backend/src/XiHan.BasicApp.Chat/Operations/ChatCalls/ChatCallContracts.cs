// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;

namespace XiHan.BasicApp.Chat.Operations.ChatCalls;

/// <summary>
/// <c>chat-calls.start</c> 的请求
/// </summary>
/// <param name="ConversationId">会话（单聊）</param>
/// <param name="Media">媒体：语音或视频</param>
public sealed record StartChatCallRequest(Guid ConversationId, ChatCallMedia Media);

/// <summary>
/// 按通话的请求
/// </summary>
/// <param name="CallId">通话</param>
public sealed record ChatCallRequest(Guid CallId);

/// <summary>
/// 没有参数的通话请求
/// </summary>
public sealed record ChatCallEmptyRequest;

/// <summary>
/// 通话的参与者
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="UserName">用户名</param>
/// <param name="DisplayName">显示名</param>
/// <param name="AvatarFileId">头像文件（前端按它换签名地址显示）</param>
/// <param name="Status">在通话里的状态</param>
/// <param name="JoinedAt">加入时间（UTC）</param>
/// <param name="LeftAt">离开时间（UTC）</param>
public sealed record ChatCallParticipantItem(Guid UserId, string? UserName, string? DisplayName, Guid? AvatarFileId, ChatCallParticipantStatus Status, DateTimeOffset? JoinedAt, DateTimeOffset? LeftAt);

/// <summary>
/// 一次通话
/// </summary>
/// <param name="CallId">通话</param>
/// <param name="ConversationId">会话</param>
/// <param name="InitiatorUserId">发起人</param>
/// <param name="Media">媒体</param>
/// <param name="Status">状态</param>
/// <param name="EndReason">结束的原因；没结束为空</param>
/// <param name="StartedAt">发起时间（UTC）</param>
/// <param name="RingExpiresAt">响铃到几时（UTC），过了还没人接即未接听</param>
/// <param name="AnsweredAt">接通时间（UTC）</param>
/// <param name="EndedAt">结束时间（UTC）</param>
/// <param name="Participants">参与者（发起人在前）</param>
public sealed record ChatCallItem(
    Guid CallId,
    Guid ConversationId,
    Guid InitiatorUserId,
    ChatCallMedia Media,
    ChatCallStatus Status,
    ChatCallEndReason? EndReason,
    DateTimeOffset StartedAt,
    DateTimeOffset RingExpiresAt,
    DateTimeOffset? AnsweredAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<ChatCallParticipantItem> Participants);

/// <summary>
/// <c>chat-calls.active</c> 的响应
/// </summary>
/// <param name="Call">我此刻所在的通话（响铃中或通话中）；没有为空</param>
/// <param name="Now">服务端此刻的时间（UTC）：端上据它算响铃还剩多久，不靠本机的钟</param>
public sealed record ChatActiveCallResponse(ChatCallItem? Call, DateTimeOffset Now);

/// <summary>
/// 一组 ICE 服务器
/// </summary>
/// <param name="Urls">地址（<c>stun:</c>、<c>turn:</c> 或 <c>turns:</c>）</param>
/// <param name="Username">TURN 的用户名；STUN 为空</param>
/// <param name="Credential">TURN 的凭据；STUN 为空</param>
public sealed record ChatIceServer(IReadOnlyList<string> Urls, string? Username, string? Credential);

/// <summary>
/// <c>chat-calls.ice-servers</c> 的响应
/// </summary>
/// <param name="IceServers">ICE 服务器；没配为空，只有同一网段或能直连的两端打得通</param>
/// <param name="ExpiresAt">TURN 凭据的到期时间（UTC）；没配 TURN 为空</param>
public sealed record ChatIceServersResponse(IReadOnlyList<ChatIceServer> IceServers, DateTimeOffset? ExpiresAt);

/// <summary>
/// 通话记录消息的正文（JSON）
/// </summary>
/// <remarks>
/// 通话结束后追加到会话里的那条消息把它存在正文里，界面按它显示「语音通话 02:15」「未接听」这类文字
/// </remarks>
/// <param name="CallId">通话</param>
/// <param name="Media">媒体</param>
/// <param name="Reason">结束的原因</param>
/// <param name="DurationSeconds">通话时长（秒）；没接通为 0</param>
public sealed record ChatCallRecord(Guid CallId, ChatCallMedia Media, ChatCallEndReason Reason, int DurationSeconds);
