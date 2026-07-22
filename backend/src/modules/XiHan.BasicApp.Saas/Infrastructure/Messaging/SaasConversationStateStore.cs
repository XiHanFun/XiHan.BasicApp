// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.Framework.Bot.Telegram.Abstractions;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS Telegram 会话状态存储（分布式缓存实现，覆盖框架进程内默认实现）
/// </summary>
/// <remarks>
/// 键 = botName:chatId:userId（<see cref="SaasCacheKeys.TelegramConversationState"/>），
/// 多实例部署下任一实例接收 Update 均可取到同一会话的多步交互状态。
/// TTL 由调用方传入（非法值回退 10 分钟，与框架进程内实现一致）。
/// <c>IDistributedCache&lt;,&gt;</c> 为 Singleton，可安全被本 Singleton 持有。
/// </remarks>
public sealed class SaasConversationStateStore : IConversationStateStore
{
    /// <summary>
    /// TTL 非法时的默认存活时长（与框架进程内实现一致）
    /// </summary>
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    private readonly IDistributedCache<SaasTelegramConversationStateCacheItem, string> _stateCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="stateCache">会话状态分布式缓存</param>
    public SaasConversationStateStore(IDistributedCache<SaasTelegramConversationStateCacheItem, string> stateCache)
    {
        _stateCache = stateCache;
    }

    /// <inheritdoc />
    public async Task<ConversationState?> GetAsync(string botName, long chatId, long userId, CancellationToken cancellationToken = default)
    {
        var item = await _stateCache.GetAsync(
            SaasCacheKeys.TelegramConversationState(botName, chatId, userId),
            token: cancellationToken);
        if (item is null)
        {
            return null;
        }

        return new ConversationState
        {
            Step = item.Step,
            Payload = item.Payload,
            CreateTime = item.CreateTime
        };
    }

    /// <inheritdoc />
    public async Task SetAsync(string botName, long chatId, long userId, ConversationState state, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        var effectiveTtl = ttl > TimeSpan.Zero ? ttl : DefaultTtl;
        await _stateCache.SetAsync(
            SaasCacheKeys.TelegramConversationState(botName, chatId, userId),
            new SaasTelegramConversationStateCacheItem
            {
                Step = state.Step,
                Payload = state.Payload,
                CreateTime = state.CreateTime
            },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveTtl
            },
            token: cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string botName, long chatId, long userId, CancellationToken cancellationToken = default)
    {
        await _stateCache.RemoveAsync(
            SaasCacheKeys.TelegramConversationState(botName, chatId, userId),
            token: cancellationToken);
    }
}
