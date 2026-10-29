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
    /// OAuth / OIDC 的签发者（不带结尾斜杠，如 <c>https://example.com/api/oauth2</c>）；不配即取请求的来源加 <c>/api/oauth2</c>。
    /// </summary>
    public string? Issuer { get; init; }

    /// <summary>
    /// 授权同意页地址（前端路由，授权端点把查询串原样带过去；缺省 <c>/#/oauth/authorize</c>，前后端同源时即可用）。
    /// </summary>
    public string ConsentPageUrl { get; init; } = "/#/oauth/authorize";

    /// <summary>
    /// id_token 有效期（秒，缺省 600，4.x 同）。
    /// </summary>
    public int IdTokenLifetimeSeconds { get; init; } = 600;

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
            Issuer = section["Issuer"] is { Length: > 0 } issuer ? issuer.TrimEnd('/') : null,
            ConsentPageUrl = section["ConsentPageUrl"] is { Length: > 0 } consent ? consent : defaults.ConsentPageUrl,
            IdTokenLifetimeSeconds = Math.Clamp(section.GetValue("IdTokenLifetimeSeconds", defaults.IdTokenLifetimeSeconds), 60, 3600),
        };
    }
}
