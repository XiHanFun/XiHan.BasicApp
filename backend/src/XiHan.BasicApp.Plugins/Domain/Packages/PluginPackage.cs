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
/// Wasm 模块在根上，声明式页面在 <c>pages/</c>，前端文案在 <c>locales/&lt;区域&gt;.json</c>，发布者签名在根上的 <c>basicapp.signature.json</c>（<see cref="PluginSignature"/>）
/// 只读条目清单、两份清单、前端文件（文案与页面描述）与签名，其余不解压；带签名时逐个条目算散列得出内容清单，供信任判定核对；条目路径要是相对、不含 <c>..</c> 的正斜杠路径，条目数与解压后的总大小有上限，防路径穿越与解压炸弹
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
    private PluginPackage(Manifest manifest, string manifestJson, PluginDescriptor descriptor, string descriptorJson, IReadOnlySet<string> entries, IReadOnlyDictionary<string, string> documents, PluginPackageSignature? signature, string sha256, long size)
    {
        Signature = signature;
        Manifest = manifest;
        ManifestJson = manifestJson;
        Descriptor = descriptor;
        DescriptorJson = descriptorJson;
        Entries = entries;
        Documents = documents;
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
    /// 前端文件的原文：包里有的 10 种区域文案与旁挂清单引用的页面描述，按条目路径
    /// </summary>
    public IReadOnlyDictionary<string, string> Documents { get; }

    /// <summary>
    /// 发布者签名
    /// </summary>
    /// <remarks>
    /// 包里没有签名文件为空
    /// </remarks>
    public PluginPackageSignature? Signature { get; }

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
    /// 框架清单读不出原样返回框架的错误，旁挂清单读不出 <c>PLUGINS_VALIDATION_102</c>，签名文件读不出 <c>PLUGINS_VALIDATION_109</c>
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

            if (descriptor is null)
            {
                return Error.Validation(PluginsErrors.DescriptorInvalid, PluginsMessages.DescriptorInvalid, MessageArguments.Of(("path", "$")));
            }

            // 缺的文件由核对报错，这里只读包里有的
            var documents = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in PluginInspector.Locales.Select(PluginTexts.EntryOf).Concat(descriptor.Pages.Select(static p => p.View.File)).Where(entries.Contains).Distinct(StringComparer.Ordinal))
            {
                var text = await ReadTextAsync(archive, path, cancellationToken).ConfigureAwait(false);
                if (!text.TryGetValue(out var content))
                {
                    return text.Error!;
                }

                documents[path] = content;
            }

            PluginPackageSignature? signature = null;
            if (entries.Contains(PluginSignature.Entry))
            {
                var signed = await ReadSignatureAsync(archive, cancellationToken).ConfigureAwait(false);
                if (!signed.TryGetValue(out signature))
                {
                    return signed.Error!;
                }
            }

            return new PluginPackage(read, manifestJson, descriptor, descriptorJson, entries, documents, signature, sha256, stream.Length);
        }
    }

    /// <summary>
    /// 读签名文件并算出包的内容清单
    /// </summary>
    /// <remarks>
    /// 算法要是 <see cref="PluginSignature.Algorithm"/>，公钥指纹是 64 位小写十六进制，签名是 Base64；不认得的字段同样当读不出
    /// </remarks>
    private static async Task<Result<PluginPackageSignature>> ReadSignatureAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var text = await ReadTextAsync(archive, PluginSignature.Entry, cancellationToken).ConfigureAwait(false);
        if (!text.TryGetValue(out var json))
        {
            return text.Error!;
        }

        PluginSignatureDocument? document;
        try
        {
            document = JsonSerializer.Deserialize(json, PluginsJsonContext.Default.PluginSignatureDocument);
        }
        catch (JsonException)
        {
            document = null;
        }

        var value = new byte[document?.Signature.Length ?? 0];
        if (document is null
            || !string.Equals(document.Algorithm, PluginSignature.Algorithm, StringComparison.Ordinal)
            || document.Key.Length != 64
            || !document.Key.All(char.IsAsciiHexDigitLower)
            || !Convert.TryFromBase64String(document.Signature, value, out var written)
            || written == 0)
        {
            return Error.Validation(PluginsErrors.SignatureInvalid, PluginsMessages.SignatureUnreadable);
        }

        var contentList = await PluginSignature.ContentListAsync(archive, cancellationToken).ConfigureAwait(false);
        return new PluginPackageSignature(document.Key, value.AsMemory(0, written), contentList);
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
    /// 读一个清单或前端文件条目的文本
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
