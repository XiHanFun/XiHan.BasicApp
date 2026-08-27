// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.VectorData;
using System.Globalization;
using System.Reflection;
using XiHan.BasicApp.WebHost.HealthChecks;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 向量库健康检查判定规则测试。
/// </summary>
/// <remarks>
/// 与 Redis 检查刻意相反：向量库是无条件注册的，解析不到就是装配错误，必须判故障而不是放行。
/// 另外两条关键约定是「只推进一次枚举器」（否则集合多时探针退化成 O(n) 网络往返）
/// 和「探活必须有超时上限」（否则目标主机不可路由会把整个 /health 拖死）。
/// </remarks>
public sealed class QdrantHealthCheckTests
{
    /// <summary>
    /// 解析不到向量库属于装配漏注册，必须判定不健康并给出约定文案，不得像 Redis 那样放行。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_VectorStoreNotRegisteredShouldReportUnhealthy()
    {
        var check = new QdrantHealthCheck(new ServiceCollection().BuildServiceProvider());

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("向量库未注册", result.Description);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// 能拿到集合名即说明服务可达，判定健康且不带描述与异常。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_FirstCollectionNameReachedShouldReportHealthy()
    {
        var store = StubVectorStore.WithNames("documents");
        var check = new QdrantHealthCheck(BuildProvider(store));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// 一个集合都没有（尚未摄取任何文档的全新部署）同样必须判定健康——
    /// 集合存不存在与服务可达性无关，若改成要求集合存在，新部署会被误判故障。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_EmptyCollectionListShouldStillReportHealthy()
    {
        var store = StubVectorStore.Empty();
        var check = new QdrantHealthCheck(BuildProvider(store));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(0, store.MoveNextCount);
    }

    /// <summary>
    /// 只允许推进枚举器一次就 break：集合数很多时把列表拉完会让探针退化成 O(n) 网络往返。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldAdvanceEnumeratorOnlyOnce()
    {
        var store = StubVectorStore.WithNames("a", "b", "c");
        var check = new QdrantHealthCheck(BuildProvider(store));

        _ = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(1, store.MoveNextCount);
    }

    /// <summary>
    /// 枚举过程中抛非取消异常必须降级为 Unhealthy，描述恰为约定文案，且原异常引用相等地透传。
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_EnumerationThrewShouldDegradeAndKeepOriginalException()
    {
        var failure = new InvalidOperationException("gRPC 通道建立失败");
        var check = new QdrantHealthCheck(BuildProvider(StubVectorStore.Throwing(failure)));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("向量库连接失败", result.Description);
        Assert.Same(failure, result.Exception);
    }

    /// <summary>
    /// 探活必须有超时上限：目标主机不可路由时枚举会一直挂着，超时后必须返回不健康，
    /// 描述里带上按源码常量算出来的秒数。
    /// </summary>
    /// <remarks>
    /// 期望文案不硬编码秒数，而是反射读私有静态常量 ProbeTimeout 后按源码同款格式拼装，
    /// 这样常量被改动时这条用例仍然成立，而「上限必须 ≤ 5 秒」由另一条用例把关。
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_ProbeHangingShouldTimeOutAndReportUnhealthy()
    {
        var store = StubVectorStore.Hanging();
        var check = new QdrantHealthCheck(BuildProvider(store));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(
            $"向量库探活超时（{ReadProbeTimeout().TotalSeconds.ToString("0", CultureInfo.InvariantCulture)} 秒）",
            result.Description);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// 超时分支与外部取消分支必须区分开：调用方主动取消时走的是「连接失败」分支，不得报成超时。
    /// </summary>
    /// <remarks>
    /// 源码用 <c>when (!cancellationToken.IsCancellationRequested)</c> 过滤器把两者分开，
    /// 一旦写反，探针端主动取消就会被误报成向量库超时，把排障方向带偏。
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_CallerCanceledShouldReportConnectionFailureNotTimeout()
    {
        var check = new QdrantHealthCheck(BuildProvider(StubVectorStore.Hanging()));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await check.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("向量库连接失败", result.Description);
        Assert.IsAssignableFrom<OperationCanceledException>(result.Exception);
    }

    /// <summary>
    /// 探活超时上限必须保持在探针可接受量级：放大到编排层 liveness 探针之上就等于没有上限。
    /// </summary>
    [Fact]
    public void ProbeTimeout_ShouldStayWithinProbeBudget()
    {
        var probeTimeout = ReadProbeTimeout();

        Assert.True(probeTimeout > TimeSpan.Zero, "探活超时必须为正值，否则每次探活都会立刻超时。");
        Assert.True(
            probeTimeout <= TimeSpan.FromSeconds(5),
            $"探活超时被放大到 {probeTimeout.TotalSeconds} 秒，会先于编排层 liveness 探针超时。");
    }

    /// <summary>
    /// 反射读取健康检查内部的探活超时常量。
    /// </summary>
    /// <returns>探活超时时长。</returns>
    private static TimeSpan ReadProbeTimeout()
    {
        var field = typeof(QdrantHealthCheck).GetField("ProbeTimeout", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);

        return Assert.IsType<TimeSpan>(field.GetValue(null));
    }

    /// <summary>
    /// 组装一个注册了指定向量库替身的服务提供者。
    /// </summary>
    /// <param name="store">向量库替身。</param>
    /// <returns>服务提供者。</returns>
    private static IServiceProvider BuildProvider(VectorStore store)
    {
        var services = new ServiceCollection();
        _ = services.AddSingleton(store);
        return services.BuildServiceProvider();
    }
}
