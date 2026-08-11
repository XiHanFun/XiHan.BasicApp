// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 打印模板解析作用域、回退顺序和缓存隔离测试。
/// </summary>
public sealed class PrintTemplateResolverTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// Auto 必须优先返回同编码的启用租户模板，且不再读取全局模板。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_AutoShouldPreferTenantTemplate()
    {
        var tenantTemplate = CreateTemplate(7, "ORDER", allowTenantUse: false);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 7 ? tenantTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto);

        Assert.NotNull(result);
        Assert.Equal(PrintTemplateScope.Tenant, result.ResolvedScope);
        fixture.Repository.Verify(
            repository => repository.FindByCodeInScopeAsync(0, "ORDER", true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 租户模板不存在或停用时，Auto 应回退到向租户开放的启用全局模板。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_AutoShouldFallBackToOpenGlobalTemplate()
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: true);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 0 ? globalTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto);

        Assert.NotNull(result);
        Assert.Equal(PrintTemplateScope.Global, result.ResolvedScope);
        fixture.CurrentTenant.Verify(
            tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()),
            Times.Once);
    }

    /// <summary>
    /// 租户显式请求 Global 时不得使用未开放的全局模板。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_GlobalShouldRejectClosedGlobalTemplateForTenant()
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: false);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 0 ? globalTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Global);

        Assert.Null(result);
    }

    /// <summary>
    /// 显式 Tenant 作用域不允许回退到全局模板。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_TenantShouldNotFallBackToGlobalTemplate()
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: true);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 0 ? globalTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Tenant);

        Assert.Null(result);
        fixture.Repository.Verify(
            repository => repository.FindByCodeInScopeAsync(0, "ORDER", true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 平台 Auto 只解析 Global，且平台自身使用不受 AllowTenantUse 限制。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_PlatformAutoShouldUseClosedGlobalTemplate()
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: false);
        var fixture = CreateFixture(null, (owner, _, _) => owner == 0 ? globalTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto);

        Assert.NotNull(result);
        Assert.Equal(PrintTemplateScope.Global, result.ResolvedScope);
    }

    /// <summary>
    /// 自由模板解析时应原样返回空数据源，缓存层不得把 null 改写为空字符串。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_FreeTemplateShouldKeepNullDataSource()
    {
        var tenantTemplate = CreateTemplate(7, "FREE", allowTenantUse: false);
        tenantTemplate.DataSourceCode = null;
        var fixture = CreateFixture(7, (owner, _, _) => owner == 7 ? tenantTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("FREE", PrintTemplateScope.Tenant);

        Assert.NotNull(result);
        Assert.Null(result.DataSourceCode);
    }

    /// <summary>
    /// 平台上下文不得解析没有明确租户标识的私有模板。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_PlatformTenantScopeShouldReject()
    {
        var fixture = CreateFixture(null, (_, _, _) => null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Tenant));

        Assert.Contains("平台上下文", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 缓存键必须同时包含请求租户、作用域枚举值和模板编码。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_CacheKeyShouldContainTenantScopeAndCode()
    {
        var fixture = CreateFixture(27, (_, _, _) => null);

        _ = await fixture.Resolver.ResolveAsync("SHIP", PrintTemplateScope.Global);

        Assert.Equal(
            SaasCacheKeys.PrintTemplate(27, (int)PrintTemplateScope.Global, "SHIP"),
            Assert.Single(fixture.CacheKeys));
    }

    /// <summary>
    /// 创建直接执行缓存工厂的解析器测试夹具。
    /// </summary>
    /// <param name="tenantId">当前请求租户；null 表示平台。</param>
    /// <param name="find">按 owner、编码、仅启用标记返回模板的仓储行为。</param>
    /// <returns>解析器、依赖模拟对象与捕获到的缓存键。</returns>
    private static ResolverFixture CreateFixture(
        long? tenantId,
        Func<long, string, bool, SysPrintTemplate?> find)
    {
        var repository = new Mock<IPrintTemplateRepository>();
        repository
            .Setup(value => value.FindByCodeInScopeAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((long owner, string code, bool enabledOnly, CancellationToken _) =>
                find(owner, code, enabledOnly));

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(tenantId);
        currentTenant
            .Setup(value => value.Change(It.IsAny<long?>(), It.IsAny<string?>()))
            .Returns(Mock.Of<IDisposable>());

        var cacheKeys = new List<string>();
        var cache = new Mock<IDistributedCache<SaasPrintTemplateCacheItem, string>>();
        cache
            .Setup(value => value.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasPrintTemplateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<SaasPrintTemplateCacheItem>>, Func<DistributedCacheEntryOptions>, bool?, bool, CancellationToken>(
                (key, factory, _, _, _, _) =>
                {
                    cacheKeys.Add(key);
                    return InvokeCacheFactoryAsync(factory);
                });

        return new ResolverFixture(
            new PrintTemplateResolver(repository.Object, currentTenant.Object, cache.Object),
            repository,
            currentTenant,
            cacheKeys);
    }

    /// <summary>
    /// 执行非空缓存工厂并适配框架可空缓存返回注解。
    /// </summary>
    private static async Task<SaasPrintTemplateCacheItem?> InvokeCacheFactoryAsync(
        Func<Task<SaasPrintTemplateCacheItem>> factory)
    {
        return await factory();
    }

    /// <summary>
    /// 创建解析用模板实体。
    /// </summary>
    private static SysPrintTemplate CreateTemplate(long tenantId, string code, bool allowTenantUse)
    {
        var template = new SysPrintTemplate
        {
            TenantId = tenantId,
            TemplateCode = code,
            DataSourceCode = "system.print-demo",
            TemplateName = code,
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            AllowTenantUse = allowTenantUse,
            Status = EnableStatus.Enabled,
            RowVersion = 4
        };
        typeof(SysPrintTemplate)
            .GetProperty(
                nameof(SysPrintTemplate.BasicId),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(template, tenantId == 0 ? 9001L : 7001L);
        return template;
    }

    /// <summary>
    /// 打印模板解析测试依赖集合。
    /// </summary>
    private sealed record ResolverFixture(
        PrintTemplateResolver Resolver,
        Mock<IPrintTemplateRepository> Repository,
        Mock<ICurrentTenant> CurrentTenant,
        List<string> CacheKeys);
}
