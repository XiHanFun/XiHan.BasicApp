// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包配置 <c>BasicApp:Auth</c>：令牌有效期、登录失败锁定、密码策略、会话门禁缓存。
/// </summary>
public sealed record IdentityOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Auth";

    /// <summary>
    /// 访问令牌有效期（<c>AccessTokenMinutes</c>，缺省 120 分钟）。
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(120);

    /// <summary>
    /// 刷新令牌（会话）有效期（<c>RefreshTokenDays</c>，缺省 7 天）。
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 连续登录失败多少次后锁定（<c>MaxFailedAccessAttempts</c>，缺省 5）。
    /// </summary>
    public int MaxFailedAccessAttempts { get; init; } = 5;

    /// <summary>
    /// 锁定时长（<c>LockoutMinutes</c>，缺省 15 分钟）。
    /// </summary>
    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// 密码最短长度（<c>PasswordMinLength</c>，缺省 8）。
    /// </summary>
    public int PasswordMinLength { get; init; } = 8;

    /// <summary>
    /// 不得与最近多少个历史密码相同（<c>PasswordHistoryDepth</c>，缺省 3；0 不检查）。
    /// </summary>
    public int PasswordHistoryDepth { get; init; } = 3;

    /// <summary>
    /// 会话门禁的缓存存活时间（<c>SessionCacheSeconds</c>，缺省 30 秒）：吊销最迟在此时长后对其他实例生效。
    /// </summary>
    public TimeSpan SessionCacheTimeToLive { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 从配置读取；不合法的值是装配错误，直接抛出。
    /// </summary>
    public static IdentityOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var options = new IdentityOptions();
        if (ReadPositive(section, "AccessTokenMinutes") is { } accessMinutes)
        {
            options = options with { AccessTokenLifetime = TimeSpan.FromMinutes(accessMinutes) };
        }

        if (ReadPositive(section, "RefreshTokenDays") is { } refreshDays)
        {
            options = options with { RefreshTokenLifetime = TimeSpan.FromDays(refreshDays) };
        }

        if (ReadPositive(section, "MaxFailedAccessAttempts") is { } attempts)
        {
            options = options with { MaxFailedAccessAttempts = (int)attempts };
        }

        if (ReadPositive(section, "LockoutMinutes") is { } lockoutMinutes)
        {
            options = options with { LockoutDuration = TimeSpan.FromMinutes(lockoutMinutes) };
        }

        if (ReadPositive(section, "PasswordMinLength") is { } minLength)
        {
            options = options with { PasswordMinLength = (int)minLength };
        }

        if (ReadNonNegative(section, "PasswordHistoryDepth") is { } depth)
        {
            options = options with { PasswordHistoryDepth = (int)depth };
        }

        if (ReadPositive(section, "SessionCacheSeconds") is { } cacheSeconds)
        {
            options = options with { SessionCacheTimeToLive = TimeSpan.FromSeconds(cacheSeconds) };
        }

        return options;
    }

    private static double? ReadPositive(IConfiguration section, string key)
    {
        var raw = section[key];
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a positive number, but was '{raw}'.");
    }

    private static double? ReadNonNegative(IConfiguration section, string key)
    {
        var raw = section[key];
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a non-negative number, but was '{raw}'.");
    }
}
