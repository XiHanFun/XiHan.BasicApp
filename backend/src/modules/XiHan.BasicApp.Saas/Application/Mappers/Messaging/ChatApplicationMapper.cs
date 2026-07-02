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
            input.ClientMessageId);
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
    /// 消息实体 → 消息项 DTO
    /// </summary>
    public static ChatMessageItemDto ToMessageItemDto(SysChatMessage message)
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
            CreatedTime = message.CreatedTime
        };
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
