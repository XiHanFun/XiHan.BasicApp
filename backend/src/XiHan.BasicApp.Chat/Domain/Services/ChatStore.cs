// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 按侧读写聊天表（作用域）
/// </summary>
/// <remarks>
/// 聊天是租户库实体，平台一侧以 <c>platform</c> 为租户落在同一张表里（两侧都能聊天），框架仓储要求调用带租户，所以经 <see cref="SideRows"/> 显式按侧读写，
/// 写之前先开事务、跟操作的其余写入一起提交或回滚。成员与消息按会话直查（成员数没有上限）；发送时对会话行加一取得序号、给其余成员的未读数加一各用一条更新语句
/// 用户在平台库、聊天在租户库，分库时不能联表，按主键批量补名字
/// </remarks>
/// <param name="rows">按侧读写</param>
/// <param name="unitOfWork">SqlSugar 工作单元</param>
/// <param name="users">用户仓储（平台库）</param>
/// <param name="memberships">租户成员（没装 Tenancy 即没有）</param>
public sealed class ChatStore(SideRows rows, SqlSugarUnitOfWork unitOfWork, IRepository<SysUser, Guid> users, IEnumerable<ITenantMembershipResolver> memberships)
{
    /// <summary>
    /// 按侧读写
    /// </summary>
    public SideRows Rows => rows;

    /// <summary>
    /// 一侧的会话
    /// </summary>
    public ISugarQueryable<SysChatConversation> Conversations(string side) => rows.Query<SysChatConversation>(side);

    /// <summary>
    /// 一侧的成员
    /// </summary>
    public ISugarQueryable<SysChatConversationMember> Members(string side) => rows.Query<SysChatConversationMember>(side);

    /// <summary>
    /// 一侧的消息
    /// </summary>
    public ISugarQueryable<SysChatMessage> Messages(string side) => rows.Records<SysChatMessage>(side);

    /// <summary>
    /// 一侧的表情回应
    /// </summary>
    public ISugarQueryable<SysChatMessageReaction> Reactions(string side) => rows.Records<SysChatMessageReaction>(side);

    /// <summary>
    /// 一侧的通话
    /// </summary>
    public ISugarQueryable<SysChatCall> Calls(string side) => rows.Query<SysChatCall>(side);

    /// <summary>
    /// 一侧的通话参与者
    /// </summary>
    public ISugarQueryable<SysChatCallParticipant> CallParticipants(string side) => rows.Query<SysChatCallParticipant>(side);

