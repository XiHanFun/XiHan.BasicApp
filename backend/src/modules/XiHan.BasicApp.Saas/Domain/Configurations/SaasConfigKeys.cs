// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Configurations;

/// <summary>
/// SaaS 模块运行时配置键。
/// </summary>
public static class SaasConfigKeys
{
    /// <summary>
    /// 配置键前缀。
    /// </summary>
    public const string Prefix = "saas";

    /// <summary>
    /// 规范化配置键。
    /// </summary>
    /// <param name="configKey">配置键。</param>
    /// <returns>规范化后的配置键。</returns>
    public static string Normalize(string configKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        var normalized = configKey.Trim().ToLowerInvariant();
        Validate(normalized);
        return normalized;
    }

    /// <summary>
    /// 校验配置键格式。
    /// </summary>
    /// <param name="configKey">配置键。</param>
    public static void Validate(string configKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);

        var normalized = configKey.Trim();
        if (normalized.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(configKey), "配置键不能超过 100 个字符。");
        }

        if (!string.Equals(normalized, normalized.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("配置键必须使用小写英文。");
        }

        var segments = normalized.Split('.');
        if (segments.Length < 2 || segments.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("配置键必须使用 module.domain.name 的点分层格式。");
        }

        foreach (var segment in segments)
        {
            if (segment[0] == '-' || segment[^1] == '-' || segment.Any(static code => !IsValidSegmentChar(code)))
            {
                throw new InvalidOperationException("配置键只能包含小写英文、数字、点和段内连字符。");
            }
        }
    }

    private static bool IsValidSegmentChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code == '-';
    }

    /// <summary>
    /// 认证配置。
    /// </summary>
    /// <remarks>
    /// 仅保留运行期被强类型读取入口（<c>SaasConfigurationService.GetLoginConfigAsync</c>）实际消费的配置键；
    /// 其余历史占位键（应用/租户/通知/密码策略/会话/审计/文件存储）均无任何代码消费者，已随种子一并移除。
    /// </remarks>
    public static class Auth
    {
        /// <summary>
        /// 登录方式，JSON 字符串数组。
        /// </summary>
        public const string LoginMethods = "saas.auth.login.methods";

        /// <summary>
        /// OAuth 提供商，JSON 数组。
        /// </summary>
        public const string OAuthProviders = "saas.auth.oauth.providers";
    }

    /// <summary>
    /// 机器人配置。
    /// </summary>
    public static class Bot
    {
        /// <summary>
        /// Telegram 机器人平台全局设置（由 <c>SaasTelegramBotSettingsStore</c> 强类型读取）。
        /// </summary>
        public static class Telegram
        {
            /// <summary>
            /// 是否启用 Telegram 机器人平台（总开关，布尔）。
            /// </summary>
            public const string Enabled = "saas.bot.telegram.enabled";

            /// <summary>
            /// Webhook 基础地址（如 https://example.com）；空 = 长轮询（Polling）模式。
            /// </summary>
            public const string WebhookBaseUrl = "saas.bot.telegram.webhook-base-url";

            /// <summary>
            /// Webhook 路由前缀。
            /// </summary>
            public const string WebhookRoutePrefix = "saas.bot.telegram.webhook-route-prefix";

            /// <summary>
            /// Webhook 密钥令牌（Webhook 模式必填，fail-closed；读侧遮蔽）。
            /// </summary>
            public const string WebhookSecretToken = "saas.bot.telegram.webhook-secret-token";

            /// <summary>
            /// 管理器刷新间隔秒数。
            /// </summary>
            public const string ManagerRefreshSeconds = "saas.bot.telegram.manager-refresh-seconds";

            /// <summary>
            /// 配置列表缓存秒数。
            /// </summary>
            public const string ConfigCacheSeconds = "saas.bot.telegram.config-cache-seconds";

            /// <summary>
            /// 是否启用兜底回复（平台级；与单机器人配置任一开启即生效）。
            /// </summary>
            public const string EnableFallbackReply = "saas.bot.telegram.enable-fallback-reply";

            /// <summary>
            /// 代理地址（如 http://127.0.0.1:7890 或 socks5://127.0.0.1:1080）；空 = 直连。
            /// </summary>
            public const string ProxyUrl = "saas.bot.telegram.proxy-url";

            /// <summary>
            /// 自建 Bot API Server 基础地址（如 https://tg-api.example.com）；空 = 官方 api.telegram.org。
            /// </summary>
            public const string BaseUrl = "saas.bot.telegram.base-url";

            /// <summary>
            /// 请求超时秒数。
            /// </summary>
            public const string TimeoutSeconds = "saas.bot.telegram.timeout-seconds";
        }
    }

    /// <summary>
    /// 配置分组。
    /// </summary>
    public static class Groups
    {
        /// <summary>
        /// 认证配置分组。
        /// </summary>
        public const string Auth = "auth";

        /// <summary>
        /// 机器人配置分组。
        /// </summary>
        public const string Bot = "bot";
    }
}
