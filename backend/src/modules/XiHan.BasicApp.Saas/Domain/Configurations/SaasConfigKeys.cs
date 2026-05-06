#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasConfigKeys
// Guid:4b4ef2e2-7f60-4d58-955c-85968109fd33
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// 应用配置。
    /// </summary>
    public static class Application
    {
        /// <summary>
        /// 应用名称。
        /// </summary>
        public const string Name = "saas.application.name";

        /// <summary>
        /// 应用 Logo。
        /// </summary>
        public const string Logo = "saas.application.logo";

        /// <summary>
        /// 默认语言。
        /// </summary>
        public const string DefaultLanguage = "saas.application.default.language";
    }

    /// <summary>
    /// 租户配置。
    /// </summary>
    public static class Tenant
    {
        /// <summary>
        /// 默认租户版本编码。
        /// </summary>
        public const string DefaultEditionCode = "saas.tenant.default.edition.code";
    }

    /// <summary>
    /// 认证配置。
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// 登录方式，JSON 字符串数组。
        /// </summary>
        public const string LoginMethods = "saas.auth.login.methods";

        /// <summary>
        /// 是否启用登录租户选择。
        /// </summary>
        public const string TenantSelectionEnabled = "saas.auth.login.tenant.selection.enabled";

        /// <summary>
        /// OAuth 提供商，JSON 数组。
        /// </summary>
        public const string OAuthProviders = "saas.auth.oauth.providers";

        /// <summary>
        /// 密码最小长度。
        /// </summary>
        public const string PasswordMinLength = "saas.auth.password.min.length";

        /// <summary>
        /// 密码是否要求数字。
        /// </summary>
        public const string PasswordRequireDigit = "saas.auth.password.require.digit";

        /// <summary>
        /// 密码是否要求大写字母。
        /// </summary>
        public const string PasswordRequireUppercase = "saas.auth.password.require.uppercase";

        /// <summary>
        /// 最大连续登录失败次数。
        /// </summary>
        public const string PasswordMaxFailedAttempts = "saas.auth.password.max.failed.attempts";

        /// <summary>
        /// 账号锁定分钟数。
        /// </summary>
        public const string AccountLockoutMinutes = "saas.auth.account.lockout.minutes";

        /// <summary>
        /// 是否允许同一用户多设备登录。
        /// </summary>
        public const string SessionAllowMultiLogin = "saas.auth.session.allow.multi.login";

        /// <summary>
        /// 默认最大在线设备数。
        /// </summary>
        public const string SessionMaxLoginDevices = "saas.auth.session.max.login.devices";

        /// <summary>
        /// 默认 OAuth ClientId。
        /// </summary>
        public const string SessionClientId = "saas.auth.session.client.id";

        /// <summary>
        /// 默认 OAuth Scope。
        /// </summary>
        public const string SessionScope = "saas.auth.session.scope";

        /// <summary>
        /// 刷新令牌有效天数。
        /// </summary>
        public const string SessionRefreshTokenDays = "saas.auth.session.refresh.token.days";
    }

    /// <summary>
    /// 通知配置。
    /// </summary>
    public static class Notification
    {
        /// <summary>
        /// 登录成功通知开关。
        /// </summary>
        public const string AuthLoginEnabled = "saas.notification.auth.login.enabled";

        /// <summary>
        /// 主动退出通知开关。
        /// </summary>
        public const string AuthLogoutEnabled = "saas.notification.auth.logout.enabled";
    }

    /// <summary>
    /// 审计配置。
    /// </summary>
    public static class Audit
    {
        /// <summary>
        /// 日志保留天数。
        /// </summary>
        public const string LogRetentionDays = "saas.audit.log.retention.days";
    }

    /// <summary>
    /// 配置分组。
    /// </summary>
    public static class Groups
    {
        public const string Application = "application";
        public const string Tenant = "tenant";
        public const string Auth = "auth";
        public const string Notification = "notification";
        public const string Audit = "audit";
    }

    /// <summary>
    /// 旧版配置键。仅用于种子数据平滑迁移，不应在业务代码继续使用。
    /// </summary>
    public static class Legacy
    {
        public const string ApplicationName = "app.name";
        public const string ApplicationDefaultLanguage = "app.default_language";
        public const string TenantDefaultEditionCode = "tenant.default_edition_code";
        public const string NotificationAuthLoginEnabled = "notification.auth_login_enabled";
        public const string NotificationAuthLogoutEnabled = "notification.auth_logout_enabled";
        public const string PasswordMinLength = "auth.password.min_length";
        public const string PasswordRequireDigit = "auth.password.require_digit";
        public const string PasswordRequireUppercase = "auth.password.require_uppercase";
        public const string SessionAllowMultiLogin = "auth.session.allow_multi_login";
        public const string SessionMaxLoginDevices = "auth.session.max_login_devices";
        public const string AccountLockoutMinutes = "security.account_lockout_minutes";
        public const string AuditLogRetentionDays = "audit.log_retention_days";
    }

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
}
