// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 权限条件的生效模式（4.x 同）。
/// </summary>
public enum PermissionConditionMode
{
    /// <summary>
    /// 不按条件判定：有条件的拒绝即拒绝、有条件的授予即授予，不记命中。
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// 照常判定并记下本该拒绝的命中，但放行。
    /// </summary>
    LogOnly = 1,

    /// <summary>
    /// 照条件判定，不满足即拒绝并记命中。
    /// </summary>
    Enforce = 2,
}

/// <summary>
/// 授权包的配置（配置节 <c>BasicApp:Access</c>）。
/// </summary>
public sealed record AccessOptions
{
    /// <summary>
    /// 配置节名。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Access";

    /// <summary>
    /// 缺省的条件时区（4.x 同）。
    /// </summary>
    public const string DefaultTimeZoneId = "Asia/Shanghai";

    /// <summary>
    /// 权限条件的生效模式（<c>PermissionConditionMode</c>，缺省 <see cref="PermissionConditionMode.Enforce"/>）。
    /// </summary>
    public PermissionConditionMode PermissionConditionMode { get; init; } = PermissionConditionMode.Enforce;

    /// <summary>
    /// 条件里时刻、小时、星期与日期所按的时区（<c>ConditionTimeZone</c>，缺省 Asia/Shanghai）。
    /// </summary>
    public TimeZoneInfo ConditionTimeZone { get; init; } = Resolve(DefaultTimeZoneId);

    /// <summary>
    /// 从配置读取；不合法的值是装配错误，直接抛出。
    /// </summary>
    public static AccessOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var options = new AccessOptions();
        if (section["PermissionConditionMode"] is { } mode)
        {
            options = options with
            {
                PermissionConditionMode = Enum.TryParse<PermissionConditionMode>(mode, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : throw new ArgumentException($"'{ConfigurationSection}:PermissionConditionMode' must be Disabled, LogOnly or Enforce, but was '{mode}'."),
            };
        }

        if (section["ConditionTimeZone"] is { } timeZone)
        {
            options = options with { ConditionTimeZone = Resolve(timeZone) };
        }

        return options;
    }

    private static TimeZoneInfo Resolve(string id)
        => TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
            ? zone
            : throw new ArgumentException($"'{ConfigurationSection}:ConditionTimeZone' is not a known time zone: '{id}'.");
}
