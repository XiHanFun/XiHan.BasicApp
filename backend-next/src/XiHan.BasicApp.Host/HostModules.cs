// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// 可选模块的开关 <c>BasicApp:Modules:&lt;名&gt;:Enabled</c>（方案 5.12）：宿主据此决定是否 <c>UseFeature</c>；前端以菜单是否下发为准，同一开关语义。
/// 没写即开启；写了必须是 <c>true</c> / <c>false</c>。
/// </summary>
public sealed record HostModules
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Modules";

    /// <summary>
    /// 工作台模块。
    /// </summary>
    public bool Workbench { get; init; } = true;

    /// <summary>
    /// 从配置读取。
    /// </summary>
    public static HostModules FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new HostModules { Workbench = Enabled(configuration, "Workbench") };
    }

    private static bool Enabled(IConfiguration configuration, string module)
    {
        var key = $"{ConfigurationSection}:{module}:Enabled";
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return bool.TryParse(value, out var enabled) ? enabled : throw new ArgumentException($"'{key}' must be 'true' or 'false', but was '{value}'.");
    }
}
