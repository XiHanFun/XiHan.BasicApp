// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// CloudEvents 入站端点开关 <c>BasicApp:CloudEvents:Enabled</c>。
/// </summary>
/// <remarks>
/// 缺省关。打开时映射 <c>POST /events</c>（框架 <c>MapXiHanCloudEvents</c>），只收登录过的调用方，供内部服务投递事件；
/// 主题映射、入站方式与出站 Webhook 在框架节 <c>XiHan:Protocols:CloudEvents</c>。
/// </remarks>
public sealed record CloudEventsSettings
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:CloudEvents";

    /// <summary>
    /// 入站端点的路径。
    /// </summary>
    public const string Path = "/events";

    /// <summary>
    /// 是否映射入站端点。
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// 从配置读取入站端点开关。
    /// </summary>
    /// <remarks>
    /// 没写即关；写了必须是 <c>true</c> / <c>false</c>。
    /// </remarks>
    public static CloudEventsSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var raw = configuration[$"{ConfigurationSection}:Enabled"];
        if (string.IsNullOrEmpty(raw))
        {
            return new CloudEventsSettings();
        }

        return bool.TryParse(raw, out var enabled)
            ? new CloudEventsSettings { Enabled = enabled }
            : throw new ArgumentException($"'{ConfigurationSection}:Enabled' must be 'true' or 'false', but was '{raw}'.");
    }
}
