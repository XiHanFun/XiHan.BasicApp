// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Operations.ChatAudit;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Queries;

/// <summary>
/// 聊天审计的查询（作用域）
/// </summary>
/// <remarks>
/// 持有审计权限即看当前一侧全部会话的消息，不要求是成员。关键字匹配正文、发送人名与附件；
/// 筛选 <c>conversationId</c>、<c>senderUserId</c>（<c>eq</c>）、<c>messageType</c>（<c>eq</c> / <c>in</c>）、<c>createdAt</c>（<c>ge</c> / <c>le</c>）、<c>isRecalled</c>（<c>eq</c>）；
/// 没给 <c>isRecalled</c> 时缺省不含已撤回的，给 <c>includeRecalled</c> 为 <c>true</c> 即都看
/// 排序 <c>createdAt</c>、<c>messageType</c>，缺省按发送时间倒序。另有消息详情与会话成员（方案 H4）
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="chats">聊天的查询（成员条目）</param>
public sealed class ChatAuditQueries(ChatStore store, ChatQueries chats)
{
    private const string FieldConversationId = "conversationId";
    private const string FieldSenderUserId = "senderUserId";
    private const string FieldMessageType = "messageType";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldIsRecalled = "isRecalled";
    private const string FieldIncludeRecalled = "includeRecalled";

