// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Chat.Domain.Entities;

namespace XiHan.BasicApp.Chat.Operations.ChatMessages;

/// <summary>
/// 消息的附件
/// </summary>
/// <param name="FileId">文件</param>
/// <param name="FileName">文件名</param>
/// <param name="FileSize">大小（字节）</param>
/// <param name="DurationSeconds">语音时长（秒）</param>
public sealed record ChatMessageAttachment(Guid FileId, [Required, StringLength(255, MinimumLength = 1)] string FileName, long? FileSize = null, int? DurationSeconds = null);

/// <summary>
/// 表情回应
/// </summary>
/// <param name="Emoji">表情</param>
/// <param name="UserId">回应的用户</param>
/// <param name="UserName">用户名</param>
public sealed record ChatReactionItem(string Emoji, Guid UserId, string? UserName);

/// <summary>
/// 一条消息
/// </summary>
/// <param name="MessageId">消息</param>
/// <param name="ConversationId">会话</param>
/// <param name="Seq">会话内序号</param>
/// <param name="SenderUserId">发送人（中性的系统提示为空）</param>
/// <param name="SenderUserName">发送人名字的快照</param>
/// <param name="MessageType">消息类型</param>
/// <param name="Content">正文（撤回后为空）</param>
/// <param name="Attachments">附件</param>
/// <param name="IsRecalled">已撤回</param>
/// <param name="ClientMessageId">客户端消息标识</param>
/// <param name="CreatedAt">发送时间（UTC）</param>
/// <param name="ReplyToMessageId">回复的消息</param>
/// <param name="ReplyPreview">回复快照</param>
/// <param name="EditedAt">编辑时间（UTC）</param>
/// <param name="MentionedUserIds">提及的用户</param>
/// <param name="IsPinned">已置顶</param>
/// <param name="Reactions">表情回应（历史里带，推送与搜索不带）</param>
public sealed record ChatMessageItem(
    Guid MessageId,
    Guid ConversationId,
    long Seq,
    Guid? SenderUserId,
    string? SenderUserName,
    ChatMessageType MessageType,
    string? Content,
    IReadOnlyList<ChatMessageAttachment> Attachments,
    bool IsRecalled,
    string? ClientMessageId,
    DateTimeOffset CreatedAt,
    Guid? ReplyToMessageId,
    string? ReplyPreview,
    DateTimeOffset? EditedAt,
    IReadOnlyList<Guid> MentionedUserIds,
    bool IsPinned,
    IReadOnlyList<ChatReactionItem> Reactions);

/// <summary>
/// <c>chat-messages.history</c> 的请求
/// </summary>
/// <remarks>
/// 给了 <paramref name="AroundMessageId"/> 即以它为中心取前后各半页（搜索命中跳转），否则取 <paramref name="BeforeMessageId"/> 之前（没给即最新）的一页
/// </remarks>
/// <param name="ConversationId">会话</param>
/// <param name="BeforeMessageId">取这条之前的</param>
/// <param name="AroundMessageId">以这条为中心</param>
/// <param name="Take">条数，1 到 100，缺省 30</param>
public sealed record ChatMessageHistoryRequest(Guid ConversationId, Guid? BeforeMessageId = null, Guid? AroundMessageId = null, [Range(1, 100)] int? Take = null);

/// <summary>
/// <c>chat-messages.search</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="Keyword">关键字（匹配正文与附件名，不含已撤回的）</param>
/// <param name="BeforeMessageId">取这条之前的</param>
/// <param name="Take">条数，1 到 50，缺省 20</param>
public sealed record ChatMessageSearchRequest(Guid ConversationId, [StringLength(200)] string? Keyword = null, Guid? BeforeMessageId = null, [Range(1, 50)] int? Take = null);

/// <summary>
/// 一页消息
/// </summary>
/// <param name="Items">消息（历史按序号正序，搜索按时间倒序）</param>
/// <param name="HasMore">还有更早的</param>
public sealed record ChatMessagePageResponse(IReadOnlyList<ChatMessageItem> Items, bool HasMore);

/// <summary>
/// 一组消息
/// </summary>
/// <param name="Items">消息</param>
public sealed record ChatMessagesResponse(IReadOnlyList<ChatMessageItem> Items);

