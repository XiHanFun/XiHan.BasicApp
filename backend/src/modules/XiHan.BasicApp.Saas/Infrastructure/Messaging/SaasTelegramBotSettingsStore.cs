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
/// 平台设置是一条 JSON 参数（<c>saas.bot.telegram</c>，结构见 <see cref="SaasTelegramBotConfig"/>），Webhook 密钥令牌加密单列；
/// 缺配置按设置类型的默认值运行，值不合法直接报错。配置值查询自带分布式缓存，读取开销可控。
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

    /// <summary>
    /// 获取当前生效的平台全局设置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>平台全局设置</returns>
    public async Task<TelegramBotSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<ISaasConfigurationService>();

        var config = await configuration.GetJsonAsync(SaasConfigKeys.Bot.Telegram.Settings, new SaasTelegramBotConfig(), cancellationToken);
        var secretToken = await configuration.GetStringAsync(SaasConfigKeys.Bot.Telegram.WebhookSecretToken, string.Empty, cancellationToken);

        return new TelegramBotSettings
        {
            Enabled = config.Enabled,
            ConfigCacheSeconds = config.ConfigCacheSeconds,
            ManagerRefreshSeconds = config.ManagerRefreshSeconds,
            WebhookBaseUrl = config.WebhookBaseUrl,
            WebhookRoutePrefix = config.WebhookRoutePrefix,
            WebhookSecretToken = secretToken ?? string.Empty,
            EnableFallbackReply = config.EnableFallbackReply,
            Network = new TelegramBotNetworkOptions
            {
                ProxyUrl = config.Network.ProxyUrl,
                BaseUrl = config.Network.BaseUrl,
                TimeoutSeconds = config.Network.TimeoutSeconds
            }
        };
    }
}
