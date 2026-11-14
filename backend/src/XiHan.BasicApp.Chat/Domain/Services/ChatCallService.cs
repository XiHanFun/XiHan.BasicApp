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
/// 只管通话的状态：谁叫了谁、谁在里面、几时结束。媒体走浏览器之间的 WebRTC，信令经聊天 Hub 转发，都不经这里
/// 单聊是一对一通话；群聊与部门群是多人通话：发起时点名邀请、不超过人数上限，没被邀请的成员可以在通话还没结束时加入
/// 状态的改动先对会话行加锁再读再改：同一会话里的发起、接听、拒绝、加入、挂断排成先后，两个人同时互相呼叫只成一通，一个群同一时刻只有一通
/// 一个人同一时刻只在一通电话里：发起时自己不能占着别的通话（受邀或已加入都算），占线与不在线的受邀人不响铃；接听与加入时再核对一次自己没在别的通话里
/// 锁只在会话上：两个不同会话里的人在同一瞬间呼叫同一个人时可能各响一通，先接的成立，另一通到点记未接听
/// 没人接（受邀的都拒绝了或没接到）、通话中只剩一个人且没人还在响铃时结束；结束时往会话里追加一条通话记录消息（没接到的人各记一条未读），再把通话的最后样子推出去
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="messages">消息服务（追加通话记录）</param>
/// <param name="texts">会话预览文案</param>
/// <param name="policy">聊天策略（响铃秒数与人数上限）</param>
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

        if (conversation.Type == ChatConversationType.Assistant)
        {
            return Error.Validation(ChatErrors.CallUnsupported, ChatMessages.CallUnsupported);
        }

        var settings = await policy.ReadAsync(context).ConfigureAwait(false);
        if (!settings.TryGetValue(out var configured))
        {
            return settings.Error!;
        }

        var memberIds = await store.MemberIdsAsync(caller.Side, conversation.Id, cancellationToken).ConfigureAwait(false);
        var isGroup = conversation.Type != ChatConversationType.Single;
        var invited = InviteesOf(caller, request, memberIds, isGroup, configured);
        if (!invited.TryGetValue(out var invitees))
        {
            return invited.Error!;
        }

        var conversationId = conversation.Id;
        await store.LockConversationAsync(caller.Side, conversationId, cancellationToken).ConfigureAwait(false);
        if (isGroup && await store.Calls(caller.Side).Where(c => c.ConversationId == conversationId && c.Status != ChatCallStatus.Ended).AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(ChatErrors.CallInProgress, ChatMessages.CallInProgress);
        }

        var busy = await EngagedAsync(caller.Side, [caller.UserId, .. invitees], cancellationToken).ConfigureAwait(false);
        if (busy.Contains(caller.UserId))
        {
            return Error.Conflict(ChatErrors.CallerBusy, ChatMessages.CallerBusy);
        }

        // 占线的、不在线的不响铃：一对一直接拒绝；多人通话里记成没接到，别人照常响
        var online = await presence.ConnectedUsersAsync(caller.Side, invitees, cancellationToken).ConfigureAwait(false);
        var reachable = invitees.Where(id => !busy.Contains(id) && online.Contains(id)).ToHashSet();
        if (reachable.Count == 0)
        {
            return isGroup
                ? Error.Conflict(ChatErrors.CallNobodyAvailable, ChatMessages.CallNobodyAvailable)
                : busy.Contains(invitees[0])
                    ? Error.Conflict(ChatErrors.CalleeBusy, ChatMessages.CalleeBusy)
                    : Error.Conflict(ChatErrors.CalleeOffline, ChatMessages.CalleeOffline);
        }

        var now = clock.GetUtcNow();
        var call = new SysChatCall
        {
            Id = ids.NewId(),
            TenantId = caller.Side,
            ConversationId = conversationId,
            InitiatorUserId = caller.UserId,
            Media = request.Media,
            Status = ChatCallStatus.Ringing,
            RingExpiresAt = now.AddSeconds(configured.CallRingSeconds),
        };
        await store.Rows.InsertAsync(call, cancellationToken).ConfigureAwait(false);
        List<SysChatCallParticipant> participants =
        [
            new() { Id = ids.NewId(), TenantId = caller.Side, CallId = call.Id, UserId = caller.UserId, Status = ChatCallParticipantStatus.Joined, JoinedAt = now },
            .. invitees.Select(id => new SysChatCallParticipant
            {
                Id = ids.NewId(),
                TenantId = caller.Side,
                CallId = call.Id,
                UserId = id,
                Status = reachable.Contains(id) ? ChatCallParticipantStatus.Invited : ChatCallParticipantStatus.Missed,
            }),
        ];
        await store.Rows.InsertRangeAsync(participants, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(caller.Side, call, conversation, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 接听
    /// </summary>
    public async Task<Result<ChatCallItem>> AcceptAsync(InvocationContext context, ChatCaller caller, Guid callId)
    {
        var loaded = await LoadAsync(caller.Side, callId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!loaded.TryGetValue(out var current))
        {
            return loaded.Error!;
        }

        return current.Call.Status == ChatCallStatus.Ended || current.Mine.Status != ChatCallParticipantStatus.Invited
            ? Error.Conflict(ChatErrors.CallNotRinging, ChatMessages.CallNotRinging)
            : await EnterAsync(context, caller, current.Call, current.Participants, current.Mine).ConfigureAwait(false);
    }

    /// <summary>
    /// 加入一通还没结束的多人通话
    /// </summary>
    /// <remarks>
    /// 给会话里没被邀请、或者先前拒绝了、没接到、离开了的成员；受邀还没接的调它等同接听，已经在里面的原样返回
    /// </remarks>
    public async Task<Result<ChatCallItem>> JoinAsync(InvocationContext context, ChatCaller caller, Guid callId)
    {
        var cancellationToken = context.CancellationToken;
        var opened = await OpenAsync(caller.Side, callId, cancellationToken).ConfigureAwait(false);
        if (!opened.TryGetValue(out var current))
        {
            return opened.Error!;
        }

        var (call, participants) = current;
        var member = await store.RequireMemberAsync(caller.Side, call.ConversationId, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        if (call.Status == ChatCallStatus.Ended)
        {
            return Error.Conflict(ChatErrors.CallEnded, ChatMessages.CallEnded);
        }

        var mine = participants.Find(p => p.UserId == caller.UserId);
        return mine?.Status == ChatCallParticipantStatus.Joined
            ? await ItemOfAsync(caller.Side, call, participants, cancellationToken).ConfigureAwait(false)
            : await EnterAsync(context, caller, call, participants, mine).ConfigureAwait(false);
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
        if (call.Status == ChatCallStatus.Ended || mine.Status != ChatCallParticipantStatus.Invited)
        {
            return Error.Conflict(ChatErrors.CallNotRinging, ChatMessages.CallNotRinging);
        }

        mine.Status = ChatCallParticipantStatus.Rejected;
        return await SettleAsync(context, caller.Side, call, participants, ChatCallEndReason.Completed).ConfigureAwait(false);
    }

    /// <summary>
    /// 挂断
    /// </summary>
    /// <remarks>
    /// 还没人接时发起人挂断，这通电话就没了（到点了才挂的算没人接，否则是取消）；受邀的人挂断即拒绝（到点了算没接到）；
    /// 通话中挂断即离开，剩下的人不够两个、又没人还在响铃时通话随之结束。已经结束的、早就不在里面的照样成功、原样返回
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
            return await ItemOfAsync(caller.Side, call, participants, cancellationToken).ConfigureAwait(false);
        }

        var now = clock.GetUtcNow();
        var expired = now >= call.RingExpiresAt - _ringSlack;
        switch (mine.Status)
        {
            case ChatCallParticipantStatus.Invited:
                mine.Status = expired ? ChatCallParticipantStatus.Missed : ChatCallParticipantStatus.Rejected;
                return await SettleAsync(context, caller.Side, call, participants, ChatCallEndReason.Completed).ConfigureAwait(false);
            case ChatCallParticipantStatus.Joined when call.Status == ChatCallStatus.Ringing:
                return await EndAsync(context, caller.Side, call, participants, expired ? ChatCallEndReason.Missed : ChatCallEndReason.Cancelled).ConfigureAwait(false);
            case ChatCallParticipantStatus.Joined:
                mine.Status = ChatCallParticipantStatus.Left;
                mine.LeftAt = now;
                return await SettleAsync(context, caller.Side, call, participants, ChatCallEndReason.Completed).ConfigureAwait(false);
            default:
                return await ItemOfAsync(caller.Side, call, participants, cancellationToken).ConfigureAwait(false);
        }
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
        return new ChatActiveCallResponse(await ItemOfAsync(caller.Side, call, participants, cancellationToken).ConfigureAwait(false), clock.GetUtcNow());
    }

    /// <summary>
    /// 一个会话里还没结束的那通电话
    /// </summary>
    /// <remarks>
    /// 给会话的成员看：没被邀请的人据此知道有通话在进行、能加入
    /// </remarks>
    public async Task<Result<ChatActiveCallResponse>> OngoingAsync(InvocationContext context, ChatCaller caller, Guid conversationId)
    {
        var cancellationToken = context.CancellationToken;
        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var open = await store.Calls(caller.Side).Where(c => c.ConversationId == conversationId && c.Status != ChatCallStatus.Ended).OrderBy(c => c.CreatedAt).Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (open.Count == 0)
        {
            return new ChatActiveCallResponse(null, clock.GetUtcNow());
        }

        var participants = await ParticipantsAsync(caller.Side, open[0].Id, cancellationToken).ConfigureAwait(false);
        return new ChatActiveCallResponse(await ItemOfAsync(caller.Side, open[0], participants, cancellationToken).ConfigureAwait(false), clock.GetUtcNow());
    }

    /// <summary>
    /// 通话的设置：响铃秒数与人数上限
    /// </summary>
    public async Task<Result<ChatCallSettingsResponse>> SettingsAsync(InvocationContext context)
    {
        var settings = await policy.ReadAsync(context).ConfigureAwait(false);
        return settings.TryGetValue(out var configured)
            ? new ChatCallSettingsResponse(configured.CallRingSeconds, configured.CallMaxVoiceParticipants, configured.CallMaxVideoParticipants)
            : settings.Error!;
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
    /// 信令的去向：这个人是不是正在这通电话里，是的话给出该转给谁
    /// </summary>
    /// <remarks>
    /// 只有接通了的通话才转发信令。指了对象即只转给他（他也得在通话里），没指即转给已经加入的其他人；
    /// 发的人不在通话里、对象不在通话里、通话没接通或已结束都没有去向
    /// </remarks>
    public async Task<IReadOnlyList<Guid>> SignalTargetsAsync(string side, Guid callId, Guid userId, Guid? toUserId, CancellationToken cancellationToken)
    {
        if (!(await store.Rows.FindAsync<SysChatCall>(side, callId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var call) || call.Status != ChatCallStatus.Ongoing)
        {
            return [];
        }

        var joined = (await ParticipantsAsync(side, callId, cancellationToken).ConfigureAwait(false)).Where(static p => p.Status == ChatCallParticipantStatus.Joined).Select(static p => p.UserId).ToList();
        if (!joined.Contains(userId))
        {
            return [];
        }

        return toUserId is { } target
            ? target != userId && joined.Contains(target) ? [target] : []
            : [.. joined.Where(id => id != userId)];
    }

    /// <summary>
    /// 有人离线了：让他离开占着的通话
    /// </summary>
    /// <remarks>
    /// 受邀还没接的记没接到；还没人接时离线的是发起人，这通电话取消；通话中的记离开，剩下的人不够时通话以中断结束
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

            var (call, participants, mine) = current;
            Result<ChatCallItem> outcome;
            if (mine.Status == ChatCallParticipantStatus.Invited)
            {
                mine.Status = ChatCallParticipantStatus.Missed;
                outcome = await SettleAsync(context, side, call, participants, ChatCallEndReason.Completed).ConfigureAwait(false);
            }
            else if (mine.Status != ChatCallParticipantStatus.Joined)
            {
                continue;
            }
            else if (call.Status == ChatCallStatus.Ringing)
            {
                outcome = await EndAsync(context, side, call, participants, ChatCallEndReason.Cancelled).ConfigureAwait(false);
            }
            else
            {
                mine.Status = ChatCallParticipantStatus.Left;
                mine.LeftAt = clock.GetUtcNow();
                outcome = await SettleAsync(context, side, call, participants, ChatCallEndReason.Interrupted).ConfigureAwait(false);
            }

            if (outcome.IsFailure)
            {
                return outcome.Error!;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 收尾一侧没人管的通话
    /// </summary>
    /// <remarks>
    /// 还没人接的：响铃过点记未接听，发起人不在线即取消。通话中的：已经不在线的记离开，过点还挂着或已经不在线的邀请记没接到，剩下的人不够时以中断结束
    /// 正常情况下这些由端上的计时器与离线消息先一步做掉，这里兜的是端崩了、消息丢了的情形
    /// </remarks>
    /// <returns>动过的通话数</returns>
    public async Task<Result<int>> SweepAsync(InvocationContext context, string side)
    {
        var cancellationToken = context.CancellationToken;
        var open = await store.Calls(side).Where(c => c.Status != ChatCallStatus.Ended).ToListAsync(cancellationToken).ConfigureAwait(false);
        var swept = 0;
        foreach (var stale in open)
        {
            var opened = await OpenAsync(side, stale.Id, cancellationToken).ConfigureAwait(false);
            if (!opened.TryGetValue(out var current) || current.Call.Status == ChatCallStatus.Ended)
            {
                continue;
            }

            var (call, participants) = current;
            var now = clock.GetUtcNow();
            var overdue = now >= call.RingExpiresAt + _sweepSlack;
            var waiting = participants.Where(static p => p.Status is ChatCallParticipantStatus.Joined or ChatCallParticipantStatus.Invited).Select(static p => p.UserId).ToList();
            var online = await presence.ConnectedUsersAsync(side, waiting, cancellationToken).ConfigureAwait(false);
            Result<ChatCallItem>? outcome = null;
            if (call.Status == ChatCallStatus.Ringing)
            {
                if (overdue)
                {
                    outcome = await EndAsync(context, side, call, participants, ChatCallEndReason.Missed).ConfigureAwait(false);
                }
                else if (!online.Contains(call.InitiatorUserId))
                {
                    outcome = await EndAsync(context, side, call, participants, ChatCallEndReason.Cancelled).ConfigureAwait(false);
                }
            }
            else
            {
                var changed = false;
                foreach (var participant in participants)
                {
                    if (participant.Status == ChatCallParticipantStatus.Joined && !online.Contains(participant.UserId))
                    {
                        participant.Status = ChatCallParticipantStatus.Left;
                        participant.LeftAt = now;
                        changed = true;
                    }
                    else if (participant.Status == ChatCallParticipantStatus.Invited && (overdue || !online.Contains(participant.UserId)))
                    {
                        participant.Status = ChatCallParticipantStatus.Missed;
                        changed = true;
                    }
                }

                if (changed)
                {
                    outcome = await SettleAsync(context, side, call, participants, ChatCallEndReason.Interrupted).ConfigureAwait(false);
                }
            }

            if (outcome is not { } result)
            {
                continue;
            }

            if (result.IsFailure)
            {
                return result.Error!;
            }

            swept++;
        }

        return swept;
    }

    /// <summary>
    /// 这次发起要邀请的人
    /// </summary>
    /// <remarks>
    /// 单聊就是对方；群聊与部门群取请求里点的名：不能为空、都得是会话成员、连发起人在内不超过这种媒体的人数上限
    /// </remarks>
    private static Result<List<Guid>> InviteesOf(ChatCaller caller, StartChatCallRequest request, List<Guid> memberIds, bool isGroup, ChatPolicySettings settings)
    {
        if (!isGroup)
        {
            var peers = memberIds.Where(id => id != caller.UserId).ToList();
            return peers.Count == 1 ? peers : Error.Validation(ChatErrors.CallUnsupported, ChatMessages.CallUnsupported);
        }

        var invitees = (request.InviteeUserIds ?? []).Where(id => id != Guid.Empty && id != caller.UserId).Distinct().ToList();
        if (invitees.Count == 0)
        {
            return Error.Validation(ChatErrors.CallInviteesRequired, ChatMessages.CallInviteesRequired);
        }

        var members = memberIds.ToHashSet();
        if (!invitees.All(members.Contains))
        {
            return Error.Validation(ChatErrors.CallInviteeNotMember, ChatMessages.CallInviteeNotMember);
        }

        var max = settings.MaxCallParticipants(request.Media);
        return invitees.Count + 1 > max
            ? Error.Validation(ChatErrors.CallTooManyParticipants, ChatMessages.CallTooManyParticipants, MessageArguments.Of(("max", max)))
            : invitees;
    }

    /// <summary>
    /// 进入通话（接听或加入）：核对没在别的通话里、人数没满，再记成已加入
    /// </summary>
    private async Task<Result<ChatCallItem>> EnterAsync(InvocationContext context, ChatCaller caller, SysChatCall call, List<SysChatCallParticipant> participants, SysChatCallParticipant? mine)
    {
        var cancellationToken = context.CancellationToken;
        var (userId, callId) = (caller.UserId, call.Id);
        if (await store.CallParticipants(caller.Side).Where(p => p.UserId == userId && p.CallId != callId && p.Status == ChatCallParticipantStatus.Joined).AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(ChatErrors.CallerBusy, ChatMessages.CallerBusy);
        }

        var settings = await policy.ReadAsync(context).ConfigureAwait(false);
        if (!settings.TryGetValue(out var configured))
        {
            return settings.Error!;
        }

        if (participants.Count(static p => p.Status == ChatCallParticipantStatus.Joined) >= configured.MaxCallParticipants(call.Media))
        {
            return Error.Conflict(ChatErrors.CallFull, ChatMessages.CallFull);
        }

        var now = clock.GetUtcNow();
        if (mine is null)
        {
            mine = new SysChatCallParticipant { Id = ids.NewId(), TenantId = caller.Side, CallId = callId, UserId = userId, Status = ChatCallParticipantStatus.Joined, JoinedAt = now };
            await store.Rows.InsertAsync(mine, cancellationToken).ConfigureAwait(false);
            participants.Add(mine);
        }
        else
        {
            mine.Status = ChatCallParticipantStatus.Joined;
            mine.JoinedAt = now;
            mine.LeftAt = null;
            await store.Rows.UpdateAsync(mine, cancellationToken).ConfigureAwait(false);
        }

        if (call.Status == ChatCallStatus.Ringing)
        {
            call.Status = ChatCallStatus.Ongoing;
            call.AnsweredAt = now;
            await store.Rows.UpdateAsync(call, cancellationToken).ConfigureAwait(false);
        }

        var conversation = await ConversationOfAsync(caller.Side, call.ConversationId, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(caller.Side, call, conversation, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 有人的状态变了之后看这通电话还成不成立
    /// </summary>
    /// <remarks>
    /// 响铃过点还挂着的邀请顺手记成没接到。还没人接：受邀的都不来了即结束（都是拒绝的记被拒绝，否则记未接听）；
    /// 通话中：在里面的不够两个、又没人还在响铃即以 <paramref name="endReason"/> 结束。还成立的把参与者写回并推出去
    /// </remarks>
    private async Task<Result<ChatCallItem>> SettleAsync(InvocationContext context, string side, SysChatCall call, List<SysChatCallParticipant> participants, ChatCallEndReason endReason)
    {
        var cancellationToken = context.CancellationToken;
        if (clock.GetUtcNow() >= call.RingExpiresAt + _ringSlack)
        {
            foreach (var participant in participants.Where(static p => p.Status == ChatCallParticipantStatus.Invited))
            {
                participant.Status = ChatCallParticipantStatus.Missed;
            }
        }

        var joined = participants.Count(static p => p.Status == ChatCallParticipantStatus.Joined);
        var ringing = participants.Any(static p => p.Status == ChatCallParticipantStatus.Invited);
        if (call.Status == ChatCallStatus.Ringing)
        {
            if (joined == 0)
            {
                return await EndAsync(context, side, call, participants, ChatCallEndReason.Cancelled).ConfigureAwait(false);
            }

            if (!ringing)
            {
                var declined = participants.Any(static p => p.Status == ChatCallParticipantStatus.Rejected) && participants.TrueForAll(static p => p.Status != ChatCallParticipantStatus.Missed);
                return await EndAsync(context, side, call, participants, declined ? ChatCallEndReason.Rejected : ChatCallEndReason.Missed).ConfigureAwait(false);
            }
        }
        else if (joined < 2 && !ringing)
        {
            return await EndAsync(context, side, call, participants, endReason).ConfigureAwait(false);
        }

        await store.Rows.UpdateRangeAsync(participants, cancellationToken).ConfigureAwait(false);
        var conversation = await ConversationOfAsync(side, call.ConversationId, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(side, call, conversation, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 取一通电话与它的参与者（先对会话行加锁）
    /// </summary>
    private async Task<Result<(SysChatCall Call, List<SysChatCallParticipant> Participants)>> OpenAsync(string side, Guid callId, CancellationToken cancellationToken)
    {
        if (!(await store.Rows.FindAsync<SysChatCall>(side, callId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var seen))
        {
            return Error.NotFound(ChatErrors.CallNotFound, ChatMessages.CallNotFound);
        }

        // 加锁之后重读：等锁的时候别的调用可能已经改了它
        await store.LockConversationAsync(side, seen.ConversationId, cancellationToken).ConfigureAwait(false);
        return (await store.Rows.FindAsync<SysChatCall>(side, callId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var call)
            ? (call, await ParticipantsAsync(side, callId, cancellationToken).ConfigureAwait(false))
            : Error.NotFound(ChatErrors.CallNotFound, ChatMessages.CallNotFound);
    }

    /// <summary>
    /// 取一通电话与调用方在里面的那一行（先对会话行加锁）
    /// </summary>
    private async Task<Result<(SysChatCall Call, List<SysChatCallParticipant> Participants, SysChatCallParticipant Mine)>> LoadAsync(string side, Guid callId, Guid userId, CancellationToken cancellationToken)
    {
        var opened = await OpenAsync(side, callId, cancellationToken).ConfigureAwait(false);
        if (!opened.TryGetValue(out var current))
        {
            return opened.Error!;
        }

        return current.Participants.Find(p => p.UserId == userId) is { } mine
            ? (current.Call, current.Participants, mine)
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
        // 没接到的人各记一条未读：他们没看见这通电话
        List<Guid> missed = [.. participants.Where(static p => p.Status == ChatCallParticipantStatus.Missed).Select(static p => p.UserId)];
        var content = JsonSerializer.Serialize(new ChatCallRecord(call.Id, call.Media, reason, duration, missed), ChatJsonContext.Default.ChatCallRecord);
        var appended = await messages.AppendCallAsync(context, side, call.ConversationId, call.InitiatorUserId, initiator?.UserName, content, texts.CallPreview(context, call.Media), missed).ConfigureAwait(false);
        if (!appended.TryGetValue(out var record))
        {
            return appended.Error!;
        }

        call.MessageId = record.Message.Id;
        await store.Rows.UpdateAsync(call, cancellationToken).ConfigureAwait(false);
        await store.Rows.UpdateRangeAsync(participants, cancellationToken).ConfigureAwait(false);
        var members = await store.MemberIdsAsync(side, call.ConversationId, cancellationToken).ConfigureAwait(false);
        await push.MessageAsync(side, ChatStore.ItemOf(record.Message), record.Conversation, members, cancellationToken).ConfigureAwait(false);
        return await PublishAsync(side, call, record.Conversation, participants, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 这些人里此刻占着通话的（受邀或已加入）
    /// </summary>
    private async Task<HashSet<Guid>> EngagedAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var candidates = userIds.ToList();
        var found = await store.CallParticipants(side)
            .Where(p => candidates.Contains(p.UserId) && (p.Status == ChatCallParticipantStatus.Invited || p.Status == ChatCallParticipantStatus.Joined))
            .Select(static p => p.UserId).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. found];
    }

    private Task<List<SysChatCallParticipant>> ParticipantsAsync(string side, Guid callId, CancellationToken cancellationToken)
        => store.CallParticipants(side).Where(p => p.CallId == callId).ToListAsync(cancellationToken);

    /// <summary>
    /// 通话所在的会话
    /// </summary>
    /// <remarks>
    /// 会话不删，通话的会话找不到是缺陷，直接抛出
    /// </remarks>
    private async Task<SysChatConversation> ConversationOfAsync(string side, Guid conversationId, CancellationToken cancellationToken)
        => (await store.Rows.FindAsync<SysChatConversation>(side, conversationId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var conversation)
            ? conversation
            : throw new InvalidOperationException($"Chat conversation '{conversationId}' of a call is missing.");

    /// <summary>
    /// 把通话的样子推出去并返回
    /// </summary>
    /// <remarks>
    /// 参与者按人点发；多人通话另发给正开着这个会话的连接
    /// </remarks>
    private async Task<ChatCallItem> PublishAsync(string side, SysChatCall call, SysChatConversation conversation, IReadOnlyList<SysChatCallParticipant> participants, CancellationToken cancellationToken)
    {
        var item = await ItemOfAsync(call, conversation, participants, cancellationToken).ConfigureAwait(false);
        await push.CallChangedAsync(side, item, [.. participants.Select(static p => p.UserId)], conversation.Type == ChatConversationType.Single ? null : conversation.Id, cancellationToken).ConfigureAwait(false);
        return item;
    }

    private async Task<ChatCallItem> ItemOfAsync(string side, SysChatCall call, IReadOnlyList<SysChatCallParticipant> participants, CancellationToken cancellationToken)
        => await ItemOfAsync(call, await ConversationOfAsync(side, call.ConversationId, cancellationToken).ConfigureAwait(false), participants, cancellationToken).ConfigureAwait(false);

    private async Task<ChatCallItem> ItemOfAsync(SysChatCall call, SysChatConversation conversation, IReadOnlyList<SysChatCallParticipant> participants, CancellationToken cancellationToken)
    {
        var users = await store.UsersAsync([.. participants.Select(static p => p.UserId)], cancellationToken).ConfigureAwait(false);
        return new ChatCallItem(
            call.Id,
            call.ConversationId,
            conversation.Type,
            conversation.Type == ChatConversationType.Single ? null : conversation.Name,
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
