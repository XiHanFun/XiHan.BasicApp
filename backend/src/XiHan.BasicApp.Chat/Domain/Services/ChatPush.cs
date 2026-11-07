// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Realtime;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 推送里的会话摘要
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="ConversationType">会话类型</param>
/// <param name="ConversationName">会话名称</param>
/// <param name="LastMessageAt">最后一条消息的时间（UTC）</param>
/// <param name="LastMessagePreview">最后一条消息的预览</param>
public sealed record ChatConversationSnapshot(Guid ConversationId, ChatConversationType ConversationType, string? ConversationName, DateTimeOffset? LastMessageAt, string? LastMessagePreview);

/// <summary>
/// <c>ReceiveChatMessage</c> 的载荷
/// </summary>
/// <param name="Message">消息</param>
/// <param name="Conversation">会话摘要</param>
public sealed record ChatMessagePushPayload(ChatMessageItem Message, ChatConversationSnapshot Conversation);

/// <summary>
/// <c>ChatMessageRecalled</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="MessageId">消息</param>
public sealed record ChatMessageRecalledPayload(Guid ConversationId, Guid MessageId);

/// <summary>
/// <c>ChatConversationChanged</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="ChangeType">变更种类（<see cref="ChatRealtimeMethods.ChangeTypes"/>）</param>
public sealed record ChatConversationChangedPayload(Guid ConversationId, string ChangeType);

/// <summary>
/// <c>ChatTyping</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="UserId">正在输入的用户</param>
/// <param name="UserName">用户名</param>
public sealed record ChatTypingPayload(Guid ConversationId, Guid UserId, string? UserName);

/// <summary>
/// <c>ChatMessageEdited</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="MessageId">消息</param>
/// <param name="Content">新的正文</param>
/// <param name="EditedAt">编辑时间（UTC）</param>
public sealed record ChatMessageEditedPayload(Guid ConversationId, Guid MessageId, string? Content, DateTimeOffset? EditedAt);

/// <summary>
/// <c>ChatReactionChanged</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="MessageId">消息</param>
/// <param name="Emoji">表情</param>
/// <param name="UserId">回应的用户</param>
/// <param name="UserName">用户名</param>
/// <param name="Added">加上为真，取消为假</param>
public sealed record ChatReactionChangedPayload(Guid ConversationId, Guid MessageId, string Emoji, Guid UserId, string? UserName, bool Added);

/// <summary>
/// <c>ChatReadPositionChanged</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="UserId">用户</param>
/// <param name="LastReadMessageId">已读到的消息</param>
/// <param name="LastReadSeq">已读到的消息序号</param>
public sealed record ChatReadPositionChangedPayload(Guid ConversationId, Guid UserId, Guid? LastReadMessageId, long LastReadSeq);

/// <summary>
/// <c>ChatAssistantDelta</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="ReplyId">前端生成的回复标识：增量按它归位到同一条占位气泡</param>
/// <param name="Delta">增量文本</param>
public sealed record ChatAssistantDeltaPayload(Guid ConversationId, string ReplyId, string Delta);

/// <summary>
/// <c>ChatAssistantCompleted</c> 的载荷
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="ReplyId">前端生成的回复标识</param>
/// <param name="MessageId">落库的消息；失败为空</param>
/// <param name="ErrorCode">失败的错误码</param>
/// <param name="Error">失败的原因（调用方的区域）</param>
public sealed record ChatAssistantCompletedPayload(Guid ConversationId, string ReplyId, Guid? MessageId, string? ErrorCode, string? Error);

