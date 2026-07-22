// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    private readonly IDistributedCache<SaasMessageTemplateCacheItem, string> _messageTemplateCache;

    private readonly IDistributedCache<SaasEditionGateCacheItem, string> _editionGateCache;

    private readonly IDistributedCache<SaasDictItemTreeCacheItem, string> _dictItemTreeCache;

    private readonly IDistributedCache<SaasSessionStateCacheItem, string> _sessionStateCache;

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
        IDistributedCache<SaasUserSettingCacheItem, string> userSettingCache,
        IDistributedCache<SaasMessageTemplateCacheItem, string> messageTemplateCache,
        IDistributedCache<SaasEditionGateCacheItem, string> editionGateCache,
        IDistributedCache<SaasDictItemTreeCacheItem, string> dictItemTreeCache,
        IDistributedCache<SaasSessionStateCacheItem, string> sessionStateCache)
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
        _messageTemplateCache = messageTemplateCache;
        _editionGateCache = editionGateCache;
        _dictItemTreeCache = dictItemTreeCache;
        _sessionStateCache = sessionStateCache;
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
            ? _authorizationSnapshotCache.RemoveByPatternAsync(SaasCacheKeys.AuthorizationSnapshotPattern(userId.Value), hideErrors: true, considerUow: true, token: cancellationToken)
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

    /// <inheritdoc />
    public Task InvalidateMessageTemplateAsync(CancellationToken cancellationToken = default)
    {
        return _messageTemplateCache.RemoveByPatternAsync(SaasCacheKeys.AllMessageTemplatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateEditionGateAsync(CancellationToken cancellationToken = default)
    {
        return _editionGateCache.RemoveByPatternAsync(SaasCacheKeys.AllEditionGatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateDictionaryAsync(CancellationToken cancellationToken = default)
    {
        return _dictItemTreeCache.RemoveByPatternAsync(SaasCacheKeys.AllDictItemTreesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateSessionStateAsync(string userSessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userSessionId))
        {
            return Task.CompletedTask;
        }

        // considerUow:true —— 与业务写同事务落地，避免"事务未提交就清缓存、别的请求立刻回填旧值"的竞态
        return _sessionStateCache.RemoveByPatternAsync(
            SaasCacheKeys.SessionStatePattern(userSessionId), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateAllSessionStatesAsync(CancellationToken cancellationToken = default)
    {
        return _sessionStateCache.RemoveByPatternAsync(
            SaasCacheKeys.AllSessionStatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }
}
