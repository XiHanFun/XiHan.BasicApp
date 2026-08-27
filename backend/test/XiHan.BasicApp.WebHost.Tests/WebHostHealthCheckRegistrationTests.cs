// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.WebHost.HealthChecks;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 宿主模块健康检查注册契约测试。
/// </summary>
/// <remarks>
/// 三个检查项名称会原样出现在 /health 响应的 checks[].name 里，是运维告警规则直接匹配的对外契约；
/// 名称与实现类型的对应关系一旦错配，某一路故障会挂在另一个名字上，排障方向会被彻底带偏。
/// </remarks>
public sealed class WebHostHealthCheckRegistrationTests
{
    /// <summary>
    /// 必须注册健康检查服务本体，否则后续 MapHealthChecks 会在启动期直接抛异常。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldRegisterHealthCheckService()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetService<HealthCheckService>());
    }

    /// <summary>
    /// 检查项名称必须恰为 database、redis、qdrant 三个，且注册顺序稳定。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldRegisterExactlyThreeChecksInStableOrder()
    {
        var names = ReadRegistrations().Select(registration => registration.Name).ToList();

        Assert.Equal(["database", "redis", "qdrant"], names);
    }

    /// <summary>
    /// 名称必须唯一且全小写：框架按名字去重，重名会静默丢掉一个检查项。
    /// </summary>
    [Fact]
    public void ConfigureServices_CheckNamesShouldBeUniqueAndLowerCase()
    {
        var names = ReadRegistrations().Select(registration => registration.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(names, name => Assert.Equal(name.ToLowerInvariant(), name, StringComparer.Ordinal));
    }

    /// <summary>
    /// 名称到实现类型的映射必须一一对应，且每个实现都能被容器真正造出来。
    /// </summary>
    /// <remarks>
    /// AddCheck 走 ActivatorUtilities 创建实例，构造签名变了但依赖没注册时，
    /// 失败会推迟到第一次探针而不是启动期，因此这里显式把三个实例都造一遍。
    /// </remarks>
    [Fact]
    public void ConfigureServices_ShouldMapEachCheckNameToItsImplementation()
    {
        using var provider = BuildProvider();
        var registrations = ReadRegistrations(provider);

        var actual = registrations.ToDictionary(
            registration => registration.Name,
            registration => registration.Factory(provider).GetType(),
            StringComparer.Ordinal);

        Assert.Equal(typeof(DatabaseHealthCheck), actual["database"]);
        Assert.Equal(typeof(RedisHealthCheck), actual["redis"]);
        Assert.Equal(typeof(QdrantHealthCheck), actual["qdrant"]);
    }

    /// <summary>
    /// 三项的失败状态都必须是 Unhealthy，不得被降级成 Degraded——否则库挂了 /health 依旧返回 200。
    /// </summary>
    [Fact]
    public void ConfigureServices_EveryCheckShouldFailAsUnhealthy()
    {
        var registrations = ReadRegistrations();

        Assert.All(registrations, registration => Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus));
    }

    /// <summary>
    /// 数据库健康检查的构造依赖必须能被容器解析，否则第一次探针才会炸。
    /// </summary>
    [Fact]
    public void ConfigureServices_DatabaseCheckShouldBeConstructibleFromContainer()
    {
        using var provider = BuildProvider();
        var registration = ReadRegistrations(provider).Single(item =>
            string.Equals(item.Name, "database", StringComparison.Ordinal));

        var instance = registration.Factory(provider);

        _ = Assert.IsType<DatabaseHealthCheck>(instance);
    }

    /// <summary>
    /// 执行宿主模块的服务配置并构建容器。
    /// </summary>
    /// <remarks>
    /// 只补上健康检查实现所需的最小依赖（SqlSugar 客户端解析器替身），不启动任何真实组件。
    /// </remarks>
    /// <returns>已构建的服务提供者。</returns>
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        // 健康检查服务本体依赖日志，真实宿主由通用主机注册，这里补齐最小依赖
        _ = services.AddLogging();
        new XiHanBasicAppWebHostModule().ConfigureServices(new ServiceConfigurationContext(services));
        _ = services.AddSingleton(new Mock<ISqlSugarClientResolver>().Object);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 读取健康检查注册项列表。
    /// </summary>
    /// <param name="provider">已构建的服务提供者；为空时内部自行构建一个。</param>
    /// <returns>按注册顺序排列的注册项。</returns>
    private static List<HealthCheckRegistration> ReadRegistrations(ServiceProvider? provider = null)
    {
        if (provider is not null)
        {
            return [.. provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations];
        }

        using var owned = BuildProvider();
        return [.. owned.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations];
    }
}
