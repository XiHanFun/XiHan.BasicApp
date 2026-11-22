// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XiHan.Framework.Kernel.Capabilities;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 读出来的插件包 <c>.xhplugin</c>
/// </summary>
/// <remarks>
/// zip：根上是框架清单 <c>xihan.manifest.json</c> 与旁挂清单 <c>basicapp.plugin.json</c>，进程外插件的可执行文件在 <c>runtimes/&lt;运行时标识&gt;/</c> 下，
/// Wasm 模块在根上，声明式页面在 <c>pages/</c>，前端文案在 <c>locales/&lt;区域&gt;.json</c>
/// 只读条目清单与两份清单，不解压；条目路径要是相对、不含 <c>..</c> 的正斜杠路径，条目数与解压后的总大小有上限，防路径穿越与解压炸弹
/// </remarks>
public sealed class PluginPackage
{
    /// <summary>
    /// 框架清单的条目名
    /// </summary>
    public const string ManifestEntry = ManifestReader.FileName;

    /// <summary>
    /// 旁挂清单的条目名
    /// </summary>
    public const string DescriptorEntry = "basicapp.plugin.json";

    /// <summary>
    /// 条目数上限
    /// </summary>
    private const int MaxEntries = 10_000;

    /// <summary>
    /// 一份清单的大小上限（字节）
    /// </summary>
    private const int MaxManifestBytes = 1024 * 1024;

    /// <summary>
    /// 严格的 UTF-8：坏字节按读不出处理，不换成替换字符
    /// </summary>
    private static readonly UTF8Encoding _utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// 创建读出的包
    /// </summary>
    private PluginPackage(Manifest manifest, string manifestJson, PluginDescriptor descriptor, string descriptorJson, IReadOnlySet<string> entries, string sha256, long size)
    {
        Manifest = manifest;
        ManifestJson = manifestJson;
        Descriptor = descriptor;
        DescriptorJson = descriptorJson;
        Entries = entries;
        Sha256 = sha256;
        Size = size;
    }

    /// <summary>
    /// 框架清单
    /// </summary>
    public Manifest Manifest { get; }

    /// <summary>
    /// 框架清单原文
    /// </summary>
    public string ManifestJson { get; }

    /// <summary>
    /// 旁挂清单
    /// </summary>
    public PluginDescriptor Descriptor { get; }

    /// <summary>
    /// 旁挂清单原文
    /// </summary>
    public string DescriptorJson { get; }

    /// <summary>
    /// 全部文件条目的路径（不含目录条目）
    /// </summary>
    public IReadOnlySet<string> Entries { get; }

    /// <summary>
    /// 包的 SHA-256（小写十六进制）
    /// </summary>
    public string Sha256 { get; }

    /// <summary>
    /// 包的字节数
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// 从可定位的流读包
    /// </summary>
    /// <remarks>
    /// 不是 zip <c>PLUGINS_VALIDATION_101</c>（<c>plugins.package.unreadable</c>），超过上限 <c>plugins.package.too_large</c>，路径不安全 <c>plugins.package.entry_unsafe</c>，缺清单 <c>plugins.package.entry_missing</c>；
    /// 框架清单读不出原样返回框架的错误，旁挂清单读不出 <c>PLUGINS_VALIDATION_102</c>
    /// </remarks>
    /// <param name="stream">包内容，可定位</param>
    /// <param name="maxBytes">包的大小上限（字节）；解压后的总大小上限是它的 4 倍</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<Result<PluginPackage>> ReadAsync(Stream stream, long maxBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The package stream must be seekable.", nameof(stream));
        }

        var megabytes = maxBytes / (1024 * 1024);
        if (stream.Length > maxBytes)
        {
            return TooLarge(megabytes);
        }

        stream.Position = 0;
        var sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
        stream.Position = 0;
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            return Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageUnreadable);
        }

        using (archive)
        {
            if (archive.Entries.Count > MaxEntries)
            {
                return TooLarge(megabytes);
            }

            var entries = new HashSet<string>(StringComparer.Ordinal);
            long expanded = 0;
            foreach (var entry in archive.Entries)
            {
                if (!IsSafe(entry.FullName))
                {
                    return Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageEntryUnsafe, MessageArguments.Of(("entry", entry.FullName)));
                }

                // 目录条目只占位，不算文件
                if (entry.FullName.EndsWith('/'))
                {
                    continue;
                }

                expanded += entry.Length;
                if (expanded > maxBytes * 4 || !entries.Add(entry.FullName))
                {
                    return expanded > maxBytes * 4 ? TooLarge(megabytes) : Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageEntryUnsafe, MessageArguments.Of(("entry", entry.FullName)));
                }
            }

            var manifestText = await ReadTextAsync(archive, ManifestEntry, cancellationToken).ConfigureAwait(false);
            if (!manifestText.TryGetValue(out var manifestJson))
            {
                return manifestText.Error!;
            }

            var manifest = ManifestReader.Read(manifestJson);
            if (!manifest.TryGetValue(out var read))
            {
                return manifest.Error!;
            }

            var descriptorText = await ReadTextAsync(archive, DescriptorEntry, cancellationToken).ConfigureAwait(false);
            if (!descriptorText.TryGetValue(out var descriptorJson))
            {
                return descriptorText.Error!;
            }

            PluginDescriptor? descriptor;
            try
            {
                descriptor = JsonSerializer.Deserialize(descriptorJson, PluginsJsonContext.Default.PluginDescriptor);
            }
            catch (JsonException exception)
            {
                return Error.Validation(PluginsErrors.DescriptorInvalid, PluginsMessages.DescriptorInvalid, MessageArguments.Of(("path", exception.Path ?? "$")));
            }

            return descriptor is null
                ? Error.Validation(PluginsErrors.DescriptorInvalid, PluginsMessages.DescriptorInvalid, MessageArguments.Of(("path", "$")))
                : new PluginPackage(read, manifestJson, descriptor, descriptorJson, entries, sha256, stream.Length);
        }
    }

    /// <summary>
    /// 条目路径是否安全
    /// </summary>
    /// <remarks>
    /// 只收相对的正斜杠路径，不许空段、<c>.</c>、<c>..</c>、反斜杠与盘符
    /// </remarks>
    private static bool IsSafe(string path)
    {
        if (path.Length == 0 || path.StartsWith('/') || path.Contains('\\', StringComparison.Ordinal) || path.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        var segments = path.TrimEnd('/').Split('/');
        return segments.All(static s => s.Length > 0 && s != "." && s != "..");
    }

    /// <summary>
    /// 读一个清单条目的文本
    /// </summary>
    private static async Task<Result<string>> ReadTextAsync(ZipArchive archive, string name, CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(name);
        if (entry is null || entry.Length > MaxManifestBytes)
        {
            return Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageEntryMissing, MessageArguments.Of(("entry", name)));
        }

        var content = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (content.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            try
            {
                return _utf8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
            }
            catch (DecoderFallbackException)
            {
                return Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageEntryMissing, MessageArguments.Of(("entry", name)));
            }
        }
    }

    /// <summary>
    /// 包超过上限的错误
    /// </summary>
    private static Error TooLarge(long megabytes)
        => Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageTooLarge, MessageArguments.Of(("max", megabytes)));
}
