// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
    /// 本机解包的目录（绝对路径）
    /// </summary>
    /// <remarks>
    /// 配置写相对路径时相对内容根目录，缺省 <c>App_Data/plugins</c>；每个版本解到 <c>&lt;清单 ID&gt;/&lt;版本 ID&gt;/</c>，插件进程从这里启动
    /// </remarks>
    public string CacheDirectory { get; init; } = Path.GetFullPath("App_Data/plugins");

    /// <summary>
    /// 一次对齐的时限（秒），缺省 60
    /// </summary>
    /// <remarks>
    /// 启动时对齐完成之前节点不就绪，超时的插件记为加载失败，节点照常就绪
    /// </remarks>
    public int ReconcileTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// 从配置读取
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <param name="environment">宿主环境（没有时按当前目录解析相对路径）</param>
    /// <returns>选项</returns>
    /// <exception cref="ArgumentException">隔离级别不认得或为空，大小上限或时限不是正整数</exception>
    public static PluginsOptions FromConfiguration(IConfiguration configuration, IHostEnvironment? environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var defaults = new PluginsOptions();
        var isolations = section.GetSection(nameof(Isolations)).GetChildren().Select(static c => c.Value).ToList();
        var cache = section[nameof(CacheDirectory)];
        return new PluginsOptions
        {
            Isolations = isolations.Count == 0 ? defaults.Isolations : isolations.Select(IsolationOf).ToHashSet(),
            MaxPackageSizeMegabytes = PositiveOf(section, nameof(MaxPackageSizeMegabytes), defaults.MaxPackageSizeMegabytes),
            CacheDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(cache) ? "App_Data/plugins" : cache, environment?.ContentRootPath ?? Directory.GetCurrentDirectory()),
            ReconcileTimeoutSeconds = PositiveOf(section, nameof(ReconcileTimeoutSeconds), defaults.ReconcileTimeoutSeconds),
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
    /// 读一个正整数键，没写取缺省
    /// </summary>
    private static int PositiveOf(IConfigurationSection section, string key, int fallback)
    {
        var value = section[key];
        return string.IsNullOrEmpty(value)
            ? fallback
            : int.TryParse(value, out var parsed) && parsed > 0 ? parsed : throw new ArgumentException($"'{Section}:{key}' must be a positive integer, but was '{value}'.");
    }

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
