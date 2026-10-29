// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 机器人渠道的广播：给本侧每个服务商的缺省机器人各写一条排队中的消息，平台一侧另给每个启用的 Telegram 机器人的每个广播群各写一条
/// （在调用的工作单元里，随业务一起提交），再唤醒发送循环。只用本侧的机器人（租户的通知不进平台的群，Telegram 只在平台，4.x 同）。
/// </summary>
/// <param name="bots">机器人配置服务。</param>
/// <param name="telegram">Telegram 机器人服务。</param>
/// <param name="messages">机器人消息仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="options">选项。</param>
/// <param name="signal">唤醒信号。</param>
public sealed class BotBroadcaster(BotConfigService bots, TelegramBotService telegram, IRepository<SysBotMessage, Guid> messages, IIdGenerator<Guid> ids, NotificationOptions options, MessageDispatchSignal signal)
{
    /// <summary>
    /// 当前一侧有没有可用的机器人。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>是否有。</returns>
    public async ValueTask<bool> CanBroadcastAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        return (await bots.DefaultsAsync(owner, context.CancellationToken).ConfigureAwait(false)).Count > 0
            || (IsPlatform(owner) && (await telegram.BroadcastTargetsAsync(context.CancellationToken).ConfigureAwait(false)).Count > 0);
    }

    /// <summary>
    /// 排队一条纯文本消息到本侧每个服务商的缺省机器人。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="text">正文。</param>
    /// <param name="businessType">关联的业务类型。</param>
    /// <param name="businessId">关联的业务 ID。</param>
    /// <returns>排队的条数。</returns>
    public async ValueTask<Result<int>> EnqueueAsync(InvocationContext context, string text, string? businessType, string? businessId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(text);
        var owner = SideRows.SideOf(context);
        var sender = Guid.TryParse(context.GetSecurity().Principal.Id, out var caller) ? caller : (Guid?)null;
        var targets = (await bots.DefaultsAsync(owner, context.CancellationToken).ConfigureAwait(false))
            .Select(static b => ((BotChannel)(int)b.Provider, b.Id, (string?)null))
            .ToList();
        if (IsPlatform(owner))
        {
            targets.AddRange((await telegram.BroadcastTargetsAsync(context.CancellationToken).ConfigureAwait(false)).Select(static t => (BotChannel.Telegram, t.Bot.Id, (string?)t.ChatId)));
        }

        foreach (var (channel, configId, chatId) in targets)
        {
            var added = await messages.AddAsync(new SysBotMessage
            {
                Id = ids.NewId(),
                OwnerTenantId = owner,
                SenderId = sender,
                Channel = channel,
                ChatId = chatId,
                ConfigId = configId,
                Content = text,
                MaxAttempts = options.MaxAttempts,
                BusinessType = businessType,
                BusinessId = businessId,
            }, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added.Error!;
            }
        }

        if (targets.Count > 0)
        {
            signal.Pulse();
        }

        return targets.Count;
    }

    private static bool IsPlatform(string owner) => string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal);
}
