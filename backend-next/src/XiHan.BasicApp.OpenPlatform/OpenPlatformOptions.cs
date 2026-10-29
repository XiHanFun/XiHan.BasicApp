// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台配置（<c>BasicApp:OpenPlatform</c>）。
/// </summary>
public sealed class OpenPlatformOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string Section = "BasicApp:OpenPlatform";

    /// <summary>
    /// 请求时间戳与服务端时间的最大偏差（秒，缺省 300，4.x 同）。随机串的保留时间是它的两倍，时间窗内重放必被识别。
    /// </summary>
    public int TimestampToleranceSeconds { get; init; } = 300;

    /// <summary>
    /// 请求体上限（字节，缺省 2 MB，4.x 同）。
    /// </summary>
    public int MaxRequestBodyBytes { get; init; } = 2 * 1024 * 1024;

    /// <summary>
    /// 时间窗。
    /// </summary>
    public TimeSpan TimestampTolerance => TimeSpan.FromSeconds(TimestampToleranceSeconds);

    /// <summary>
    /// 从配置读取。
    /// </summary>
    public static OpenPlatformOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var defaults = new OpenPlatformOptions();
        return new OpenPlatformOptions
        {
            TimestampToleranceSeconds = Math.Clamp(section.GetValue("TimestampToleranceSeconds", defaults.TimestampToleranceSeconds), 30, 3600),
            MaxRequestBodyBytes = Math.Clamp(section.GetValue("MaxRequestBodyBytes", defaults.MaxRequestBodyBytes), 1024, 64 * 1024 * 1024),
        };
    }
}
