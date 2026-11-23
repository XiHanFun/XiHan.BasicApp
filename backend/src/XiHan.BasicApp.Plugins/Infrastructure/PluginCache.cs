// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 插件包在本机的解包目录（单例）
/// </summary>
/// <remarks>
/// 每个版本解到 <c>&lt;缓存目录&gt;/&lt;清单 ID&gt;/&lt;版本 ID&gt;/</c>：版本的内容不会变，目录在就直接用；不在时从 Files 取包、按安装时的 SHA-256 核对、解到临时目录再整体改名，半途失败不留半个目录
/// 当前平台的 <c>runtimes/&lt;运行时标识&gt;/</c> 铺到目录的根上，与框架清单放在一起：框架宿主按清单的 <c>entry</c> 相对插件目录找入口，插件按自己所在的目录读清单；别的平台的运行时目录不解
/// 非 Windows 上给铺过来的运行时文件加执行位（zip 带的权限不可靠）
/// 存储读不出（对象或它的元数据坏了）同样算包不可用，异常写进日志
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="options">插件模块选项</param>
/// <param name="logger">日志</param>
public sealed partial class PluginCache(IServiceScopeFactory scopes, PluginsOptions options, ILogger<PluginCache> logger)
{
    private const int BufferSize = 81920;

    /// <summary>
    /// 一个版本的解包目录
    /// </summary>
    /// <param name="manifestId">清单 ID</param>
    /// <param name="versionId">版本 ID</param>
    public string DirectoryOf(string manifestId, Guid versionId) => Path.Combine(options.CacheDirectory, manifestId, versionId.ToString("N"));

    /// <summary>
    /// 确保版本已解包
    /// </summary>
    /// <param name="release">要加载的版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解包目录；包文件不在或散列不符 <c>PLUGINS_UNAVAILABLE_102</c></returns>
    public async Task<Result<string>> EnsureAsync(PluginRelease release, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(release);
        var target = DirectoryOf(release.ManifestId, release.VersionId);
        if (Directory.Exists(target))
        {
            return target;
        }

        var staging = $"{target}.staging-{Guid.NewGuid():N}";
        try
        {
            Result extracted;
            try
            {
                extracted = await StoreScope.ReadAsync(scopes, (services, ct) => ExtractAsync(services.GetRequiredService<FileService>(), release, staging, ct), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogUnreadable(logger, exception, release.ManifestId, release.Version);
                return Unavailable(release);
            }

            if (extracted.IsFailure)
            {
                return extracted.Error!;
            }

            try
            {
                Directory.Move(staging, target);
            }
            catch (IOException) when (Directory.Exists(target))
            {
                // 另一次对齐同时解好了同一版本：内容相同，用它的
            }

            return target;
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static Error Unavailable(PluginRelease release)
        => Error.Unavailable(PluginsErrors.PackageUnavailable, PluginsMessages.PackageUnavailable, MessageArguments.Of(("name", release.Name), ("version", release.Version)));

    /// <summary>
    /// 取包、核对散列并解到临时目录
    /// </summary>
    private async Task<Result> ExtractAsync(FileService files, PluginRelease release, string staging, CancellationToken cancellationToken)
    {
        var found = await files.FindAsync(TenantIds.Platform, release.FileId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return Unavailable(release);
        }

        var opened = await files.OpenAsync(file, null, cancellationToken).ConfigureAwait(false);
        if (!opened.TryGetValue(out var content))
        {
            return Unavailable(release);
        }

        var temporary = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, BufferSize, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await using (temporary.ConfigureAwait(false))
        {
            await using (content.ConfigureAwait(false))
            {
                await content.Content.CopyToAsync(temporary, BufferSize, cancellationToken).ConfigureAwait(false);
            }

            // 安装时核对过的包：这里只认散列，条目的安全规则由读包时一并再查一遍
            var read = await PluginPackage.ReadAsync(temporary, options.MaxPackageBytes, cancellationToken).ConfigureAwait(false);
            if (!read.TryGetValue(out var package) || !string.Equals(package.Sha256, release.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return Unavailable(release);
            }

            temporary.Position = 0;
            using var archive = new ZipArchive(temporary, ZipArchiveMode.Read, leaveOpen: true);
            var root = Path.GetFullPath(staging) + Path.DirectorySeparatorChar;
            var executables = $"runtimes/{RuntimeInformation.RuntimeIdentifier}/";
            foreach (var entry in archive.Entries.Where(static e => !e.FullName.EndsWith('/')))
            {
                var runtime = entry.FullName.StartsWith(executables, StringComparison.Ordinal);
                if (!runtime && entry.FullName.StartsWith("runtimes/", StringComparison.Ordinal))
                {
                    continue;
                }

                var destination = Path.GetFullPath(Path.Combine(staging, runtime ? entry.FullName[executables.Length..] : entry.FullName));
                if (!destination.StartsWith(root, StringComparison.Ordinal))
                {
                    return Unavailable(release);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                // 安装时已核对过运行时文件不与根上的同名，这里撞上同样当包不可用
                await entry.ExtractToFileAsync(destination, overwrite: false, cancellationToken).ConfigureAwait(false);
                if (!OperatingSystem.IsWindows() && runtime)
                {
                    File.SetUnixFileMode(destination, File.GetUnixFileMode(destination) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                }
            }
        }

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The package of plugin {ManifestId} {Version} could not be read from file storage.")]
    private static partial void LogUnreadable(ILogger logger, Exception exception, string manifestId, string version);
}