    /// <summary>
    /// 一个会话
    /// </summary>
    /// <remarks>
    /// 不存在为失败
    /// </remarks>
    public async Task<Result<SysChatConversation>> ConversationAsync(string side, Guid conversationId, CancellationToken cancellationToken)
        => (await rows.FindAsync<SysChatConversation>(side, conversationId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var conversation)
            ? conversation
            : Error.NotFound(ChatErrors.ConversationNotFound, ChatMessages.ConversationNotFound, MessageArguments.Of(("id", conversationId)));

    /// <summary>
    /// 会话的全部成员
    /// </summary>
    public Task<List<SysChatConversationMember>> MembersAsync(string side, Guid conversationId, CancellationToken cancellationToken)
        => Members(side).Where(m => m.ConversationId == conversationId).ToListAsync(cancellationToken);

    /// <summary>
    /// 会话全部成员的用户
    /// </summary>
    public async Task<List<Guid>> MemberIdsAsync(string side, Guid conversationId, CancellationToken cancellationToken)
        => await Members(side).Where(m => m.ConversationId == conversationId).Select(static m => m.UserId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 一个成员
    /// </summary>
    public async Task<SysChatConversationMember?> MemberAsync(string side, Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var found = await Members(side).Where(m => m.ConversationId == conversationId && m.UserId == userId).Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return found.Count == 0 ? null : found[0];
    }

    /// <summary>
    /// 要求是成员
    /// </summary>
    public async Task<Result<SysChatConversationMember>> RequireMemberAsync(string side, Guid conversationId, Guid userId, CancellationToken cancellationToken)
        => await MemberAsync(side, conversationId, userId, cancellationToken).ConfigureAwait(false) is { } member
            ? member
            : Error.Denied(ChatErrors.NotMember, ChatMessages.NotMember, MessageArguments.Of(("conversationId", conversationId)));

    /// <summary>
    /// 要求是群主或管理员
    /// </summary>
    public async Task<Result<SysChatConversationMember>> RequireManagerAsync(string side, Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await RequireMemberAsync(side, conversationId, userId, cancellationToken).ConfigureAwait(false);
        if (!member.TryGetValue(out var found))
        {
            return member;
        }

        return found.Role is ChatMemberRole.Owner or ChatMemberRole.Admin
            ? found
            : Error.Denied(ChatErrors.ManagerRequired, ChatMessages.ManagerRequired);
    }

    /// <summary>
    /// 一条消息
    /// </summary>
    public async Task<SysChatMessage?> MessageAsync(string side, Guid messageId, CancellationToken cancellationToken)
    {
        var found = await Messages(side).Where(m => m.Id == messageId).Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return found.Count == 0 ? null : found[0];
    }

    /// <summary>
    /// 要求消息存在
    /// </summary>
    public async Task<Result<SysChatMessage>> RequireMessageAsync(string side, Guid messageId, CancellationToken cancellationToken)
        => await MessageAsync(side, messageId, cancellationToken).ConfigureAwait(false) is { } message
            ? message
            : Error.NotFound(ChatErrors.MessageNotFound, ChatMessages.MessageNotFound, MessageArguments.Of(("id", messageId)));

    /// <summary>
    /// 一批消息的表情回应（按回应时间）
    /// </summary>
    public async Task<ILookup<Guid, ChatReactionItem>> ReactionsAsync(string side, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return Array.Empty<SysChatMessageReaction>().ToLookup(static r => r.MessageId, static r => new ChatReactionItem(r.Emoji, r.UserId, r.UserName));
        }

        var ids = messageIds.ToList();
        var found = await Reactions(side).Where(r => ids.Contains(r.MessageId)).OrderBy(r => r.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        return found.ToLookup(static r => r.MessageId, static r => new ChatReactionItem(r.Emoji, r.UserId, r.UserName));
    }

    /// <summary>
    /// 对会话行加一取得下一个消息序号，再读回会话
    /// </summary>
    /// <remarks>
    /// 更新语句拿着会话行的写锁直到提交，同一会话的发送因此串行，序号不重不漏；读回在加锁之后，拿到的是最新的会话
    /// </remarks>
    public async Task<Result<(SysChatConversation Conversation, long Seq)>> NextSeqAsync(string side, Guid conversationId, CancellationToken cancellationToken)
    {
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysChatConversation)).Updateable<SysChatConversation>()
            .SetColumns(c => new SysChatConversation { LastSeq = c.LastSeq + 1 })
            .Where(c => c.Id == conversationId && c.TenantId == side)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        var reloaded = await ConversationAsync(side, conversationId, cancellationToken).ConfigureAwait(false);
        return reloaded.TryGetValue(out var conversation) ? (conversation, conversation.LastSeq) : reloaded.Error!;
    }

