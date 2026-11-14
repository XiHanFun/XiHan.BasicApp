// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatCalls;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 通话（作用域）
/// </summary>
/// <remarks>
/// 只管通话的状态：谁叫了谁、接没接、几时结束。媒体走浏览器之间的 WebRTC，信令经聊天 Hub 转发，都不经这里
/// 状态的改动先对会话行加锁再读再改：同一会话里的发起、接听、拒绝、挂断排成先后，两个人同时互相呼叫只成一通，多端同时接听只有一端成功
/// 发起时核对两个人都没占在别的通话里（受邀或已加入都算占着），接听时再核对一次自己没在别的通话里；对方不在线不响铃、直接拒绝
/// 锁只在会话上：两个人在同一瞬间呼叫同一个人时可能各响一通，先接的成立，另一通到点记未接听
/// 结束时往会话里追加一条通话记录消息（没接通的给对方记一条未读），再把通话的最后样子推给参与者
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="messages">消息服务（追加通话记录）</param>
/// <param name="texts">会话预览文案</param>
/// <param name="policy">聊天策略（响铃秒数）</param>
/// <param name="push">推送</param>
/// <param name="options">穿透参数</param>
/// <param name="presence">在线状态</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
public sealed class ChatCallService(
    ChatStore store,
    ChatMessageService messages,
    ChatTexts texts,
    ChatPolicy policy,
    ChatPush push,
    ChatCallOptions options,
    IRealtimePresence presence,
    IIdGenerator<Guid> ids,
    TimeProvider clock)
{
    /// <summary>
    /// 响铃到点前这么久之内挂断也算未接听
    /// </summary>
    /// <remarks>
    /// 响铃到点由两端的计时器来挂，钟差一点不该把「没人接」记成「已取消」或「已拒绝」
    /// </remarks>
    private static readonly TimeSpan _ringSlack = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 响铃过点这么久还没人管，由收尾作业记成未接听
    /// </summary>
    private static readonly TimeSpan _sweepSlack = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 发起通话
    /// </summary>
    public async Task<Result<ChatCallItem>> StartAsync(InvocationContext context, ChatCaller caller, StartChatCallRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cancellationToken = context.CancellationToken;
        var member = await store.RequireMemberAsync(caller.Side, request.ConversationId, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var found = await store.ConversationAsync(caller.Side, request.ConversationId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        var peers = (await store.MemberIdsAsync(caller.Side, conversation.Id, cancellationToken).ConfigureAwait(false)).Where(id => id != caller.UserId).ToList();
        if (conversation.Type != ChatConversationType.Single || peers.Count != 1)
        {
            return Error.Validation(ChatErrors.CallSingleOnly, ChatMessages.CallSingleOnly);
        }

        var settings = await policy.ReadAsync(context).ConfigureAwait(false);
        if (!settings.TryGetValue(out var configured))
        {
            return settings.Error!;
        }

        var peer = peers[0];
        await store.LockConversationAsync(caller.Side, conversation.Id, cancellationToken).ConfigureAwait(false);
        if (await EngagedAsync(caller.Side, caller.UserId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Error.Conflict(ChatErrors.CallerBusy, ChatMessages.CallerBusy);
        }

        if (await EngagedAsync(caller.Side, peer, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Error.Conflict(ChatErrors.CalleeBusy, ChatMessages.CalleeBusy);
        }

        if (!(await presence.ConnectedUsersAsync(caller.Side, [peer], cancellationToken).ConfigureAwait(false)).Contains(peer))
        {
            return Error.Conflict(ChatErrors.CalleeOffline, ChatMessages.CalleeOffline);
        }

        var now = clock.GetUtcNow();
        var call = new SysChatCall
        {
            Id = ids.NewId(),
            TenantId = caller.Side,
            ConversationId = conversation.Id,
            InitiatorUserId = caller.UserId,
            Media = request.Media,
            Status = ChatCallStatus.Ringing,
            RingExpiresAt = now.AddSeconds(configured.CallRingSeconds),
        };
        await store.Rows.InsertAsync(call, cancellationToken).ConfigureAwait(false);
        List<SysChatCallParticipant> participants =
        [
            new() { Id = ids.NewId(), TenantId = caller.Side, CallId = call.Id, UserId = caller.UserId, Status = ChatCallParticipantStatus.Joined, JoinedAt = now },
            new() { Id = ids.NewId(), TenantId = caller.Side, CallId = call.Id, UserId = peer, Status = ChatCallParticipantStatus.Invited },
        ];
        await store.Rows.InsertRangeAsync(participants, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(caller.Side, call, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 接听
    /// </summary>
    public async Task<Result<ChatCallItem>> AcceptAsync(InvocationContext context, ChatCaller caller, Guid callId)
    {
        var cancellationToken = context.CancellationToken;
        var loaded = await LoadAsync(caller.Side, callId, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (!loaded.TryGetValue(out var current))
        {
            return loaded.Error!;
        }

        var (call, participants, mine) = current;
        if (call.Status != ChatCallStatus.Ringing || mine.Status != ChatCallParticipantStatus.Invited)
        {
            return Error.Conflict(ChatErrors.CallNotRinging, ChatMessages.CallNotRinging);
        }

        var userId = caller.UserId;
        if (await store.CallParticipants(caller.Side).Where(p => p.UserId == userId && p.CallId != callId && p.Status == ChatCallParticipantStatus.Joined).AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(ChatErrors.CallerBusy, ChatMessages.CallerBusy);
        }

        var now = clock.GetUtcNow();
        mine.Status = ChatCallParticipantStatus.Joined;
        mine.JoinedAt = now;
        call.Status = ChatCallStatus.Ongoing;
        call.AnsweredAt = now;
        await store.Rows.UpdateAsync(mine, cancellationToken).ConfigureAwait(false);
        await store.Rows.UpdateAsync(call, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(caller.Side, call, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 拒绝
    /// </summary>
    public async Task<Result<ChatCallItem>> RejectAsync(InvocationContext context, ChatCaller caller, Guid callId)
    {
        var loaded = await LoadAsync(caller.Side, callId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!loaded.TryGetValue(out var current))
        {
            return loaded.Error!;
        }

        var (call, participants, mine) = current;
        if (call.Status != ChatCallStatus.Ringing || mine.Status != ChatCallParticipantStatus.Invited)
        {
            return Error.Conflict(ChatErrors.CallNotRinging, ChatMessages.CallNotRinging);
        }

        mine.Status = ChatCallParticipantStatus.Rejected;
        return await EndAsync(context, caller.Side, call, participants, ChatCallEndReason.Rejected).ConfigureAwait(false);
    }

    /// <summary>
    /// 挂断
    /// </summary>
    /// <remarks>
    /// 已经结束的照样成功、原样返回：两边可能同时挂，系统也可能先一步收了尾
    /// </remarks>
    public async Task<Result<ChatCallItem>> HangupAsync(InvocationContext context, ChatCaller caller, Guid callId)
    {
        var cancellationToken = context.CancellationToken;
        var loaded = await LoadAsync(caller.Side, callId, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (!loaded.TryGetValue(out var current))
        {
            return loaded.Error!;
        }

        var (call, participants, mine) = current;
        if (call.Status == ChatCallStatus.Ended)
        {
            return await ItemOfAsync(call, participants, cancellationToken).ConfigureAwait(false);
        }

        if (call.Status == ChatCallStatus.Ongoing)
        {
            return await EndAsync(context, caller.Side, call, participants, ChatCallEndReason.Completed).ConfigureAwait(false);
        }

        // 响铃中：到点了才挂的算没人接（两端的计时器都会来挂）；没到点时受邀的人挂断即拒绝，发起人挂断即取消
        if (clock.GetUtcNow() >= call.RingExpiresAt - _ringSlack)
        {
            return await EndAsync(context, caller.Side, call, participants, ChatCallEndReason.Missed).ConfigureAwait(false);
        }

        if (mine.UserId == call.InitiatorUserId)
        {
            return await EndAsync(context, caller.Side, call, participants, ChatCallEndReason.Cancelled).ConfigureAwait(false);
        }

        mine.Status = ChatCallParticipantStatus.Rejected;
        return await EndAsync(context, caller.Side, call, participants, ChatCallEndReason.Rejected).ConfigureAwait(false);
    }

    /// <summary>
    /// 我此刻所在的通话
    /// </summary>
    public async Task<ChatActiveCallResponse> ActiveAsync(InvocationContext context, ChatCaller caller)
    {
        var cancellationToken = context.CancellationToken;
        var userId = caller.UserId;
        var engaged = await store.CallParticipants(caller.Side)
            .Where(p => p.UserId == userId && (p.Status == ChatCallParticipantStatus.Invited || p.Status == ChatCallParticipantStatus.Joined))
            .Select(static p => p.CallId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (engaged.Count == 0)
        {
            return new ChatActiveCallResponse(null, clock.GetUtcNow());
        }

        // 正常只有一通；同一瞬间被两个人呼叫时可能各响一通，先给已经接通的，再给最早的
        var open = await store.Calls(caller.Side).Where(c => engaged.Contains(c.Id) && c.Status != ChatCallStatus.Ended).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (open.OrderByDescending(static c => c.Status == ChatCallStatus.Ongoing).ThenBy(static c => c.CreatedAt).FirstOrDefault() is not { } call)
        {
            return new ChatActiveCallResponse(null, clock.GetUtcNow());
        }

        var participants = await ParticipantsAsync(caller.Side, call.Id, cancellationToken).ConfigureAwait(false);
        return new ChatActiveCallResponse(await ItemOfAsync(call, participants, cancellationToken).ConfigureAwait(false), clock.GetUtcNow());
    }

    /// <summary>
    /// 给调用方的 ICE 服务器
    /// </summary>
    public ChatIceServersResponse IceServers(ChatCaller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        return options.IceServersFor(caller.UserId, clock.GetUtcNow());
    }

    /// <summary>
    /// 信令的去向：这个人是不是正在这通电话里，是的话给出通话里的其他人
    /// </summary>
    /// <remarks>
    /// 只有接通了的通话才转发信令，转给已经加入的其他人；不是参与者、通话没接通或已结束都没有去向
    /// </remarks>
    public async Task<IReadOnlyList<Guid>> SignalTargetsAsync(string side, Guid callId, Guid userId, CancellationToken cancellationToken)
    {
        if (!(await store.Rows.FindAsync<SysChatCall>(side, callId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var call) || call.Status != ChatCallStatus.Ongoing)
        {
            return [];
        }

        var joined = (await ParticipantsAsync(side, callId, cancellationToken).ConfigureAwait(false)).Where(static p => p.Status == ChatCallParticipantStatus.Joined).Select(static p => p.UserId).ToList();
        return joined.Contains(userId) ? [.. joined.Where(id => id != userId)] : [];
    }

    /// <summary>
    /// 有人离线了：结束他占着的通话
    /// </summary>
    /// <remarks>
    /// 响铃中：他是发起人即取消，是受邀的人即未接听；通话中即中断
    /// </remarks>
    public async Task<Result> EndForOfflineAsync(InvocationContext context, string side, Guid userId)
    {
        var cancellationToken = context.CancellationToken;
        var engaged = await store.CallParticipants(side)
            .Where(p => p.UserId == userId && (p.Status == ChatCallParticipantStatus.Invited || p.Status == ChatCallParticipantStatus.Joined))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var row in engaged)
        {
            var loaded = await LoadAsync(side, row.CallId, userId, cancellationToken).ConfigureAwait(false);
            if (!loaded.TryGetValue(out var current) || current.Call.Status == ChatCallStatus.Ended)
            {
                continue;
            }

            var reason = current.Call.Status == ChatCallStatus.Ongoing
                ? ChatCallEndReason.Interrupted
                : current.Call.InitiatorUserId == userId ? ChatCallEndReason.Cancelled : ChatCallEndReason.Missed;
            var ended = await EndAsync(context, side, current.Call, current.Participants, reason).ConfigureAwait(false);
            if (ended.IsFailure)
            {
                return ended.Error!;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 收尾一侧没人管的通话
    /// </summary>
    /// <remarks>
    /// 响铃过点的记未接听；发起人不在线的响铃取消；通话中有人不在线的中断。正常情况下这些由端上的计时器与离线消息先一步做掉，这里兜的是端崩了、消息丢了的情形
    /// </remarks>
    /// <returns>收掉的通话数</returns>
    public async Task<Result<int>> SweepAsync(InvocationContext context, string side)
    {
        var cancellationToken = context.CancellationToken;
        var open = await store.Calls(side).Where(c => c.Status != ChatCallStatus.Ended).ToListAsync(cancellationToken).ConfigureAwait(false);
        var swept = 0;
        foreach (var stale in open)
        {
            var loaded = await LoadAsync(side, stale.Id, stale.InitiatorUserId, cancellationToken).ConfigureAwait(false);
            if (!loaded.TryGetValue(out var current) || current.Call.Status == ChatCallStatus.Ended)
            {
                continue;
            }

            var (call, participants, _) = current;
            var joined = participants.Where(static p => p.Status == ChatCallParticipantStatus.Joined).Select(static p => p.UserId).ToList();
            var online = await presence.ConnectedUsersAsync(side, joined, cancellationToken).ConfigureAwait(false);
            ChatCallEndReason? reason = call.Status switch
            {
                ChatCallStatus.Ringing when clock.GetUtcNow() >= call.RingExpiresAt + _sweepSlack => ChatCallEndReason.Missed,
                ChatCallStatus.Ringing when !online.Contains(call.InitiatorUserId) => ChatCallEndReason.Cancelled,
                ChatCallStatus.Ongoing when online.Count < joined.Count => ChatCallEndReason.Interrupted,
                _ => null,
            };
            if (reason is not { } why)
            {
                continue;
            }

            var ended = await EndAsync(context, side, call, participants, why).ConfigureAwait(false);
            if (ended.IsFailure)
            {
                return ended.Error!;
            }

            swept++;
        }

        return swept;
    }

    /// <summary>
    /// 取一通电话与调用方在里面的那一行（先对会话行加锁）
    /// </summary>
    private async Task<Result<(SysChatCall Call, List<SysChatCallParticipant> Participants, SysChatCallParticipant Mine)>> LoadAsync(string side, Guid callId, Guid userId, CancellationToken cancellationToken)
    {
        if (!(await store.Rows.FindAsync<SysChatCall>(side, callId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var seen))
        {
            return Error.NotFound(ChatErrors.CallNotFound, ChatMessages.CallNotFound);
        }

        // 加锁之后重读：等锁的时候别的调用可能已经改了它
        await store.LockConversationAsync(side, seen.ConversationId, cancellationToken).ConfigureAwait(false);
        if (!(await store.Rows.FindAsync<SysChatCall>(side, callId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var call))
        {
            return Error.NotFound(ChatErrors.CallNotFound, ChatMessages.CallNotFound);
        }

        var participants = await ParticipantsAsync(side, callId, cancellationToken).ConfigureAwait(false);
        return participants.Find(p => p.UserId == userId) is { } mine
            ? (call, participants, mine)
            : Error.Denied(ChatErrors.NotCallParticipant, ChatMessages.NotCallParticipant);
    }

    /// <summary>
    /// 结束通话：定下每个人最后的状态、追加通话记录消息、推送
    /// </summary>
    private async Task<Result<ChatCallItem>> EndAsync(InvocationContext context, string side, SysChatCall call, List<SysChatCallParticipant> participants, ChatCallEndReason reason)
    {
        var cancellationToken = context.CancellationToken;
        var now = clock.GetUtcNow();
        foreach (var participant in participants)
        {
            switch (participant.Status)
            {
                case ChatCallParticipantStatus.Joined:
                    participant.Status = ChatCallParticipantStatus.Left;
                    participant.LeftAt = now;
                    break;
                case ChatCallParticipantStatus.Invited:
                    participant.Status = ChatCallParticipantStatus.Missed;
                    break;
            }
        }

        var duration = call.AnsweredAt is { } answered ? Math.Max(0, (int)(now - answered).TotalSeconds) : 0;
        call.Status = ChatCallStatus.Ended;
        call.EndReason = reason;
        call.EndedAt = now;
        var initiator = (await store.UsersAsync([call.InitiatorUserId], cancellationToken).ConfigureAwait(false)).GetValueOrDefault(call.InitiatorUserId);
        var content = JsonSerializer.Serialize(new ChatCallRecord(call.Id, call.Media, reason, duration), ChatJsonContext.Default.ChatCallRecord);
        // 没接通的给对方记一条未读：他没看见这通电话
        var appended = await messages.AppendCallAsync(context, side, call.ConversationId, call.InitiatorUserId, initiator?.UserName, content, texts.CallPreview(context, call.Media), reason is ChatCallEndReason.Missed or ChatCallEndReason.Cancelled).ConfigureAwait(false);
        if (!appended.TryGetValue(out var record))
        {
            return appended.Error!;
        }

        call.MessageId = record.Message.Id;
        await store.Rows.UpdateAsync(call, cancellationToken).ConfigureAwait(false);
        await store.Rows.UpdateRangeAsync(participants, cancellationToken).ConfigureAwait(false);
        var members = await store.MemberIdsAsync(side, call.ConversationId, cancellationToken).ConfigureAwait(false);
        await push.MessageAsync(side, ChatStore.ItemOf(record.Message), record.Conversation, members, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(side, call, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 这个人此刻占着的通话行（受邀或已加入）
    /// </summary>
    private async Task<SysChatCallParticipant?> EngagedAsync(string side, Guid userId, CancellationToken cancellationToken)
    {
        var found = await store.CallParticipants(side)
            .Where(p => p.UserId == userId && (p.Status == ChatCallParticipantStatus.Invited || p.Status == ChatCallParticipantStatus.Joined))
            .Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return found.Count == 0 ? null : found[0];
    }

    private Task<List<SysChatCallParticipant>> ParticipantsAsync(string side, Guid callId, CancellationToken cancellationToken)
        => store.CallParticipants(side).Where(p => p.CallId == callId).ToListAsync(cancellationToken);

    /// <summary>
    /// 把通话的样子推给参与者并返回
    /// </summary>
    private async Task<ChatCallItem> PublishAsync(string side, SysChatCall call, IReadOnlyList<SysChatCallParticipant> participants, CancellationToken cancellationToken)
    {
        var item = await ItemOfAsync(call, participants, cancellationToken).ConfigureAwait(false);
        await push.CallChangedAsync(side, item, [.. participants.Select(static p => p.UserId)], cancellationToken).ConfigureAwait(false);
        return item;
    }

    private async Task<ChatCallItem> ItemOfAsync(SysChatCall call, IReadOnlyList<SysChatCallParticipant> participants, CancellationToken cancellationToken)
    {
        var users = await store.UsersAsync([.. participants.Select(static p => p.UserId)], cancellationToken).ConfigureAwait(false);
        return new ChatCallItem(
            call.Id,
            call.ConversationId,
            call.InitiatorUserId,
            call.Media,
            call.Status,
            call.EndReason,
            call.CreatedAt,
            call.RingExpiresAt,
            call.AnsweredAt,
            call.EndedAt,
            [.. participants
                .OrderByDescending(p => p.UserId == call.InitiatorUserId)
                .ThenBy(static p => p.UserId)
                .Select(p =>
                {
                    var user = users.GetValueOrDefault(p.UserId);
                    return new ChatCallParticipantItem(p.UserId, user?.UserName, user?.DisplayName, user?.AvatarFileId, p.Status, p.JoinedAt, p.LeftAt);
                })]);
    }
}