    /// <summary>
    /// 分页列出消息
    /// </summary>
    public async ValueTask<Result<PageResult<ChatAuditMessageResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = SideRows.SideOf(context);
        var query = store.Messages(side);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(m => (m.Content != null && m.Content.Contains(keyword)) || (m.SenderName != null && m.SenderName.Contains(keyword)) || (m.Attachments != null && m.Attachments.Contains(keyword)));
        }

        var recalledFiltered = false;
        foreach (var filter in request.Filters)
        {
            var value = filter.Value?.Trim() ?? string.Empty;
            if (string.Equals(filter.Field, FieldIncludeRecalled, StringComparison.Ordinal))
            {
                if (!string.Equals(filter.Operator, "eq", StringComparison.Ordinal) || !bool.TryParse(value, out var include))
                {
                    return Unsupported(filter);
                }

                recalledFiltered |= include;
                continue;
            }

            var filtered = Apply(query, filter, value);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }

            recalledFiltered |= string.Equals(filter.Field, FieldIsRecalled, StringComparison.Ordinal);
        }

        if (!recalledFiltered)
        {
            query = query.Where(m => !m.IsRecalled);
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(m => m.CreatedAt, OrderByType.Desc).OrderBy(m => m.Id, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCreatedAt => query.OrderBy(m => m.CreatedAt, direction),
                FieldMessageType => query.OrderBy(m => m.Type, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(ChatErrors.QueryUnsupported, ChatMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        var conversations = await ConversationsAsync(side, [.. rows.Select(static m => m.ConversationId)], context.CancellationToken).ConfigureAwait(false);
        return new PageResult<ChatAuditMessageResponse>([.. rows.Select(m => ItemOf(m, conversations.GetValueOrDefault(m.ConversationId)))], total.Value, request.Page);
    }

    /// <summary>
    /// 消息详情
    /// </summary>
    public async Task<Result<ChatAuditMessageDetailResponse>> GetAsync(InvocationContext context, Guid messageId)
    {
        var side = SideRows.SideOf(context);
        var found = await store.RequireMessageAsync(side, messageId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var message))
        {
            return found.Error!;
        }

        var conversation = await store.ConversationAsync(side, message.ConversationId, context.CancellationToken).ConfigureAwait(false);
        if (!conversation.TryGetValue(out var owner))
        {
            return conversation.Error!;
        }

        var reactions = await store.ReactionsAsync(side, [message.Id], context.CancellationToken).ConfigureAwait(false);
        return new ChatAuditMessageDetailResponse(
            ItemOf(message, owner),
            ConversationOf(owner),
            message.Seq,
            ChatAttachmentsJson.Deserialize(message.Attachments),
            ChatAttachmentsJson.MentionsOf(message.MentionedUserIds),
            message.MentionAll,
            message.ReplyToMessageId,
            message.ReplyPreview,
            message.RecalledAt,
            message.IsPinned,
            message.PinnedAt,
            message.ClientMessageId,
            [.. reactions[message.Id]]);
    }

    /// <summary>
    /// 会话与它现在的成员
    /// </summary>
    public async Task<Result<ChatAuditMembersResponse>> MembersAsync(InvocationContext context, Guid conversationId)
    {
        var side = SideRows.SideOf(context);
        var found = await store.ConversationAsync(side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        return new ChatAuditMembersResponse(ConversationOf(conversation), await chats.MemberItemsAsync(side, conversationId, context.CancellationToken).ConfigureAwait(false));
    }

    private static ChatAuditConversationResponse ConversationOf(SysChatConversation conversation)
        => new(conversation.Id, conversation.Type, conversation.Name, conversation.MemberCount, conversation.OwnerUserId, conversation.DepartmentId, conversation.Announcement, conversation.CreatedAt, conversation.LastMessageAt);

    private static ChatAuditMessageResponse ItemOf(SysChatMessage message, SysChatConversation? conversation)
    {
        var names = ChatAttachmentsJson.Deserialize(message.Attachments).Select(static a => a.FileName).Where(static n => !string.IsNullOrWhiteSpace(n)).ToList();
        return new ChatAuditMessageResponse(
            message.Id,
            message.ConversationId,
            conversation?.Name,
            conversation?.Type ?? ChatConversationType.Single,
            message.SenderUserId,
            message.SenderName,
            message.Type,
            message.Content,
            names.Count == 0 ? null : string.Join(", ", names),
            message.IsRecalled,
            message.EditedAt,
            message.CreatedAt);
    }

    private static Result<ISugarQueryable<SysChatMessage>> Apply(ISugarQueryable<SysChatMessage> query, PageFilter filter, string value)
    {
        switch (filter.Field, filter.Operator)
        {
            case (FieldConversationId, "eq"):
                return Guid.TryParse(value, out var conversationId) ? Result.Success(query.Where(m => m.ConversationId == conversationId)) : Unsupported(filter);
            case (FieldSenderUserId, "eq"):
                return Guid.TryParse(value, out var senderId) ? Result.Success(query.Where(m => m.SenderUserId == senderId)) : Unsupported(filter);
            case (FieldMessageType, "eq"):
                return Enum.TryParse<ChatMessageType>(value, ignoreCase: true, out var type) && Enum.IsDefined(type) ? Result.Success(query.Where(m => m.Type == type)) : Unsupported(filter);
            case (FieldMessageType, "in"):
                var types = new List<ChatMessageType>();
                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Enum.TryParse<ChatMessageType>(part, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                    {
                        return Unsupported(filter);
                    }

                    types.Add(parsed);
                }

                return Result.Success(query.Where(m => types.Contains(m.Type)));
            case (FieldCreatedAt, "ge" or "le"):
                if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
                {
                    return Unsupported(filter);
                }

                return Result.Success(string.Equals(filter.Operator, "ge", StringComparison.Ordinal) ? query.Where(m => m.CreatedAt >= at) : query.Where(m => m.CreatedAt <= at));
            case (FieldIsRecalled, "eq"):
                return bool.TryParse(value, out var recalled) ? Result.Success(query.Where(m => m.IsRecalled == recalled)) : Unsupported(filter);
            default:
                return Unsupported(filter);
        }
    }

    private static Error Unsupported(PageFilter filter)
        => Error.Validation(ChatErrors.QueryUnsupported, ChatMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));

    private async Task<IReadOnlyDictionary<Guid, SysChatConversation>> ConversationsAsync(string side, IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken)
    {
        var ids = conversationIds.Distinct().ToList();
        return ids.Count == 0 ? new Dictionary<Guid, SysChatConversation>() : (await store.Conversations(side).Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(static c => c.Id);
    }
}
