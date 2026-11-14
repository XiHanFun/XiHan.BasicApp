// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatCalls;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Operations.ChatMessages;

namespace XiHan.BasicApp.Chat.Operations.ChatAudit;

/// <summary>
/// 审计里的一条消息
/// </summary>
/// <param name="Id">消息</param>
/// <param name="ConversationId">会话</param>
/// <param name="ConversationName">会话名称（单聊为空）</param>
/// <param name="ConversationType">会话类型</param>
/// <param name="SenderUserId">发送人（中性的系统提示为空）</param>
/// <param name="SenderUserName">发送人名字的快照</param>
/// <param name="MessageType">消息类型</param>
/// <param name="Content">正文（撤回后为空）</param>
/// <param name="FileName">附件文件名的摘要</param>
/// <param name="IsRecalled">已撤回</param>
/// <param name="EditedAt">编辑时间（UTC）</param>
/// <param name="CreatedAt">发送时间（UTC）</param>
public sealed record ChatAuditMessageResponse(
    Guid Id,
    Guid ConversationId,
    string? ConversationName,
    ChatConversationType ConversationType,
    Guid? SenderUserId,
    string? SenderUserName,
    ChatMessageType MessageType,
    string? Content,
    string? FileName,
    bool IsRecalled,
    DateTimeOffset? EditedAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// 审计里的会话
/// </summary>
/// <param name="Id">会话</param>
/// <param name="ConversationType">会话类型</param>
/// <param name="ConversationName">会话名称（单聊为空）</param>
/// <param name="MemberCount">成员数</param>
/// <param name="OwnerUserId">群主</param>
/// <param name="DepartmentId">部门群对应的部门</param>
/// <param name="Announcement">群公告</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="LastMessageAt">最后一条消息的时间（UTC）</param>
/// <param name="DissolvedAt">解散的时间（UTC）；没解散为空</param>
public sealed record ChatAuditConversationResponse(
    Guid Id,
    ChatConversationType ConversationType,
    string? ConversationName,
    int MemberCount,
    Guid? OwnerUserId,
    Guid? DepartmentId,
    string? Announcement,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    DateTimeOffset? DissolvedAt);

/// <summary>
/// 审计里的消息详情
/// </summary>
/// <remarks>
/// 撤回清空的正文与编辑前的正文不另留（方案 H4）
/// </remarks>
/// <param name="Message">消息摘要</param>
/// <param name="Conversation">所在会话</param>
/// <param name="Seq">会话内序号</param>
/// <param name="Attachments">附件</param>
/// <param name="MentionedUserIds">提及的用户</param>
/// <param name="MentionAll">提及了所有人</param>
/// <param name="ReplyToMessageId">回复的消息</param>
/// <param name="ReplyPreview">回复快照</param>
/// <param name="RecalledAt">撤回时间（UTC）</param>
/// <param name="IsPinned">已置顶</param>
/// <param name="PinnedAt">置顶时间（UTC）</param>
/// <param name="ClientMessageId">客户端消息标识</param>
/// <param name="Reactions">表情回应</param>
public sealed record ChatAuditMessageDetailResponse(
    ChatAuditMessageResponse Message,
    ChatAuditConversationResponse Conversation,
    long Seq,
    IReadOnlyList<ChatMessageAttachment> Attachments,
    IReadOnlyList<Guid> MentionedUserIds,
    bool MentionAll,
    Guid? ReplyToMessageId,
    string? ReplyPreview,
    DateTimeOffset? RecalledAt,
    bool IsPinned,
    DateTimeOffset? PinnedAt,
    string? ClientMessageId,
    IReadOnlyList<ChatReactionItem> Reactions);

/// <summary>
/// 审计里的会话成员
/// </summary>
/// <param name="Conversation">会话</param>
/// <param name="Items">现有成员，已解散的群是解散时的成员（按角色再按入会时间）</param>
public sealed record ChatAuditMembersResponse(ChatAuditConversationResponse Conversation, IReadOnlyList<ChatMemberItem> Items);

/// <summary>
/// 审计里的一通电话
/// </summary>
/// <remarks>
/// 只有通话的状态与参与者；媒体在浏览器之间直连、不经服务端，没有通话内容可看
/// </remarks>
/// <param name="Id">通话</param>
/// <param name="ConversationId">会话</param>
/// <param name="ConversationName">会话名称（单聊为空）</param>
/// <param name="ConversationType">会话类型</param>
/// <param name="InitiatorUserId">发起人</param>
/// <param name="InitiatorUserName">发起人的用户名</param>
/// <param name="Media">媒体</param>
/// <param name="Status">状态</param>
/// <param name="EndReason">结束的原因；没结束为空</param>
/// <param name="StartedAt">发起时间（UTC）</param>
/// <param name="AnsweredAt">接通时间（UTC）；没接通为空</param>
/// <param name="EndedAt">结束时间（UTC）；没结束为空</param>
/// <param name="DurationSeconds">通话时长（秒）；没接通或还没结束为空</param>
/// <param name="Participants">参与者（发起人在前）</param>
public sealed record ChatAuditCallResponse(
    Guid Id,
    Guid ConversationId,
    string? ConversationName,
    ChatConversationType ConversationType,
    Guid InitiatorUserId,
    string? InitiatorUserName,
    ChatCallMedia Media,
    ChatCallStatus Status,
    ChatCallEndReason? EndReason,
    DateTimeOffset StartedAt,
    DateTimeOffset? AnsweredAt,
    DateTimeOffset? EndedAt,
    int? DurationSeconds,
    IReadOnlyList<ChatCallParticipantItem> Participants);

/// <summary>
/// <c>chat-audit.get</c> 的请求
/// </summary>
/// <param name="Id">消息</param>
public sealed record GetChatAuditMessageRequest(Guid Id);
