// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;

namespace XiHan.BasicApp.Contracts;

/// <summary>
/// 应用标识：名称与版本。
/// </summary>
/// <remarks>
/// 版本取自程序集的信息版本（<c>Directory.Build.props</c> 的 <c>Version</c>），各包清单与接口文档共用这一份。
/// </remarks>
public static class AppInfo
{
    /// <summary>
    /// 应用名。
    /// </summary>
    /// <remarks>
    /// 清单 ID 的中段（<c>xihan.basicapp.&lt;包&gt;</c>）、gRPC 包名与接口文档标题都由它派生。
    /// </remarks>
    public const string Name = "XiHan.BasicApp";

    /// <summary>
    /// 清单 ID 前缀：<c>xihan.basicapp.</c>。
    /// </summary>
    public const string ManifestIdPrefix = "xihan.basicapp.";

    /// <summary>
    /// 包间能力 ID 的前缀。
    /// </summary>
    /// <remarks>
    /// 前缀是 <c>basicapp.</c>，能力写成 <c>basicapp.&lt;包&gt;@1</c>。
    /// </remarks>
    public const string CapabilityPrefix = "basicapp.";

    /// <summary>
    /// 包间能力的版本号。
    /// </summary>
    public const int CapabilityVersion = 1;

    /// <summary>
    /// 应用版本（SemVer）。
    /// </summary>
    /// <remarks>
    /// 如 <c>6.0.0-preview.1</c>；信息版本里 <c>+</c> 之后的构建元数据去掉。
    /// </remarks>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var informational = typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
        {
            return typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? informational : informational[..plus];
    }
}