/// <summary>
/// 聊天的推送（作用域）
/// </summary>
/// <remarks>
/// 经 <see cref="RealtimeHubs.Chat"/> 按成员点发（受众是会话这一侧的这些用户，同一个人切到别的租户时收不到），调用提交之后才发出
/// </remarks>
/// <param name="push">实时推送</param>
public sealed class ChatPush(IRealtimePush push)
{
    /// <summary>
    /// 推一条消息
    /// </summary>
    public ValueTask MessageAsync(string side, ChatMessageItem message, SysChatConversation conversation, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        var payload = new ChatMessagePushPayload(message, new ChatConversationSnapshot(conversation.Id, conversation.Type, conversation.Name, conversation.LastMessageAt, conversation.LastMessagePreview));
        return push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, recipients), ChatRealtimeMethods.ReceiveChatMessage, payload, ChatJsonContext.Default.ChatMessagePushPayload, cancellationToken);
    }

    /// <summary>
    /// 立即推一段助手增量，只给本人
    /// </summary>
    /// <remarks>
    /// 增量不落库、不等提交：回复操作在写库之前没有开事务（方案 D10）
    /// </remarks>
    public ValueTask AssistantDeltaAsync(string side, Guid conversationId, string replyId, string delta, Guid userId, CancellationToken cancellationToken)
        => push.SendNowAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, [userId]), ChatRealtimeMethods.ChatAssistantDelta, new ChatAssistantDeltaPayload(conversationId, replyId, delta), ChatJsonContext.Default.ChatAssistantDeltaPayload, cancellationToken);

    /// <summary>
    /// 立即推助手回复失败：调用随后回滚，等不到提交
    /// </summary>
    public ValueTask AssistantFailedAsync(string side, Guid conversationId, string replyId, string errorCode, string error, Guid userId, CancellationToken cancellationToken)
        => push.SendNowAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, [userId]), ChatRealtimeMethods.ChatAssistantCompleted, new ChatAssistantCompletedPayload(conversationId, replyId, null, errorCode, error), ChatJsonContext.Default.ChatAssistantCompletedPayload, cancellationToken);

    /// <summary>
    /// 推助手回复完成：提交之后发出，排在落库消息的推送之后
    /// </summary>
    public ValueTask AssistantCompletedAsync(string side, Guid conversationId, string replyId, Guid messageId, Guid userId, CancellationToken cancellationToken)
        => push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, [userId]), ChatRealtimeMethods.ChatAssistantCompleted, new ChatAssistantCompletedPayload(conversationId, replyId, messageId, null, null), ChatJsonContext.Default.ChatAssistantCompletedPayload, cancellationToken);

    /// <summary>
    /// 推撤回
    /// </summary>
    public ValueTask RecalledAsync(string side, Guid conversationId, Guid messageId, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
        => push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, recipients), ChatRealtimeMethods.ChatMessageRecalled, new ChatMessageRecalledPayload(conversationId, messageId), ChatJsonContext.Default.ChatMessageRecalledPayload, cancellationToken);

    /// <summary>
    /// 推会话变更
    /// </summary>
    public ValueTask ChangedAsync(string side, Guid conversationId, string changeType, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
        => push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, recipients), ChatRealtimeMethods.ChatConversationChanged, new ChatConversationChangedPayload(conversationId, changeType), ChatJsonContext.Default.ChatConversationChangedPayload, cancellationToken);

    /// <summary>
    /// 推编辑
    /// </summary>
    public ValueTask EditedAsync(string side, SysChatMessage message, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        return push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, recipients), ChatRealtimeMethods.ChatMessageEdited, new ChatMessageEditedPayload(message.ConversationId, message.Id, message.Content, message.EditedAt), ChatJsonContext.Default.ChatMessageEditedPayload, cancellationToken);
    }

    /// <summary>
    /// 推表情回应
    /// </summary>
    public ValueTask ReactionAsync(string side, ChatReactionChangedPayload payload, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
        => push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, recipients), ChatRealtimeMethods.ChatReactionChanged, payload, ChatJsonContext.Default.ChatReactionChangedPayload, cancellationToken);

    /// <summary>
    /// 推已读位
    /// </summary>
    public ValueTask ReadPositionAsync(string side, ChatReadPositionChangedPayload payload, IReadOnlyCollection<Guid> recipients, CancellationToken cancellationToken)
        => push.SendAsync(RealtimeHubs.Chat, RealtimeAudience.UsersIn(side, recipients), ChatRealtimeMethods.ChatReadPositionChanged, payload, ChatJsonContext.Default.ChatReadPositionChangedPayload, cancellationToken);
}
