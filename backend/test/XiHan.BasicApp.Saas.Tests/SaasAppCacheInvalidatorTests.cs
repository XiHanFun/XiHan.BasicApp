// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 缓存失效器行为测试。
/// </summary>
/// <remarks>
/// 本仓库九类分布式缓存的失效全部经 <see cref="ISaasCacheInvalidator"/> 收口，
/// 采用「按模式全失效」策略。这里逐条锁定三件事：
/// <list type="number">
/// <item>每个失效方法清的是**哪一份**缓存（清错缓存 = 该清的没清）；</item>
/// <item>清的是哪个**模式**（模式与写入键对不上就是静默失灵）；</item>
/// <item>一律 <c>hideErrors:true</c> + <c>considerUow:true</c>——前者保证缓存故障不炸业务写，
/// 后者保证与业务写同事务落地，避免"事务未提交就清缓存、别的请求立刻回填旧值"的竞态。</item>
/// </list>
/// </remarks>
public sealed class SaasAppCacheInvalidatorTests
{
    private readonly Mock<IDistributedCache<SaasConfigValueCacheItem, string>> _configValueCache = new();
    private readonly Mock<IDistributedCache<SaasAuthorizationSnapshotCacheItem, string>> _authorizationSnapshotCache = new();
    private readonly Mock<IDistributedCache<SaasMenuRoutesCacheItem, string>> _menuRoutesCache = new();
    private readonly Mock<IDistributedCache<SaasPermissionSelectCacheItem, string>> _permissionSelectCache = new();
    private readonly Mock<IDistributedCache<SaasRoleSelectCacheItem, string>> _roleSelectCache = new();
    private readonly Mock<IDistributedCache<SaasEnabledEditionsCacheItem, string>> _tenantEditionCache = new();
    private readonly Mock<IDistributedCache<SaasResourceSelectCacheItem, string>> _resourceSelectCache = new();
    private readonly Mock<IDistributedCache<SaasOperationSelectCacheItem, string>> _operationSelectCache = new();
    private readonly Mock<IDistributedCache<SaasDepartmentTreeCacheItem, string>> _departmentTreeCache = new();
    private readonly Mock<IDistributedCache<SaasDepartmentSelectCacheItem, string>> _departmentSelectCache = new();
    private readonly Mock<IDistributedCache<SaasPositionSelectCacheItem, string>> _positionSelectCache = new();
    private readonly Mock<IDistributedCache<SaasPermissionCatalogCacheItem, string>> _permissionCatalogCache = new();
    private readonly Mock<IDistributedCache<SaasUserSettingCacheItem, string>> _userSettingCache = new();
    private readonly Mock<IDistributedCache<SaasMessageTemplateCacheItem, string>> _messageTemplateCache = new();
    private readonly Mock<IDistributedCache<SaasEditionGateCacheItem, string>> _editionGateCache = new();
    private readonly Mock<IDistributedCache<SaasDictItemTreeCacheItem, string>> _dictItemTreeCache = new();
    private readonly Mock<IDistributedCache<SaasSessionStateCacheItem, string>> _sessionStateCache = new();

    private readonly SaasCacheInvalidator _invalidator;

    /// <summary>
    /// 构造被测失效器（17 份缓存全部以 Moq 替身注入，不触达任何真实缓存）。
    /// </summary>
    public SaasAppCacheInvalidatorTests()
    {
        _invalidator = new SaasCacheInvalidator(
            _configValueCache.Object,
            _authorizationSnapshotCache.Object,
            _menuRoutesCache.Object,
            _permissionSelectCache.Object,
            _roleSelectCache.Object,
            _tenantEditionCache.Object,
            _resourceSelectCache.Object,
            _operationSelectCache.Object,
            _departmentTreeCache.Object,
            _departmentSelectCache.Object,
            _positionSelectCache.Object,
            _permissionCatalogCache.Object,
            _userSettingCache.Object,
            _messageTemplateCache.Object,
            _editionGateCache.Object,
            _dictItemTreeCache.Object,
            _sessionStateCache.Object);
    }

    /// <summary>
    /// 不指定配置键时失效全部租户的全部配置值。
    /// </summary>
    [Fact]
    public async Task InvalidateConfigurationAsync_WithoutKey_ShouldRemoveAllConfigValues()
    {
        await _invalidator.InvalidateConfigurationAsync();

        VerifyRemoved(_configValueCache, SaasCacheKeys.AllConfigValuesPattern());
    }

    /// <summary>
    /// 空白配置键与不指定等价，仍走全量模式（不会构造出一个含空白的脏模式）。
    /// </summary>
    /// <param name="configKey">传入的配置键。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidateConfigurationAsync_BlankKey_ShouldFallBackToAllPattern(string? configKey)
    {
        await _invalidator.InvalidateConfigurationAsync(configKey);

        VerifyRemoved(_configValueCache, SaasCacheKeys.AllConfigValuesPattern());
    }

