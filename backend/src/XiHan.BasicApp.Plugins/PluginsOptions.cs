// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块的配置（<c>BasicApp:Plugins</c>）
/// </summary>
/// <remarks>
/// 写错的值启动即失败，不悄悄退回缺省
/// </remarks>
public sealed class PluginsOptions
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string Section = "BasicApp:Plugins";

    /// <summary>
    /// 允许安装的隔离级别
    /// </summary>
    /// <remarks>
    /// 配置写 <c>out-of-process</c> / <c>wasm</c>；缺省只有进程外，Wasm 仍是实验阶段，要显式打开
    /// </remarks>
    public IReadOnlySet<PluginIsolation> Isolations { get; init; } = new HashSet<PluginIsolation> { PluginIsolation.OutOfProcess };

    /// <summary>
    /// 插件包的大小上限（MB），缺省 100，与文件上传的缺省上限一致
    /// </summary>
    public int MaxPackageSizeMegabytes { get; init; } = 100;

    /// <summary>
    /// 插件包的大小上限（字节）
    /// </summary>
    public long MaxPackageBytes => MaxPackageSizeMegabytes * 1024L * 1024L;

    /// <summary>
    /// 从配置读取
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <returns>选项</returns>
    /// <exception cref="ArgumentException">隔离级别不认得或为空，大小上限不是正整数</exception>
    public static PluginsOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var defaults = new PluginsOptions();
        var isolations = section.GetSection(nameof(Isolations)).GetChildren().Select(static c => c.Value).ToList();
        var size = section[nameof(MaxPackageSizeMegabytes)];
        return new PluginsOptions
        {
            Isolations = isolations.Count == 0 ? defaults.Isolations : isolations.Select(IsolationOf).ToHashSet(),
            MaxPackageSizeMegabytes = string.IsNullOrEmpty(size)
                ? defaults.MaxPackageSizeMegabytes
                : int.TryParse(size, out var megabytes) && megabytes > 0 ? megabytes : throw new ArgumentException($"'{Section}:{nameof(MaxPackageSizeMegabytes)}' must be a positive integer, but was '{size}'."),
        };
    }

    /// <summary>
    /// 配置里的隔离级别名
    /// </summary>
    /// <param name="isolation">隔离级别</param>
    public static string NameOf(PluginIsolation isolation) => isolation switch
    {
        PluginIsolation.OutOfProcess => "out-of-process",
        PluginIsolation.Wasm => "wasm",
        _ => "in-process",
    };

    /// <summary>
    /// 解析配置里的一个隔离级别
    /// </summary>
    private static PluginIsolation IsolationOf(string? value) => value switch
    {
        "out-of-process" => PluginIsolation.OutOfProcess,
        "wasm" => PluginIsolation.Wasm,
        _ => throw new ArgumentException($"'{Section}:{nameof(Isolations)}' accepts 'out-of-process' and 'wasm', but got '{value}'."),
    };
}
