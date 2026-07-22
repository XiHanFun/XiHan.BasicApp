// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace XiHan.BasicApp.WebHost.HealthChecks;

/// <summary>
/// Redis 健康检查：解析可选的 <see cref="IConnectionMultiplexer"/> 做 PING 探活
/// </summary>
/// <remarks>
/// 框架仅在启用 Redis 时注册 <see cref="IConnectionMultiplexer"/>（缓存配置 IsEnabled+Configuration）。
/// 未启用 Redis（进程内回退）时无该单例，视为健康并标注，避免误判为故障。
/// </remarks>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RedisHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var multiplexer = _serviceProvider.GetService<IConnectionMultiplexer>();
        if (multiplexer is null)
        {
            return HealthCheckResult.Healthy("Redis 未启用（进程内回退）");
        }

        try
        {
            _ = await multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis 连接失败", ex);
        }
    }
}