    /// <summary>
    /// 对会话行加锁直到提交
    /// </summary>
    /// <remarks>
    /// 一条不改值的更新语句拿着会话行的写锁：同一会话里要排先后的改动（通话的发起、接听、挂断）先调它，再读再改
    /// </remarks>
    public async Task LockConversationAsync(string side, Guid conversationId, CancellationToken cancellationToken)
    {
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysChatConversation)).Updateable<SysChatConversation>()
            .SetColumns(c => new SysChatConversation { LastSeq = c.LastSeq })
            .Where(c => c.Id == conversationId && c.TenantId == side)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 给发送人之外的成员的未读数加一
    /// </summary>
    public async Task IncrementUnreadAsync(string side, Guid conversationId, Guid senderUserId, CancellationToken cancellationToken)
    {
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysChatConversationMember)).Updateable<SysChatConversationMember>()
            .SetColumns(m => new SysChatConversationMember { UnreadCount = m.UnreadCount + 1 })
            .Where(m => m.ConversationId == conversationId && m.TenantId == side && m.UserId != senderUserId && !m.IsDeleted)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 给指定的几位成员的未读数加一
    /// </summary>
    public async Task IncrementUnreadForAsync(string side, Guid conversationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        if (userIds.Count == 0)
        {
            return;
        }

        var ids = userIds.ToList();
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysChatConversationMember)).Updateable<SysChatConversationMember>()
            .SetColumns(m => new SysChatConversationMember { UnreadCount = m.UnreadCount + 1 })
            .Where(m => m.ConversationId == conversationId && m.TenantId == side && ids.Contains(m.UserId) && !m.IsDeleted)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 整行更新一条消息（消息只追加、不软删除，撤回、编辑与置顶改它自己的列）
    /// </summary>
    public async Task UpdateMessageAsync(SysChatMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        message.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await unitOfWork.ConnectionFor(typeof(SysChatMessage)).Updateable(message).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 删一条表情回应（物理删除）
    /// </summary>
    public async Task DeleteReactionAsync(SysChatMessageReaction reaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reaction);
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        var id = reaction.Id;
        var side = reaction.TenantId;
        await unitOfWork.ConnectionFor(typeof(SysChatMessageReaction)).Deleteable<SysChatMessageReaction>().Where(r => r.Id == id && r.TenantId == side).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 一批用户（平台库）
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, SysUser>> UsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Where(static id => id != Guid.Empty).Distinct().ToList();
        return ids.Count == 0 ? new Dictionary<Guid, SysUser>() : (await users.FindManyAsync(ids, cancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
    }

    /// <summary>
    /// 一侧的租户成员
    /// </summary>
    /// <remarks>
    /// 平台没有租户成员
    /// </remarks>
    public async Task<HashSet<Guid>> TenantMembersAsync(string side, CancellationToken cancellationToken)
    {
        var members = new HashSet<Guid>();
        if (!string.Equals(side, TenantIds.Platform, StringComparison.Ordinal))
        {
            foreach (var resolver in memberships)
            {
                members.UnionWith(await resolver.MembersOfAsync(side, cancellationToken).ConfigureAwait(false));
            }
        }

        return members;
    }

    /// <summary>
    /// 核对用户都存在、都属于当前一侧
    /// </summary>
    /// <remarks>
    /// 平台一侧只收注册在平台的账号，租户一侧收注册在该租户的用户与租户成员（成员行记的是建行时的一侧，不拦就会建出在对方那一侧看不到的会话）
    /// </remarks>
    public async Task<Result<IReadOnlyDictionary<Guid, SysUser>>> RequireUsersInSideAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var found = await UsersAsync(userIds, cancellationToken).ConfigureAwait(false);
        foreach (var id in userIds)
        {
            if (!found.ContainsKey(id))
            {
                return Error.NotFound(ChatErrors.UserNotFound, ChatMessages.UserNotFound, MessageArguments.Of(("id", id)));
            }
        }

        var members = await TenantMembersAsync(side, cancellationToken).ConfigureAwait(false);
        return found.Values.Any(u => !string.Equals(u.HomeTenantId, side, StringComparison.Ordinal) && !members.Contains(u.Id))
            ? Error.Validation(ChatErrors.UsersOutsideSide, ChatMessages.UsersOutsideSide)
            : Result.Success(found);
    }

    /// <summary>
    /// 消息投影
    /// </summary>
    public static ChatMessageItem ItemOf(SysChatMessage message, IEnumerable<ChatReactionItem>? reactions = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new ChatMessageItem(
            message.Id,
            message.ConversationId,
            message.Seq,
            message.SenderUserId,
            message.SenderName,
            message.Type,
            message.Content,
            ChatAttachmentsJson.Deserialize(message.Attachments),
            message.IsRecalled,
            message.ClientMessageId,
            message.CreatedAt,
            message.ReplyToMessageId,
            message.ReplyPreview,
            message.EditedAt,
            ChatAttachmentsJson.MentionsOf(message.MentionedUserIds),
            message.IsPinned,
            reactions?.ToList() ?? []);
    }
}
