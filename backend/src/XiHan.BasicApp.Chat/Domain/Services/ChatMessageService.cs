// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Files;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 追加了一条消息之后的消息与会话
/// </summary>
/// <param name="Message">消息</param>
/// <param name="Conversation">会话（已更新最后消息）</param>
public sealed record ChatAppended(SysChatMessage Message, SysChatConversation Conversation);

/// <summary>
/// 消息（作用域）
/// </summary>
/// <remarks>
/// 只有成员能发，禁言的不能发与编辑；文本必填不超过 4000，图片与文件至少一个附件，语音恰好一个附件、1 到 60 秒、没有正文；
/// 系统提示、助手回复与通话记录只由服务端追加，通话记录不能撤回与转发；提及最多 20 人且都是成员；回复只能回本会话未撤回的消息、存快照；撤回只本人 2 分钟内（清正文、留行与附件），编辑只本人的文本 5 分钟内；
/// 置顶消息单聊双方都能、群与部门群只有群主与管理员、每会话最多 10 条。附件以会话为访问范围（方案 H2）：发送时核对附件是自己在聊天里上传的文件，文件名与大小以文件记录为准
/// 转发（方案 H1）是服务端单条转发到一个或多个会话，原样复制类型、正文与附件、不带「转发自」，助手回复按文本转出
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="attachments">附件文件（Files 实现）</param>
/// <param name="policy">聊天策略（敏感词）</param>
/// <param name="texts">预览文案</param>
/// <param name="push">推送</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
public sealed class ChatMessageService(
    ChatStore store,
    IEnumerable<IAttachmentFiles> attachments,
    ChatPolicy policy,
    ChatTexts texts,
    ChatPush push,
    IIdGenerator<Guid> ids,
    TimeProvider clock)
{
    private const int MaxAttachmentCandidates = 200;

    private readonly IAttachmentFiles? _files = attachments.FirstOrDefault();

    /// <summary>
    /// 发一条消息
    /// </summary>
    public async Task<Result<ChatMessageItem>> SendAsync(InvocationContext context, ChatCaller caller, SendChatMessageRequest request)
    {
        var found = await store.ConversationAsync(caller.Side, request.ConversationId, context.CancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.Error!;
        }

        var current = await store.MembersAsync(caller.Side, request.ConversationId, context.CancellationToken).ConfigureAwait(false);
        var sender = current.FirstOrDefault(m => m.UserId == caller.UserId);
        if (sender is null)
        {
            return Error.Denied(ChatErrors.NotMember, ChatMessages.NotMember, MessageArguments.Of(("conversationId", request.ConversationId)));
        }

        if (sender.IsSilenced)
        {
            return Error.Denied(ChatErrors.Silenced, ChatMessages.Silenced);
        }

        var allowed = await policy.EnsureAllowedAsync(context, request.Content).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var payload = await PayloadOfAsync(context, caller, request).ConfigureAwait(false);
        if (!payload.TryGetValue(out var checkedPayload))
        {
            return payload.Error!;
        }

        var mentions = MentionsOf(request.MentionedUserIds, current);
        if (!mentions.TryGetValue(out var mentioned))
        {
            return mentions.Error!;
        }

        var reply = await ReplyPreviewAsync(context, caller.Side, request.ReplyToMessageId, request.ConversationId).ConfigureAwait(false);
        if (!reply.TryGetValue(out var replyPreview))
        {
            return reply.Error!;
        }

        var users = await store.UsersAsync([caller.UserId], context.CancellationToken).ConfigureAwait(false);
        var appended = await AppendAsync(context, caller.Side, request.ConversationId, new SysChatMessage
        {
            SenderUserId = caller.UserId,
            SenderName = users.GetValueOrDefault(caller.UserId)?.UserName,
            Type = request.MessageType,
            Content = checkedPayload.Content,
            Attachments = ChatAttachmentsJson.Serialize(checkedPayload.Attachments),
            ClientMessageId = ChatRules.Optional(request.ClientMessageId, 50),
            ReplyToMessageId = request.ReplyToMessageId,
            ReplyPreview = replyPreview.TryGetValue(out var preview) ? preview : null,
            MentionedUserIds = ChatAttachmentsJson.MentionsText(mentioned),
        }, ChatRules.Optional(checkedPayload.Content, ChatRules.MaxContentLength), checkedPayload.Attachments, caller.UserId, sender).ConfigureAwait(false);
        if (!appended.TryGetValue(out var sent))
        {
            return appended.Error!;
        }

        var item = ChatStore.ItemOf(sent.Message);
        await push.MessageAsync(caller.Side, item, sent.Conversation, [.. current.Select(static m => m.UserId)], context.CancellationToken).ConfigureAwait(false);
        return item;
    }

    /// <summary>
    /// 转发一条消息到一个或多个会话：逐个会话成功或失败
    /// </summary>
    /// <remarks>
    /// 调用方须是原消息所在会话的成员；系统提示、通话记录与已撤回的不能转发；目标会话不存在、不是成员或被禁言的逐项失败
    /// </remarks>
    public async Task<Result<BatchResult>> ForwardAsync(InvocationContext context, ChatCaller caller, Guid messageId, IReadOnlyList<Guid> conversationIds)
    {
        var source = await store.RequireMessageAsync(caller.Side, messageId, context.CancellationToken).ConfigureAwait(false);
        if (!source.TryGetValue(out var original))
        {
            return source.Error!;
        }

        var member = await store.RequireMemberAsync(caller.Side, original.ConversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        if (original.IsRecalled || original.Type is ChatMessageType.System or ChatMessageType.Call)
        {
            return Error.Validation(ChatErrors.NotForwardable, ChatMessages.NotForwardable);
        }

        var allowed = await policy.EnsureAllowedAsync(context, original.Content).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var type = original.Type == ChatMessageType.Assistant ? ChatMessageType.Text : original.Type;
        var files = ChatAttachmentsJson.Deserialize(original.Attachments);
        var users = await store.UsersAsync([caller.UserId], context.CancellationToken).ConfigureAwait(false);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var conversationId in conversationIds.Distinct())
        {
            if ((await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false)).IsFailure)
            {
                failed.Add(new BatchFailure(conversationId, ChatErrors.ConversationNotFound, ChatMessages.ConversationNotFound.L()));
                continue;
            }

            var current = await store.MembersAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
            var sender = current.FirstOrDefault(m => m.UserId == caller.UserId);
            if (sender is null || sender.IsSilenced)
            {
                failed.Add(sender is null
                    ? new BatchFailure(conversationId, ChatErrors.NotMember, ChatMessages.NotMember.L())
                    : new BatchFailure(conversationId, ChatErrors.Silenced, ChatMessages.Silenced.L()));
                continue;
            }

            var appended = await AppendAsync(context, caller.Side, conversationId, new SysChatMessage
            {
                SenderUserId = caller.UserId,
                SenderName = users.GetValueOrDefault(caller.UserId)?.UserName,
                Type = type,
                Content = original.Content,
                Attachments = original.Attachments,
            }, original.Content, files, caller.UserId, sender).ConfigureAwait(false);
            if (!appended.TryGetValue(out var sent))
            {
                return appended.Error!;
            }

            await push.MessageAsync(caller.Side, ChatStore.ItemOf(sent.Message), sent.Conversation, [.. current.Select(static m => m.UserId)], context.CancellationToken).ConfigureAwait(false);
            succeeded.Add(conversationId);
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 撤回（本人、限时内）
    /// </summary>
    public async Task<Result> RecallAsync(InvocationContext context, ChatCaller caller, Guid messageId)
    {
        var found = await store.RequireMessageAsync(caller.Side, messageId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var message))
        {
            return found.Error!;
        }

        if (message.SenderUserId != caller.UserId)
        {
            return Error.Denied(ChatErrors.NotSender, ChatMessages.NotSender);
        }

        if (message.IsRecalled)
        {
            return Error.Validation(ChatErrors.Recalled, ChatMessages.Recalled);
        }

        // 通话记录以发起人为发送人，但它记的是发生过的事，不是他写的话
        if (message.Type == ChatMessageType.Call)
        {
            return Error.Validation(ChatErrors.CallRecordLocked, ChatMessages.CallRecordLocked);
        }

        var now = clock.GetUtcNow();
        if (now - message.CreatedAt > TimeSpan.FromMinutes(ChatRules.RecallWindowMinutes))
        {
            return Error.Validation(ChatErrors.RecallExpired, ChatMessages.RecallExpired, MessageArguments.Of(("minutes", ChatRules.RecallWindowMinutes)));
        }

        message.IsRecalled = true;
        message.RecalledAt = now;
        message.Content = null;
        await store.UpdateMessageAsync(message, context.CancellationToken).ConfigureAwait(false);

        // 撤回的是最后一条时同步刷新会话预览
        var refreshed = await RefreshPreviewAsync(caller.Side, message, texts.Render(context, ChatMessages.PreviewRecalled), context.CancellationToken).ConfigureAwait(false);
        if (refreshed.IsFailure)
        {
            return refreshed;
        }

        var recipients = await store.MemberIdsAsync(caller.Side, message.ConversationId, context.CancellationToken).ConfigureAwait(false);
        await push.RecalledAsync(caller.Side, message.ConversationId, message.Id, recipients, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 编辑（本人的文本、限时内、没被禁言）
    /// </summary>
    public async Task<Result<ChatMessageItem>> EditAsync(InvocationContext context, ChatCaller caller, Guid messageId, string content)
    {
        var found = await store.RequireMessageAsync(caller.Side, messageId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var message))
        {
            return found.Error!;
        }

        if (message.SenderUserId != caller.UserId)
        {
            return Error.Denied(ChatErrors.NotSender, ChatMessages.NotSender);
        }

        if (message.IsRecalled)
        {
            return Error.Validation(ChatErrors.Recalled, ChatMessages.Recalled);
        }

        if (message.Type != ChatMessageType.Text)
        {
            return Error.Validation(ChatErrors.EditTextOnly, ChatMessages.EditTextOnly);
        }

        var now = clock.GetUtcNow();
        if (now - message.CreatedAt > TimeSpan.FromMinutes(ChatRules.EditWindowMinutes))
        {
            return Error.Validation(ChatErrors.EditExpired, ChatMessages.EditExpired, MessageArguments.Of(("minutes", ChatRules.EditWindowMinutes)));
        }

        var member = await store.RequireMemberAsync(caller.Side, message.ConversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var editor))
        {
            return member.Error!;
        }

        if (editor.IsSilenced)
        {
            return Error.Denied(ChatErrors.Silenced, ChatMessages.Silenced);
        }

        var text = content.Trim();
        if (text.Length is 0 or > ChatRules.MaxContentLength)
        {
            return Error.Validation(ChatErrors.ContentInvalid, ChatMessages.ContentInvalid, MessageArguments.Of(("max", ChatRules.MaxContentLength)));
        }

        var allowed = await policy.EnsureAllowedAsync(context, text).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        message.Content = text;
        message.EditedAt = now;
        await store.UpdateMessageAsync(message, context.CancellationToken).ConfigureAwait(false);
        var refreshed = await RefreshPreviewAsync(caller.Side, message, texts.Preview(context, message.Type, text, []), context.CancellationToken).ConfigureAwait(false);
        if (refreshed.IsFailure)
        {
            return refreshed.Error!;
        }

        var recipients = await store.MemberIdsAsync(caller.Side, message.ConversationId, context.CancellationToken).ConfigureAwait(false);
        await push.EditedAsync(caller.Side, message, recipients, context.CancellationToken).ConfigureAwait(false);
        return ChatStore.ItemOf(message);
    }

    /// <summary>
    /// 表情回应的开关（同一消息、同一用户、同一表情再点即取消）
    /// </summary>
    public async Task<Result<ChatReactionResponse>> ReactAsync(InvocationContext context, ChatCaller caller, Guid messageId, string emoji)
    {
        var normalized = emoji.Trim();
        if (normalized.Length is 0 or > 16)
        {
            return Error.Validation(ChatErrors.EmojiInvalid, ChatMessages.EmojiInvalid);
        }

        var found = await store.RequireMessageAsync(caller.Side, messageId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var message))
        {
            return found.Error!;
        }

        if (message.IsRecalled)
        {
            return Error.Validation(ChatErrors.Recalled, ChatMessages.Recalled);
        }

        var member = await store.RequireMemberAsync(caller.Side, message.ConversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var users = await store.UsersAsync([caller.UserId], context.CancellationToken).ConfigureAwait(false);
        var userName = users.GetValueOrDefault(caller.UserId)?.UserName;
        var existing = (await store.Reactions(caller.Side).Where(r => r.MessageId == messageId && r.UserId == caller.UserId && r.Emoji == normalized).Take(1).ToListAsync(context.CancellationToken).ConfigureAwait(false)).FirstOrDefault();
        if (existing is not null)
        {
            await store.DeleteReactionAsync(existing, context.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            await store.Rows.AppendAsync(new SysChatMessageReaction { Id = ids.NewId(), TenantId = caller.Side, ConversationId = message.ConversationId, MessageId = message.Id, UserId = caller.UserId, UserName = userName, Emoji = normalized }, context.CancellationToken).ConfigureAwait(false);
        }

        var added = existing is null;
        var recipients = await store.MemberIdsAsync(caller.Side, message.ConversationId, context.CancellationToken).ConfigureAwait(false);
        await push.ReactionAsync(caller.Side, new ChatReactionChangedPayload(message.ConversationId, message.Id, normalized, caller.UserId, userName, added), recipients, context.CancellationToken).ConfigureAwait(false);
        return new ChatReactionResponse(added);
    }

    /// <summary>
    /// 置顶或取消置顶一条消息
    /// </summary>
    public async Task<Result> PinAsync(InvocationContext context, ChatCaller caller, Guid messageId, bool pin)
    {
        var found = await store.RequireMessageAsync(caller.Side, messageId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var message))
        {
            return found.Error!;
        }

        var conversation = await store.ConversationAsync(caller.Side, message.ConversationId, context.CancellationToken).ConfigureAwait(false);
        if (!conversation.TryGetValue(out var owner))
        {
            return conversation.Error!;
        }

        var member = await store.RequireMemberAsync(caller.Side, owner.Id, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var pinner))
        {
            return member.Error!;
        }

        if (owner.Type != ChatConversationType.Single && pinner.Role is not (ChatMemberRole.Owner or ChatMemberRole.Admin))
        {
            return Error.Denied(ChatErrors.ManagerRequired, ChatMessages.ManagerRequired);
        }

        if (pin && message.IsRecalled)
        {
            return Error.Validation(ChatErrors.Recalled, ChatMessages.Recalled);
        }

        if (pin != message.IsPinned)
        {
            if (pin && await store.Messages(caller.Side).Where(m => m.ConversationId == owner.Id && m.IsPinned).CountAsync(context.CancellationToken).ConfigureAwait(false) >= ChatRules.MaxPinnedMessages)
            {
                return Error.Conflict(ChatErrors.PinLimit, ChatMessages.PinLimit, MessageArguments.Of(("max", ChatRules.MaxPinnedMessages)));
            }

            message.IsPinned = pin;
            message.PinnedByUserId = pin ? caller.UserId : null;
            message.PinnedAt = pin ? clock.GetUtcNow() : null;
            await store.UpdateMessageAsync(message, context.CancellationToken).ConfigureAwait(false);
        }

        var recipients = await store.MemberIdsAsync(caller.Side, owner.Id, context.CancellationToken).ConfigureAwait(false);
        await push.ChangedAsync(caller.Side, owner.Id, ChatRealtimeMethods.ChangeTypes.PinnedChanged, recipients, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 标记已读：未读清零、已读位前移（不回退），已读位推给全体成员
    /// </summary>
    public async Task<Result<ChatReadPosition>> MarkReadAsync(InvocationContext context, ChatCaller caller, Guid conversationId, Guid? upToMessageId)
    {
        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var reader))
        {
            return member.Error!;
        }

        Guid? targetId;
        long targetSeq;
        if (upToMessageId is { } upTo)
        {
            var message = await store.MessageAsync(caller.Side, upTo, context.CancellationToken).ConfigureAwait(false);
            if (message is null || message.ConversationId != conversationId)
            {
                return Error.NotFound(ChatErrors.MessageNotFound, ChatMessages.MessageNotFound, MessageArguments.Of(("id", upTo)));
            }

            (targetId, targetSeq) = (message.Id, message.Seq);
        }
        else
        {
            var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var conversation))
            {
                return found.Error!;
            }

            (targetId, targetSeq) = (conversation.LastMessageId, conversation.LastSeq);
        }

        reader.UnreadCount = 0;
        reader.LastReadAt = clock.GetUtcNow();
        if (targetSeq > reader.LastReadSeq)
        {
            reader.LastReadSeq = targetSeq;
            reader.LastReadMessageId = targetId;
        }

        await store.Rows.UpdateAsync(reader, context.CancellationToken).ConfigureAwait(false);
        var recipients = await store.MemberIdsAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        await push.ReadPositionAsync(caller.Side, new ChatReadPositionChangedPayload(conversationId, caller.UserId, reader.LastReadMessageId, reader.LastReadSeq), recipients, context.CancellationToken).ConfigureAwait(false);
        return new ChatReadPosition(caller.UserId, reader.LastReadMessageId, reader.LastReadSeq);
    }

    /// <summary>
    /// 追加一条系统提示（刷新会话预览，不加未读）
    /// </summary>
    /// <remarks>
    /// 发送人为空即中性的时间线提示；带操作人即公告这类有归属的提示。给了 <paramref name="memberCount"/> 就一并重算会话的成员数
    /// </remarks>
    public async Task<Result<ChatAppended>> AppendSystemAsync(InvocationContext context, string side, Guid conversationId, string content, string preview, Guid? senderUserId, string? senderName, int? memberCount = null)
    {
        var allocated = await store.NextSeqAsync(side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!allocated.TryGetValue(out var next))
        {
            return allocated.Error!;
        }

        var message = new SysChatMessage
        {
            Id = ids.NewId(),
            TenantId = side,
            ConversationId = conversationId,
            Seq = next.Seq,
            SenderUserId = senderUserId,
            SenderName = senderName,
            Type = ChatMessageType.System,
            Content = ChatRules.Truncate(content, ChatRules.MaxContentLength),
        };
        await store.Rows.AppendAsync(message, context.CancellationToken).ConfigureAwait(false);
        var conversation = next.Conversation;
        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.CreatedAt;
        conversation.LastMessagePreview = ChatRules.Truncate(preview, ChatRules.MaxPreviewLength);
        if (memberCount is { } count)
        {
            conversation.MemberCount = count;
        }

        await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        return new ChatAppended(message, conversation);
    }

    /// <summary>
    /// 追加一条助手回复（刷新会话预览，不加未读）
    /// </summary>
    /// <remarks>
    /// 助手不是成员：不核对成员、不算未读；超长截断而不是拒绝：模型已经生成完，丢掉整条代价更大
    /// </remarks>
    public async Task<Result<ChatAppended>> AppendAssistantAsync(InvocationContext context, string side, Guid conversationId, Guid assistantId, string assistantName, string content)
    {
        var allocated = await store.NextSeqAsync(side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!allocated.TryGetValue(out var next))
        {
            return allocated.Error!;
        }

        var text = ChatRules.Truncate(content, ChatRules.MaxAssistantContentLength);
        var message = new SysChatMessage
        {
            Id = ids.NewId(),
            TenantId = side,
            ConversationId = conversationId,
            Seq = next.Seq,
            SenderUserId = assistantId,
            SenderName = ChatRules.Optional(assistantName, 50),
            Type = ChatMessageType.Assistant,
            Content = text,
        };
        await store.Rows.AppendAsync(message, context.CancellationToken).ConfigureAwait(false);
        var conversation = next.Conversation;
        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.CreatedAt;
        conversation.LastMessagePreview = texts.Preview(context, ChatMessageType.Assistant, text, []);
        await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        return new ChatAppended(message, conversation);
    }

    /// <summary>
    /// 追加一条通话记录（刷新会话预览）
    /// </summary>
    /// <remarks>
    /// 发送人是发起人，正文是通话记录的 JSON；<paramref name="missedUserIds"/> 里的人各记一条未读（他们没接到这通电话）
    /// </remarks>
    public async Task<Result<ChatAppended>> AppendCallAsync(InvocationContext context, string side, Guid conversationId, Guid initiatorUserId, string? initiatorName, string content, string preview, IReadOnlyCollection<Guid> missedUserIds)
    {
        var allocated = await store.NextSeqAsync(side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!allocated.TryGetValue(out var next))
        {
            return allocated.Error!;
        }

        var message = new SysChatMessage
        {
            Id = ids.NewId(),
            TenantId = side,
            ConversationId = conversationId,
            Seq = next.Seq,
            SenderUserId = initiatorUserId,
            SenderName = initiatorName,
            Type = ChatMessageType.Call,
            Content = content,
        };
        await store.Rows.AppendAsync(message, context.CancellationToken).ConfigureAwait(false);
        var conversation = next.Conversation;
        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.CreatedAt;
        conversation.LastMessagePreview = ChatRules.Truncate(preview, ChatRules.MaxPreviewLength);
        await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        await store.IncrementUnreadForAsync(side, conversationId, missedUserIds, context.CancellationToken).ConfigureAwait(false);

        return new ChatAppended(message, conversation);
    }

    /// <summary>
    /// 秒传一个聊天附件
    /// </summary>
    public async Task<Result<ChatAttachmentUploadResponse>> FastUploadAsync(InvocationContext context, FastUploadChatAttachmentRequest request)
    {
        if (_files is null)
        {
            return new ChatAttachmentUploadResponse(null);
        }

        var reused = await _files.FastUploadAsync(context, ChatRules.AttachmentSource, request.Hash, request.Size, request.FileName, request.ContentType).ConfigureAwait(false);
        if (!reused.TryGetValue(out var hit))
        {
            return reused.Error!;
        }

        return new ChatAttachmentUploadResponse(hit.TryGetValue(out var file) ? new ChatAttachmentResponse(file.Id, file.Name, file.Size, file.ContentType) : null);
    }

    /// <summary>
    /// 文件是不是这个会话里某条消息的附件
    /// </summary>
    /// <remarks>
    /// 文本匹配只用来缩小范围：附件 JSON 里有上传方给的文件名，写上别人的文件 ID 也能命中，要解析出来逐条比。命中文本的消息只有这么多，超过上限的不再看
    /// </remarks>
    private async Task<bool> IsAttachedAsync(string side, Guid conversationId, Guid fileId, CancellationToken cancellationToken)
    {
        var marker = fileId.ToString();
        var candidates = await store.Messages(side)
            .Where(m => m.ConversationId == conversationId && m.Attachments != null && m.Attachments.Contains(marker))
            .OrderByDescending(m => m.Seq)
            .Take(MaxAttachmentCandidates)
            .Select(m => m.Attachments)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return candidates.Any(json => ChatAttachmentsJson.Deserialize(json).Any(a => a.FileId == fileId));
    }

    /// <summary>
    /// 会话成员凭会话换附件的下载地址
    /// </summary>
    /// <remarks>
    /// 附件须出现在这个会话的消息里、是会话现在的头像，或是调用方自己在聊天里上传、还没发出的文件（发送前的预览）
    /// </remarks>
    public async Task<Result<ChatAttachmentUrlResponse>> PresignAsync(InvocationContext context, ChatCaller caller, PresignChatAttachmentRequest request)
    {
        var member = await store.RequireMemberAsync(caller.Side, request.ConversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        if (_files is null)
        {
            return AttachmentNotFound(request.FileId);
        }

        var marker = request.FileId.ToString();
        var conversationId = request.ConversationId;
        var referenced = await store.Conversations(caller.Side).Where(c => c.Id == conversationId && c.Avatar == marker).AnyAsync(context.CancellationToken).ConfigureAwait(false)
            || await IsAttachedAsync(caller.Side, conversationId, request.FileId, context.CancellationToken).ConfigureAwait(false);
        if (!referenced)
        {
            var own = await _files.FindAsync(context, [request.FileId]).ConfigureAwait(false);
            if (!own.TryGetValue(request.FileId, out var file) || !IsOwnChatFile(file, caller))
            {
                return AttachmentNotFound(request.FileId);
            }
        }

        var signed = await _files.SignAsync(context, request.FileId, request.Download).ConfigureAwait(false);
        return signed.TryGetValue(out var link) ? new ChatAttachmentUrlResponse(link.Url, link.ExpiresAt) : signed.Error!;
    }

    /// <summary>
    /// 核对群头像
    /// </summary>
    /// <remarks>
    /// 给的是文件主键时须是调用方自己在聊天里上传的（会话成员凭会话换得到它的地址），没变的照旧；直链与清空不核对
    /// </remarks>
    internal async Task<Result> EnsureAvatarAsync(InvocationContext context, ChatCaller caller, string? current, string? avatar)
    {
        if (avatar is null || string.Equals(avatar, current, StringComparison.Ordinal) || !Guid.TryParse(avatar, out var fileId))
        {
            return Result.Success();
        }

        var owned = await OwnAttachmentsAsync(context, caller, [new ChatMessageAttachment(fileId, avatar)]).ConfigureAwait(false);
        return owned.IsFailure ? owned.Error! : Result.Success();
    }

    private static Error AttachmentNotFound(Guid fileId) => Error.NotFound(ChatErrors.AttachmentNotFound, ChatMessages.AttachmentNotFound, MessageArguments.Of(("id", fileId)));

    private static bool IsOwnChatFile(AttachmentFile file, ChatCaller caller)
        => string.Equals(file.Source, ChatRules.AttachmentSource, StringComparison.Ordinal) && Guid.TryParse(file.UploadedBy, out var uploader) && uploader == caller.UserId;

    /// <summary>
    /// 追加一条用户消息
    /// </summary>
    /// <remarks>
    /// 取序号、落消息、刷新会话的最后消息、其余成员未读加一、发送人的已读位前移
    /// </remarks>
    private async Task<Result<ChatAppended>> AppendAsync(InvocationContext context, string side, Guid conversationId, SysChatMessage message, string? content, IReadOnlyList<ChatMessageAttachment> files, Guid senderUserId, SysChatConversationMember sender)
    {
        var allocated = await store.NextSeqAsync(side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!allocated.TryGetValue(out var next))
        {
            return allocated.Error!;
        }

        message.Id = ids.NewId();
        message.TenantId = side;
        message.ConversationId = conversationId;
        message.Seq = next.Seq;
        await store.Rows.AppendAsync(message, context.CancellationToken).ConfigureAwait(false);
        var conversation = next.Conversation;
        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.CreatedAt;
        conversation.LastMessagePreview = texts.Preview(context, message.Type, content, files);
        await store.Rows.UpdateAsync(conversation, context.CancellationToken).ConfigureAwait(false);
        await store.IncrementUnreadAsync(side, conversationId, senderUserId, context.CancellationToken).ConfigureAwait(false);
        sender.LastReadMessageId = message.Id;
        sender.LastReadSeq = message.Seq;
        sender.LastReadAt = message.CreatedAt;
        await store.Rows.UpdateAsync(sender, context.CancellationToken).ConfigureAwait(false);
        return new ChatAppended(message, conversation);
    }

    /// <summary>
    /// 被撤回或编辑的是最后一条时刷新会话预览
    /// </summary>
    private async Task<Result> RefreshPreviewAsync(string side, SysChatMessage message, string preview, CancellationToken cancellationToken)
    {
        var found = await store.ConversationAsync(side, message.ConversationId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.LastMessageId != message.Id)
        {
            return Result.Success();
        }

        conversation.LastMessagePreview = ChatRules.Truncate(preview, ChatRules.MaxPreviewLength);
        await store.Rows.UpdateAsync(conversation, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 按消息类型核对正文与附件，附件以文件记录为准
    /// </summary>
    private async Task<Result<(string? Content, IReadOnlyList<ChatMessageAttachment> Attachments)>> PayloadOfAsync(InvocationContext context, ChatCaller caller, SendChatMessageRequest request)
    {
        var requested = request.Attachments ?? [];
        switch (request.MessageType)
        {
            case ChatMessageType.Text:
                var text = request.Content?.Trim();
                if (string.IsNullOrEmpty(text) || text.Length > ChatRules.MaxContentLength)
                {
                    return Error.Validation(ChatErrors.ContentInvalid, ChatMessages.ContentInvalid, MessageArguments.Of(("max", ChatRules.MaxContentLength)));
                }

                return (text, []);
            case ChatMessageType.Voice:
                if (requested.Count != 1 || requested[0].DurationSeconds is not (> 0 and <= ChatRules.MaxVoiceSeconds))
                {
                    return Error.Validation(ChatErrors.VoiceInvalid, ChatMessages.VoiceInvalid, MessageArguments.Of(("max", ChatRules.MaxVoiceSeconds)));
                }

                // 语音没有正文：说明文字要么没有，要么该走文本消息
                var voice = await OwnAttachmentsAsync(context, caller, requested).ConfigureAwait(false);
                return voice.TryGetValue(out var voiceFiles) ? (null, voiceFiles) : voice.Error!;
            case ChatMessageType.Image:
            case ChatMessageType.File:
                if (requested.Count == 0)
                {
                    return Error.Validation(ChatErrors.AttachmentsRequired, ChatMessages.AttachmentsRequired);
                }

                var files = await OwnAttachmentsAsync(context, caller, requested).ConfigureAwait(false);
                return files.TryGetValue(out var owned) ? (ChatRules.Optional(request.Content, ChatRules.MaxContentLength), owned) : files.Error!;
            default:
                return Error.Validation(ChatErrors.TypeNotSendable, ChatMessages.TypeNotSendable);
        }
    }

    /// <summary>
    /// 核对附件是调用方自己在聊天里上传的文件（方案 H2），文件名与大小取文件记录
    /// </summary>
    private async Task<Result<IReadOnlyList<ChatMessageAttachment>>> OwnAttachmentsAsync(InvocationContext context, ChatCaller caller, IReadOnlyList<ChatMessageAttachment> requested)
    {
        if (_files is null)
        {
            return AttachmentNotFound(requested[0].FileId);
        }

        var found = await _files.FindAsync(context, [.. requested.Select(static a => a.FileId)]).ConfigureAwait(false);
        var checkedFiles = new List<ChatMessageAttachment>(requested.Count);
        foreach (var attachment in requested)
        {
            if (!found.TryGetValue(attachment.FileId, out var file))
            {
                return AttachmentNotFound(attachment.FileId);
            }

            if (!IsOwnChatFile(file, caller))
            {
                return Error.Denied(ChatErrors.AttachmentNotOwned, ChatMessages.AttachmentNotOwned, MessageArguments.Of(("id", attachment.FileId), ("name", file.Name)));
            }

            checkedFiles.Add(new ChatMessageAttachment(file.Id, file.Name, file.Size, attachment.DurationSeconds));
        }

        return checkedFiles;
    }

    /// <summary>
    /// 核对提及：都是成员、不超过上限
    /// </summary>
    private static Result<IReadOnlyList<Guid>> MentionsOf(IReadOnlyList<Guid>? mentionedUserIds, IReadOnlyList<SysChatConversationMember> current)
    {
        var mentioned = (mentionedUserIds ?? []).Where(static id => id != Guid.Empty).Distinct().ToList();
        if (mentioned.Count > ChatRules.MaxMentionCount)
        {
            return Error.Validation(ChatErrors.TooManyMentions, ChatMessages.TooManyMentions, MessageArguments.Of(("max", ChatRules.MaxMentionCount)));
        }

        var memberIds = current.Select(static m => m.UserId).ToHashSet();
        return mentioned.All(memberIds.Contains) ? mentioned : Error.Validation(ChatErrors.MentionNotMember, ChatMessages.MentionNotMember);
    }

    /// <summary>
    /// 回复快照「发送人: 内容」（被回复的须在本会话、未撤回）
    /// </summary>
    private async Task<Result<Maybe<string>>> ReplyPreviewAsync(InvocationContext context, string side, Guid? replyToMessageId, Guid conversationId)
    {
        if (replyToMessageId is not { } replyTo)
        {
            return Maybe.None<string>();
        }

        var original = await store.MessageAsync(side, replyTo, context.CancellationToken).ConfigureAwait(false);
        if (original is null || original.ConversationId != conversationId || original.IsRecalled)
        {
            return Error.Validation(ChatErrors.ReplyInvalid, ChatMessages.ReplyInvalid);
        }

        var body = texts.Preview(context, original.Type, original.Content, ChatAttachmentsJson.Deserialize(original.Attachments));
        return Maybe.Some(ChatRules.Truncate($"{original.SenderName}: {body}", ChatRules.MaxReplyPreviewLength));
    }
}
