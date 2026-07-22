// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Text.Json.Serialization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Tasks.BackgroundServices;

namespace XiHan.BasicApp.Saas.Infrastructure.Tenancy;

/// <summary>
/// 租户到期自动停用后台服务：周期扫描 <see cref="SysTenant.ExpirationTime"/> 已过期的正常租户，标记为 <see cref="TenantStatus.Expired"/>。
/// </summary>
/// <remarks>
/// 继承 <see cref="XiHanBackgroundServiceBase{T}"/>（<c>IBackgroundWorker : ISingletonDependency</c>）、类名以 <c>HostedService</c> 结尾，
/// 由约定注册自动暴露为 <c>IHostedService</c> 托管，切勿再手动 <c>AddHostedService</c>（否则重复托管）。
/// <para>
/// 扫描是跨租户的：后台无租户上下文（平台态），全局租户过滤器本就放行全部，此处再显式 <c>ClearFilter&lt;IMultiTenantEntity&gt;</c> 表明意图（软删过滤仍生效）。
/// 只把仍处 <see cref="TenantStatus.Normal"/> 且已过期的置 <see cref="TenantStatus.Expired"/>——置后不再匹配 Normal，是终止条件，天然幂等，多实例并发也安全。
/// 停用后失效版本门控与授权快照缓存，避免过期租户凭旧快照继续被放行；<see cref="TenantStatus.Expired"/> 的租户在“租户是否可用”闸门（仅 Normal 可用）被拦下。
/// </para>
/// </remarks>
public sealed class TenantExpirationHostedService : XiHanBackgroundServiceBase<TenantExpirationHostedService>
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantExpirationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<XiHanBackgroundServiceOptions> options,
        ILogger<TenantExpirationHostedService> logger)
        : base(logger, options, BuildConfig(options))
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<IBackgroundTaskItem>> FetchWorkItemsAsync(int maxCount, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>().GetCurrentClient();
        var now = DateTimeOffset.UtcNow;

        var hasOverdue = await db.Queryable<SysTenant>()
            .ClearFilter<IMultiTenantEntity>()
            .Where(tenant => tenant.TenantStatus == TenantStatus.Normal
                && tenant.ExpirationTime != null
                && tenant.ExpirationTime < now)
            .AnyAsync();

        // 有过期租户才返回一个扫描触发项交给 ProcessItem 处理；没有则返回空，由基类进入空闲等待（即扫描周期）。
        return hasOverdue ? [new TenantExpirationTrigger { CreatedAt = now }] : [];
    }

    /// <inheritdoc />
    protected override async Task ProcessItemAsync(IBackgroundTaskItem item, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>().GetCurrentClient();
        var invalidator = scope.ServiceProvider.GetRequiredService<ISaasCacheInvalidator>();
        var now = DateTimeOffset.UtcNow;

        // 取整行实体（不做 DateTimeOffset 标量投影），跨租户
        var overdue = await db.Queryable<SysTenant>()
            .ClearFilter<IMultiTenantEntity>()
            .Where(tenant => tenant.TenantStatus == TenantStatus.Normal
                && tenant.ExpirationTime != null
                && tenant.ExpirationTime < now)
            .ToListAsync();

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var tenant in overdue)
        {
            tenant.TenantStatus = TenantStatus.Expired;
        }

        // 只更新状态列、按主键批量更新（幂等；全局过滤器不注入 Updateable，平台态下亦无租户约束）
        await db.Updateable(overdue).UpdateColumns(tenant => new { tenant.TenantStatus }).ExecuteCommandAsync();

        // 版本门控 + 授权快照是鉴权热路径缓存，租户停用后须失效
        await invalidator.InvalidateEditionGateAsync(cancellationToken);
        await invalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        Logger.LogInformation("租户到期自动停用：{Count} 个租户标记为 Expired（{Codes}）",
            overdue.Count, string.Join(", ", overdue.Select(tenant => tenant.TenantCode)));
    }

    /// <summary>
    /// 单实例扫描（限并发 1，避免同进程内重复处理）；每 60s 扫描一次。
    /// </summary>
    private static IDynamicServiceConfig BuildConfig(IOptions<XiHanBackgroundServiceOptions> options)
    {
        var config = new DynamicServiceConfig(options);
        config.UpdateMaxConcurrentTasks(1);
        config.UpdateIdleDelay(60_000);
        return config;
    }

    /// <summary>
    /// 扫描触发项：仅作为“有过期租户待处理”的信号，本身不携带数据。
    /// </summary>
    private sealed class TenantExpirationTrigger : IBackgroundTaskItem
    {
        /// <inheritdoc />
        public string TaskId => "tenant-expiration-scan";

        /// <inheritdoc />
        [JsonIgnore]
        public object? Data => null;

        /// <inheritdoc />
        public DateTimeOffset CreatedAt { get; init; }

        /// <inheritdoc />
        public int RetryCount { get; set; }
    }
}
