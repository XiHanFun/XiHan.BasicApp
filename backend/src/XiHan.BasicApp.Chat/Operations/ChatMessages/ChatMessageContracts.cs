// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Services;

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
/// <param name="MentionAll">提及了所有人</param>
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
    bool MentionAll,
    bool IsPinned,
    IReadOnlyList<ChatReactionItem> Reactions);

/// <summary>
/// 聊天记录里的一条消息
/// </summary>
/// <param name="SenderUserId">发送人</param>
/// <param name="SenderUserName">发送人名字的快照</param>
/// <param name="CreatedAt">原来的发送时间（UTC）</param>
/// <param name="MessageType">消息类型（助手回复照旧是助手回复；聊天记录套着聊天记录时正文是里面那份的 JSON）</param>
/// <param name="Content">正文</param>
/// <param name="Attachments">附件</param>
public sealed record ChatRecordItem(Guid? SenderUserId, string? SenderUserName, DateTimeOffset CreatedAt, ChatMessageType MessageType, string? Content, IReadOnlyList<ChatMessageAttachment> Attachments);

/// <summary>
/// 聊天记录消息的正文
/// </summary>
/// <remarks>
/// 合并转发时由服务端写进消息的 <c>content</c>，前端据此写标题（「甲和乙的聊天记录」「某群的聊天记录」）并列出里面的消息
/// </remarks>
/// <param name="SourceType">来自什么会话</param>
/// <param name="SourceName">来自的群名；单聊与助手会话为空</param>
/// <param name="Participants">单聊是两个人的名字，助手会话是里面消息的发送人；群为空</param>
/// <param name="Items">里面的消息，按原来的先后</param>
public sealed record ChatRecord(ChatConversationType SourceType, string? SourceName, IReadOnlyList<string> Participants, IReadOnlyList<ChatRecordItem> Items);

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
/// <c>chat-messages.search-all</c> 的请求
/// </summary>
/// <param name="Keyword">关键字（匹配正文与附件名，不含已撤回的）</param>
/// <param name="Skip">跳过前面的条数，缺省 0</param>
/// <param name="Take">条数，1 到 50，缺省 20</param>
public sealed record ChatMessageSearchAllRequest([StringLength(200)] string? Keyword = null, [Range(0, 10000)] int? Skip = null, [Range(1, 50)] int? Take = null);

/// <summary>
/// 会话里的附件按哪一类汇总
/// </summary>
public enum ChatMediaKind
{
    /// <summary>
    /// 图片消息
    /// </summary>
    Image = 1,

    /// <summary>
    /// 文件消息
    /// </summary>
    File = 2,
}

/// <summary>
/// <c>chat-messages.media</c> 的请求
/// </summary>
/// <param name="ConversationId">会话</param>
/// <param name="Kind">图片还是文件</param>
/// <param name="BeforeMessageId">取这条之前的</param>
/// <param name="Take">条数，1 到 100，缺省 30</param>
public sealed record ChatMessageMediaRequest(Guid ConversationId, ChatMediaKind Kind, Guid? BeforeMessageId = null, [Range(1, 100)] int? Take = null);

/// <summary>
/// 一页消息
/// </summary>
/// <param name="Items">消息（历史按序号正序，搜索与附件汇总按时间倒序）</param>
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
/// <param name="MentionAll">提及所有人（只有群聊的群主与管理员能用）</param>
public sealed record SendChatMessageRequest(
    Guid ConversationId,
    ChatMessageType MessageType,
    [StringLength(4000)] string? Content = null,
    [MaxLength(20)] IReadOnlyList<ChatMessageAttachment>? Attachments = null,
    [StringLength(50)] string? ClientMessageId = null,
    Guid? ReplyToMessageId = null,
    [MaxLength(50)] IReadOnlyList<Guid>? MentionedUserIds = null,
    bool MentionAll = false);

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
/// <param name="MessageIds">要转发的消息：同一个会话里的一条或多条</param>
/// <param name="ConversationIds">转到的会话</param>
/// <param name="Merge">合并成一条聊天记录转发；否则按原来的先后逐条转发</param>
public sealed record ForwardChatMessageRequest([Required, MinLength(1), MaxLength(ChatRules.MaxForwardMessages)] IReadOnlyList<Guid> MessageIds, [Required, MinLength(1), MaxLength(20)] IReadOnlyList<Guid> ConversationIds, bool Merge = false);

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

/// <summary>
/// <c>chat-messages.reply-assistant</c> 的请求
/// </summary>
/// <param name="ConversationId">助手会话</param>
/// <param name="ReplyId">前端生成的回复标识：增量按它归位到同一条占位气泡</param>
public sealed record ReplyAssistantRequest(Guid ConversationId, [Required, StringLength(64, MinimumLength = 1)] string ReplyId);

/// <summary>
/// <c>chat-messages.reply-assistant</c> 的响应
/// </summary>
/// <param name="MessageId">落库的助手消息</param>
public sealed record ReplyAssistantResponse(Guid MessageId);
