#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StorageProviderResolver
// Guid:f6a7b8c9-d0e1-4b53-bf6a-7b8c9d0e1f2a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.ObjectStorage;
using XiHan.Framework.ObjectStorage.Constants;
using XiHan.Framework.ObjectStorage.Options;
using XiHan.Framework.ObjectStorage.Providers;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 存储提供程序解析器实现（单例）
/// </summary>
/// <remarks>
/// <para>单例持有按配置指纹缓存的 Provider；指纹随凭证/端点/桶/启停变化即重建，实现"改配置即生效"的热更新。</para>
/// <para>仅"默认且启用、对象存储、凭证齐全"的 <c>SysStorageConfig</c> 由 DB 驱动；本地存储与残缺配置回退框架 appsettings 路由。</para>
/// <para>局限：所有 DB 驱动上传都落到当前默认配置，下载按提供程序名匹配默认配置类型；若把默认配置改成不同类型，旧文件需 appsettings 兜底。</para>
/// </remarks>
public sealed class StorageProviderResolver : IStorageProviderResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStorageSecretProtector _secretProtector;
    private readonly ConcurrentDictionary<long, CachedProvider> _cache = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public StorageProviderResolver(IServiceScopeFactory scopeFactory, IStorageSecretProtector secretProtector)
    {
        _scopeFactory = scopeFactory;
        _secretProtector = secretProtector;
    }

    /// <inheritdoc />
    public string ResolveProviderName(string? routeKey = null, string? providerName = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<IFileStorageRouter>();
        return router.ResolveProviderName(routeKey, providerName);
    }

    /// <inheritdoc />
    public async Task<IFileStorageProvider> RouteForUploadAsync(string? routeKey, string? providerName, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var config = await GetDbDrivenDefaultAsync(scope.ServiceProvider, cancellationToken);
        if (config is not null)
        {
            return GetOrBuild(config);
        }

        return scope.ServiceProvider.GetRequiredService<IFileStorageRouter>().Route(routeKey, providerName);
    }

    /// <inheritdoc />
    public async Task<IFileStorageProvider> RouteForProviderAsync(string? providerName, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            var config = await GetDbDrivenDefaultAsync(scope.ServiceProvider, cancellationToken);
            if (config is not null
                && string.Equals(ProviderNameForType(config.StorageType), providerName, StringComparison.OrdinalIgnoreCase))
            {
                return GetOrBuild(config);
            }
        }

        return scope.ServiceProvider.GetRequiredService<IFileStorageRouter>().Route(providerName: providerName);
    }

    /// <summary>
    /// 取"默认且启用、对象存储、凭证齐全"的配置；否则返回 null（交由 appsettings 兜底）
    /// </summary>
    private static async Task<SysStorageConfig?> GetDbDrivenDefaultAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var repository = provider.GetRequiredService<IStorageConfigRepository>();
        var config = await repository.GetDefaultAsync(cancellationToken);
        if (config is null || config.StorageType == StorageConfigType.Local || !HasCredentials(config))
        {
            return null;
        }

        return config;
    }

    private IFileStorageProvider GetOrBuild(SysStorageConfig config)
    {
        var fingerprint = Fingerprint(config);
        var entry = _cache.AddOrUpdate(
            config.BasicId,
            _ => new CachedProvider(fingerprint, Build(config)),
            (_, existing) => existing.Fingerprint == fingerprint ? existing : new CachedProvider(fingerprint, Build(config)));
        return entry.Provider;
    }

    private IFileStorageProvider Build(SysStorageConfig config)
    {
        var secret = _secretProtector.Unprotect(config.SecretAccessKey) ?? string.Empty;
        var bucket = config.BucketName ?? string.Empty;
        var endpoint = config.Endpoint ?? string.Empty;

        return config.StorageType switch
        {
            StorageConfigType.S3 => new MinioFileStorageProvider(Options.Create(new MinioStorageOptions
            {
                Endpoint = StripScheme(endpoint),
                AccessKey = config.AccessKeyId ?? string.Empty,
                SecretKey = secret,
                DefaultBucket = bucket,
                Region = config.Region,
                UseSSL = endpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase)
            })),
            StorageConfigType.OSS => new AliyunOssStorageProvider(Options.Create(new AliyunOssStorageOptions
            {
                AccessKeyId = config.AccessKeyId ?? string.Empty,
                AccessKeySecret = secret,
                Endpoint = endpoint,
                DefaultBucket = bucket
            })),
            StorageConfigType.COS => new TencentCosStorageProvider(Options.Create(new TencentCosStorageOptions
            {
                SecretId = config.AccessKeyId ?? string.Empty,
                SecretKey = secret,
                Region = config.Region ?? string.Empty,
                DefaultBucket = bucket
                // 注：SysStorageConfig 无 AppId 字段；腾讯云 COS 桶名通常已含 appid 后缀，必要时另补字段
            })),
            _ => throw new InvalidOperationException($"不支持由存储配置构建的存储类型：{config.StorageType}")
        };
    }

    private static bool HasCredentials(SysStorageConfig config)
    {
        return !string.IsNullOrWhiteSpace(config.AccessKeyId)
            && !string.IsNullOrWhiteSpace(config.SecretAccessKey)
            && !string.IsNullOrWhiteSpace(config.BucketName);
    }

    private static string ProviderNameForType(StorageConfigType type)
    {
        return type switch
        {
            StorageConfigType.S3 => ObjectStorageProviderNames.Minio,
            StorageConfigType.OSS => ObjectStorageProviderNames.AliyunOss,
            StorageConfigType.COS => ObjectStorageProviderNames.TencentCos,
            _ => ObjectStorageProviderNames.Local
        };
    }

    private static string Fingerprint(SysStorageConfig config)
    {
        return string.Join('|',
            config.BasicId,
            (int)config.StorageType,
            config.Endpoint,
            config.Region,
            config.BucketName,
            config.AccessKeyId,
            config.SecretAccessKey,
            config.IsEnabled);
    }

    private static string StripScheme(string endpoint)
    {
        var index = endpoint.IndexOf("://", StringComparison.Ordinal);
        return index >= 0 ? endpoint[(index + 3)..] : endpoint;
    }

    private sealed record CachedProvider(string Fingerprint, IFileStorageProvider Provider);
}
