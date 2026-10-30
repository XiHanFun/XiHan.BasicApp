// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 结构升级的配置。
/// </summary>
/// <remarks>
/// 配置节 <c>BasicApp:Schema</c>。
/// </remarks>
public sealed record SchemaOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Schema";

    /// <summary>
    /// 启动时是否自动升级。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>AutoUpgrade</c>，缺省开。关掉时由运维先执行 <c>schema upgrade</c> 命令再启动。
    /// </remarks>
    public bool AutoUpgrade { get; init; } = true;

    /// <summary>
    /// 脚本目录。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>Directory</c>，缺省应用目录下的 <c>Schema</c>；其下每个版本一个子目录（如 <c>6.0.0</c>），目录里的 <c>.sql</c> 按文件名顺序执行。
    /// </remarks>
    public string Directory { get; init; } = Path.Combine(AppContext.BaseDirectory, "Schema");

    /// <summary>
    /// 升级锁的失效时间。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LockExpiryMinutes</c>，缺省 30 分钟；持锁节点中途退出时，过了这么久别的节点才能接手。
    /// </remarks>
    public TimeSpan LockExpiry { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 等别的节点升级完的最长时间。
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LockWaitMinutes</c>，缺省 10 分钟；等不到即升级失败、中断启动。
    /// </remarks>
    public TimeSpan LockWait { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 读取配置。
    /// </summary>
    public static SchemaOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new SchemaOptions();
        var section = configuration.GetSection(ConfigurationSection);
        if (section["AutoUpgrade"] is { Length: > 0 } auto)
        {
            options = bool.TryParse(auto, out var enabled)
                ? options with { AutoUpgrade = enabled }
                : throw new ArgumentException($"'{ConfigurationSection}:AutoUpgrade' must be 'true' or 'false', but was '{auto}'.");
        }

        if (section["Directory"] is { Length: > 0 } directory)
        {
            options = options with { Directory = Path.IsPathRooted(directory) ? directory : Path.Combine(AppContext.BaseDirectory, directory) };
        }

        options = options with
        {
            LockExpiry = Minutes(section, "LockExpiryMinutes", options.LockExpiry),
            LockWait = Minutes(section, "LockWaitMinutes", options.LockWait),
        };
        return options;
    }

    private static TimeSpan Minutes(IConfigurationSection section, string key, TimeSpan fallback)
        => section[key] is not { Length: > 0 } text
            ? fallback
            : int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) && minutes is >= 1 and <= 1440
                ? TimeSpan.FromMinutes(minutes)
                : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a whole number of minutes between 1 and 1440, but was '{text}'.");
}
