// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包配置（<c>BasicApp:Auth</c>）。
/// </summary>
/// <remarks>
/// 包括令牌有效期、登录失败锁定、密码策略、会话门禁缓存、图形验证码、登录节流与双因素。
/// </remarks>
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
    /// 连续登录失败多少次后锁定。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>MaxFailedAccessAttempts</c>，缺省 5。
    /// </remarks>
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
    /// 不得与最近多少个历史密码相同。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>PasswordHistoryDepth</c>，缺省 3；0 不检查。
    /// </remarks>
    public int PasswordHistoryDepth { get; init; } = 3;

    /// <summary>
    /// 会话门禁的缓存存活时间。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>SessionCacheSeconds</c>，缺省 30 秒。吊销最迟在此时长后对其他实例生效。
    /// </remarks>
    public TimeSpan SessionCacheTimeToLive { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 密码登录是否要求图形验证码（<c>CaptchaEnabled</c>，缺省开）。
    /// </summary>
    public bool CaptchaEnabled { get; init; } = true;

    /// <summary>
    /// 图形验证码有效期（<c>CaptchaSeconds</c>，缺省 300 秒）。
    /// </summary>
    public TimeSpan CaptchaLifetime { get; init; } = TimeSpan.FromSeconds(300);

    /// <summary>
    /// 登录节流的固定窗口。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LoginThrottleWindowSeconds</c>，缺省 60 秒。
    /// </remarks>
    public TimeSpan LoginThrottleWindow { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 同一账号在同一地址上每个窗口允许的失败次数。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LoginThrottleAccountLimit</c>，缺省 5。
    /// </remarks>
    public int LoginThrottleAccountLimit { get; init; } = 5;

    /// <summary>
    /// 同一地址每个窗口允许的失败次数，跨账号累计。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LoginThrottleIpLimit</c>，缺省 30。
    /// </remarks>
    public int LoginThrottleIpLimit { get; init; } = 30;

    /// <summary>
    /// 两步验证票据有效期。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>TwoFactorTicketMinutes</c>，缺省 10 分钟。从签发起算、不顺延。
    /// </remarks>
    public TimeSpan TwoFactorTicketLifetime { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 一张票据允许输错验证码的次数。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>TwoFactorTicketMaxFailures</c>，缺省 5。用完即作废、须重新登录。
    /// </remarks>
    public int TwoFactorTicketMaxFailures { get; init; } = 5;

    /// <summary>
    /// 认证器里显示的签发方。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>TotpIssuer</c>，缺省 <c>XiHan BasicApp</c>。
    /// </remarks>
    public string TotpIssuer { get; init; } = "XiHan BasicApp";

    /// <summary>
    /// 邮件 / 短信验证码的有效期。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>VerificationCodeMinutes</c>，缺省 10 分钟（4.x 同）。
    /// </remarks>
    public TimeSpan VerificationCodeLifetime { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 同一用途、同一目标两次发码的最短间隔。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>VerificationCodeIntervalSeconds</c>，缺省 60 秒（4.x 同）。
    /// </remarks>
    public TimeSpan VerificationCodeInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 一个邮箱 / 手机号每天（UTC）最多收几次验证码。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>VerificationCodeDailyPerTarget</c>，缺省 10（4.x 同），不分用途。
    /// </remarks>
    public int VerificationCodeDailyPerTarget { get; init; } = 10;

    /// <summary>
    /// 一个客户端地址每天（UTC）最多发几次验证码。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>VerificationCodeDailyPerAddress</c>，缺省 50（4.x 同），不分用途。
    /// </remarks>
    public int VerificationCodeDailyPerAddress { get; init; } = 50;

    /// <summary>
    /// 一个验证码允许输错的次数。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>VerificationCodeMaxFailures</c>，缺省 5（4.x 同）；用完即作废并锁定 <see cref="VerificationCodeLockout"/>。
    /// </remarks>
    public int VerificationCodeMaxFailures { get; init; } = 5;

    /// <summary>
    /// 输错用完后同一用途、同一主体不能再发码与校验的时长。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>VerificationCodeLockoutMinutes</c>，缺省 10 分钟（4.x 同）。
    /// </remarks>
    public TimeSpan VerificationCodeLockout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 从配置读取身份包配置。
    /// </summary>
    /// <exception cref="ArgumentException">配置里有不合法的值时抛出（装配错误）。</exception>
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

        if (ReadBoolean(section, "CaptchaEnabled") is { } captchaEnabled)
        {
            options = options with { CaptchaEnabled = captchaEnabled };
        }

        if (ReadPositive(section, "CaptchaSeconds") is { } captchaSeconds)
        {
            options = options with { CaptchaLifetime = TimeSpan.FromSeconds(captchaSeconds) };
        }

        if (ReadPositive(section, "LoginThrottleWindowSeconds") is { } windowSeconds)
        {
            options = options with { LoginThrottleWindow = TimeSpan.FromSeconds(windowSeconds) };
        }

        if (ReadPositive(section, "LoginThrottleAccountLimit") is { } accountLimit)
        {
            options = options with { LoginThrottleAccountLimit = (int)accountLimit };
        }

        if (ReadPositive(section, "LoginThrottleIpLimit") is { } ipLimit)
        {
            options = options with { LoginThrottleIpLimit = (int)ipLimit };
        }

        if (ReadPositive(section, "TwoFactorTicketMinutes") is { } ticketMinutes)
        {
            options = options with { TwoFactorTicketLifetime = TimeSpan.FromMinutes(ticketMinutes) };
        }

        if (ReadPositive(section, "TwoFactorTicketMaxFailures") is { } ticketFailures)
        {
            options = options with { TwoFactorTicketMaxFailures = (int)ticketFailures };
        }

        if (ReadPositive(section, "VerificationCodeMinutes") is { } codeMinutes)
        {
            options = options with { VerificationCodeLifetime = TimeSpan.FromMinutes(codeMinutes) };
        }

        if (ReadPositive(section, "VerificationCodeIntervalSeconds") is { } codeInterval)
        {
            options = options with { VerificationCodeInterval = TimeSpan.FromSeconds(codeInterval) };
        }

        if (ReadPositive(section, "VerificationCodeDailyPerTarget") is { } perTarget)
        {
            options = options with { VerificationCodeDailyPerTarget = (int)perTarget };
        }

        if (ReadPositive(section, "VerificationCodeDailyPerAddress") is { } perAddress)
        {
            options = options with { VerificationCodeDailyPerAddress = (int)perAddress };
        }

        if (ReadPositive(section, "VerificationCodeMaxFailures") is { } codeFailures)
        {
            options = options with { VerificationCodeMaxFailures = (int)codeFailures };
        }

        if (ReadPositive(section, "VerificationCodeLockoutMinutes") is { } codeLockout)
        {
            options = options with { VerificationCodeLockout = TimeSpan.FromMinutes(codeLockout) };
        }

        if (section["TotpIssuer"] is { } issuer)
        {
            options = options with { TotpIssuer = !string.IsNullOrWhiteSpace(issuer) && !issuer.Contains(':', StringComparison.Ordinal) ? issuer.Trim() : throw new ArgumentException($"'{ConfigurationSection}:TotpIssuer' must be non-blank and must not contain ':', but was '{issuer}'.") };
        }

        return options;
    }

    private static bool? ReadBoolean(IConfiguration section, string key)
    {
        var raw = section[key];
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return bool.TryParse(raw, out var value) ? value : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be 'true' or 'false', but was '{raw}'.");
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
