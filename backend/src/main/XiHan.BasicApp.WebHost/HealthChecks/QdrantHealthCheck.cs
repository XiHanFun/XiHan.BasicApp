// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.VectorData;

namespace XiHan.BasicApp.WebHost.HealthChecks;

/// <summary>
/// 向量库（Qdrant）健康检查：解析 <see cref="VectorStore"/> 做一次轻量探活
/// </summary>
/// <remarks>
/// <para>
/// 向量库连接器在启动期只读配置、不建连（gRPC 通道惰性建立），因此服务没起也能正常启动，
/// 故障要等到第一次检索或摄取才暴露。这个检查把它提前到探针上。
/// </para>
/// <para>
/// 与 Redis 检查不同，向量库是无条件注册的，不存在「未启用」态，因此解析不到
/// <see cref="VectorStore"/> 属于装配错误，直接判定为不健康而不是放行。
/// </para>
/// </remarks>
public sealed class QdrantHealthCheck : IHealthCheck
{
    /// <summary>
    /// 探活超时；目标主机不可路由时 gRPC 可能长时间等待，必须设上限避免拖住探针。
    /// </summary>
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public QdrantHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var vectorStore = _serviceProvider.GetService<VectorStore>();
        if (vectorStore is null)
        {
            return HealthCheckResult.Unhealthy("向量库未注册");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        try
        {
            // 列集合是最轻的一次真实往返；只要能拿到枚举器的第一次推进结果就说明服务可达，
            // 集合本身存不存在与健康无关（尚未摄取任何文档时集合就是不存在的）。
            await foreach (var _ in vectorStore.ListCollectionNamesAsync(timeout.Token))
            {
                break;
            }

            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"向量库探活超时（{ProbeTimeout.TotalSeconds:0} 秒）");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("向量库连接失败", ex);
        }
    }
}
