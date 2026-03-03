#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheService
// Guid:9a67eaf4-3eca-4072-b5c3-0ae47f3b66ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:17:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 系统缓存服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class CacheService : ApplicationServiceBase, ICacheService
{
    private readonly IRbacAuthorizationCacheService _authorizationCacheService;
    private readonly IRbacLookupCacheService _lookupCacheService;
    private readonly IMessageCacheService _messageCacheService;
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CacheService(
        IRbacAuthorizationCacheService authorizationCacheService,
        IRbacLookupCacheService lookupCacheService,
        IMessageCacheService messageCacheService,
        IDistributedCache distributedCache)
    {
        _authorizationCacheService = authorizationCacheService;
        _lookupCacheService = lookupCacheService;
        _messageCacheService = messageCacheService;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 获取缓存字符串
    /// </summary>
    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return await _distributedCache.GetStringAsync(key, cancellationToken);
    }

    /// <summary>
    /// 设置缓存字符串
    /// </summary>
    public async Task SetStringAsync(string key, string value, int expireSeconds = 300, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var seconds = expireSeconds > 0 ? expireSeconds : 300;
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds)
        };

        await _distributedCache.SetStringAsync(key, value, options, cancellationToken);
    }

    /// <summary>
    /// 删除缓存项
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    public async Task RemoveManyAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys.Where(static key => !string.IsNullOrWhiteSpace(key)))
        {
            await _distributedCache.RemoveAsync(key.Trim(), cancellationToken);
        }
    }

    /// <summary>
    /// 失效授权缓存
    /// </summary>
    public Task InvalidateAuthorizationAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return _authorizationCacheService.InvalidateAllAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// 失效查找缓存
    /// </summary>
    public Task InvalidateLookupAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return _lookupCacheService.InvalidateAllAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// 失效消息缓存
    /// </summary>
    public Task InvalidateMessageAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        return _messageCacheService.InvalidateUnreadCountAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// 失效全部缓存
    /// </summary>
    public async Task InvalidateAllAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        await _authorizationCacheService.InvalidateAllAsync(tenantId, cancellationToken);
        await _lookupCacheService.InvalidateAllAsync(tenantId, cancellationToken);
        await _messageCacheService.InvalidateUnreadCountAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// 获取缓存版本快照
    /// </summary>
    public async Task<SysCacheSnapshotDto> GetSnapshotAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var authorizationSnapshot = await _authorizationCacheService.GetVersionSnapshotAsync(tenantId, cancellationToken);
        var lookupSnapshot = await _lookupCacheService.GetVersionSnapshotAsync(tenantId, cancellationToken);
        var messageVersion = await _messageCacheService.GetUnreadVersionAsync(tenantId, cancellationToken);

        return new SysCacheSnapshotDto
        {
            TenantId = tenantId,
            PermissionVersion = authorizationSnapshot.PermissionVersion,
            DataScopeVersion = authorizationSnapshot.DataScopeVersion,
            FileLookupVersion = lookupSnapshot.FileLookupVersion,
            TaskLookupVersion = lookupSnapshot.TaskLookupVersion,
            OAuthAppLookupVersion = lookupSnapshot.OAuthAppLookupVersion,
            MessageUnreadVersion = messageVersion,
            CollectedAt = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空", nameof(key));
        }
    }
}
