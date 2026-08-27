// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 模板渲染器解析器测试。
/// </summary>
/// <remarks>
/// 解析器是"模板引擎枚举 → 渲染器实现"的唯一入口：
/// 未注册的引擎必须显式拒绝（带出引擎名与当前支持范围），
/// 而不是回落到默认渲染器把 T4 模板当 Scriban 渲染出一堆空值。
/// </remarks>
public sealed class TemplateRendererResolverTests
{
    /// <summary>
    /// 构造入参为 null 立即拒绝（DI 注册异常时尽早暴露，而不是解析时才空引用）。
    /// </summary>
    [Fact]
    public void Constructor_NullRenderersShouldThrow()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TemplateRendererResolver(null!));
    }

    /// <summary>
    /// 按引擎解析到对应渲染器。
    /// </summary>
    [Fact]
    public void Resolve_ShouldReturnRendererMatchingEngine()
    {
        var scriban = CreateRenderer(TemplateEngine.Scriban);
        var t4 = CreateRenderer(TemplateEngine.T4);
        var resolver = new TemplateRendererResolver([scriban, t4]);

        Assert.Same(scriban, resolver.Resolve(TemplateEngine.Scriban));
        Assert.Same(t4, resolver.Resolve(TemplateEngine.T4));
    }

    /// <summary>
    /// 真实的 Scriban 渲染器注册后必须能被 Scriban 引擎解析到（引擎属性与注册键一致）。
    /// </summary>
    [Fact]
    public void Resolve_RealScribanRendererShouldBeReachable()
    {
        var renderer = new ScribanTemplateRenderer();
        var resolver = new TemplateRendererResolver([renderer]);

        Assert.Equal(TemplateEngine.Scriban, renderer.Engine);
        Assert.Same(renderer, resolver.Resolve(TemplateEngine.Scriban));
    }

    /// <summary>
    /// 同一引擎注册多个时后注册覆盖先注册，便于用自定义实现替换默认渲染器。
    /// </summary>
    [Fact]
    public void Resolve_LaterRegistrationShouldOverrideEarlierForSameEngine()
    {
        var first = CreateRenderer(TemplateEngine.Scriban);
        var second = CreateRenderer(TemplateEngine.Scriban);

        var resolver = new TemplateRendererResolver([first, second]);

        Assert.Same(second, resolver.Resolve(TemplateEngine.Scriban));
    }

    /// <summary>
    /// 未注册的引擎必须抛 <see cref="NotSupportedException"/>，
    /// 且消息带出引擎名与当前支持范围，让用户能据此改模板配置。
    /// </summary>
    [Fact]
    public void Resolve_UnregisteredEngineShouldThrowNotSupported()
    {
        var resolver = new TemplateRendererResolver([CreateRenderer(TemplateEngine.Scriban)]);

        var exception = Assert.Throws<NotSupportedException>(() => resolver.Resolve(TemplateEngine.T4));

        Assert.Contains("T4", exception.Message, StringComparison.Ordinal);
        Assert.Contains("当前仅支持 Scriban", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 一个渲染器都没注册时任何解析都拒绝，不得静默返回 null 让上层空引用。
    /// </summary>
    /// <param name="engine">模板引擎</param>
    [Theory]
    [InlineData(TemplateEngine.Scriban)]
    [InlineData(TemplateEngine.T4)]
    public void Resolve_EmptyRegistrationShouldAlwaysThrow(TemplateEngine engine)
    {
        var resolver = new TemplateRendererResolver([]);

        _ = Assert.Throws<NotSupportedException>(() => resolver.Resolve(engine));
    }

    /// <summary>
    /// 构造一个只声明引擎的渲染器桩。
    /// </summary>
    /// <param name="engine">桩渲染器声明的引擎</param>
    private static ITemplateRenderer CreateRenderer(TemplateEngine engine)
    {
        var mock = new Mock<ITemplateRenderer>();
        mock.SetupGet(renderer => renderer.Engine).Returns(engine);
        return mock.Object;
    }
}
