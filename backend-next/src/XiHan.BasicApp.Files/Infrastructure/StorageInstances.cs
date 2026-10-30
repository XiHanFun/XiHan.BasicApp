// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Hosting.AspNetCore.Blobs;
using XiHan.Framework.ObjectStorage.Abstractions.Blobs;
using XiHan.Framework.ObjectStorage.Abstractions.Signing;
using XiHan.Framework.ObjectStorage.Abstractions.Stores;
using XiHan.Framework.ObjectStorage.FileSystem;
using XiHan.Framework.ObjectStorage.S3;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 存储配置对应的框架命名存储实例。
/// </summary>
/// <remarks>
/// 实例由框架的注册表 <see cref="BlobStores"/> 建、按名缓存与释放（ADR-0050），这里只把存储配置译成定义：
/// 本地存储共用一个实例 <see cref="LocalName"/>（以 <see cref="FilesOptions.LocalRootPath"/> 为根，桶是根下的子目录，启动时登记）；
/// S3 兼容存储每条配置一个实例 <c>storage-&lt;配置 ID&gt;</c>，配置改了（地址、区域、桶、密钥）注册表建新实例替换旧的，删配置时移除。
/// 容量由租户配额管，本地实例不设目录存储自己的总量上限。
/// 签名地址（ADR-0049）：S3 兼容存储的实例自己签名、客户端直连存储；本地实例由宿主的签名端点（<c>MapXiHanBlobs()</c>）代收发。
/// </remarks>
/// <param name="stores">框架的命名存储实例注册表。</param>
/// <param name="hosted">宿主代收发的签名器。</param>
/// <param name="options">文件包配置。</param>
/// <param name="secrets">密钥保护（解开访问密钥）。</param>
public sealed class StorageInstances(BlobStores stores, HostedBlobUrlSigner hosted, FilesOptions options, ISecretProtector secrets)
{
    /// <summary>
    /// 访问密钥的保护用途。
    /// </summary>
    public const string SecretPurpose = "XiHan.BasicApp.Files.StorageSecret";

    /// <summary>
    /// 本地存储的实例名。
    /// </summary>
    public const string LocalName = "local";

    private const string RemotePrefix = "storage-";

    private readonly HostedBlobUrlSigner _localSigner = hosted.ForStore(LocalName);

    /// <summary>
    /// 配置对应的实例名。
    /// </summary>
    /// <param name="config">存储配置。</param>
    /// <returns>实例名。</returns>
    public static string NameOf(SysStorageConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.Type == StorageType.Local ? LocalName : RemotePrefix + config.Id.ToString("N");
    }

    /// <summary>
    /// 取本地存储的实例。
    /// </summary>
    /// <remarks>
    /// 启动时调一次：签名地址在进程重启后仍有效，宿主的签名端点按实例名到注册表里取，实例要先登记上。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实例；根目录建不出来时失败。</returns>
    public ValueTask<Result<IBlobStore>> LocalAsync(CancellationToken cancellationToken)
        => stores.GetOrAddAsync(new BlobStoreDefinition(LocalName, FileSystemBlobStoreProvider.ProviderName, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RootPath"] = options.LocalRootPath,
            ["MaxTotalBytes"] = long.MaxValue.ToString(CultureInfo.InvariantCulture),
            ["MaxBlobBytes"] = options.MaxUploadBytes.ToString(CultureInfo.InvariantCulture),
        }), cancellationToken);

    /// <summary>
    /// 取这条配置的实例。
    /// </summary>
    /// <param name="config">存储配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实例；访问密钥解不开或设置不合法时失败。</returns>
    public async ValueTask<Result<IBlobStore>> GetAsync(SysStorageConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Type == StorageType.Local)
        {
            return await LocalAsync(cancellationToken).ConfigureAwait(false);
        }

        if (config.SecretAccessKey is null || !secrets.TryUnprotect(SecretPurpose, config.SecretAccessKey, out var secret))
        {
            return Error.Unavailable(FilesErrors.SecretUndecryptable, FilesMessages.SecretUndecryptable, MessageArguments.Of(("code", config.Code)));
        }

        var settings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ServiceUrl"] = config.Endpoint ?? string.Empty,
            ["Region"] = string.IsNullOrWhiteSpace(config.Region) ? "us-east-1" : config.Region,
            ["AccessKey"] = config.AccessKeyId ?? string.Empty,
            ["SecretKey"] = secret,
            // OSS 与 COS 只接受虚拟主机式访问，S3 兼容的自建存储（MinIO 等）多用路径式
            ["ForcePathStyle"] = config.Type is StorageType.S3 or StorageType.MinIO ? "true" : "false",
            // 健康项探这条配置的桶：只授权了一个桶的密钥列不了桶
            ["HealthBucket"] = config.Bucket,
        };
        return await stores.GetOrAddAsync(new BlobStoreDefinition(NameOf(config), S3BlobStoreProvider.ProviderName, settings), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 这条配置的实例用的签名器。
    /// </summary>
    /// <param name="config">存储配置。</param>
    /// <param name="store">它的实例。</param>
    /// <returns>签名器。</returns>
    public IBlobUrlSigner SignerOf(SysStorageConfig config, IBlobStore store)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(store);
        return config.Type == StorageType.Local
            ? _localSigner
            : store as IBlobUrlSigner ?? throw new InvalidOperationException($"The store of storage config '{config.Code}' cannot sign URLs.");
    }

    /// <summary>
    /// 删配置后移除它的实例。
    /// </summary>
    /// <remarks>
    /// 本地实例为全部本地配置共用，不移除。之后再取用会按当时的配置重建，事务回滚了也不影响。
    /// </remarks>
    /// <param name="config">存储配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async ValueTask RemoveAsync(SysStorageConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Type != StorageType.Local)
        {
            await stores.RemoveAsync(NameOf(config), cancellationToken).ConfigureAwait(false);
        }
    }
}
