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
    string? ClientMessageId);

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
