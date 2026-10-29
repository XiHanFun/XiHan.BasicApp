// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Amazon.S3;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.ObjectStorage.Abstractions.Blobs;
using XiHan.Framework.ObjectStorage.FileSystem;
using XiHan.Framework.ObjectStorage.S3;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 按存储配置给出对象存储实例（框架只按启动配置装一个 <see cref="IBlobStore"/>，存储配置是运行时数据，实例由这里建）：
/// 本地存储共用一个以 <see cref="FilesOptions.LocalRootPath"/> 为根的目录存储，桶是根下的子目录；S3 兼容存储每条配置一个客户端，配置改了（地址、区域、桶、密钥）就重建。
/// 换下来的客户端可能还有进行中的读写，不立即释放，随应用停止一并释放。容量由租户配额管，不用目录存储自己的总量上限。
/// 过渡实现：框架的命名存储实例（开发计划 P5-31）推送、锁定升上去后改用框架的，这个类随之拿掉。
/// </summary>
/// <param name="options">文件包配置。</param>
/// <param name="secrets">密钥保护（解开访问密钥）。</param>
/// <param name="clock">时钟。</param>
public sealed class BlobStores(FilesOptions options, ISecretProtector secrets, TimeProvider clock) : IDisposable
{
    /// <summary>
    /// 访问密钥的保护用途。
    /// </summary>
    public const string SecretPurpose = "XiHan.BasicApp.Files.StorageSecret";

    private readonly ConcurrentDictionary<Guid, Entry> _remote = new();
    private readonly ConcurrentBag<IAmazonS3> _clients = [];
    private readonly Lock _gate = new();
    private readonly Lazy<FileSystemBlobStore> _local = new(() => new FileSystemBlobStore(new FileSystemObjectStorageOptions
    {
        RootPath = options.LocalRootPath,
        MaxTotalBytes = long.MaxValue,
        MaxBlobBytes = options.MaxUploadBytes,
    }, clock));

    /// <summary>
    /// 这条配置的对象存储。
    /// </summary>
    /// <param name="config">存储配置。</param>
    /// <returns>对象存储；访问密钥解不开时失败。</returns>
    public Result<IBlobStore> For(SysStorageConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Type == StorageType.Local)
        {
            return Result.Success<IBlobStore>(_local.Value);
        }

        var fingerprint = string.Join('|', config.Type, config.Endpoint, config.Region, config.Bucket, config.AccessKeyId, config.SecretAccessKey);
        if (_remote.TryGetValue(config.Id, out var cached) && string.Equals(cached.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return Result.Success<IBlobStore>(cached.Store);
        }

        if (config.SecretAccessKey is null || !secrets.TryUnprotect(SecretPurpose, config.SecretAccessKey, out var secret))
        {
            return Error.Unavailable(FilesErrors.SecretUndecryptable, FilesMessages.SecretUndecryptable, MessageArguments.Of(("code", config.Code)));
        }

        lock (_gate)
        {
            if (_remote.TryGetValue(config.Id, out cached) && string.Equals(cached.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                return Result.Success<IBlobStore>(cached.Store);
            }

            var settings = new S3ObjectStorageOptions
            {
                ServiceUrl = config.Endpoint,
                Region = string.IsNullOrWhiteSpace(config.Region) ? "us-east-1" : config.Region,
                AccessKey = config.AccessKeyId,
                SecretKey = secret,
                // OSS 与 COS 只接受虚拟主机式访问，S3 兼容的自建存储（MinIO 等）多用路径式
                ForcePathStyle = config.Type is StorageType.S3 or StorageType.MinIO,
            };
            var client = S3ObjectStorageFeature.CreateClient(settings);
            _clients.Add(client);
            var store = new S3BlobStore(client, settings, clock);
            _remote[config.Id] = new Entry(fingerprint, store);
            return Result.Success<IBlobStore>(store);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        _clients.Clear();
        _remote.Clear();
    }

    private sealed record Entry(string Fingerprint, IBlobStore Store);
}
