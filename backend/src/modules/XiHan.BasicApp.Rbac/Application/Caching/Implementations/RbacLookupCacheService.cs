#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacLookupCacheService
// Guid:a0d193e2-b6f7-4eac-afbe-f6e2ac74918d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:42:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Rbac.Application.Caching.Implementations;

/// <summary>
/// RBAC 查找缓存服务实现
/// </summary>
public class RbacLookupCacheService : IRbacLookupCacheService
{
    private static readonly DistributedCacheEntryOptions VersionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    private static readonly DistributedCacheEntryOptions LookupCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    private readonly IDistributedCache<LookupCacheVersionItem> _versionCache;
    private readonly IDistributedCache<FileLookupCacheItem> _fileLookupCache;
    private readonly IDistributedCache<TaskLookupCacheItem> _taskLookupCache;
    private readonly IDistributedCache<OAuthAppLookupCacheItem> _oauthAppLookupCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacLookupCacheService(
        IDistributedCache<LookupCacheVersionItem> versionCache,
        IDistributedCache<FileLookupCacheItem> fileLookupCache,
        IDistributedCache<TaskLookupCacheItem> taskLookupCache,
        IDistributedCache<OAuthAppLookupCacheItem> oauthAppLookupCache)
    {
        _versionCache = versionCache;
        _fileLookupCache = fileLookupCache;
        _taskLookupCache = taskLookupCache;
        _oauthAppLookupCache = oauthAppLookupCache;
    }

    /// <summary>
    /// 获取文件（按哈希）
    /// </summary>
    public async Task<FileDto?> GetFileByHashAsync(
        string fileHash,
        long? tenantId,
        Func<CancellationToken, Task<FileDto?>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        ArgumentNullException.ThrowIfNull(factory);

        var version = await GetVersionAsync(BuildFileLookupVersionKey(tenantId), cancellationToken);
        var key = BuildFileLookupItemKey(tenantId, fileHash, version);
        var item = await _fileLookupCache.GetOrAddAsync(
            key,
            async () => new FileLookupCacheItem
            {
                Item = await factory(cancellationToken)
            },
            optionsFactory: () => LookupCacheOptions,
            token: cancellationToken);

        return item?.Item;
    }

    /// <summary>
    /// 获取任务（按编码）
    /// </summary>
    public async Task<TaskDto?> GetTaskByCodeAsync(
        string taskCode,
        long? tenantId,
        Func<CancellationToken, Task<TaskDto?>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskCode);
        ArgumentNullException.ThrowIfNull(factory);

        var version = await GetVersionAsync(BuildTaskLookupVersionKey(tenantId), cancellationToken);
        var key = BuildTaskLookupItemKey(tenantId, taskCode, version);
        var item = await _taskLookupCache.GetOrAddAsync(
            key,
            async () => new TaskLookupCacheItem
            {
                Item = await factory(cancellationToken)
            },
            optionsFactory: () => LookupCacheOptions,
            token: cancellationToken);

        return item?.Item;
    }

    /// <summary>
    /// 获取 OAuth 应用（按客户端ID）
    /// </summary>
    public async Task<OAuthAppDto?> GetOAuthAppByClientIdAsync(
        string clientId,
        long? tenantId,
        Func<CancellationToken, Task<OAuthAppDto?>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(factory);

        var version = await GetVersionAsync(BuildOAuthAppLookupVersionKey(tenantId), cancellationToken);
        var key = BuildOAuthAppLookupItemKey(tenantId, clientId, version);
        var item = await _oauthAppLookupCache.GetOrAddAsync(
            key,
            async () => new OAuthAppLookupCacheItem
            {
                Item = await factory(cancellationToken)
            },
            optionsFactory: () => LookupCacheOptions,
            token: cancellationToken);

        return item?.Item;
    }

    /// <summary>
    /// 失效文件查找缓存
    /// </summary>
    public Task InvalidateFileLookupAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        return IncreaseVersionAsync(BuildFileLookupVersionKey(tenantId), cancellationToken);
    }

    /// <summary>
    /// 失效任务查找缓存
    /// </summary>
    public Task InvalidateTaskLookupAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        return IncreaseVersionAsync(BuildTaskLookupVersionKey(tenantId), cancellationToken);
    }

    /// <summary>
    /// 失效 OAuth 应用查找缓存
    /// </summary>
    public Task InvalidateOAuthAppLookupAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        return IncreaseVersionAsync(BuildOAuthAppLookupVersionKey(tenantId), cancellationToken);
    }

    /// <summary>
    /// 失效全部查找缓存
    /// </summary>
    public async Task InvalidateAllAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        await InvalidateFileLookupAsync(tenantId, cancellationToken);
        await InvalidateTaskLookupAsync(tenantId, cancellationToken);
        await InvalidateOAuthAppLookupAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// 获取版本快照
    /// </summary>
    public async Task<LookupCacheVersionSnapshot> GetVersionSnapshotAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var fileVersion = await GetVersionAsync(BuildFileLookupVersionKey(tenantId), cancellationToken);
        var taskVersion = await GetVersionAsync(BuildTaskLookupVersionKey(tenantId), cancellationToken);
        var oauthVersion = await GetVersionAsync(BuildOAuthAppLookupVersionKey(tenantId), cancellationToken);

        return new LookupCacheVersionSnapshot
        {
            FileLookupVersion = fileVersion,
            TaskLookupVersion = taskVersion,
            OAuthAppLookupVersion = oauthVersion
        };
    }

    private async Task<long> GetVersionAsync(string key, CancellationToken cancellationToken)
    {
        var item = await _versionCache.GetOrAddAsync(
            key,
            () => Task.FromResult(new LookupCacheVersionItem { Version = 1 }),
            optionsFactory: () => VersionCacheOptions,
            token: cancellationToken);

        return item?.Version > 0 ? item.Version : 1;
    }

    private async Task IncreaseVersionAsync(string key, CancellationToken cancellationToken)
    {
        var version = await GetVersionAsync(key, cancellationToken);
        await _versionCache.SetAsync(
            key,
            new LookupCacheVersionItem { Version = version + 1 },
            options: VersionCacheOptions,
            considerUow: true,
            token: cancellationToken);
    }

    private static string BuildFileLookupVersionKey(long? tenantId)
    {
        return $"lookup:file:ver:{FormatTenantSegment(tenantId)}";
    }

    private static string BuildTaskLookupVersionKey(long? tenantId)
    {
        return $"lookup:task:ver:{FormatTenantSegment(tenantId)}";
    }

    private static string BuildOAuthAppLookupVersionKey(long? tenantId)
    {
        return $"lookup:oauth:ver:{FormatTenantSegment(tenantId)}";
    }

    private static string BuildFileLookupItemKey(long? tenantId, string fileHash, long version)
    {
        return $"lookup:file:item:{FormatTenantSegment(tenantId)}:{NormalizeKey(fileHash)}:v{version}";
    }

    private static string BuildTaskLookupItemKey(long? tenantId, string taskCode, long version)
    {
        return $"lookup:task:item:{FormatTenantSegment(tenantId)}:{NormalizeKey(taskCode)}:v{version}";
    }

    private static string BuildOAuthAppLookupItemKey(long? tenantId, string clientId, long version)
    {
        return $"lookup:oauth:item:{FormatTenantSegment(tenantId)}:{NormalizeKey(clientId)}:v{version}";
    }

    private static string FormatTenantSegment(long? tenantId)
    {
        return tenantId?.ToString() ?? "host";
    }

    private static string NormalizeKey(string raw)
    {
        return raw.Trim().ToLowerInvariant();
    }
}
