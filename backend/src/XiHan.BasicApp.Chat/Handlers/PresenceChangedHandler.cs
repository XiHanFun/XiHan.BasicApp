// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Handlers;

/// <summary>
/// 有人上线或离线了：把在线状态推给相关的人
/// </summary>
/// <remarks>
/// 订阅 Realtime 的 <see cref="Topics.RealtimePresenceChanged"/>（竞争消费，信封带侧）
/// 消息只说「变了」：在线与否读处理时的状态，乱序或重复投递推出去的都是当时的事实
/// 受众是他的单聊对端（按人点发），以及正开着他所在的群或部门群的连接（会话组）；没参加任何会话的人不推
/// 解不开或读库失败即投递失败、由代理重试
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="presence">在线状态</param>
/// <param name="push">推送</param>
public sealed class PresenceChangedHandler(ChatStore store, IRealtimePresence presence, ChatPush push) : IMessageHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(context);
        var changed = JsonSerializer.Deserialize(delivery.Envelope.Data.Span, ChatJsonContext.Default.PresenceChanged)
            ?? throw new InvalidOperationException($"Message '{delivery.Envelope.Id}' on '{delivery.Topic}' has no data.");
        var side = SideRows.SideOf(context);
        var userId = changed.UserId;
        var joined = await store.Members(side).Where(m => m.UserId == userId).Select(static m => m.ConversationId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (joined.Count == 0)
        {
            return Result.Success();
        }

        var conversations = await store.Conversations(side).Where(c => joined.Contains(c.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var singles = conversations.Where(static c => c.Type == ChatConversationType.Single).Select(static c => c.Id).ToList();
        var groups = conversations.Where(static c => c.Type is ChatConversationType.Group or ChatConversationType.Department).Select(static c => c.Id).ToList();
        var peers = singles.Count == 0
            ? []
            : await store.Members(side).Where(m => singles.Contains(m.ConversationId) && m.UserId != userId).Select(static m => m.UserId).ToListAsync(cancellationToken).ConfigureAwait(false);
        var online = (await presence.ConnectedUsersAsync(side, [userId], cancellationToken).ConfigureAwait(false)).Contains(userId);
        await push.PresenceAsync(side, new ChatPresenceChangedPayload(userId, online), peers, groups, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