    /// <summary>
    /// 指定配置键时只清该键在各租户下的缓存，模式必须与写入键构造一致。
    /// </summary>
    [Fact]
    public async Task InvalidateConfigurationAsync_WithKey_ShouldRemoveOnlyThatKeyPattern()
    {
        await _invalidator.InvalidateConfigurationAsync("saas.demo");

        VerifyRemoved(_configValueCache, SaasCacheKeys.ConfigValuePattern("saas.demo"));
    }

    /// <summary>
    /// 指定用户时按用户模式精准失效授权快照，而不是全量清空。
    /// </summary>
    [Fact]
    public async Task InvalidateAuthorizationAsync_WithUser_ShouldRemoveThatUserPatternOnly()
    {
        await _invalidator.InvalidateAuthorizationAsync(88);

        VerifyRemoved(_authorizationSnapshotCache, SaasCacheKeys.AuthorizationSnapshotPattern(88));
        _authorizationSnapshotCache.Verify(
            cache => cache.RemoveByPatternAsync("*", It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 不指定用户（角色级变更）时全量清空授权快照。
    /// </summary>
    [Fact]
    public async Task InvalidateAuthorizationAsync_WithoutUser_ShouldRemoveEverything()
    {
        await _invalidator.InvalidateAuthorizationAsync();

        VerifyRemoved(_authorizationSnapshotCache, "*");
    }

    /// <summary>
    /// 菜单路由缓存按权限集合哈希成键，无法精准定位，只能全量清空。
    /// </summary>
    [Fact]
    public async Task InvalidateNavigationAsync_ShouldRemoveAllMenuRoutes()
    {
        await _invalidator.InvalidateNavigationAsync();

        VerifyRemoved(_menuRoutesCache, "*");
    }

    /// <summary>
    /// 权限定义变更必须同时清「权限选择项」与「权限全量目录」两份缓存，漏一份就会读到旧目录。
    /// </summary>
    [Fact]
    public async Task InvalidatePermissionDefinitionAsync_ShouldRemoveBothSelectAndCatalog()
    {
        await _invalidator.InvalidatePermissionDefinitionAsync();

        VerifyRemoved(_permissionSelectCache, "*");
        VerifyRemoved(_permissionCatalogCache, "*");
    }

    /// <summary>
    /// 角色定义变更清空角色选择项缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateRoleDefinitionAsync_ShouldRemoveRoleSelect()
    {
        await _invalidator.InvalidateRoleDefinitionAsync();

        VerifyRemoved(_roleSelectCache, "*");
    }

    /// <summary>
    /// 租户版本变更清空已启用版本列表缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateTenantEditionAsync_ShouldRemoveEnabledEditions()
    {
        await _invalidator.InvalidateTenantEditionAsync();

        VerifyRemoved(_tenantEditionCache, "*");
    }

    /// <summary>
    /// 资源定义变更清空资源选择项缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateResourceDefinitionAsync_ShouldRemoveResourceSelect()
    {
        await _invalidator.InvalidateResourceDefinitionAsync();

        VerifyRemoved(_resourceSelectCache, "*");
    }

    /// <summary>
    /// 操作定义变更清空操作选择项缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateOperationDefinitionAsync_ShouldRemoveOperationSelect()
    {
        await _invalidator.InvalidateOperationDefinitionAsync();

        VerifyRemoved(_operationSelectCache, "*");
    }

    /// <summary>
    /// 组织结构变更必须同时清部门树、部门选择项、岗位选择项三份缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateOrganizationAsync_ShouldRemoveTreeSelectAndPosition()
    {
        await _invalidator.InvalidateOrganizationAsync();

        VerifyRemoved(_departmentTreeCache, "*");
        VerifyRemoved(_departmentSelectCache, "*");
        VerifyRemoved(_positionSelectCache, "*");
    }

    /// <summary>
    /// 用户设置写后只清该用户的全部场景，不波及其它用户。
    /// </summary>
    [Fact]
    public async Task InvalidateUserSettingAsync_ShouldRemoveOnlyThatUserPattern()
    {
        await _invalidator.InvalidateUserSettingAsync(21);

        VerifyRemoved(_userSettingCache, SaasCacheKeys.UserSettingPattern(21));
    }

    /// <summary>
    /// 消息模板写后按全量模板模式失效（发送链路按 渠道+编码 读取，无法精准定位受影响项）。
    /// </summary>
    [Fact]
    public async Task InvalidateMessageTemplateAsync_ShouldUseAllTemplatesPattern()
    {
        await _invalidator.InvalidateMessageTemplateAsync();

        VerifyRemoved(_messageTemplateCache, SaasCacheKeys.AllMessageTemplatesPattern());
    }

    /// <summary>
    /// 版本门控写后按全量门控模式失效。
    /// </summary>
    [Fact]
    public async Task InvalidateEditionGateAsync_ShouldUseAllEditionGatesPattern()
    {
        await _invalidator.InvalidateEditionGateAsync();

        VerifyRemoved(_editionGateCache, SaasCacheKeys.AllEditionGatesPattern());
    }

    /// <summary>
    /// 字典写后按全量字典树模式失效。
    /// </summary>
    [Fact]
    public async Task InvalidateDictionaryAsync_ShouldUseAllDictItemTreesPattern()
    {
        await _invalidator.InvalidateDictionaryAsync();

        VerifyRemoved(_dictItemTreeCache, SaasCacheKeys.AllDictItemTreesPattern());
    }

    /// <summary>
    /// 单会话失效用的模式必须与写入会话状态时用的键完全一致，否则踢下线清不掉缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateSessionStateAsync_ShouldUsePatternIdenticalToWrittenKey()
    {
        await _invalidator.InvalidateSessionStateAsync("sess-1");

        VerifyRemoved(_sessionStateCache, SaasCacheKeys.SessionState("sess-1"));
    }

    /// <summary>
    /// 会话标识为空时直接返回，不得退化成 <c>session:</c> 这类会误伤的模式。
    /// </summary>
    /// <param name="sessionId">传入的会话标识。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidateSessionStateAsync_BlankSessionId_ShouldDoNothing(string? sessionId)
    {
        await _invalidator.InvalidateSessionStateAsync(sessionId!);

        _sessionStateCache.Verify(
            cache => cache.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 批量吊销时清空全部会话状态缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateAllSessionStatesAsync_ShouldUseAllSessionStatesPattern()
    {
        await _invalidator.InvalidateAllSessionStatesAsync();

        VerifyRemoved(_sessionStateCache, SaasCacheKeys.AllSessionStatesPattern());
    }

    /// <summary>
    /// 取消令牌必须透传到底层缓存调用，不能被吞成 default。
    /// </summary>
    [Fact]
    public async Task InvalidateAsync_ShouldForwardCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _invalidator.InvalidateNavigationAsync(cts.Token);
        await _invalidator.InvalidateUserSettingAsync(1, cts.Token);
        await _invalidator.InvalidateSessionStateAsync("sess-1", cts.Token);

        _menuRoutesCache.Verify(
            cache => cache.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), cts.Token),
            Times.Once);
        _userSettingCache.Verify(
            cache => cache.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), cts.Token),
            Times.Once);
        _sessionStateCache.Verify(
            cache => cache.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), cts.Token),
            Times.Once);
    }

    /// <summary>
    /// 一次失效只应触碰它自己那份缓存：这里以会话失效为例，验证不会顺手清掉授权快照等其它缓存。
    /// </summary>
    [Fact]
    public async Task InvalidateSessionStateAsync_ShouldNotTouchUnrelatedCaches()
    {
        await _invalidator.InvalidateSessionStateAsync("sess-1");

        VerifyNeverRemoved(_authorizationSnapshotCache);
        VerifyNeverRemoved(_menuRoutesCache);
        VerifyNeverRemoved(_configValueCache);
        VerifyNeverRemoved(_userSettingCache);
    }

    /// <summary>
    /// 断言指定缓存按给定模式被清理了一次，且带上了约定的 hideErrors / considerUow 标志。
    /// </summary>
    /// <typeparam name="TCacheItem">缓存项类型。</typeparam>
    /// <param name="cache">缓存替身。</param>
    /// <param name="expectedPattern">期望的失效模式。</param>
    private static void VerifyRemoved<TCacheItem>(Mock<IDistributedCache<TCacheItem, string>> cache, string expectedPattern)
        where TCacheItem : class
    {
        cache.Verify(
            target => target.RemoveByPatternAsync(expectedPattern, true, true, It.IsAny<CancellationToken>()),
            Times.Once,
            $"应按模式 {expectedPattern} 失效 {typeof(TCacheItem).Name}，且必须 hideErrors:true + considerUow:true。");
    }

    /// <summary>
    /// 断言指定缓存完全没有被清理过。
    /// </summary>
    /// <typeparam name="TCacheItem">缓存项类型。</typeparam>
    /// <param name="cache">缓存替身。</param>
    private static void VerifyNeverRemoved<TCacheItem>(Mock<IDistributedCache<TCacheItem, string>> cache)
        where TCacheItem : class
    {
        cache.Verify(
            target => target.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never,
            $"{typeof(TCacheItem).Name} 不应被本次失效波及。");
    }
}
