// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.Framework.Bot.Telegram.Abstractions;
using XiHan.Framework.Bot.Telegram.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS Telegram 机器人平台全局设置存储（SysConfig 数据库实现，覆盖框架默认 Options 实现）
/// </summary>
/// <remarks>
/// 从系统参数（<c>saas.bot.telegram.*</c>，由 <c>SaasConfigurationSeeder</c> 播种）强类型读取；
/// 读不到的键回退 <see cref="TelegramBotSettings"/> 默认值。配置值查询自带分布式缓存，读取开销可控。
/// Singleton 生命周期，Scoped 配置服务经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public sealed class SaasTelegramBotSettingsStore : ITelegramBotSettingsStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 配置服务）</param>
    public SaasTelegramBotSettingsStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<TelegramBotSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // 以框架默认值实例为兜底基准，读不到的键保持默认
        var defaults = new TelegramBotSettings();
        var networkDefaults = defaults.Network;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<ISaasConfigurationService>();

        return new TelegramBotSettings
        {
            Enabled = await configuration.GetBooleanAsync(SaasConfigKeys.Bot.Telegram.Enabled, defaults.Enabled, cancellationToken),
            ConfigCacheSeconds = await configuration.GetInt32Async(SaasConfigKeys.Bot.Telegram.ConfigCacheSeconds, defaults.ConfigCacheSeconds, cancellationToken),
            ManagerRefreshSeconds = await configuration.GetInt32Async(SaasConfigKeys.Bot.Telegram.ManagerRefreshSeconds, defaults.ManagerRefreshSeconds, cancellationToken),
            WebhookBaseUrl = await configuration.GetStringAsync(SaasConfigKeys.Bot.Telegram.WebhookBaseUrl, defaults.WebhookBaseUrl, cancellationToken) ?? string.Empty,
            WebhookRoutePrefix = await configuration.GetStringAsync(SaasConfigKeys.Bot.Telegram.WebhookRoutePrefix, defaults.WebhookRoutePrefix, cancellationToken) ?? TelegramBotPlatformConsts.DefaultWebhookRoutePrefix,
            WebhookSecretToken = await configuration.GetStringAsync(SaasConfigKeys.Bot.Telegram.WebhookSecretToken, defaults.WebhookSecretToken, cancellationToken) ?? string.Empty,
            EnableFallbackReply = await configuration.GetBooleanAsync(SaasConfigKeys.Bot.Telegram.EnableFallbackReply, defaults.EnableFallbackReply, cancellationToken),
            Network = new TelegramBotNetworkOptions
            {
                ProxyUrl = await configuration.GetStringAsync(SaasConfigKeys.Bot.Telegram.ProxyUrl, networkDefaults.ProxyUrl, cancellationToken) ?? string.Empty,
                BaseUrl = await configuration.GetStringAsync(SaasConfigKeys.Bot.Telegram.BaseUrl, networkDefaults.BaseUrl, cancellationToken) ?? string.Empty,
                TimeoutSeconds = await configuration.GetInt32Async(SaasConfigKeys.Bot.Telegram.TimeoutSeconds, networkDefaults.TimeoutSeconds, cancellationToken)
            }
        };
    }
}
