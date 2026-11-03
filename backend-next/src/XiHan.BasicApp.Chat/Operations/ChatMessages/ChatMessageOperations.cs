// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Operations.ChatMessages;

/// <summary>
/// 做完了的消息
/// </summary>
/// <param name="MessageId">消息</param>
public sealed record ChatMessageIdResponse(Guid MessageId);

/// <summary>
/// 消息历史
/// </summary>
/// <remarks>
/// <c>GET /api/chat-messages:history?conversationId=&amp;beforeMessageId=&amp;aroundMessageId=&amp;take=</c>（4.x <c>ChatQuery/MessageHistory</c>）。只给成员看，带表情回应
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesRead)]
public sealed class ChatMessageHistory(ChatQueries queries) : IOperation<ChatMessageHistoryRequest, ChatMessagePageResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.history";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessagePageResponse>> HandleAsync(ChatMessageHistoryRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.HistoryAsync(context, me, request).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 会话内搜索
/// </summary>
/// <remarks>
/// <c>GET /api/chat-messages:search?conversationId=&amp;keyword=&amp;beforeMessageId=&amp;take=</c>（4.x <c>ChatQuery/MessageSearch</c>）。匹配正文与附件、不含已撤回的，按时间倒序、不带回应
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesRead)]
public sealed class SearchChatMessages(ChatQueries queries) : IOperation<ChatMessageSearchRequest, ChatMessagePageResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.search";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessagePageResponse>> HandleAsync(ChatMessageSearchRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.SearchAsync(context, me, request).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 会话里置顶的消息
/// </summary>
/// <remarks>
/// <c>GET /api/chat-messages:pinned?conversationId=</c>（4.x <c>ChatQuery/PinnedMessages</c>）。最近置顶的在前
/// </remarks>
/// <param name="queries">聊天的查询</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesRead)]
public sealed class ListPinnedChatMessages(ChatQueries queries) : IOperation<ChatConversationRequest, ChatMessagesResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.pinned";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessagesResponse>> HandleAsync(ChatConversationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await queries.PinnedAsync(context, me, request.ConversationId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 发消息
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:send</c>（4.x <c>Chat/SendMessage</c>）。敏感词、类型与附件、提及、回复照 4.x 核对，落库后推给全体成员（含发送人的其他端）；附件须是自己在聊天里上传的文件
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class SendChatMessage(ChatMessageService service) : IOperation<SendChatMessageRequest, ChatMessageItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.send";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessageItem>> HandleAsync(SendChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.SendAsync(context, me, request).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 撤回
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:recall</c>（4.x <c>Chat/RecallMessage</c>）。本人、发出 2 分钟内；清空正文、保留行与附件
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class RecallChatMessage(ChatMessageService service) : IOperation<ChatMessageRequest, ChatMessageIdResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.recall";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessageIdResponse>> HandleAsync(ChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        if (!caller.TryGetValue(out var me))
        {
            return caller.Error!;
        }

        var recalled = await service.RecallAsync(context, me, request.MessageId).ConfigureAwait(false);
        return recalled.IsFailure ? recalled.Error! : new ChatMessageIdResponse(request.MessageId);
    }
}

/// <summary>
/// 编辑
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:edit</c>（4.x <c>Chat/Message</c>）。本人的文本、发出 5 分钟内、没被禁言
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class EditChatMessage(ChatMessageService service) : IOperation<EditChatMessageRequest, ChatMessageItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.edit";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessageItem>> HandleAsync(EditChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.EditAsync(context, me, request.MessageId, request.Content).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 表情回应的开关
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:react</c>（4.x <c>Chat/ToggleReaction</c>）。同一消息、同一用户、同一表情再点即取消
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class ReactChatMessage(ChatMessageService service) : IOperation<ReactChatMessageRequest, ChatReactionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.react";

    /// <inheritdoc />
    public async ValueTask<Result<ChatReactionResponse>> HandleAsync(ReactChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.ReactAsync(context, me, request.MessageId, request.Emoji).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 置顶消息
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:pin</c>（4.x <c>Chat/PinMessage</c>）。单聊双方都能，群与部门群只有群主与管理员，每会话最多 10 条
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class PinChatMessage(ChatMessageService service) : IOperation<ChatMessageRequest, ChatMessageIdResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.pin";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessageIdResponse>> HandleAsync(ChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        if (!caller.TryGetValue(out var me))
        {
            return caller.Error!;
        }

        var pinned = await service.PinAsync(context, me, request.MessageId, pin: true).ConfigureAwait(false);
        return pinned.IsFailure ? pinned.Error! : new ChatMessageIdResponse(request.MessageId);
    }
}

/// <summary>
/// 取消置顶消息
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:unpin</c>（4.x <c>Chat/UnpinMessage</c>）。权限同置顶
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class UnpinChatMessage(ChatMessageService service) : IOperation<ChatMessageRequest, ChatMessageIdResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.unpin";

    /// <inheritdoc />
    public async ValueTask<Result<ChatMessageIdResponse>> HandleAsync(ChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        if (!caller.TryGetValue(out var me))
        {
            return caller.Error!;
        }

        var unpinned = await service.PinAsync(context, me, request.MessageId, pin: false).ConfigureAwait(false);
        return unpinned.IsFailure ? unpinned.Error! : new ChatMessageIdResponse(request.MessageId);
    }
}

/// <summary>
/// 转发
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:forward</c>（4.x 只在前端原样重发，6.0 改为服务端操作，方案 H1）。单条转发到一个或多个会话，原样复制类型、正文与附件，不带「转发自」，助手回复按文本转出；
/// 逐个会话成功或失败
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class ForwardChatMessage(ChatMessageService service) : IOperation<ForwardChatMessageRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.forward";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(ForwardChatMessageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.ForwardAsync(context, me, request.MessageId, request.ConversationIds).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 秒传聊天附件
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages:fast-upload</c>（4.x 走通用文件的秒传，要文件的新建权限）。同一侧已有内容相同的正常文件时直接得到一个附件，没命中返回空，前端再走普通上传
/// （<c>POST /api/chat-messages/attachments</c>）。只要聊天的发送权限（方案 H2）
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesSend)]
public sealed class FastUploadChatAttachment(ChatMessageService service) : IOperation<FastUploadChatAttachmentRequest, ChatAttachmentUploadResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.fast-upload";

    /// <inheritdoc />
    public ValueTask<Result<ChatAttachmentUploadResponse>> HandleAsync(FastUploadChatAttachmentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return new(service.FastUploadAsync(context, request));
    }
}

/// <summary>
/// 凭会话换附件的下载地址
/// </summary>
/// <remarks>
/// <c>GET /api/chat-messages:presign?conversationId=&amp;fileId=&amp;download=</c>（4.x 走通用文件的签名，要文件的读权限）。须是会话成员，附件须出现在这个会话的消息里，
/// 或是自己在聊天里上传、还没发出的文件（方案 H2）。地址原样使用、不能再拼参数
/// </remarks>
/// <param name="service">消息服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatMessagesRead)]
public sealed class PresignChatAttachment(ChatMessageService service) : IOperation<PresignChatAttachmentRequest, ChatAttachmentUrlResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-messages.presign";

    /// <inheritdoc />
    public async ValueTask<Result<ChatAttachmentUrlResponse>> HandleAsync(PresignChatAttachmentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await service.PresignAsync(context, me, request).ConfigureAwait(false) : caller.Error!;
    }
}
