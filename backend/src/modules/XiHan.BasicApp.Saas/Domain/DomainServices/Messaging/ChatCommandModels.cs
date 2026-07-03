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

using System.Text.Json;
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
/// 消息附件载荷（FileId → SysFile；FileName/FileSize 为发送时快照）
/// </summary>
public sealed record ChatMessageAttachment(long FileId, string FileName, long? FileSize);

/// <summary>
/// 消息附件 JSON 序列化辅助（实体 Attachments 列 ↔ 附件载荷，camelCase 存储）
/// </summary>
public static class ChatMessageAttachments
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 序列化附件列表（空列表返回 null，不落空数组）
    /// </summary>
    public static string? Serialize(IReadOnlyList<ChatMessageAttachment>? attachments)
    {
        return attachments is { Count: > 0 } ? JsonSerializer.Serialize(attachments, JsonOptions) : null;
    }

    /// <summary>
    /// 反序列化附件列表（空/异常返回空列表，避免单条脏数据拖垮整页历史查询）
    /// </summary>
    public static IReadOnlyList<ChatMessageAttachment> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ChatMessageAttachment>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

/// <summary>
/// 发送消息命令（图片/文件消息可带多个附件，正文为可选说明文字同条发送）
/// </summary>
public sealed record ChatMessageSendCommand(
    long ConversationId,
    long SenderUserId,
    ChatMessageType MessageType,
    string? Content,
    IReadOnlyList<ChatMessageAttachment>? Attachments,
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
/// 群信息更新命令（群聊可改名；部门群名称随部门禁改；公告变更追加系统提示；Avatar 为文件主键或直链）
/// </summary>
public sealed record ChatConversationInfoUpdateCommand(
    long ConversationId,
    long OperatorUserId,
    string? ConversationName,
    string? Announcement,
    string? Description,
    string? Avatar = null);

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
