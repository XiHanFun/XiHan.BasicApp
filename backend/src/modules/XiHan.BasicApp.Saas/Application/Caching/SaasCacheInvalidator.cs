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
public sealed class SaasCacheInvalidator
    : ISaasCacheInvalidator
{
    private readonly IDistributedCache<SaasConfigValueCacheItem, string> _configValueCache;

    private readonly IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> _authorizationSnapshotCache;

    private readonly IDistributedCache<SaasMenuRoutesCacheItem, string> _menuRoutesCache;

    private readonly IDistributedCache<SaasPermissionSelectCacheItem, string> _permissionSelectCache;

    private readonly IDistributedCache<SaasRoleSelectCacheItem, string> _roleSelectCache;

    private readonly IDistributedCache<SaasEnabledEditionsCacheItem, string> _tenantEditionCache;

    private readonly IDistributedCache<SaasResourceSelectCacheItem, string> _resourceSelectCache;

    private readonly IDistributedCache<SaasOperationSelectCacheItem, string> _operationSelectCache;

    private readonly IDistributedCache<SaasDepartmentTreeCacheItem, string> _departmentTreeCache;

    private readonly IDistributedCache<SaasUserSettingCacheItem, string> _userSettingCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasCacheInvalidator(
        IDistributedCache<SaasConfigValueCacheItem, string> configValueCache,
        IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> authorizationSnapshotCache,
        IDistributedCache<SaasMenuRoutesCacheItem, string> menuRoutesCache,
        IDistributedCache<SaasPermissionSelectCacheItem, string> permissionSelectCache,
        IDistributedCache<SaasRoleSelectCacheItem, string> roleSelectCache,
        IDistributedCache<SaasEnabledEditionsCacheItem, string> tenantEditionCache,
        IDistributedCache<SaasResourceSelectCacheItem, string> resourceSelectCache,
        IDistributedCache<SaasOperationSelectCacheItem, string> operationSelectCache,
        IDistributedCache<SaasDepartmentTreeCacheItem, string> departmentTreeCache,
        IDistributedCache<SaasUserSettingCacheItem, string> userSettingCache)
    {
        _configValueCache = configValueCache;
        _authorizationSnapshotCache = authorizationSnapshotCache;
        _menuRoutesCache = menuRoutesCache;
        _permissionSelectCache = permissionSelectCache;
        _roleSelectCache = roleSelectCache;
        _tenantEditionCache = tenantEditionCache;
        _resourceSelectCache = resourceSelectCache;
        _operationSelectCache = operationSelectCache;
        _departmentTreeCache = departmentTreeCache;
        _userSettingCache = userSettingCache;
    }
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

    /// <inheritdoc />
    public Task InvalidatePermissionDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _permissionSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateRoleDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _roleSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateTenantEditionAsync(CancellationToken cancellationToken = default)
    {
        return _tenantEditionCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateResourceDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _resourceSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateOperationDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _operationSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateOrganizationAsync(CancellationToken cancellationToken = default)
    {
        return _departmentTreeCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateUserSettingAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _userSettingCache.RemoveByPatternAsync(SaasCacheKeys.UserSettingPattern(userId), hideErrors: true, considerUow: true, token: cancellationToken);
    }
}
