// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 跨域 <c>BasicApp:Cors</c>。
/// </summary>
/// <remarks>
/// 前端与后端不同源部署时打开（照 4.x 的 <c>Api:Cors</c>）：<c>AllowedOrigins</c> 列出来源（协议 + 主机 + 端口，不带路径），或 <c>AllowAnyOrigin</c> 放开全部来源。
/// 两样都没写即不启用跨域，只能同源访问。方法与请求头缺省放开；携带凭据缺省开，与任意来源互斥（同时写启动即报错，不悄悄丢掉其一）。
/// </remarks>
public sealed record CorsSettings
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Cors";

    /// <summary>
    /// 允许的来源。
    /// </summary>
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    /// <summary>
    /// 是否允许任意来源。
    /// </summary>
    public bool AllowAnyOrigin { get; init; }

    /// <summary>
    /// 允许的方法。
    /// </summary>
    /// <remarks>
    /// <see cref="AllowAnyMethod"/> 为假时才用。
    /// </remarks>
    public IReadOnlyList<string> AllowedMethods { get; init; } = [];

    /// <summary>
    /// 是否允许任意方法。
    /// </summary>
    public bool AllowAnyMethod { get; init; } = true;

    /// <summary>
    /// 允许的请求头。
    /// </summary>
    /// <remarks>
    /// <see cref="AllowAnyHeader"/> 为假时才用。
    /// </remarks>
    public IReadOnlyList<string> AllowedHeaders { get; init; } = [];

    /// <summary>
    /// 是否允许任意请求头。
    /// </summary>
    public bool AllowAnyHeader { get; init; } = true;

    /// <summary>
    /// 是否允许携带凭据。
    /// </summary>
    public bool AllowCredentials { get; init; } = true;

    /// <summary>
    /// 暴露给前端脚本的响应头。
    /// </summary>
    public IReadOnlyList<string> ExposedHeaders { get; init; } = [];

    /// <summary>
    /// 预检结果的缓存秒数。
    /// </summary>
    /// <remarks>
    /// 0 为不设置。
    /// </remarks>
    public int PreflightMaxAgeSeconds { get; init; }

    /// <summary>
    /// 是否启用跨域。
    /// </summary>
    public bool Enabled => AllowAnyOrigin || AllowedOrigins.Count > 0;

    /// <summary>
    /// 从配置读取跨域设置。
    /// </summary>
    /// <remarks>
    /// 来源不是不带路径的 http(s) 地址、任意来源与凭据或列出的来源同时写、方法或请求头放开为假却一个没列、缓存秒数为负时抛出 <see cref="ArgumentException"/>。
    /// </remarks>
    public static CorsSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var settings = new CorsSettings
        {
            AllowedOrigins = Values(section, nameof(AllowedOrigins)),
            AllowAnyOrigin = Flag(section, nameof(AllowAnyOrigin), false),
            AllowedMethods = Values(section, nameof(AllowedMethods)),
            AllowAnyMethod = Flag(section, nameof(AllowAnyMethod), true),
            AllowedHeaders = Values(section, nameof(AllowedHeaders)),
            AllowAnyHeader = Flag(section, nameof(AllowAnyHeader), true),
            AllowCredentials = Flag(section, nameof(AllowCredentials), true),
            ExposedHeaders = Values(section, nameof(ExposedHeaders)),
            PreflightMaxAgeSeconds = Number(section, nameof(PreflightMaxAgeSeconds)),
        };

        foreach (var origin in settings.AllowedOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || uri.PathAndQuery != "/" || origin.EndsWith('/'))
            {
                throw new ArgumentException($"'{ConfigurationSection}:{nameof(AllowedOrigins)}' contains '{origin}'; an origin is scheme, host and optional port without a path, such as 'https://admin.example.com'.");
            }
        }

        if (settings.AllowAnyOrigin && settings.AllowedOrigins.Count > 0)
        {
            throw new ArgumentException($"'{ConfigurationSection}': write either {nameof(AllowAnyOrigin)} or {nameof(AllowedOrigins)}, not both.");
        }

        if (settings.AllowAnyOrigin && settings.AllowCredentials)
        {
            throw new ArgumentException($"'{ConfigurationSection}': {nameof(AllowAnyOrigin)} cannot be combined with {nameof(AllowCredentials)}; list the origins, or set {nameof(AllowCredentials)} to false.");
        }

        if (!settings.AllowAnyMethod && settings.AllowedMethods.Count == 0)
        {
            throw new ArgumentException($"'{ConfigurationSection}': {nameof(AllowAnyMethod)} is false but {nameof(AllowedMethods)} is empty.");
        }

        return !settings.AllowAnyHeader && settings.AllowedHeaders.Count == 0
            ? throw new ArgumentException($"'{ConfigurationSection}': {nameof(AllowAnyHeader)} is false but {nameof(AllowedHeaders)} is empty.")
            : settings;
    }

    /// <summary>
    /// 写进缺省的跨域策略。
    /// </summary>
    public void Apply(CorsPolicyBuilder policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (AllowAnyOrigin)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins([.. AllowedOrigins]);
        }

        if (AllowAnyMethod)
        {
            policy.AllowAnyMethod();
        }
        else
        {
            policy.WithMethods([.. AllowedMethods]);
        }

        if (AllowAnyHeader)
        {
            policy.AllowAnyHeader();
        }
        else
        {
            policy.WithHeaders([.. AllowedHeaders]);
        }

        if (AllowCredentials)
        {
            policy.AllowCredentials();
        }

        if (ExposedHeaders.Count > 0)
        {
            policy.WithExposedHeaders([.. ExposedHeaders]);
        }

        if (PreflightMaxAgeSeconds > 0)
        {
            policy.SetPreflightMaxAge(TimeSpan.FromSeconds(PreflightMaxAgeSeconds));
        }
    }

    private static List<string> Values(IConfigurationSection section, string key)
        => [.. section.GetSection(key).GetChildren().Select(static child => child.Value?.Trim()).Where(static value => !string.IsNullOrEmpty(value)).Select(static value => value!)];

    private static bool Flag(IConfigurationSection section, string key, bool defaultValue)
    {
        var raw = section[key];
        if (string.IsNullOrEmpty(raw))
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var value)
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be 'true' or 'false', but was '{raw}'.");
    }

    private static int Number(IConfigurationSection section, string key)
    {
        var raw = section[key];
        if (string.IsNullOrEmpty(raw))
        {
            return 0;
        }

        return int.TryParse(raw, out var value) && value >= 0
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a non-negative integer, but was '{raw}'.");
    }
}
