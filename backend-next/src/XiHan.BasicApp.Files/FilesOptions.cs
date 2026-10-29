// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包的配置（<c>BasicApp:Files</c>）。
/// </summary>
public sealed class FilesOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string Section = "BasicApp:Files";

    /// <summary>
    /// 本地存储的根目录。
    /// </summary>
    /// <remarks>
    /// 相对路径按内容根目录解析。缺省 <c>App_Data/files</c>。各本地存储配置的桶是它下面的子目录。
    /// </remarks>
    public string LocalRootPath { get; init; } = "App_Data/files";

    /// <summary>
    /// 单个文件的大小上限（字节），缺省 100 MiB。
    /// </summary>
    public long MaxUploadBytes { get; init; } = 100L * 1024 * 1024;

    /// <summary>
    /// 签名下载地址的缺省有效期，缺省 30 分钟。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public TimeSpan LinkLifetime { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 签名下载地址的最长有效期，缺省 7 天。
    /// </summary>
    public TimeSpan MaxLinkLifetime { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 从配置读取文件包选项。
    /// </summary>
    /// <remarks>
    /// 本地根目录按宿主的内容根目录解析成绝对路径。
    /// </remarks>
    /// <param name="configuration">配置。</param>
    /// <param name="environment">宿主环境（没有时按当前目录解析）。</param>
    /// <returns>选项。</returns>
    public static FilesOptions FromConfiguration(IConfiguration configuration, IHostEnvironment? environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var defaults = new FilesOptions();
        var root = section[nameof(LocalRootPath)] is { Length: > 0 } configured ? configured : defaults.LocalRootPath;
        return new FilesOptions
        {
            LocalRootPath = Path.GetFullPath(root, environment?.ContentRootPath ?? Directory.GetCurrentDirectory()),
            MaxUploadBytes = long.TryParse(section[nameof(MaxUploadBytes)], out var max) && max > 0 ? max : defaults.MaxUploadBytes,
            LinkLifetime = TimeSpan.TryParse(section[nameof(LinkLifetime)], out var lifetime) && lifetime > TimeSpan.Zero ? lifetime : defaults.LinkLifetime,
            MaxLinkLifetime = TimeSpan.TryParse(section[nameof(MaxLinkLifetime)], out var longest) && longest > TimeSpan.Zero ? longest : defaults.MaxLinkLifetime,
        };
    }
}
