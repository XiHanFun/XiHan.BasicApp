#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasTelegramBotConfigStore
// Guid:ba62adf7-3ae3-4f37-95c1-573dc19b2e7a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Bot.Telegram.Abstractions;
using XiHan.Framework.Bot.Telegram.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS Telegram 机器人配置存储（数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 读取全部「启用」的 <c>SysTelegramBot</c>（含租户过滤）并解密 Token，
/// 映射为框架自有模型 <see cref="TelegramBotConfig"/> 列表（Id=BasicId，逗号串解析为数组）；
/// Token 解密失败或列表项解析失败的行/项跳过并记日志（fail-closed，不拉起可疑机器人）。
/// 结果按平台设置 <c>ConfigCacheSeconds</c> 做进程内 TTL 缓存，缓解管理器轮询压力。
/// Singleton 生命周期，Scoped 仓储经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public sealed class SaasTelegramBotConfigStore : ITelegramBotConfigStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotTokenProtector _tokenProtector;
    private readonly ITelegramBotSettingsStore _settingsStore;
    private readonly ILogger<SaasTelegramBotConfigStore> _logger;

    private CacheEntry? _cache;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 仓储）</param>
    /// <param name="tokenProtector">Telegram 机器人 Token 保护器</param>
    /// <param name="settingsStore">平台全局设置存储（读取缓存秒数）</param>
    /// <param name="logger">日志记录器</param>
    public SaasTelegramBotConfigStore(
        IServiceScopeFactory scopeFactory,
        ITelegramBotTokenProtector tokenProtector,
        ITelegramBotSettingsStore settingsStore,
        ILogger<SaasTelegramBotConfigStore> logger)
    {
        _scopeFactory = scopeFactory;
        _tokenProtector = tokenProtector;
        _settingsStore = settingsStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TelegramBotConfig>> GetBotConfigsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.GetSettingsAsync(cancellationToken);
        var cacheSeconds = Math.Max(0, settings.ConfigCacheSeconds);

        var cached = _cache;
        if (cached is not null && cacheSeconds > 0
            && DateTimeOffset.UtcNow - cached.CachedAt < TimeSpan.FromSeconds(cacheSeconds))
        {
            return cached.Configs;
        }

        List<SysTelegramBot> bots;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ITelegramBotRepository>();
            bots = await repository.GetEnabledListAsync(cancellationToken);
        }

        var configs = new List<TelegramBotConfig>(bots.Count);
        foreach (var bot in bots)
        {
            string? token;
            try
            {
                token = _tokenProtector.Unprotect(bot.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram 机器人 Token 解密失败，跳过该机器人。BotName={BotName}", bot.BotName);
                continue;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Telegram 机器人 Token 为空，跳过该机器人。BotName={BotName}", bot.BotName);
                continue;
            }

            configs.Add(new TelegramBotConfig
            {
                Id = bot.BasicId,
                Name = bot.BotName,
                Token = token,
                AdminUsers = ParseLongList(bot.AdminUsers, bot.BotName, "AdminUsers"),
                AllowedGroupChatIds = ParseLongList(bot.AllowedGroupChatIds, bot.BotName, "AllowedGroupChatIds"),
                AllowedCommands = ParseStringList(bot.AllowedCommands),
                EnableFallbackReply = bot.EnableFallbackReply,
                Remark = bot.Remark
            });
        }

        IReadOnlyList<TelegramBotConfig> result = configs;
        _cache = new CacheEntry(result, DateTimeOffset.UtcNow);
        return result;
    }

    /// <summary>
    /// 解析逗号分隔的 long 串（写入侧已校验；此处防御性兜底：非法项跳过并记日志）
    /// </summary>
    private long[] ParseLongList(string? value, string botName, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var items = new List<long>();
        foreach (var segment in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (long.TryParse(segment, out var parsed))
            {
                items.Add(parsed);
            }
            else
            {
                _logger.LogWarning("Telegram 机器人 {FieldName} 含非法项「{Segment}」，已跳过。BotName={BotName}", fieldName, segment, botName);
            }
        }

        return [.. items.Distinct()];
    }

    private static string[] ParseStringList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private sealed record CacheEntry(IReadOnlyList<TelegramBotConfig> Configs, DateTimeOffset CachedAt);
}
