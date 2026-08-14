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

    private readonly IDistributedCache<SaasPermissionCatalogCacheItem, string> _permissionCatalogCache;

    private readonly IDistributedCache<SaasRoleSelectCacheItem, string> _roleSelectCache;

    private readonly IDistributedCache<SaasEnabledEditionsCacheItem, string> _tenantEditionCache;

    private readonly IDistributedCache<SaasResourceSelectCacheItem, string> _resourceSelectCache;

    private readonly IDistributedCache<SaasOperationSelectCacheItem, string> _operationSelectCache;

    private readonly IDistributedCache<SaasDepartmentTreeCacheItem, string> _departmentTreeCache;

    private readonly IDistributedCache<SaasDepartmentSelectCacheItem, string> _departmentSelectCache;

    private readonly IDistributedCache<SaasPositionSelectCacheItem, string> _positionSelectCache;

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
        IDistributedCache<SaasDepartmentSelectCacheItem, string> departmentSelectCache,
        IDistributedCache<SaasPositionSelectCacheItem, string> positionSelectCache,
        IDistributedCache<SaasPermissionCatalogCacheItem, string> permissionCatalogCache,
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
        _departmentSelectCache = departmentSelectCache;
        _positionSelectCache = positionSelectCache;
        _permissionCatalogCache = permissionCatalogCache;
        _userSettingCache = userSettingCache;
        _messageTemplateCache = messageTemplateCache;
        _editionGateCache = editionGateCache;
        _dictItemTreeCache = dictItemTreeCache;
        _sessionStateCache = sessionStateCache;
    }

    /// <summary>
    /// 失效配置缓存。
    /// </summary>
    public Task InvalidateConfigurationAsync(string? configKey = null, CancellationToken cancellationToken = default)
    {
        var pattern = string.IsNullOrWhiteSpace(configKey)
            ? SaasCacheKeys.AllConfigValuesPattern()
            : SaasCacheKeys.ConfigValuePattern(configKey);
        return _configValueCache.RemoveByPatternAsync(pattern, hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效授权快照缓存。
    /// </summary>
    public Task InvalidateAuthorizationAsync(long? userId = null, CancellationToken cancellationToken = default)
    {
        return userId.HasValue
            ? _authorizationSnapshotCache.RemoveByPatternAsync(SaasCacheKeys.AuthorizationSnapshotPattern(userId.Value), hideErrors: true, considerUow: true, token: cancellationToken)
            : _authorizationSnapshotCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效菜单路由缓存。
    /// </summary>
    public Task InvalidateNavigationAsync(CancellationToken cancellationToken = default)
    {
        return _menuRoutesCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效权限定义（可选权限选择项）缓存。
    /// </summary>
    public Task InvalidatePermissionDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(
            _permissionSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken),
            _permissionCatalogCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken));
    }

    /// <summary>
    /// 失效角色定义（已启用角色选择项）缓存。
    /// </summary>
    public Task InvalidateRoleDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _roleSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效已启用租户版本列表缓存。
    /// </summary>
    public Task InvalidateTenantEditionAsync(CancellationToken cancellationToken = default)
    {
        return _tenantEditionCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效资源定义（可选资源选择项）缓存。
    /// </summary>
    public Task InvalidateResourceDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _resourceSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效操作定义（可选操作选择项）缓存。
    /// </summary>
    public Task InvalidateOperationDefinitionAsync(CancellationToken cancellationToken = default)
    {
        return _operationSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效组织结构（部门树）缓存。
    /// </summary>
    public Task InvalidateOrganizationAsync(CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(
            _departmentTreeCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken),
            _departmentSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken),
            _positionSelectCache.RemoveByPatternAsync("*", hideErrors: true, considerUow: true, token: cancellationToken));
    }

    /// <summary>
    /// 失效指定用户的设置缓存（写后整体失效该用户全部场景）。
    /// </summary>
    public Task InvalidateUserSettingAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _userSettingCache.RemoveByPatternAsync(SaasCacheKeys.UserSettingPattern(userId), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效消息模板缓存（模板增删改/启停后调用，发送链路按 渠道+编码 高频读取）。
    /// </summary>
    public Task InvalidateMessageTemplateAsync(CancellationToken cancellationToken = default)
    {
        return _messageTemplateCache.RemoveByPatternAsync(SaasCacheKeys.AllMessageTemplatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效版本门控缓存（版本权限白名单变更/租户换版本后调用，鉴权快照热路径）。
    /// </summary>
    public Task InvalidateEditionGateAsync(CancellationToken cancellationToken = default)
    {
        return _editionGateCache.RemoveByPatternAsync(SaasCacheKeys.AllEditionGatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效字典项树缓存（字典/字典项增删改/启停后调用）。
    /// </summary>
    public Task InvalidateDictionaryAsync(CancellationToken cancellationToken = default)
    {
        return _dictItemTreeCache.RemoveByPatternAsync(SaasCacheKeys.AllDictItemTreesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }

    /// <summary>
    /// 失效指定会话的状态缓存（锁定/解锁/吊销/登出后必须调用）。
    /// </summary>
    /// <remarks>
    /// 会话闸门每请求读这份缓存。任何改写 <c>SysUserSession.Status</c> 或 <c>IsLocked</c> 的写路径
    /// <b>都必须</b>补这一刀，否则改动最长要等缓存过期才生效——踢下线踢不掉、锁定锁不住。
    /// </remarks>
    /// <param name="userSessionId">会话业务标识（JWT 的 session_id）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
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

    /// <summary>
    /// 失效全部会话状态缓存（批量吊销某用户全部会话时调用）。
    /// </summary>
    public Task InvalidateAllSessionStatesAsync(CancellationToken cancellationToken = default)
    {
        return _sessionStateCache.RemoveByPatternAsync(
            SaasCacheKeys.AllSessionStatesPattern(), hideErrors: true, considerUow: true, token: cancellationToken);
    }
}
