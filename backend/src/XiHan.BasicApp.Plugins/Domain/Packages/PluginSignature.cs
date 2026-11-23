// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 插件包的发布者签名
/// </summary>
/// <remarks>
/// 签名文件在包根上（<see cref="Entry"/>），签的是包的内容清单：除签名文件之外的每个文件一行「SHA-256（小写十六进制）、两个空格、条目路径、换行」，按路径序数排序，与 <c>sha256sum</c> 的输出同形
/// 算法固定为 ECDSA P-256 + SHA-256，签名按 DER 编码（与 OpenSSL 一致）；公钥指纹是公钥 SubjectPublicKeyInfo DER 的 SHA-256（小写十六进制）
/// 旁挂清单在内容清单里，签名因此也绑定了发布者；包改过一个字节，内容清单就对不上
/// </remarks>
public static class PluginSignature
{
    /// <summary>
    /// 签名文件的条目名
    /// </summary>
    public const string Entry = "basicapp.signature.json";

    /// <summary>
    /// 签名算法
    /// </summary>
    public const string Algorithm = "ecdsa-p256-sha256";

    private const int BufferSize = 81920;

    /// <summary>
    /// P-256 曲线的 OID
    /// </summary>
    private const string P256 = "1.2.840.10045.3.1.7";

    /// <summary>
    /// 公钥指纹
    /// </summary>
    /// <param name="subjectPublicKeyInfo">公钥的 SubjectPublicKeyInfo DER</param>
    public static string KeyIdOf(ReadOnlySpan<byte> subjectPublicKeyInfo) => Convert.ToHexStringLower(SHA256.HashData(subjectPublicKeyInfo));

    /// <summary>
    /// 读出公钥：要是 P-256 的 SubjectPublicKeyInfo DER
    /// </summary>
    /// <param name="subjectPublicKeyInfo">公钥的 SubjectPublicKeyInfo DER</param>
    /// <returns>公钥；读不出或不是 P-256 为空</returns>
    public static ECDsa? ImportPublicKey(ReadOnlySpan<byte> subjectPublicKeyInfo)
    {
        var key = ECDsa.Create();
        try
        {
            key.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out var read);
            // Windows 上导出的命名曲线可能只带友好名
            var curve = key.ExportParameters(false).Curve.Oid;
            if (read == subjectPublicKeyInfo.Length && (curve.Value == P256 || (curve.Value is null && curve.FriendlyName is "nistP256" or "ECDSA_P256")))
            {
                return key;
            }
        }
        catch (CryptographicException)
        {
            // 落到下面：不是能用的公钥
        }

        key.Dispose();
        return null;
    }

    /// <summary>
    /// 算包的内容清单
    /// </summary>
    /// <remarks>
    /// 逐个解压条目算散列，不整个读进内存；条目路径与大小的安全规则由调用方先核对
    /// </remarks>
    /// <param name="archive">插件包</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<string> ContentListAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archive);
        var lines = new StringBuilder();
        var buffer = new byte[BufferSize];
        foreach (var entry in archive.Entries.Where(static e => !e.FullName.EndsWith('/') && e.FullName != Entry).OrderBy(static e => e.FullName, StringComparer.Ordinal))
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var content = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using (content.ConfigureAwait(false))
            {
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    hash.AppendData(buffer, 0, read);
                }
            }

            lines.Append(Convert.ToHexStringLower(hash.GetHashAndReset())).Append("  ").Append(entry.FullName).Append('\n');
        }

        return lines.ToString();
    }

    /// <summary>
    /// 核对签名
    /// </summary>
    /// <param name="contentList">包的内容清单</param>
    /// <param name="signature">签名（DER）</param>
    /// <param name="publicKey">发布者的公钥</param>
    public static bool Verify(string contentList, ReadOnlySpan<byte> signature, ECDsa publicKey)
    {
        ArgumentNullException.ThrowIfNull(contentList);
        ArgumentNullException.ThrowIfNull(publicKey);
        return publicKey.VerifyData(Encoding.UTF8.GetBytes(contentList), signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    /// <summary>
    /// 给插件包签名：写入（或换掉）签名文件
    /// </summary>
    /// <remarks>
    /// 旁挂清单要写了发布者：可信发布者按发布者名与公钥指纹一起找，没有发布者的包签了也无从受信
    /// </remarks>
    /// <param name="package">插件包，可读写、可定位</param>
    /// <param name="privateKey">P-256 私钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>公钥指纹</returns>
    /// <exception cref="InvalidDataException">不是 zip、没有旁挂清单或旁挂清单没写发布者</exception>
    public static async Task<string> SignAsync(Stream package, ECDsa privateKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(privateKey);
        string contentList;
        package.Position = 0;
        using (var archive = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true))
        {
            await RequirePublisherAsync(archive, cancellationToken).ConfigureAwait(false);
            contentList = await ContentListAsync(archive, cancellationToken).ConfigureAwait(false);
        }

        var keyId = KeyIdOf(privateKey.ExportSubjectPublicKeyInfo());
        var document = new PluginSignatureDocument
        {
            Algorithm = Algorithm,
            Key = keyId,
            Signature = Convert.ToBase64String(privateKey.SignData(Encoding.UTF8.GetBytes(contentList), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)),
        };
        package.Position = 0;
        using (var archive = new ZipArchive(package, ZipArchiveMode.Update, leaveOpen: true))
        {
            archive.GetEntry(Entry)?.Delete();
            var content = await archive.CreateEntry(Entry).OpenAsync(cancellationToken).ConfigureAwait(false);
            await using (content.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(content, document, PluginsJsonContext.Default.PluginSignatureDocument, cancellationToken).ConfigureAwait(false);
            }
        }

        return keyId;
    }

    /// <summary>
    /// 旁挂清单要写了发布者
    /// </summary>
    private static async Task RequirePublisherAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(PluginPackage.DescriptorEntry) ?? throw new InvalidDataException($"The package has no {PluginPackage.DescriptorEntry}.");
        var content = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (content.ConfigureAwait(false))
        {
            using var descriptor = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!descriptor.RootElement.TryGetProperty("publisher", out var publisher) || publisher.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(publisher.GetString()))
            {
                throw new InvalidDataException($"{PluginPackage.DescriptorEntry} names no publisher; trusted publishers are looked up by publisher and key.");
            }
        }
    }
}

/// <summary>
/// 签名文件 <c>basicapp.signature.json</c>
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginSignatureDocument
{
    /// <summary>
    /// 算法，当前只有 <c>ecdsa-p256-sha256</c>
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// 签名公钥的指纹
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 签名（DER 的 Base64）
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// 从包里读出的签名
/// </summary>
/// <param name="KeyId">签名公钥的指纹</param>
/// <param name="Value">签名（DER）</param>
/// <param name="ContentList">按包现在的内容算出的内容清单</param>
public sealed record PluginPackageSignature(string KeyId, ReadOnlyMemory<byte> Value, string ContentList);
