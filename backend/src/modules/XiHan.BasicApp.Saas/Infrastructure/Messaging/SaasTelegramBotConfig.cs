// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Bot.Telegram.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// Telegram 机器人平台设置（配置键 <c>saas.bot.telegram</c>）
/// </summary>
/// <remarks>
/// Webhook 密钥令牌不在这里：它加密存储，单列在 <c>saas.bot.telegram.webhook-secret-token</c>。
/// </remarks>
public sealed class SaasTelegramBotConfig
{
    /// <summary>
    /// 总开关（关闭时不拉起任何机器人）
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Webhook 基础地址（如 https://example.com）；留空走长轮询
    /// </summary>
    public string WebhookBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Webhook 接收路由前缀（匹配 POST {前缀}/{机器人名}）
    /// </summary>
    public string WebhookRoutePrefix { get; set; } = TelegramBotPlatformConsts.DefaultWebhookRoutePrefix;

    /// <summary>
    /// 管理器探测机器人增删改的周期（秒）
    /// </summary>
    public int ManagerRefreshSeconds { get; set; } = 5;

    /// <summary>
    /// 机器人配置列表的进程内缓存时长（秒）
    /// </summary>
    public int ConfigCacheSeconds { get; set; } = 5;

    /// <summary>
    /// 没有处理器命中普通消息时是否回复提示文案
    /// </summary>
    public bool EnableFallbackReply { get; set; }

    /// <summary>
    /// 网络设置
    /// </summary>
    public SaasTelegramBotNetworkConfig Network { get; set; } = new();
}

/// <summary>
/// Telegram 机器人网络设置
/// </summary>
public sealed class SaasTelegramBotNetworkConfig
{
    /// <summary>
    /// 代理地址（如 http://127.0.0.1:7890 或 socks5://127.0.0.1:1080）；留空直连
    /// </summary>
    public string ProxyUrl { get; set; } = string.Empty;

    /// <summary>
    /// 自建 Bot API Server 基础地址；留空用官方 api.telegram.org
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 请求超时（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 100;
}
