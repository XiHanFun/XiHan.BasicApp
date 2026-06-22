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
    /// 配置分组。
    /// </summary>
    public static class Groups
    {
        /// <summary>
        /// 认证配置分组。
        /// </summary>
        public const string Auth = "auth";
    }
}
