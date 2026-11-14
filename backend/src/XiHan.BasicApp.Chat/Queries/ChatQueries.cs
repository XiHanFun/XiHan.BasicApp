// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Queries;

/// <summary>
/// 聊天的查询（作用域）
/// </summary>
/// <remarks>
/// 只看调用方所在一侧，会话里的内容只给成员看。历史按会话内序号取，一次 1 到 100 条、多取一条判断还有没有更早的；
/// 搜索匹配正文与附件、不含已撤回的，一次 1 到 50 条、按时间倒序、不带回应
/// 会话列表的单聊对端、会话成员与选人候选都带此刻的在线状态（任一 Hub 上有连接即在线），之后的变化由推送补
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="presence">在线状态</param>
public sealed class ChatQueries(ChatStore store, IRealtimePresence presence)
{
    private const int DefaultHistoryTake = 30;
    private const int DefaultSearchTake = 20;
    private const int DefaultUserLimit = 50;

    /// <summary>
    /// 我的会话（置顶优先，再按最后消息时间倒序）
    /// </summary>
    public async Task<ChatConversationsResponse> MineAsync(InvocationContext context, ChatCaller caller)
    {
        var cancellationToken = context.CancellationToken;
        var mine = await store.Members(caller.Side).Where(m => m.UserId == caller.UserId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (mine.Count == 0)
        {
            return new ChatConversationsResponse([]);
        }

        var conversationIds = mine.Select(static m => m.ConversationId).Distinct().ToList();
        var conversations = (await store.Conversations(caller.Side).Where(c => conversationIds.Contains(c.Id)).ToListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(static c => c.Id);

        // 单聊：一次查成员、一次查用户，解析对端
        var singleIds = conversations.Values.Where(static c => c.Type == ChatConversationType.Single).Select(static c => c.Id).ToList();
        var peers = singleIds.Count == 0
            ? []
            : await store.Members(caller.Side).Where(m => singleIds.Contains(m.ConversationId) && m.UserId != caller.UserId).ToListAsync(cancellationToken).ConfigureAwait(false);
        var peerUsers = await store.UsersAsync([.. peers.Select(static m => m.UserId)], cancellationToken).ConfigureAwait(false);
        var peerOf = peers.GroupBy(static m => m.ConversationId).ToDictionary(static g => g.Key, static g => g.First().UserId);
        var online = await presence.ConnectedUsersAsync(caller.Side, [.. peerOf.Values.Distinct()], cancellationToken).ConfigureAwait(false);

        var items = new List<ChatConversationListItem>(mine.Count);
        foreach (var member in mine)
        {
            if (!conversations.TryGetValue(member.ConversationId, out var conversation))
            {
                continue;
            }

            var single = conversation.Type == ChatConversationType.Single;
            var peerId = single && peerOf.TryGetValue(conversation.Id, out var id) ? id : (Guid?)null;
            var peer = peerId is { } p ? peerUsers.GetValueOrDefault(p) : null;
            items.Add(new ChatConversationListItem(
                conversation.Id,
                conversation.Type,
                single ? peer?.UserName ?? string.Empty : conversation.Name ?? string.Empty,
                single ? peer?.AvatarFileId?.ToString() : conversation.Avatar,
                peerId,
                conversation.DepartmentId,
                conversation.AssistantId,
                conversation.MemberCount,
                member.Role,
                member.UnreadCount,
                member.IsMuted,
                member.IsPinned,
                member.IsSilenced,
                conversation.Announcement,
                conversation.Description,
                conversation.LastMessageAt,
                conversation.LastMessagePreview,
                conversation.LastSeq,
                peerId is { } who && online.Contains(who)));
        }

        return new ChatConversationsResponse([.. items
            .OrderByDescending(static i => i.IsPinned)
            .ThenByDescending(static i => i.LastMessageAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(static i => i.ConversationId)]);
    }

    /// <summary>
    /// 消息历史
    /// </summary>
    public async Task<Result<ChatMessagePageResponse>> HistoryAsync(InvocationContext context, ChatCaller caller, ChatMessageHistoryRequest request)
    {
        var member = await store.RequireMemberAsync(caller.Side, request.ConversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var take = request.Take ?? DefaultHistoryTake;
        var conversationId = request.ConversationId;
        var messages = store.Messages(caller.Side).Where(m => m.ConversationId == conversationId);
        if (request.AroundMessageId is { } aroundId)
        {
            // 定位：以目标消息为中心取前后各半页（搜索命中跳转），前面多取一条判断还有没有更早的
            var around = await SeqOfAsync(caller.Side, conversationId, aroundId, context.CancellationToken).ConfigureAwait(false);
            if (!around.TryGetValue(out var center))
            {
                return around.Error!;
            }

            var half = Math.Max(1, take / 2);
            var older = await messages.Clone().Where(m => m.Seq < center).OrderBy(m => m.Seq, OrderByType.Desc).Take(half + 1).ToListAsync(context.CancellationToken).ConfigureAwait(false);
            var newer = await messages.Clone().Where(m => m.Seq >= center).OrderBy(m => m.Seq, OrderByType.Asc).Take(half).ToListAsync(context.CancellationToken).ConfigureAwait(false);
            var hasOlder = older.Count > half;
            var page = older.Take(half).Reverse().Concat(newer).ToList();
            return new ChatMessagePageResponse(await WithReactionsAsync(caller.Side, page, context.CancellationToken).ConfigureAwait(false), hasOlder);
        }

        if (request.BeforeMessageId is { } beforeId)
        {
            var before = await SeqOfAsync(caller.Side, conversationId, beforeId, context.CancellationToken).ConfigureAwait(false);
            if (!before.TryGetValue(out var cursor))
            {
                return before.Error!;
            }

            messages = messages.Where(m => m.Seq < cursor);
        }

        var rows = await messages.OrderBy(m => m.Seq, OrderByType.Desc).Take(take + 1).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var hasMore = rows.Count > take;
        var ordered = rows.Take(take).OrderBy(static m => m.Seq).ToList();
        return new ChatMessagePageResponse(await WithReactionsAsync(caller.Side, ordered, context.CancellationToken).ConfigureAwait(false), hasMore);
    }

    /// <summary>
    /// 会话内搜索
    /// </summary>
    public async Task<Result<ChatMessagePageResponse>> SearchAsync(InvocationContext context, ChatCaller caller, ChatMessageSearchRequest request)
    {
        var member = await store.RequireMemberAsync(caller.Side, request.ConversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var keyword = request.Keyword?.Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            return new ChatMessagePageResponse([], false);
        }

        var take = request.Take ?? DefaultSearchTake;
        var conversationId = request.ConversationId;
        // 通话记录与聊天记录的正文是 JSON，不参与搜索
        var query = store.Messages(caller.Side).Where(m => m.ConversationId == conversationId && !m.IsRecalled && m.Type != ChatMessageType.Call && m.Type != ChatMessageType.Record && ((m.Content != null && m.Content.Contains(keyword)) || (m.Attachments != null && m.Attachments.Contains(keyword))));
        if (request.BeforeMessageId is { } beforeId)
        {
            var before = await SeqOfAsync(caller.Side, conversationId, beforeId, context.CancellationToken).ConfigureAwait(false);
            if (!before.TryGetValue(out var cursor))
            {
                return before.Error!;
            }

            query = query.Where(m => m.Seq < cursor);
        }

        var rows = await query.OrderBy(m => m.Seq, OrderByType.Desc).Take(take + 1).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return new ChatMessagePageResponse([.. rows.Take(take).Select(static m => ChatStore.ItemOf(m))], rows.Count > take);
    }

    /// <summary>
    /// 会话里置顶的消息（最近置顶的在前）
    /// </summary>
    public async Task<Result<ChatMessagesResponse>> PinnedAsync(InvocationContext context, ChatCaller caller, Guid conversationId)
    {
        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var rows = await store.Messages(caller.Side).Where(m => m.ConversationId == conversationId && m.IsPinned).OrderBy(m => m.PinnedAt, OrderByType.Desc).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return new ChatMessagesResponse([.. rows.Select(static m => ChatStore.ItemOf(m))]);
    }

    /// <summary>
    /// 会话成员（按角色再按入会时间）
    /// </summary>
    public async Task<Result<ChatMembersResponse>> MembersAsync(InvocationContext context, ChatCaller caller, Guid conversationId)
    {
        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        return new ChatMembersResponse(await MemberItemsAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 会话成员的条目（聊天审计也用）
    /// </summary>
    public async Task<IReadOnlyList<ChatMemberItem>> MemberItemsAsync(string side, Guid conversationId, CancellationToken cancellationToken)
    {
        var members = await store.MembersAsync(side, conversationId, cancellationToken).ConfigureAwait(false);
        var users = await store.UsersAsync([.. members.Select(static m => m.UserId)], cancellationToken).ConfigureAwait(false);
        var online = await presence.ConnectedUsersAsync(side, [.. members.Select(static m => m.UserId)], cancellationToken).ConfigureAwait(false);
        return [.. members
            .OrderBy(static m => m.Role)
            .ThenBy(static m => m.JoinedAt)
            .Select(m =>
            {
                var user = users.GetValueOrDefault(m.UserId);
                return new ChatMemberItem(m.UserId, user?.UserName, user?.DisplayName, m.Role, m.IsSilenced, m.JoinedAt, user?.AvatarFileId, online.Contains(m.UserId));
            })];
    }

    /// <summary>
    /// 成员的已读位（群的已读回执）
    /// </summary>
    public async Task<Result<ChatReadPositionsResponse>> ReadPositionsAsync(InvocationContext context, ChatCaller caller, Guid conversationId)
    {
        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var members = await store.MembersAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        return new ChatReadPositionsResponse([.. members.Select(static m => new ChatReadPosition(m.UserId, m.LastReadMessageId, m.LastReadSeq))]);
    }

    /// <summary>
    /// 当前一侧启用的部门树（平台没有部门）
    /// </summary>
    public static async Task<ChatDepartmentTreeResponse> DepartmentTreeAsync(InvocationContext context)
    {
        var departments = await TenantScopedQuery.For<SysDepartment>(context).Where(d => d.Status == OrganizationStatus.Active).OrderBy(d => d.Order).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var ids = departments.Select(static d => d.Id).ToHashSet();
        var children = departments.ToLookup(d => d.ParentId is { } parent && ids.Contains(parent) ? parent : Guid.Empty);
        return new ChatDepartmentTreeResponse(Build(Guid.Empty));

        IReadOnlyList<ChatDepartmentNode> Build(Guid parent) => [.. children[parent].Select(d => new ChatDepartmentNode(d.Id, d.Name, Build(d.Id)))];
    }

    /// <summary>
    /// 选人：当前一侧的正常账号
    /// </summary>
    /// <remarks>
    /// 平台是注册在平台的账号，租户是注册在本租户的用户与租户成员；按显示名排序、多取一条判断后面还有没有
    /// 按部门筛选取部门自身与全部下级里任职的人；用户在平台库、任职在租户库，不能联表，先取任职的用户再按主键筛，主部门的名称也按主键批量补
    /// </remarks>
    public async Task<ChatUserOptionsResponse> UserOptionsAsync(InvocationContext context, ChatCaller caller, ChatUserOptionsRequest request)
    {
        var cancellationToken = context.CancellationToken;
        var members = (await store.TenantMembersAsync(caller.Side, cancellationToken).ConfigureAwait(false)).ToList();
        var side = caller.Side;
        var query = TenantScopedQuery.For<SysUser>(context).Where(u => u.Status == UserStatus.Active && (u.HomeTenantId == side || members.Contains(u.Id)));
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(u => u.UserName.Contains(keyword) || u.DisplayName.Contains(keyword));
        }

        if (request.DepartmentId is { } departmentId)
        {
            var staff = await DepartmentStaffAsync(context, departmentId).ConfigureAwait(false);
            if (staff.Count == 0)
            {
                return new ChatUserOptionsResponse([], false);
            }

            query = query.Where(u => staff.Contains(u.Id));
        }

        var limit = request.Limit ?? DefaultUserLimit;
        var users = await query.OrderBy(u => u.DisplayName).OrderBy(u => u.Id).Skip(request.Skip ?? 0).Take(limit + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
        var page = users.Take(limit).ToList();
        List<Guid> pageIds = [.. page.Select(static u => u.Id)];
        var departments = await PrimaryDepartmentNamesAsync(context, pageIds).ConfigureAwait(false);
        var online = await presence.ConnectedUsersAsync(side, pageIds, cancellationToken).ConfigureAwait(false);
        return new ChatUserOptionsResponse([.. page.Select(u => new ChatUserOption(u.Id, u.UserName, u.DisplayName, u.AvatarFileId, departments.GetValueOrDefault(u.Id), online.Contains(u.Id)))], users.Count > limit);
    }

    private static async Task<List<Guid>> DepartmentStaffAsync(InvocationContext context, Guid departmentId)
    {
        // 闭包表带深度 0 的自身行，一次取到部门自身与全部下级
        var subtree = await TenantScopedQuery.For<SysDepartmentHierarchy>(context).Where(h => h.AncestorId == departmentId).Select(static h => h.DescendantId).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return subtree.Count == 0
            ? []
            : [.. (await TenantScopedQuery.For<SysUserDepartment>(context).Where(m => subtree.Contains(m.DepartmentId)).Select(static m => m.UserId).ToListAsync(context.CancellationToken).ConfigureAwait(false)).Distinct()];
    }

    private static async Task<Dictionary<Guid, string>> PrimaryDepartmentNamesAsync(InvocationContext context, List<Guid> userIds)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var primary = await TenantScopedQuery.For<SysUserDepartment>(context).Where(m => userIds.Contains(m.UserId) && m.IsPrimary).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        if (primary.Count == 0)
        {
            return [];
        }

        var departmentIds = primary.Select(static m => m.DepartmentId).Distinct().ToList();
        var names = (await TenantScopedQuery.For<SysDepartment>(context).Where(d => departmentIds.Contains(d.Id)).ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToDictionary(static d => d.Id, static d => d.Name);
        var result = new Dictionary<Guid, string>(primary.Count);
        foreach (var membership in primary)
        {
            if (names.TryGetValue(membership.DepartmentId, out var name))
            {
                result[membership.UserId] = name;
            }
        }

        return result;
    }

    private async Task<Result<long>> SeqOfAsync(string side, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        var message = await store.MessageAsync(side, messageId, cancellationToken).ConfigureAwait(false);
        return message is not null && message.ConversationId == conversationId
            ? message.Seq
            : Error.NotFound(ChatErrors.MessageNotFound, ChatMessages.MessageNotFound, MessageArguments.Of(("id", messageId)));
    }

    private async Task<IReadOnlyList<ChatMessageItem>> WithReactionsAsync(string side, IReadOnlyList<SysChatMessage> messages, CancellationToken cancellationToken)
    {
        var reactions = await store.ReactionsAsync(side, [.. messages.Select(static m => m.Id)], cancellationToken).ConfigureAwait(false);
        return [.. messages.Select(m => ChatStore.ItemOf(m, reactions[m.Id]))];
    }
}
