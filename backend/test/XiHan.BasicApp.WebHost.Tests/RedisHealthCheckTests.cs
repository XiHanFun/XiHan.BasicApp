// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using StackExchange.Redis;
using XiHan.BasicApp.WebHost.HealthChecks;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// Redis 健康检查判定规则测试。
/// </summary>
/// <remarks>
/// 这个检查最关键的一条是「未启用即放行」：框架只在缓存配置打开 Redis 时才注册
/// <see cref="IConnectionMultiplexer"/>，走进程内缓存回退的部署里根本没有这个单例。
/// 如果解析不到就判故障，所有未启用 Redis 的环境都会被探针判死、K8s 会反复重启 Pod。
/// </remarks>
public sealed class RedisHealthCheckTests
{
    /// <summary>
    /// 未注册多路复用器（进程内缓存回退）时必须判定健康，并带上可被运维识别的回退标记文案。
    /// </summary>
    /// <remarks>
    /// 文案属于对外契约：告警规则与看板按这串中文（含中文括号）匹配，改字面量即视为破坏契约。
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_MultiplexerNotRegisteredShouldReportHealthyWithFallbackNotice()
    {
        var check = new RedisHealthCheck(new ServiceCollection().BuildServiceProvider());

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Redis 未启用（进程内回退）", result.Description);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// PING 成功时必须判定健康且不带描述——真连上与降级放行必须能被运维区分开。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_PingSucceededShouldReportHealthyWithoutDescription()
    {
        var check = new RedisHealthCheck(BuildProviderWithMultiplexer(out _, out var database));
        _ = database.Setup(value => value.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(1));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// PING 抛异常时必须降级为 Unhealthy，描述恰为约定文案，且原异常引用相等地透传。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_PingThrewShouldDegradeAndKeepOriginalException()
    {
        var failure = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "无法连接到 6379");
        var check = new RedisHealthCheck(BuildProviderWithMultiplexer(out _, out var database));
        _ = database.Setup(value => value.PingAsync(It.IsAny<CommandFlags>())).ThrowsAsync(failure);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis 连接失败", result.Description);
        Assert.Same(failure, result.Exception);
    }

    /// <summary>
    /// 取库（GetDatabase）阶段抛异常（多路复用器已释放或尚未就绪）也必须落进同一个 catch 降级，
    /// 不得把异常抛给健康检查框架。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_GetDatabaseThrewShouldDegradeInsteadOfPropagating()
    {
        var failure = new ObjectDisposedException(nameof(IConnectionMultiplexer));
        var multiplexer = new Mock<IConnectionMultiplexer>();
        _ = multiplexer.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Throws(failure);

        var services = new ServiceCollection();
        _ = services.AddSingleton(multiplexer.Object);
        var check = new RedisHealthCheck(services.BuildServiceProvider());

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis 连接失败", result.Description);
        Assert.Same(failure, result.Exception);
    }

    /// <summary>
    /// 每次检查都必须重新解析多路复用器，不得把首次解析结果缓存进字段；
    /// 否则 Redis 后启用或连接重建之后，探针会永远停留在旧判定上。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldResolveMultiplexerOnEveryProbe()
    {
        var database = new Mock<IDatabase>();
        _ = database.Setup(value => value.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(1));

        var multiplexer = new Mock<IConnectionMultiplexer>();
        _ = multiplexer.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(database.Object);

        // 第一次解析不到（Redis 尚未启用），第二次解析得到实例（Redis 已启用）
        var provider = new SequencedServiceProvider(typeof(IConnectionMultiplexer), null, multiplexer.Object);
        var check = new RedisHealthCheck(provider);

        var first = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        var second = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal("Redis 未启用（进程内回退）", first.Description);
        Assert.Null(second.Description);
        Assert.Equal(2, provider.ResolveCount);
    }

    /// <summary>
    /// 锁定当前真实行为：取消令牌既不被检查也不被透传给 PING，已取消令牌下探活照常进行。
    /// </summary>
    /// <remarks>
    /// 与数据库健康检查同源的口径问题，已作为源码疑点上报；这条用例只负责让行为漂移可见。
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_CanceledTokenShouldNotThrowUnderCurrentImplementation()
    {
        var check = new RedisHealthCheck(BuildProviderWithMultiplexer(out _, out var database));
        _ = database.Setup(value => value.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(1));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await check.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        database.Verify(value => value.PingAsync(It.IsAny<CommandFlags>()), Times.Once);
    }

    /// <summary>
    /// 组装一个注册了多路复用器替身的服务提供者。
    /// </summary>
    /// <param name="multiplexer">多路复用器替身。</param>
    /// <param name="database">数据库替身。</param>
    /// <returns>服务提供者。</returns>
    private static IServiceProvider BuildProviderWithMultiplexer(
        out Mock<IConnectionMultiplexer> multiplexer,
        out Mock<IDatabase> database)
    {
        database = new Mock<IDatabase>();
        var databaseObject = database.Object;

        multiplexer = new Mock<IConnectionMultiplexer>();
        _ = multiplexer.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseObject);

        var services = new ServiceCollection();
        _ = services.AddSingleton(multiplexer.Object);
        return services.BuildServiceProvider();
    }
}
