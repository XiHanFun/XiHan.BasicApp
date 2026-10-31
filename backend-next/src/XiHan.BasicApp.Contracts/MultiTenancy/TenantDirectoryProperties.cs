// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// BasicApp 写进框架租户目录的属性。
/// </summary>
/// <remarks>
/// Tenancy 同步目录时写入，Identity 登录、刷新、切换租户与模仿登录时读取（Identity 不引用 Tenancy）。
/// </remarks>
public static class TenantDirectoryProperties
{
    /// <summary>
    /// 到期时间的属性键。
    /// </summary>
    /// <remarks>
    /// UTC，往返格式（<c>O</c>）；没有即不限。到了这一刻租户就进不去了，不等到期作业把租户标为到期。
    /// </remarks>
    public const string ExpiresAt = "BasicApp.ExpiresAt";

    /// <summary>
    /// 到期时间写成属性值。
    /// </summary>
    public static string FormatExpiresAt(DateTimeOffset expiresAt) => expiresAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// 读出目录属性里的到期时间。
    /// </summary>
    /// <returns>没有这一项即 <see langword="null"/>（不限）。</returns>
    /// <exception cref="FormatException">属性值不是往返格式的时间。</exception>
    public static DateTimeOffset? ExpiresAtOf(IReadOnlyDictionary<string, string> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.TryGetValue(ExpiresAt, out var value)
            ? DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            : null;
    }
}
