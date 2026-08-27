// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SqlSugar;
using XiHan.BasicApp.WebHost.HealthChecks;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 数据库健康检查判定规则测试。
/// </summary>
/// <remarks>
/// 守三件事：探针只探「当前租户库」而不是遍历所有库；探活语句恒为不触碰任何业务表的字面量；
/// 任何异常都必须就地降级成 Unhealthy 而不是穿透出去把整个 /health 端点打成 500。
/// </remarks>
public sealed class DatabaseHealthCheckTests
{
    /// <summary>
    /// 探活成功时必须判定健康，且不带任何描述与异常——描述与异常是留给故障态的诊断位。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ScalarSucceededShouldReportHealthyWithoutDetails()
    {
        var check = CreateCheck(out _, out _, scalarResult: 1);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// 标量返回 null（某些驱动对 SELECT 1 的返回形态不同）同样算连通，不得因此误判故障。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_NullScalarShouldStillReportHealthy()
    {
        var check = CreateCheck(out _, out _, scalarResult: null);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    /// <summary>
    /// 探活必须走 GetCurrentClient（当前租户库），不得改用按 ConfigId 取库或遍历所有库；
    /// 一旦改掉，租户库挂了平台库还会报健康。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldProbeCurrentTenantClientOnly()
    {
        var check = CreateCheck(out var resolver, out _, scalarResult: 1);

        _ = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        resolver.Verify(value => value.GetCurrentClient(), Times.Once);
        resolver.Verify(value => value.GetClient(It.IsAny<string>()), Times.Never);
        resolver.Verify(value => value.GetAllClients(), Times.Never);
        resolver.Verify(value => value.GetAllConfigIds(), Times.Never);
        resolver.Verify(value => value.AsTenant(), Times.Never);
    }

    /// <summary>
    /// 探活语句必须恒为字面量 SELECT 1：一旦改成查业务表，健康探针就会随表存在与否、
    /// 权限变化而误报，还会在高频探针下产生真实表扫描。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldExecuteConstantSelectOneOnly()
    {
        var check = CreateCheck(out _, out var ado, scalarResult: 1);

        _ = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        ado.Verify(value => value.GetScalarAsync("SELECT 1"), Times.Once);
        ado.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 执行探活抛异常时必须降级为 Unhealthy，描述恰为约定文案，且原异常对象原样透传，
    /// 以便健康检查框架侧留下可诊断的线索。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ScalarThrewShouldDegradeAndKeepOriginalException()
    {
        var failure = new InvalidOperationException("连接串指向的库不可达");
        var resolver = new Mock<ISqlSugarClientResolver>();
        var ado = new Mock<IAdo>();
        _ = ado.Setup(value => value.GetScalarAsync("SELECT 1")).ThrowsAsync(failure);

        var client = new Mock<ISqlSugarClient>();
        _ = client.Setup(value => value.Ado).Returns(ado.Object);
        _ = resolver.Setup(value => value.GetCurrentClient()).Returns(client.Object);

        var result = await new DatabaseHealthCheck(resolver.Object)
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("数据库连接失败", result.Description);
        Assert.Same(failure, result.Exception);
    }

    /// <summary>
    /// 解析器本身抛异常（多租户上下文/连接装配失败）也必须被同一个 catch 吞掉降级，
    /// 不得外抛——否则 /health 端点会直接 500 而不是返回不健康。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ResolverThrewShouldDegradeInsteadOfPropagating()
    {
        var failure = new InvalidOperationException("租户连接解析失败");
        var resolver = new Mock<ISqlSugarClientResolver>();
        _ = resolver.Setup(value => value.GetCurrentClient()).Throws(failure);

        var result = await new DatabaseHealthCheck(resolver.Object)
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("数据库连接失败", result.Description);
        Assert.Same(failure, result.Exception);
    }

    /// <summary>
    /// 锁定当前真实行为：检查方法既不检查也不透传取消令牌，令牌已取消时依旧照常执行探活。
    /// </summary>
    /// <remarks>
    /// 这条不是在宣称「正确」，而是防止行为在无人察觉时漂移。
    /// 当前实现不调用 ThrowIfCancellationRequested、也不把令牌交给 GetScalarAsync，
    /// 因此传入已取消令牌时仍会返回探活结果本身（此处为 Healthy），不会抛 OperationCanceledException。
    /// 相关口径问题已作为源码疑点上报。
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_CanceledTokenShouldNotThrowUnderCurrentImplementation()
    {
        var check = CreateCheck(out _, out var ado, scalarResult: 1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await check.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        ado.Verify(value => value.GetScalarAsync("SELECT 1"), Times.Once);
    }

    /// <summary>
    /// 组装一个探活成功的健康检查实例。
    /// </summary>
    /// <param name="resolver">SqlSugar 客户端解析器替身。</param>
    /// <param name="ado">Ado 执行器替身。</param>
    /// <param name="scalarResult">GetScalarAsync 的返回标量。</param>
    /// <returns>被测健康检查实例。</returns>
    private static DatabaseHealthCheck CreateCheck(
        out Mock<ISqlSugarClientResolver> resolver,
        out Mock<IAdo> ado,
        object? scalarResult)
    {
        ado = new Mock<IAdo>();
        // Setup 表达式与生产代码逐字同形，确保绑定到同一个可选参数重载，避免 Setup 不命中导致假绿
        _ = ado.Setup(value => value.GetScalarAsync("SELECT 1")).ReturnsAsync(scalarResult!);

        var client = new Mock<ISqlSugarClient>();
        _ = client.Setup(value => value.Ado).Returns(ado.Object);

        resolver = new Mock<ISqlSugarClientResolver>();
        _ = resolver.Setup(value => value.GetCurrentClient()).Returns(client.Object);

        return new DatabaseHealthCheck(resolver.Object);
    }
}
