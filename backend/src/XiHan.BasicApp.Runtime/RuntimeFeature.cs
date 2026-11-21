// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.Middleware;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.BasicApp.Runtime.Security;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions.Resolution;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Runtime;

/// <summary>
/// 运行时基座能力包（Tier 0）
/// </summary>
/// <remarks>
/// 在 Transaction 阶段把当前主体交给 <see cref="Persistence.AuditorStamp"/>，供 SqlSugar 回调填操作人字段；把调用里的缓存失效推到工作单元之后（<see cref="Persistence.CacheInvalidation"/>）；提供 <c>basicapp.runtime@1</c> 供各能力包依赖
/// 维护模式与租户独立库核对的轮询随本包启动与停止：内核启动核对通过之后才开始，宿主起不来时它们没有开始过
/// </remarks>
public sealed class RuntimeFeature : IHostedFeature, IDisposable
{
    private ILogger? _sqlLogger;

    private CancellationTokenSource? _stopping;

    private Task? _polling;

    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "runtime",
        Package = "XiHan.BasicApp.Runtime",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Stable,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Pipeline =
            [
                new PipelineContribution(typeof(CacheInvalidationMiddleware).FullName!, PipelineStage.Transaction) { Before = [typeof(UnitOfWorkMiddleware).FullName!] },
                new PipelineContribution(typeof(AuditorStampMiddleware).FullName!, PipelineStage.Transaction),
                new PipelineContribution(typeof(TenantSchemaGateMiddleware).FullName!, PipelineStage.Tenant) { After = [typeof(TenantResolutionMiddleware).FullName!] },
            ],
            Permissions = RuntimePermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(RuntimeFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddSingleton(SeedOptions.FromConfiguration(context.Configuration));
        context.Services.AddSingleton<SeedRunner>();
        context.Services.AddSingleton(SchemaOptions.FromConfiguration(context.Configuration));
        context.Services.AddSingleton<SchemaUpgrader>();
        context.Services.AddSingleton<TenantSchemas>();
        // 实体类型清单在容器建好后第一次取用时才收集，那时各包都已登记完
        var registrations = context.Services.GetRepositoryRegistrations();
        context.Services.AddSingleton(_ => EntityTypeCatalog.From(registrations));
        // 维护模式与独立库核对的轮询在 StartAsync 里开始：内核启动核对通过之后才跑，不登记成宿主的后台服务
        context.Services.AddSingleton<MaintenanceModeState>();
        // 没有升级到当前结构版本的租户独立库：启动升级与重试记下，进入服务状态后核对一遍、之后复查，调用拦下、健康项降级
        context.Services.AddSingleton<SchemaHealth>();
        context.Services.AddSingleton<IHealthContributor, SchemaHealthContributor>();
        context.Services.AddSingleton<TenantSchemaWatch>();
        context.Services.AddRepository<SysVersion, Guid>();
        context.Services.AddRepository<SysMigrationHistory, Guid>();
        context.Services.AddSingleton<ITenantScopeRunner, TenantScopeRunner>();
        context.Services.AddScoped<DiffLogBuffer>();
        context.Services.AddScoped<SideRows>();
        context.Services.AddScoped<TenantSides>();
        context.Services.AddSecretProtection(context.Configuration);
        context.Services.AddMessageCatalog(RuntimeMessages.Catalog);
        context.Services.AddPermissionCatalog("runtime", RuntimePermissions.All);
        foreach (var permission in RuntimePermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        // 租户目录里的连接串是密文：框架的解析器之外包一层解密（框架用 TryAdd 登记，先后都以这里为准）
        context.Services.TryAddSingleton<TenantStoreShardResolver>();
        context.Services.RemoveAll<ITenantShardResolver>();
        context.Services.AddSingleton<ITenantShardResolver, ProtectedTenantShardResolver>();
        // 持久化 Provider 的工作单元中间件也在 Transaction 阶段；本包依赖 persistence.relational@1，按拓扑序排在它之后
        context.AddMiddleware<AuditorStampMiddleware>(PipelineStage.Transaction);
        // 失效要落在工作单元提交或回滚之后：排在 Transaction 阶段开头，并显式排在框架工作单元中间件之前
        context.AddMiddleware<CacheInvalidationMiddleware>(PipelineStage.Transaction, Placement.Beginning);
        context.Pipeline.Before<CacheInvalidationMiddleware, UnitOfWorkMiddleware>();
        // 租户解析之后才知道调用的租户
        context.AddMiddleware<TenantSchemaGateMiddleware>(PipelineStage.Tenant);
        context.Pipeline.After<TenantSchemaGateMiddleware, TenantResolutionMiddleware>();
    }

    /// <inheritdoc />
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        _sqlLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("XiHan.BasicApp.Sql");
        SqlDiagnostics.UseSharedLogger(_sqlLogger);
        _stopping = new CancellationTokenSource();
        _polling = Task.WhenAll(
            services.GetRequiredService<MaintenanceModeState>().RunAsync(_stopping.Token),
            services.GetRequiredService<TenantSchemaWatch>().RunAsync(_stopping.Token));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stopping is not null && _polling is not null)
        {
            await _stopping.CancelAsync().ConfigureAwait(false);
            await _polling.ConfigureAwait(false);
            _stopping.Dispose();
            _stopping = null;
            _polling = null;
        }

        if (_sqlLogger is { } logger)
        {
            SqlDiagnostics.ReleaseSharedLogger(logger);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping?.Dispose();
        _stopping = null;
    }
}
