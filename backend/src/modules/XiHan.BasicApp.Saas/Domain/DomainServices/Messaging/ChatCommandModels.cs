#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatCommandModels
// Guid:9e3f2a5b-7ae2-4fa2-c831-2d3e4f5a6b7c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 获取或创建单聊会话命令
/// </summary>
public sealed record ChatSingleConversationCommand(long UserId, long PeerUserId);

/// <summary>
/// 创建群聊命令
/// </summary>
public sealed record ChatGroupCreateCommand(long OwnerUserId, string ConversationName, IReadOnlyCollection<long> MemberUserIds);

/// <summary>
/// 获取或创建部门群命令（成员按部门归属同步）
/// </summary>
public sealed record ChatDepartmentConversationCommand(long DepartmentId, long OperatorUserId);

/// <summary>
/// 群成员添加命令
/// </summary>
public sealed record ChatMemberAddCommand(long ConversationId, long OperatorUserId, IReadOnlyCollection<long> UserIds);

/// <summary>
/// 群成员移除/退群命令（UserId 为被移出者；等于 OperatorUserId 即主动退群）
/// </summary>
public sealed record ChatMemberRemoveCommand(long ConversationId, long OperatorUserId, long UserId);

/// <summary>
/// 发送消息命令
/// </summary>
public sealed record ChatMessageSendCommand(
    long ConversationId,
    long SenderUserId,
    ChatMessageType MessageType,
    string? Content,
    long? FileId,
    string? FileName,
    long? FileSize,
    string? ClientMessageId,
    long? ReplyToMessageId = null,
    IReadOnlyCollection<long>? MentionedUserIds = null);

/// <summary>
/// 编辑消息命令（仅文本、仅本人、限时窗口）
/// </summary>
public sealed record ChatMessageEditCommand(long MessageId, long OperatorUserId, string Content);

/// <summary>
/// 表情回应 toggle 命令（已存在则取消，否则新增）
/// </summary>
public sealed record ChatReactionToggleCommand(long MessageId, long OperatorUserId, string Emoji);

/// <summary>
/// 消息 Pin/取消 Pin 命令
/// </summary>
public sealed record ChatMessagePinCommand(long MessageId, long OperatorUserId, bool Pin);

/// <summary>
/// 成员行个人开关命令（会话置顶/免打扰 toggle）
/// </summary>
public sealed record ChatMemberToggleCommand(long ConversationId, long UserId);

/// <summary>
/// 群信息更新命令（群聊可改名；部门群名称随部门禁改；公告变更追加系统提示）
/// </summary>
public sealed record ChatConversationInfoUpdateCommand(
    long ConversationId,
    long OperatorUserId,
    string? ConversationName,
    string? Announcement,
    string? Description);

/// <summary>
/// 转让群主命令（仅群聊、仅群主；旧主降为普通成员）
/// </summary>
public sealed record ChatOwnerTransferCommand(long ConversationId, long OperatorUserId, long NewOwnerUserId);

/// <summary>
/// 成员禁言命令（群主/管理员可禁言普通成员）
/// </summary>
public sealed record ChatMemberSilenceCommand(long ConversationId, long OperatorUserId, long TargetUserId, bool IsSilenced);

/// <summary>
/// 成员角色设置命令（仅群主可设/撤管理员）
/// </summary>
public sealed record ChatMemberRoleCommand(long ConversationId, long OperatorUserId, long TargetUserId, ChatMemberRole MemberRole);

/// <summary>
/// 群治理结果（SystemMessage 非空时随消息链路推送时间线提示）
/// </summary>
public sealed record ChatGovernanceResult(SysChatConversation Conversation, SysChatMessage? SystemMessage, IReadOnlyList<long> RecipientUserIds);

/// <summary>
/// 撤回消息命令
/// </summary>
public sealed record ChatMessageRecallCommand(long MessageId, long OperatorUserId);

/// <summary>
/// 标记会话已读命令
/// </summary>
public sealed record ChatMarkReadCommand(long ConversationId, long UserId, long? UpToMessageId);

/// <summary>
/// 会话命令结果
/// </summary>
public sealed record ChatConversationCommandResult(SysChatConversation Conversation, bool Created);

/// <summary>
/// 发送消息结果（RecipientUserIds 含发送者本人，用于多端回显）
/// </summary>
public sealed record ChatMessageSendResult(SysChatMessage Message, SysChatConversation Conversation, IReadOnlyList<long> RecipientUserIds);

/// <summary>
/// 撤回消息结果
/// </summary>
public sealed record ChatMessageRecallResult(SysChatMessage Message, IReadOnlyList<long> RecipientUserIds);

/// <summary>
/// 编辑消息结果
/// </summary>
public sealed record ChatMessageEditResult(SysChatMessage Message, IReadOnlyList<long> RecipientUserIds);

/// <summary>
/// 表情回应 toggle 结果（Added=true 为新增回应，false 为取消回应）
/// </summary>
public sealed record ChatReactionToggleResult(
    long ConversationId,
    long MessageId,
    long UserId,
    string? UserName,
    string Emoji,
    bool Added,
    IReadOnlyList<long> RecipientUserIds);

/// <summary>
/// 消息 Pin 结果
/// </summary>
public sealed record ChatMessagePinResult(SysChatMessage Message, IReadOnlyList<long> RecipientUserIds);

/// <summary>
/// 标记已读结果（RecipientUserIds 为全体成员，用于已读位实时扇出）
/// </summary>
public sealed record ChatMarkReadResult(long ConversationId, long UserId, long? LastReadMessageId, IReadOnlyList<long> RecipientUserIds);
