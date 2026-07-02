#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatRealtimePushService
// Guid:8e3a2b5c-7bed-4cbb-fac0-1a2b3c4d5e6f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 聊天实时推送服务接口（落库后按成员 userId 点发，best-effort）
/// </summary>
public interface IChatRealtimePushService
{
    /// <summary>
    /// 推送新消息（含发送者本人，多端回显）
    /// </summary>
    Task PushMessageAsync(ChatMessageItemDto message, SysChatConversation conversation, IReadOnlyList<long> recipientUserIds);

    /// <summary>
    /// 推送消息撤回
    /// </summary>
    Task PushRecalledAsync(long conversationId, long messageId, IReadOnlyList<long> recipientUserIds);

    /// <summary>
    /// 推送会话变更（成员增删/入群/被移出，提示前端刷新会话列表）
    /// </summary>
    Task PushConversationChangedAsync(long conversationId, string changeType, IReadOnlyList<long> recipientUserIds);
}

/// <summary>
/// 聊天实时推送服务实现
/// </summary>
/// <remarks>
/// 推送失败只记日志，绝不影响消息落库（与站内信投递同款 best-effort 语义）。
/// 按成员 userId 点发而非 SignalR 组：不受断线重连丢组影响，未打开聊天页也能收到角标更新。
/// </remarks>
public sealed class ChatRealtimePushService : IChatRealtimePushService, IScopedDependency
{
    private readonly IRealtimeNotificationService<BasicAppChatHub> _realtimeService;

    private readonly ILogger<ChatRealtimePushService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatRealtimePushService(
        IRealtimeNotificationService<BasicAppChatHub> realtimeService,
        ILogger<ChatRealtimePushService> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PushMessageAsync(ChatMessageItemDto message, SysChatConversation conversation, IReadOnlyList<long> recipientUserIds)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(conversation);

        try
        {
            // SignalR 的 PayloadSerializerOptions 只配了 camelCase，没有 REST 侧的雪花 ID→string 与枚举→成员名转换器；
            // 载荷必须手动投影，否则 long 走 JSON number 超出 JS 2^53 精度、枚举变数字与 REST 契约漂移
            var payload = new
            {
                message = new
                {
                    messageId = message.MessageId.ToString(),
                    conversationId = message.ConversationId.ToString(),
                    senderUserId = message.SenderUserId.ToString(),
                    senderUserName = message.SenderUserName,
                    messageType = message.MessageType.ToString(),
                    content = message.Content,
                    fileId = message.FileId?.ToString(),
                    fileName = message.FileName,
                    fileSize = message.FileSize,
                    isRecalled = message.IsRecalled,
                    clientMessageId = message.ClientMessageId,
                    createdTime = message.CreatedTime
                },
                conversation = new
                {
                    conversationId = conversation.BasicId.ToString(),
                    conversationType = conversation.ConversationType.ToString(),
                    conversationName = conversation.ConversationName,
                    lastMessageTime = conversation.LastMessageTime,
                    lastMessagePreview = conversation.LastMessagePreview
                }
            };
            await _realtimeService.SendToUsersAsync(ToUserIdStrings(recipientUserIds), ChatRealtimeMethods.ReceiveChatMessage, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "聊天消息实时推送失败（不影响落库），MessageId={MessageId}", message.MessageId);
        }
    }

    /// <inheritdoc />
    public async Task PushRecalledAsync(long conversationId, long messageId, IReadOnlyList<long> recipientUserIds)
    {
        try
        {
            await _realtimeService.SendToUsersAsync(ToUserIdStrings(recipientUserIds), ChatRealtimeMethods.ChatMessageRecalled,
                new { conversationId = conversationId.ToString(), messageId = messageId.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "聊天撤回实时推送失败，MessageId={MessageId}", messageId);
        }
    }

    /// <inheritdoc />
    public async Task PushConversationChangedAsync(long conversationId, string changeType, IReadOnlyList<long> recipientUserIds)
    {
        try
        {
            await _realtimeService.SendToUsersAsync(ToUserIdStrings(recipientUserIds), ChatRealtimeMethods.ChatConversationChanged,
                new { conversationId = conversationId.ToString(), changeType });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "聊天会话变更实时推送失败，ConversationId={ConversationId}", conversationId);
        }
    }

    private static IReadOnlyList<string> ToUserIdStrings(IReadOnlyList<long> userIds)
    {
        return [.. userIds.Select(id => id.ToString())];
    }
}