/// <summary>
/// <c>chat-messages.send</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="MessageType">消息类型（文本、图片、语音、文件）</param>
/// <param name="Content">正文（文本必填，图片与文件可带说明，语音没有）</param>
/// <param name="Attachments">附件（须是自己在聊天里上传的文件）</param>
/// <param name="ClientMessageId">客户端消息标识</param>
/// <param name="ReplyToMessageId">回复的消息</param>
/// <param name="MentionedUserIds">提及的成员</param>
public sealed record SendChatMessageRequest(
    Guid ConversationId,
    ChatMessageType MessageType,
    [StringLength(4000)] string? Content = null,
    [MaxLength(20)] IReadOnlyList<ChatMessageAttachment>? Attachments = null,
    [StringLength(50)] string? ClientMessageId = null,
    Guid? ReplyToMessageId = null,
    [MaxLength(50)] IReadOnlyList<Guid>? MentionedUserIds = null);

/// <summary>
/// 按消息的请求
/// </summary>
/// <param name="MessageId">消息</param>
public sealed record ChatMessageRequest(Guid MessageId);

/// <summary>
/// <c>chat-messages.edit</c> 的请求
/// </summary>
/// <param name="MessageId">消息</param>
/// <param name="Content">新的正文</param>
public sealed record EditChatMessageRequest(Guid MessageId, [Required, StringLength(4000, MinimumLength = 1)] string Content);

/// <summary>
/// <c>chat-messages.react</c> 的请求
/// </summary>
/// <param name="MessageId">消息</param>
/// <param name="Emoji">表情（再点同一个即取消）</param>
public sealed record ReactChatMessageRequest(Guid MessageId, [Required, StringLength(16, MinimumLength = 1)] string Emoji);

/// <summary>
/// 回应开关的结果
/// </summary>
/// <param name="Added">加上为真，取消为假</param>
public sealed record ChatReactionResponse(bool Added);

/// <summary>
/// <c>chat-messages.forward</c> 的请求
/// </summary>
/// <param name="MessageId">要转发的消息</param>
/// <param name="ConversationIds">转到的会话</param>
public sealed record ForwardChatMessageRequest(Guid MessageId, [Required, MinLength(1), MaxLength(20)] IReadOnlyList<Guid> ConversationIds);

/// <summary>
/// <c>chat-messages.fast-upload</c> 的请求
/// </summary>
/// <param name="Hash">SHA-256（十六进制）</param>
/// <param name="Size">大小（字节）</param>
/// <param name="FileName">文件名</param>
/// <param name="ContentType">内容类型</param>
public sealed record FastUploadChatAttachmentRequest([Required, StringLength(64, MinimumLength = 64)] string Hash, [Range(1, long.MaxValue)] long Size, [Required, StringLength(255, MinimumLength = 1)] string FileName, [StringLength(100)] string? ContentType = null);

/// <summary>
/// 上传好的附件
/// </summary>
/// <param name="FileId">文件</param>
/// <param name="FileName">文件名</param>
/// <param name="FileSize">大小（字节）</param>
/// <param name="ContentType">内容类型</param>
public sealed record ChatAttachmentResponse(Guid FileId, string FileName, long FileSize, string ContentType);

/// <summary>
/// 上传或秒传的结果
/// </summary>
/// <param name="Attachment">附件；秒传没命中为空，前端再走普通上传（<c>POST /api/chat-messages/attachments</c>，总有值）</param>
public sealed record ChatAttachmentUploadResponse(ChatAttachmentResponse? Attachment);

/// <summary>
/// <c>chat-messages.presign</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="FileId">附件的文件</param>
/// <param name="Download">以附件下载（带原始文件名）；否则内联显示</param>
public sealed record PresignChatAttachmentRequest(Guid ConversationId, Guid FileId, bool Download = false);

/// <summary>
/// 附件的下载地址
/// </summary>
/// <param name="Url">地址（原样使用，不能再拼参数）</param>
/// <param name="ExpiresAt">到期时间（UTC）</param>
public sealed record ChatAttachmentUrlResponse(string Url, DateTimeOffset ExpiresAt);
