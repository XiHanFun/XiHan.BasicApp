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
    /// 同一功能的设置合成一条 JSON 配置，结构见各设置类型；缺配置时按类型里的默认值运行。
    /// </remarks>
    public static class Auth
    {
        /// <summary>
        /// 登录设置（JSON，<see cref="SaasLoginSettings"/>）：登录页开放的方式与展示的第三方登录。
        /// </summary>
        public const string Login = "saas.auth.login";

        /// <summary>
        /// 密码设置（JSON，<see cref="SaasPasswordSettings"/>）：是否强制修改由他人设置的密码。
        /// </summary>
        public const string Password = "saas.auth.password";

        /// <summary>
        /// 模仿登录设置（JSON，<see cref="SaasImpersonationSettings"/>）：会话存活时长与是否通知被模仿者。
        /// </summary>
        public const string Impersonation = "saas.auth.impersonation";
    }

    /// <summary>
    /// 机器人配置。
    /// </summary>
    public static class Bot
    {
        /// <summary>
        /// Telegram 机器人平台配置。
        /// </summary>
        public static class Telegram
        {
            /// <summary>
            /// 平台设置（JSON，由 <c>SaasTelegramBotSettingsStore</c> 读取）：总开关、Webhook 地址、刷新与缓存周期、网络。
            /// </summary>
            public const string Settings = "saas.bot.telegram";

            /// <summary>
            /// Webhook 密钥令牌（加密存储，单列一条：加密作用于整条配置值，不能和明文设置合在一起）。
            /// </summary>
            public const string WebhookSecretToken = "saas.bot.telegram.webhook-secret-token";
        }
    }

    /// <summary>
    /// 日志配置。
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 日志保留天数（数字，由 <c>LogRetentionCleanupTask</c> 读取）。
        /// </summary>
        public const string RetentionDays = "saas.log.retention-days";
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

        /// <summary>
        /// 日志配置分组。
        /// </summary>
        public const string Log = "log";
    }
}
