// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// 接口文档开关 <c>BasicApp:ApiDocs:Enabled</c>：缺省只在 Development 打开；打开时映射 <c>/openapi.json</c>（绕过内核管道、不鉴权）与 <c>/scalar</c>。
/// </summary>
public sealed record ApiDocsOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:ApiDocs";

    /// <summary>
    /// 是否映射接口文档端点。
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// 从配置读取；没写时按环境决定。
    /// </summary>
    public static ApiDocsOptions FromConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        var raw = configuration[$"{ConfigurationSection}:Enabled"];
        if (string.IsNullOrEmpty(raw))
        {
            return new ApiDocsOptions { Enabled = environment.IsDevelopment() };
        }

        return bool.TryParse(raw, out var enabled)
            ? new ApiDocsOptions { Enabled = enabled }
            : throw new ArgumentException($"'{ConfigurationSection}:Enabled' must be 'true' or 'false', but was '{raw}'.");
    }
}
