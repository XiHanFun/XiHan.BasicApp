// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.Framework.Bot.Telegram.Abstractions;
using XiHan.Framework.Bot.Telegram.Stores;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS Telegram Update 幂等去重器（分布式实现，覆盖框架进程内默认实现）
/// </summary>
/// <remarks>
/// 框架缓存抽象 <c>IDistributedCache&lt;,&gt;</c> 无原子 set-if-not-exists 语义，
/// 故直接使用 <see cref="IConnectionMultiplexer"/> 以 <c>SET NX + TTL</c>（30 分钟）实现跨实例原子占位；
/// 取消处理时 <c>TryUnmark</c> 删除占位键，保证 at-least-once（允许重发/重投后重新处理）。
/// <b>Redis 未启用时构造期探测回退框架进程内实现（多实例部署必须启用 Redis，否则跨实例重复投递无法拦截）。</b>
/// </remarks>
public sealed class SaasTelegramUpdateDeduplicator : ITelegramUpdateDeduplicator
{
    /// <summary>
    /// 幂等占位键存活时长（Telegram Webhook 重发/轮询重投窗口远小于该值）
    /// </summary>
    private static readonly TimeSpan EntryTtl = TimeSpan.FromMinutes(30);

    private readonly IConnectionMultiplexer? _redis;
    private readonly InMemoryTelegramUpdateDeduplicator? _fallback;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器（<see cref="IConnectionMultiplexer"/> 仅在启用 Redis 时注册，故可选解析）</param>
    /// <param name="logger">日志记录器</param>
    public SaasTelegramUpdateDeduplicator(
        IServiceProvider serviceProvider,
        ILogger<SaasTelegramUpdateDeduplicator> logger)
    {
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
        if (_redis is null)
        {
            _fallback = new InMemoryTelegramUpdateDeduplicator();
            logger.LogWarning("Redis 未启用，Telegram Update 去重回退进程内实现；多实例部署必须启用 Redis，否则跨实例重复投递无法拦截");
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkProcessedAsync(string botName, int updateId, CancellationToken cancellationToken = default)
    {
        if (_redis is null)
        {
            return await _fallback!.TryMarkProcessedAsync(botName, updateId, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // SET NX + TTL：原子占位，true=首次处理，false=重复投递
        var db = _redis.GetDatabase();
        return await db.StringSetAsync(BuildKey(botName, updateId), "1", EntryTtl, When.NotExists);
    }

    /// <inheritdoc />
    public async Task TryUnmarkAsync(string botName, int updateId, CancellationToken cancellationToken = default)
    {
        if (_redis is null)
        {
            await _fallback!.TryUnmarkAsync(botName, updateId, cancellationToken);
            return;
        }

        var db = _redis.GetDatabase();
        _ = await db.KeyDeleteAsync(BuildKey(botName, updateId));
    }

    /// <summary>
    /// 构造幂等占位键（使用机器人名称而非 Token，避免 Token 泄漏面）
    /// </summary>
    private static string BuildKey(string botName, int updateId)
    {
        return $"{SaasCacheNames.TelegramUpdateDedup}:{botName}:{updateId}";
    }
}
