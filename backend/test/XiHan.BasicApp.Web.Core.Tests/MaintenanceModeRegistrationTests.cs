// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Web.Core.Upgrade;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Services;
using XiHan.Framework.Web.Api;
using XiHan.Framework.Web.Core;
using XiHan.Framework.Web.Docs;
using XiHan.Framework.Web.Gateway;
using XiHan.Framework.Web.Mcp;
using XiHan.Framework.Web.RealTime;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// Web 核心模块注册形状与管线装配测试。
/// </summary>
/// <remarks>
/// 维护模式由三处协作才能成立：状态必须是单例（升级引擎置位的实例与中间件读取的实例必须是同一个）、
/// 管理器必须替换掉框架的空实现、中间件必须真的被装进管线。任何一处走样都会让维护模式
/// 在不报错的情况下静默失效，因此这里把注册形状与真实管线行为一起钉死。
/// </remarks>
public sealed class MaintenanceModeRegistrationTests
{
    /// <summary>
    /// 状态必须注册为单例：否则升级引擎置位的实例与中间件读取的实例不是同一个，维护模式静默失效。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldRegisterStateAsSingleton()
    {
        var services = ConfigureModuleServices();

        var descriptor = Assert.Single(services, value => value.ServiceType == typeof(MaintenanceModeState));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(MaintenanceModeState), descriptor.ImplementationType);
    }

    /// <summary>
    /// 注册状态用的是 TryAdd 语义：宿主若已预先注册过自定义状态实例，模块不得顶掉它。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldNotOverridePreRegisteredState()
    {
        var preRegistered = new MaintenanceModeState();
        var services = new ServiceCollection();
        _ = services.AddSingleton(preRegistered);

        new XiHanBasicAppWebCoreModule().ConfigureServices(new ServiceConfigurationContext(services));

        var descriptor = Assert.Single(services, value => value.ServiceType == typeof(MaintenanceModeState));
        Assert.Same(preRegistered, descriptor.ImplementationInstance);
        Assert.Same(preRegistered, services.BuildServiceProvider().GetRequiredService<MaintenanceModeState>());
    }

    /// <summary>
    /// 管理器必须替换为本应用实现，且描述符唯一：否则可能解析回框架的空实现
    /// <c>DefaultUpgradeMaintenanceModeManager</c>（只打日志不置位），升级期间完全不进维护模式且不报错。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldReplaceMaintenanceModeManagerWithAppImplementation()
    {
        var services = ConfigureModuleServices();

        var descriptor = Assert.Single(services, value => value.ServiceType == typeof(IUpgradeMaintenanceModeManager));
        Assert.Equal(typeof(BasicAppUpgradeMaintenanceModeManager), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    /// Replace 语义必须真的生效：预置框架空实现后调用模块配置，容器里只能剩下本应用实现，
    /// 不能出现「后注册者胜出」的顺序依赖。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldRemoveFrameworkDefaultManagerDescriptor()
    {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IUpgradeMaintenanceModeManager, DefaultUpgradeMaintenanceModeManager>();

        new XiHanBasicAppWebCoreModule().ConfigureServices(new ServiceConfigurationContext(services));

        var descriptor = Assert.Single(services, value => value.ServiceType == typeof(IUpgradeMaintenanceModeManager));
        Assert.Equal(typeof(BasicAppUpgradeMaintenanceModeManager), descriptor.ImplementationType);
        Assert.DoesNotContain(
            services,
            value => value.ImplementationType == typeof(DefaultUpgradeMaintenanceModeManager));
    }

    /// <summary>
    /// 模块只登记这两条注册，不得夹带未声明的副作用注册。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldRegisterExactlyTwoDescriptors()
    {
        var services = ConfigureModuleServices();

        Assert.Equal(2, services.Count);
    }

    /// <summary>
    /// 模块必须继承框架模块基类，否则框架的模块发现机制根本不会加载它，
    /// 全部注册与管线装配一并丢失。
    /// </summary>
    [Fact]
    public void Module_ShouldInheritXiHanModule()
    {
        Assert.True(typeof(XiHanModule).IsAssignableFrom(typeof(XiHanBasicAppWebCoreModule)));
    }

    /// <summary>
    /// 依赖集合必须精确等于既定的 7 个模块：少一个则对应能力在宿主里悄悄缺席，
    /// 多一个则引入未声明的启动依赖。任何增删都必须显式改红本测试。
    /// </summary>
    [Fact]
    public void Module_DependsOnShouldBeExactlySevenModules()
    {
        var attribute = Assert.Single(
            typeof(XiHanBasicAppWebCoreModule).GetCustomAttributes(typeof(DependsOnAttribute), inherit: false)
                .Cast<DependsOnAttribute>());

        Type[] expected =
        [
            typeof(XiHanBasicAppCoreModule),
            typeof(XiHanWebCoreModule),
            typeof(XiHanWebApiModule),
            typeof(XiHanWebDocsModule),
            typeof(XiHanWebRealTimeModule),
            typeof(XiHanWebGatewayModule),
            typeof(XiHanWebMcpModule)
        ];

        Assert.Equal(
            expected.Select(value => value.FullName).OrderBy(value => value, StringComparer.Ordinal),
            attribute.GetDependedTypes().Select(value => value.FullName).OrderBy(value => value, StringComparer.Ordinal));
    }

    /// <summary>
    /// 维护模式三个类型必须 public sealed：不 sealed 就能被继承出分叉实现绕开单例语义，
    /// 不 public 则宿主无法注册与装配。
    /// </summary>
    /// <param name="typeName">被约束的类型全名</param>
    [Theory]
    [InlineData("XiHan.BasicApp.Web.Core.Upgrade.MaintenanceModeState")]
    [InlineData("XiHan.BasicApp.Web.Core.Upgrade.BasicAppUpgradeMaintenanceModeManager")]
    [InlineData("XiHan.BasicApp.Web.Core.Upgrade.MaintenanceModeMiddleware")]
    public void MaintenanceModeTypes_ShouldBePublicSealed(string typeName)
    {
        var type = typeof(MaintenanceModeState).Assembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.False(type.IsAbstract);
    }

    /// <summary>
    /// 注册扩展必须是 public static 类，否则扩展方法对宿主不可见。
    /// </summary>
    [Fact]
    public void ApplicationBuilderExtensions_ShouldBePublicStaticClass()
    {
        var type = typeof(MaintenanceModeApplicationBuilderExtensions);

        Assert.True(type.IsPublic);
        Assert.True(type.IsAbstract && type.IsSealed);
    }

    /// <summary>
    /// UseMaintenanceMode 必须返回传入的同一个 builder，否则调用方链式装配的后续中间件会挂到别的管线上。
    /// </summary>
    [Fact]
    public void UseMaintenanceMode_ShouldReturnSameBuilderInstance()
    {
        using var serviceProvider = MaintenanceModeTestHelper.CreateServiceProvider(new MaintenanceModeState());
        var app = new ApplicationBuilder(serviceProvider);

        var returned = app.UseMaintenanceMode();

        Assert.Same(app, returned);
    }

    /// <summary>
    /// 真实管线装配后，维护期间业务路径必须拿到 503 且不落到终端委托，
    /// 证明注册方法不只是名义上生效。
    /// </summary>
    [Fact]
    public async Task UseMaintenanceMode_ActiveShouldReturn503WithoutReachingTerminal()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        using var serviceProvider = MaintenanceModeTestHelper.CreateServiceProvider(state);
        var terminalCalls = 0;
        var pipeline = MaintenanceModeTestHelper.BuildPipeline(serviceProvider, _ =>
        {
            terminalCalls++;
            return Task.CompletedTask;
        });
        var context = MaintenanceModeTestHelper.CreateContext("/api/order");

        await pipeline(context);

        Assert.Equal(0, terminalCalls);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal("30", context.Response.Headers["Retry-After"]);
    }

    /// <summary>
    /// 未进入维护模式时，真实管线必须把请求交给终端委托，常态流量不能被吃掉。
    /// </summary>
    [Fact]
    public async Task UseMaintenanceMode_InactiveShouldReachTerminal()
    {
        using var serviceProvider = MaintenanceModeTestHelper.CreateServiceProvider(new MaintenanceModeState());
        var terminalCalls = 0;
        var pipeline = MaintenanceModeTestHelper.BuildPipeline(serviceProvider, _ =>
        {
            terminalCalls++;
            return Task.CompletedTask;
        });
        var context = MaintenanceModeTestHelper.CreateContext("/api/order");

        await pipeline(context);

        Assert.Equal(1, terminalCalls);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    /// <summary>
    /// 容器里缺少状态时，管线构建必须立即失败，而不是运行期静默放行——
    /// 漏注册状态若能悄悄跑起来，维护模式会在无人察觉的情况下失效。
    /// </summary>
    [Fact]
    public void UseMaintenanceMode_MissingStateShouldFailAtBuild()
    {
        using var serviceProvider = MaintenanceModeTestHelper.CreateServiceProvider(null);

        _ = Assert.ThrowsAny<InvalidOperationException>(
            () => MaintenanceModeTestHelper.BuildPipeline(serviceProvider, _ => Task.CompletedTask));
    }

    /// <summary>
    /// 模块初始化必须经 IObjectAccessor&lt;IApplicationBuilder&gt; 取到 builder 并装上中间件：
    /// 装配后维护期业务路径返回 503，否则模块声明了维护能力却从未接入管线。
    /// </summary>
    [Fact]
    public async Task OnApplicationInitialization_ShouldPlugMiddlewareIntoPipeline()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var (pipeline, terminalCalls) = InitializeModulePipeline(state);
        var context = MaintenanceModeTestHelper.CreateContext("/api/order");

        await pipeline(context);

        Assert.Equal(0, terminalCalls.Value);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    /// <summary>
    /// 模块初始化装上的中间件在非维护期必须放行，不能把常态流量也拦了。
    /// </summary>
    [Fact]
    public async Task OnApplicationInitialization_InactiveShouldReachTerminal()
    {
        var (pipeline, terminalCalls) = InitializeModulePipeline(new MaintenanceModeState());
        var context = MaintenanceModeTestHelper.CreateContext("/api/order");

        await pipeline(context);

        Assert.Equal(1, terminalCalls.Value);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    /// <summary>
    /// 执行模块的服务配置并返回收集到的服务集合。
    /// </summary>
    /// <returns>模块登记后的服务集合</returns>
    private static IServiceCollection ConfigureModuleServices()
    {
        var services = new ServiceCollection();
        new XiHanBasicAppWebCoreModule().ConfigureServices(new ServiceConfigurationContext(services));
        return services;
    }

    /// <summary>
    /// 按框架真实路径走一遍模块初始化：容器提供 IObjectAccessor&lt;IApplicationBuilder&gt;，
    /// 模块从中取出 builder 装配中间件，再补一个终端委托后构建管线。
    /// </summary>
    /// <param name="state">维护模式状态</param>
    /// <returns>可驱动的管线与终端命中计数</returns>
    private static (RequestDelegate Pipeline, StrongBox<int> TerminalCalls) InitializeModulePipeline(MaintenanceModeState state)
    {
        var accessor = new ObjectAccessor<IApplicationBuilder>();
        var services = new ServiceCollection();
        _ = services.AddSingleton(state);
        _ = services.AddSingleton<IObjectAccessor<IApplicationBuilder>>(accessor);
        var serviceProvider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(serviceProvider);
        accessor.Value = app;

        new XiHanBasicAppWebCoreModule().OnApplicationInitialization(new ApplicationInitializationContext(serviceProvider));

        var terminalCalls = new StrongBox<int>(0);
        app.Run(_ =>
        {
            terminalCalls.Value++;
            return Task.CompletedTask;
        });

        return (app.Build(), terminalCalls);
    }
}
