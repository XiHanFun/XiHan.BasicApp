#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasCacheInvalidator
// Guid:b20238f6-3842-4cb6-a98c-c27410d908cf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 模块缓存失效器实现。
/// </summary>
public sealed class SaasCacheInvalidator(
    IDistributedCache<SaasConfigValueCacheItem, string> configValueCache,
    IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> authorizationSnapshotCache,
    IDistributedCache<SaasMenuRoutesCacheItem, string> menuRoutesCache)
    : ISaasCacheInvalidator
{
    private readonly IDistributedCache<SaasConfigValueCacheItem, string> _configValueCache = configValueCache;
    private readonly IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> _authorizationSnapshotCache = authorizationSnapshotCache;
    private readonly IDistributedCache<SaasMenuRoutesCacheItem, string> _menuRoutesCache = menuRoutesCache;

    /// <inheritdoc />
    public Task InvalidateConfigurationAsync(string? configKey = null, CancellationToken cancellationToken = default)
    {
        var pattern = string.IsNullOrWhiteSpace(configKey)
            ? SaasCacheKeys.AllConfigValuesPattern()
            : SaasCacheKeys.ConfigValuePattern(configKey);
        return _configValueCache.RemoveByPatternAsync(pattern, hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateAuthorizationAsync(long? userId = null, CancellationToken cancellationToken = default)
    {
        return userId.HasValue
            ? _authorizationSnapshotCache.RemoveAsync(SaasCacheKeys.AuthorizationSnapshot(userId.Value), hideErrors: true, considerUow: true, token: cancellationToken)
            : _authorizationSnapshotCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateNavigationAsync(CancellationToken cancellationToken = default)
    {
        return _menuRoutesCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }
}
