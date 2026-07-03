#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatApplicationMapper
// Guid:5b0d9e2f-4eba-4fe8-cd97-8d9e0f1a2b3c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 聊天应用层映射器
/// </summary>
public static class ChatApplicationMapper
{
    /// <summary>
    /// 发送消息 DTO → 领域命令
    /// </summary>
    public static ChatMessageSendCommand ToSendCommand(ChatMessageSendDto input, long senderUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ChatMessageSendCommand(
            input.ConversationId,
            senderUserId,
            input.MessageType,
            input.Content,
            input.FileId,
            input.FileName,
            input.FileSize,
            input.ClientMessageId,
            input.ReplyToMessageId,
            input.MentionedUserIds);
    }

    /// <summary>
    /// 会话实体 → 会话摘要 DTO
    /// </summary>
    public static ChatConversationDto ToConversationDto(SysChatConversation conversation, bool created)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        return new ChatConversationDto
        {
            ConversationId = conversation.BasicId,
            ConversationType = conversation.ConversationType,
            ConversationName = conversation.ConversationName,
            Created = created
        };
    }

    /// <summary>
    /// 消息实体 → 消息项 DTO（可选带回应列表）
    /// </summary>
    public static ChatMessageItemDto ToMessageItemDto(SysChatMessage message, IEnumerable<SysChatMessageReaction>? reactions = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ChatMessageItemDto
        {
            MessageId = message.BasicId,
            ConversationId = message.ConversationId,
            SenderUserId = message.SenderUserId,
            SenderUserName = message.SenderUserName,
            MessageType = message.MessageType,
            Content = message.Content,
            FileId = message.FileId,
            FileName = message.FileName,
            FileSize = message.FileSize,
            IsRecalled = message.IsRecalled,
            ClientMessageId = message.ClientMessageId,
            CreatedTime = message.CreatedTime,
            ReplyToMessageId = message.ReplyToMessageId,
            ReplyPreview = message.ReplyPreview,
            EditedTime = message.EditedTime,
            MentionedUserIds = ParseMentionedUserIds(message.MentionedUserIds),
            IsPinned = message.IsPinned,
            Reactions = reactions?.Select(ToReactionItemDto).ToList() ?? []
        };
    }

    /// <summary>
    /// 回应实体 → 回应项 DTO
    /// </summary>
    public static ChatReactionItemDto ToReactionItemDto(SysChatMessageReaction reaction)
    {
        ArgumentNullException.ThrowIfNull(reaction);

        return new ChatReactionItemDto
        {
            Emoji = reaction.Emoji,
            UserId = reaction.UserId,
            UserName = reaction.UserName
        };
    }

    /// <summary>
    /// 解析 @ 用户ID 逗号串（容忍脏值）
    /// </summary>
    public static List<long> ParseMentionedUserIds(string? mentionedUserIds)
    {
        if (string.IsNullOrWhiteSpace(mentionedUserIds))
        {
            return [];
        }

        var result = new List<long>();
        foreach (var part in mentionedUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (long.TryParse(part, out var id) && id > 0)
            {
                result.Add(id);
            }
        }

        return result;
    }

    /// <summary>
    /// 成员实体（+用户名）→ 成员项 DTO
    /// </summary>
    public static ChatMemberItemDto ToMemberItemDto(SysChatConversationMember member, string? userName)
    {
        ArgumentNullException.ThrowIfNull(member);

        return new ChatMemberItemDto
        {
            UserId = member.UserId,
            UserName = userName,
            MemberRole = member.MemberRole,
            JoinTime = member.JoinTime
        };
    }
}
