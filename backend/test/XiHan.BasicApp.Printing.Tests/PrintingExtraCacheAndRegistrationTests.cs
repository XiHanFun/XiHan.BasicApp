// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Extensions;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模块缓存失效器与数据源注册扩展的行为测试。
/// </summary>
/// <remarks>
/// 失效必须带 <c>considerUow: true</c>：清理动作要延迟到事务成功提交之后执行。
/// 若改成立即清理，其它请求会在事务还没提交的窗口里重新把**旧**模板读进缓存，
/// 等事务提交完成后缓存里躺的仍是旧设计，且不会再有任何一次失效来纠正它。
/// 这条约定源码注释写了两遍，这里用会红的断言把它固定下来。
/// </remarks>
public sealed class PrintingExtraCacheAndRegistrationTests
{
    /// <summary>
    /// 模板缓存失效必须覆盖全部租户与作用域，并延迟到工作单元提交之后执行。
    /// </summary>
    [Fact]
    public async Task InvalidatePrintTemplateAsync_ShouldRemoveAllKeysAfterUnitOfWorkCommit()
    {
        var cache = new Mock<IDistributedCache<PrintTemplateCacheItem, string>>();
        var invalidator = new PrintingCacheInvalidator(cache.Object);

        await invalidator.InvalidatePrintTemplateAsync();

        cache.Verify(
            value => value.RemoveByPatternAsync(
                PrintingCacheKeys.AllPrintTemplatesPattern(),
                true,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 取消令牌必须透传给缓存组件，取消才能在真实的分布式调用处生效。
    /// </summary>
    [Fact]
    public async Task InvalidatePrintTemplateAsync_ShouldForwardCancellationToken()
    {
        var cache = new Mock<IDistributedCache<PrintTemplateCacheItem, string>>();
        var invalidator = new PrintingCacheInvalidator(cache.Object);
        using var cancellation = new CancellationTokenSource();

        await invalidator.InvalidatePrintTemplateAsync(cancellation.Token);

        cache.Verify(
            value => value.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), cancellation.Token),
            Times.Once);
    }

    /// <summary>
    /// 数据源注册扩展可重复调用：内置示例只登记一次，重复调用不会在启动时撞上重复编码。
    /// </summary>
    [Fact]
    public void AddPrintingDataSources_CalledTwice_ShouldRegisterBuiltInDemoOnce()
    {
        var services = new ServiceCollection();

        _ = services.AddPrintingDataSources();
        _ = services.AddPrintingDataSources();

        var demoRegistrations = services
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<PrintDataSourceRegistration>()
            .Count(registration => registration.Definition.Code == BuiltInPrintDataSources.SystemPrintDemo.Code);
        Assert.Equal(1, demoRegistrations);
    }

    /// <summary>
    /// 重复调用注册扩展后仍能构造出注册表单例，且内置示例可被查到。
    /// </summary>
    [Fact]
    public void AddPrintingDataSources_ShouldResolveSingletonRegistryContainingDemo()
    {
        var services = new ServiceCollection();
        _ = services.AddPrintingDataSources();
        _ = services.AddPrintingDataSources();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IPrintDataSourceRegistry>();

        Assert.True(registry.IsRegistered(BuiltInPrintDataSources.SystemPrintDemo.Code));
        Assert.Same(registry, provider.GetRequiredService<IPrintDataSourceRegistry>());
    }

    /// <summary>
    /// 业务模块追加的数据源与内置示例共存，目录按编码序数排序输出。
    /// </summary>
    [Fact]
    public void RegisterPrintDataSource_ShouldAppendModuleDefinitionAlongsideBuiltIn()
    {
        var services = new ServiceCollection();
        _ = services.AddPrintingDataSources();
        _ = services.RegisterPrintDataSource(new PrintDataSourceDefinition(
            "erp.order", "订单", [new("title", "标题")], "{\"title\":\"示例\"}"));

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IPrintDataSourceRegistry>();

        Assert.Equal(["erp.order", "system.print-demo"], registry.GetAll().Select(source => source.Code));
    }

    /// <summary>
    /// 重复编码的数据源在解析注册表时抛出，使冲突暴露在启动阶段而不是首个请求。
    /// </summary>
    [Fact]
    public void RegisterPrintDataSource_DuplicateCode_ShouldFailWhenRegistryIsResolved()
    {
        var services = new ServiceCollection();
        _ = services.AddPrintingDataSources();
        _ = services.RegisterPrintDataSource(BuiltInPrintDataSources.SystemPrintDemo);

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IPrintDataSourceRegistry>());
        Assert.Contains(BuiltInPrintDataSources.SystemPrintDemo.Code, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 登记空定义必须立即以 <see cref="ArgumentNullException"/> 拒绝，不留到解析注册表时才炸。
    /// </summary>
    [Fact]
    public void RegisterPrintDataSource_NullDefinition_ShouldThrowArgumentNull()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentNullException>(() => services.RegisterPrintDataSource(null!));
    }
}
