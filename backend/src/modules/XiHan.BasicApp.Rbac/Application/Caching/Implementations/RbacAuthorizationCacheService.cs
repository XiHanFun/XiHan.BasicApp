#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAuthorizationCacheService
// Guid:9dc996fd-29d3-41a2-aae1-c2ad299102f5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 17:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Rbac.Application.Caching.Implementations;

/// <summary>
/// RBAC 授权缓存服务实现
/// </summary>
public class RbacAuthorizationCacheService : IRbacAuthorizationCacheService
{
    private static readonly DistributedCacheEntryOptions VersionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    private static readonly DistributedCacheEntryOptions PermissionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    private static readonly DistributedCacheEntryOptions DataScopeCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    private readonly IDistributedCache<AuthorizationCacheVersionItem> _versionCache;
    private readonly IDistributedCache<UserPermissionCodesCacheItem> _permissionCodesCache;
    private readonly IDistributedCache<UserDataScopeDepartmentIdsCacheItem> _dataScopeCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="versionCache"></param>
    /// <param name="permissionCodesCache"></param>
    /// <param name="dataScopeCache"></param>
    public RbacAuthorizationCacheService(
        IDistributedCache<AuthorizationCacheVersionItem> versionCache,
        IDistributedCache<UserPermissionCodesCacheItem> permissionCodesCache,
        IDistributedCache<UserDataScopeDepartmentIdsCacheItem> dataScopeCache)
    {
        _versionCache = versionCache;
        _permissionCodesCache = permissionCodesCache;
        _dataScopeCache = dataScopeCache;
    }

    /// <summary>
    /// 获取用户权限编码（带缓存）
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="factory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(
        long userId,
        long? tenantId,
        Func<CancellationToken, Task<IReadOnlyCollection<string>>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (userId <= 0)
        {
            return [];
        }

        var version = await GetPermissionVersionAsync(tenantId, cancellationToken);
        var key = BuildPermissionUserCacheKey(tenantId, userId, version);
        var item = await _permissionCodesCache.GetOrAddAsync(
            key,
            async () => new UserPermissionCodesCacheItem
            {
                PermissionCodes = [.. (await factory(cancellationToken))]
            },
            optionsFactory: () => PermissionCacheOptions,
            token: cancellationToken);

        return item?.PermissionCodes ?? [];
    }

    /// <summary>
    /// 获取用户数据范围部门ID（带缓存）
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="factory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(
        long userId,
        long? tenantId,
        Func<CancellationToken, Task<IReadOnlyCollection<long>>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (userId <= 0)
        {
            return [];
        }

        var version = await GetDataScopeVersionAsync(tenantId, cancellationToken);
        var key = BuildDataScopeUserCacheKey(tenantId, userId, version);
        var item = await _dataScopeCache.GetOrAddAsync(
            key,
            async () => new UserDataScopeDepartmentIdsCacheItem
            {
                DepartmentIds = [.. (await factory(cancellationToken))]
            },
            optionsFactory: () => DataScopeCacheOptions,
            token: cancellationToken);

        return item?.DepartmentIds ?? [];
    }

    /// <summary>
    /// 失效权限缓存
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task InvalidatePermissionAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var versionKey = BuildPermissionVersionKey(tenantId);
        var currentVersion = await GetPermissionVersionAsync(tenantId, cancellationToken);
        await _versionCache.SetAsync(
            versionKey,
            new AuthorizationCacheVersionItem { Version = currentVersion + 1 },
            options: VersionCacheOptions,
            considerUow: true,
            token: cancellationToken);
    }

    /// <summary>
    /// 失效数据范围缓存
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task InvalidateDataScopeAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var versionKey = BuildDataScopeVersionKey(tenantId);
        var currentVersion = await GetDataScopeVersionAsync(tenantId, cancellationToken);
        await _versionCache.SetAsync(
            versionKey,
            new AuthorizationCacheVersionItem { Version = currentVersion + 1 },
            options: VersionCacheOptions,
            considerUow: true,
            token: cancellationToken);
    }

    /// <summary>
    /// 失效权限与数据范围缓存
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task InvalidateAllAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        await InvalidatePermissionAsync(tenantId, cancellationToken);
        await InvalidateDataScopeAsync(tenantId, cancellationToken);
    }

    /// <summary>
    /// 获取授权缓存版本快照
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AuthorizationCacheVersionSnapshot> GetVersionSnapshotAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var permissionVersion = await GetPermissionVersionAsync(tenantId, cancellationToken);
        var dataScopeVersion = await GetDataScopeVersionAsync(tenantId, cancellationToken);

        return new AuthorizationCacheVersionSnapshot
        {
            PermissionVersion = permissionVersion,
            DataScopeVersion = dataScopeVersion
        };
    }

    private static string BuildPermissionVersionKey(long? tenantId)
    {
        return $"perm:ver:{FormatTenantSegment(tenantId)}";
    }

    private static string BuildDataScopeVersionKey(long? tenantId)
    {
        return $"scope:ver:{FormatTenantSegment(tenantId)}";
    }

    private static string BuildPermissionUserCacheKey(long? tenantId, long userId, long version)
    {
        return $"perm:user:{FormatTenantSegment(tenantId)}:{userId}:v{version}";
    }

    private static string BuildDataScopeUserCacheKey(long? tenantId, long userId, long version)
    {
        return $"scope:user:{FormatTenantSegment(tenantId)}:{userId}:v{version}";
    }

    private static string FormatTenantSegment(long? tenantId)
    {
        return tenantId?.ToString() ?? "host";
    }

    private async Task<long> GetPermissionVersionAsync(long? tenantId, CancellationToken cancellationToken)
    {
        var item = await _versionCache.GetOrAddAsync(
            BuildPermissionVersionKey(tenantId),
            () => Task.FromResult(new AuthorizationCacheVersionItem { Version = 1 }),
            optionsFactory: () => VersionCacheOptions,
            token: cancellationToken);

        return item?.Version > 0 ? item.Version : 1;
    }

    private async Task<long> GetDataScopeVersionAsync(long? tenantId, CancellationToken cancellationToken)
    {
        var item = await _versionCache.GetOrAddAsync(
            BuildDataScopeVersionKey(tenantId),
            () => Task.FromResult(new AuthorizationCacheVersionItem { Version = 1 }),
            optionsFactory: () => VersionCacheOptions,
            token: cancellationToken);

        return item?.Version > 0 ? item.Version : 1;
    }
}
