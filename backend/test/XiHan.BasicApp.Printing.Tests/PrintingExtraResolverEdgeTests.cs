// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Application.Services;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板解析器的入参边界、回退组合与缓存键隔离测试。
/// </summary>
/// <remarks>
/// 缓存键由「请求租户 × 请求作用域 × 模板编码」三段构成，缺任何一段都会串号：
/// 少了租户段，甲租户的私有设计会被乙租户读到；少了作用域段，同一编码在 Auto 与 Global 下
/// 命中的是不同实体却共用一条缓存。编码在进缓存键之前必须先规范化，
/// 否则 "ORDER" 与 " ORDER " 会各占一条缓存、失效时只清掉其中一条。
/// </remarks>
public sealed class PrintingExtraResolverEdgeTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 未定义的作用域枚举值必须先于任何查询被拒绝。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_UndefinedScope_ShouldReject()
    {
        var fixture = CreateFixture(7, (_, _, _) => null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Resolver.ResolveAsync("ORDER", (PrintTemplateScope)9));

        Assert.Contains("作用域无效", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.CacheKeys);
    }

    /// <summary>
    /// 空编码属于参数错误，抛的是参数校验异常族而不是业务异常。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_BlankCode_ShouldThrowArgumentFamily(string? templateCode)
    {
        var fixture = CreateFixture(7, (_, _, _) => null);

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Resolver.ResolveAsync(templateCode!));
        Assert.Empty(fixture.CacheKeys);
    }

    /// <summary>
    /// 内含空白或超长的编码会污染缓存键，必须以业务异常拒绝；恰好 100 字符仍然放行。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    [Theory]
    [InlineData("OR DER")]
    [InlineData("OR\tDER")]
    public async Task ResolveAsync_MalformedCode_ShouldReject(string templateCode)
    {
        var fixture = CreateFixture(7, (_, _, _) => null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Resolver.ResolveAsync(templateCode));

        Assert.Contains("打印模板编码无效", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.CacheKeys);
    }

    /// <summary>
    /// 编码长度上限 100 与领域写路径保持一致：100 放行、101 拒绝。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_CodeLength_ShouldStopAtHundred()
    {
        var fixture = CreateFixture(7, (_, _, _) => null);

        _ = await fixture.Resolver.ResolveAsync(new string('A', 100));
        Assert.Single(fixture.CacheKeys);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Resolver.ResolveAsync(new string('A', 101)));
        Assert.Contains("打印模板编码无效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 编码在进入缓存键与仓储查询之前必须去掉首尾空白，避免同一模板占用多条缓存。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldNormalizeCodeBeforeCacheAndQuery()
    {
        var fixture = CreateFixture(7, (_, _, _) => null);

        _ = await fixture.Resolver.ResolveAsync("  ORDER  ", PrintTemplateScope.Tenant);

        Assert.Equal(PrintingCacheKeys.PrintTemplate(7, (int)PrintTemplateScope.Tenant, "ORDER"), Assert.Single(fixture.CacheKeys));
        fixture.Repository.Verify(
            repository => repository.FindByCodeInScopeAsync(7, "ORDER", true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 已取消的令牌必须在读缓存之前生效。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_CancelledToken_ShouldThrowBeforeCache()
    {
        var fixture = CreateFixture(7, (_, _, _) => null);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto, cancellation.Token));

        Assert.Empty(fixture.CacheKeys);
    }

    /// <summary>
    /// 解析路径只接受启用模板：无论哪条分支，传给仓储的 enabledOnly 恒为 true。
    /// </summary>
    /// <param name="tenantId">当前租户；null 表示平台。</param>
    /// <param name="scope">解析作用域。</param>
    [Theory]
    [InlineData(7L, PrintTemplateScope.Auto)]
    [InlineData(7L, PrintTemplateScope.Tenant)]
    [InlineData(7L, PrintTemplateScope.Global)]
    [InlineData(null, PrintTemplateScope.Auto)]
    [InlineData(null, PrintTemplateScope.Global)]
    public async Task ResolveAsync_ShouldOnlyQueryEnabledTemplates(long? tenantId, PrintTemplateScope scope)
    {
        var fixture = CreateFixture(tenantId, (_, _, _) => null);

        _ = await fixture.Resolver.ResolveAsync("ORDER", scope);

        fixture.Repository.Verify(
            repository => repository.FindByCodeInScopeAsync(It.IsAny<long>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Auto 回退到全局时仍要求全局模板已向租户开放，未开放则整体解析失败。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_AutoFallbackToClosedGlobal_ShouldReturnNull()
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: false);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 0 ? globalTemplate : null);

        Assert.Null(await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto));
        fixture.Repository.Verify(
            repository => repository.FindByCodeInScopeAsync(7, "ORDER", true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 显式 Tenant 作用域不跨库读取，因此不需要也不应该切换到平台上下文。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_TenantScope_ShouldNotSwitchToPlatformContext()
    {
        var tenantTemplate = CreateTemplate(7, "ORDER", allowTenantUse: false);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 7 ? tenantTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Tenant);

        Assert.NotNull(result);
        fixture.CurrentTenant.Verify(tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// 平台态解析全局模板本就在平台上下文里，不需要再切一次租户上下文。
    /// </summary>
    /// <param name="tenantId">平台态的两种表示：无租户与 0 号租户。</param>
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public async Task ResolveAsync_PlatformContext_ShouldNotSwitchTenantAndIgnoreOpenFlag(long? tenantId)
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: false);
        var fixture = CreateFixture(tenantId, (owner, _, _) => owner == 0 ? globalTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Global);

        Assert.NotNull(result);
        Assert.Equal(PrintTemplateScope.Global, result.ResolvedScope);
        fixture.CurrentTenant.Verify(tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// 解析结果必须同时带回"请求作用域"与"实际命中作用域"，前端才能区分是命中私有还是回退到全局。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ShouldReportRequestedAndResolvedScope()
    {
        var globalTemplate = CreateTemplate(0, "ORDER", allowTenantUse: true);
        var fixture = CreateFixture(7, (owner, _, _) => owner == 0 ? globalTemplate : null);

        var result = await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto);

        Assert.NotNull(result);
        Assert.Equal(PrintTemplateScope.Auto, result.RequestedScope);
        Assert.Equal(PrintTemplateScope.Global, result.ResolvedScope);
        Assert.Equal("4", result.RowVersion);
        Assert.Equal(9001, result.BasicId);
    }

    /// <summary>
    /// 未命中时写入的是 Found=false 的负缓存哨兵，解析结果仍然是 null。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Miss_ShouldStoreNegativeSentinelAndReturnNull()
    {
        var fixture = CreateFixture(7, (_, _, _) => null);

        Assert.Null(await fixture.Resolver.ResolveAsync("GHOST", PrintTemplateScope.Tenant));

        var cached = Assert.Single(fixture.CachedItems);
        Assert.False(cached.Found);
        Assert.Equal("GHOST", cached.TemplateCode);
    }

    /// <summary>
    /// 缓存层在故障降级时可能返回 null，解析器必须把它当作未命中而不是抛空引用。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_CacheReturnsNull_ShouldBeTreatedAsMiss()
    {
        var fixture = CreateFixture(7, (_, _, _) => null, cacheReturnsNull: true);

        Assert.Null(await fixture.Resolver.ResolveAsync("ORDER", PrintTemplateScope.Tenant));
    }

    /// <summary>
    /// 不同租户、不同作用域、不同编码必须落在三条互不相同的缓存键上。
    /// </summary>
    [Fact]
    public void PrintTemplateCacheKey_ShouldIsolateTenantScopeAndCode()
    {
        var keys = new[]
        {
            PrintingCacheKeys.PrintTemplate(7, (int)PrintTemplateScope.Auto, "ORDER"),
            PrintingCacheKeys.PrintTemplate(8, (int)PrintTemplateScope.Auto, "ORDER"),
            PrintingCacheKeys.PrintTemplate(7, (int)PrintTemplateScope.Global, "ORDER"),
            PrintingCacheKeys.PrintTemplate(7, (int)PrintTemplateScope.Auto, "SHIP")
        };

        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 平台态的三种表示（null、0、负数）都归一到同一个 platform 段，不会各建一条缓存。
    /// </summary>
    /// <param name="tenantId">请求租户。</param>
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void PrintTemplateCacheKey_PlatformTenants_ShouldShareOneSegment(long? tenantId)
    {
        Assert.Equal(
            "tenant:platform:scope:2:code:ORDER",
            PrintingCacheKeys.PrintTemplate(tenantId, (int)PrintTemplateScope.Global, "ORDER"));
    }

    /// <summary>
    /// 业务租户的缓存键带真实租户号，格式必须与失效模式互相匹配。
    /// </summary>
    [Fact]
    public void PrintTemplateCacheKey_ShouldMatchInvalidationPattern()
    {
        var key = PrintingCacheKeys.PrintTemplate(7, (int)PrintTemplateScope.Auto, "ORDER");
        var pattern = PrintingCacheKeys.AllPrintTemplatesPattern();

        Assert.Equal("tenant:7:scope:0:code:ORDER", key);
        Assert.Equal("tenant:*:scope:*:code:*", pattern);
        Assert.Equal(
            pattern.Split(':').Length,
            key.Split(':').Length);
    }

    /// <summary>
    /// 缓存项自带业务键的租户段，必须显式关闭框架的租户前缀，否则平台与租户会各存一份互相看不见。
    /// </summary>
    [Fact]
    public void PrintTemplateCacheItem_ShouldOptOutOfFrameworkTenantPrefix()
    {
        var attributeNames = typeof(PrintTemplateCacheItem).GetCustomAttributesData()
            .Select(attribute => attribute.AttributeType.Name)
            .ToList();

        Assert.Contains("IgnoreMultiTenancyAttribute", attributeNames, StringComparer.Ordinal);
        Assert.Contains("CacheNameAttribute", attributeNames, StringComparer.Ordinal);
        Assert.Equal("basicapp:printing:print:template", PrintingCacheNames.PrintTemplate);
    }

    /// <summary>
    /// 创建解析器测试夹具，缓存工厂直接执行并记录缓存键与写入的缓存项。
    /// </summary>
    /// <param name="tenantId">当前请求租户；null 表示平台。</param>
    /// <param name="find">按 owner、编码、仅启用标记返回模板的仓储行为。</param>
    /// <param name="cacheReturnsNull">缓存是否模拟降级返回 null。</param>
    private static ResolverEdgeFixture CreateFixture(
        long? tenantId,
        Func<long, string, bool, SysPrintTemplate?> find,
        bool cacheReturnsNull = false)
    {
        var repository = new Mock<IPrintTemplateRepository>();
        repository
            .Setup(value => value.FindByCodeInScopeAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long owner, string code, bool enabledOnly, CancellationToken _) => find(owner, code, enabledOnly));

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(tenantId);
        currentTenant
            .Setup(value => value.Change(It.IsAny<long?>(), It.IsAny<string?>()))
            .Returns(Mock.Of<IDisposable>());

        var cacheKeys = new List<string>();
        var cachedItems = new List<PrintTemplateCacheItem>();
        var cache = new Mock<IDistributedCache<PrintTemplateCacheItem, string>>();
        cache
            .Setup(value => value.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PrintTemplateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<PrintTemplateCacheItem>>, Func<DistributedCacheEntryOptions>, bool?, bool, CancellationToken>(
                async (key, factory, _, _, _, _) =>
                {
                    cacheKeys.Add(key);
                    var item = await factory();
                    cachedItems.Add(item);
                    return cacheReturnsNull ? null : item;
                });

        return new ResolverEdgeFixture(
            new PrintTemplateResolver(repository.Object, currentTenant.Object, cache.Object),
            repository,
            currentTenant,
            cacheKeys,
            cachedItems);
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
            .GetProperty(nameof(SysPrintTemplate.BasicId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(template, tenantId == 0 ? 9001L : 7001L);
        return template;
    }

    /// <summary>
    /// 解析器边界测试依赖集合。
    /// </summary>
    /// <param name="Resolver">被测解析器。</param>
    /// <param name="Repository">仓储替身。</param>
    /// <param name="CurrentTenant">当前租户替身。</param>
    /// <param name="CacheKeys">实际使用的缓存键。</param>
    /// <param name="CachedItems">实际写入缓存的缓存项。</param>
    private sealed record ResolverEdgeFixture(
        PrintTemplateResolver Resolver,
        Mock<IPrintTemplateRepository> Repository,
        Mock<ICurrentTenant> CurrentTenant,
        List<string> CacheKeys,
        List<PrintTemplateCacheItem> CachedItems);
}
