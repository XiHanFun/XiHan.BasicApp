// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Auditing.Infrastructure;
using XiHan.BasicApp.Auditing.Jobs;
using XiHan.BasicApp.Auditing.Middleware;
using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Auditing.Abstractions.Pipeline;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Scheduling.Abstractions;
using XiHan.Framework.Security.Abstractions;
using FrameworkAuditing = XiHan.Framework.Auditing.Abstractions.AuditingCapabilities;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计能力包（Tier 2）。
/// </summary>
/// <remarks>
/// 七类按月分表的日志（访问、异常、登录、数据变更、权限变更、条件命中、开放接口）与操作日志的查询面，按链路聚合，保留清理作业。
/// 操作审计由框架 <c>Auditing.Database</c> 落库（ADR-0028），本包的 Observe 阶段中间件排在框架审计中间件里面：按权限的审计标记声明动作、补充细节，并写访问与异常日志、
/// 取走 Tier 1 各包在调用里登记的登录与权限变更日志。分表日志经有界队列后台批量落库，启动时开刷写循环，停止时把队列刷完。
/// </remarks>
public sealed class AuditingFeature : IHostedFeature, IDisposable
{
    private AuditLogWriter? _writer;
    private CancellationTokenSource? _stopping;
    private Task? _loop;

    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "auditing",
        Package = "XiHan.BasicApp.Auditing",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(AuditingCapabilities.Auditing, AuditingCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(FrameworkAuditing.Trail, FrameworkAuditing.Version),
            new CapabilityRequirement(SchedulingCapabilities.Jobs, SchedulingCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Pipeline = [new PipelineContribution(typeof(AuditLogMiddleware).FullName!, PipelineStage.Observe, After: [typeof(AuditMiddleware).FullName!])],
            Permissions = AuditingPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(AuditingFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(AuditingMessages.Catalog);
        var options = AuditLogOptions.FromConfiguration(context.Configuration);
        context.Services.AddSingleton(options);
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton<AuditLogWriter>();
        context.Services.AddSingleton<AuditedOperations>();
        context.Services.AddScoped<AccessLogQueries>();
        context.Services.AddScoped<ExceptionLogQueries>();
        context.Services.AddScoped<LoginLogQueries>();
        context.Services.AddScoped<DiffLogQueries>();
        context.Services.AddScoped<PermissionChangeLogQueries>();
        context.Services.AddScoped<ConditionHitQueries>();
        context.Services.AddScoped<OpenApiLogQueries>();
        context.Services.AddScoped<IWorkbenchWidgetProvider, AuditingWidgets>();
        context.Services.AddScoped<OperationLogQueries>();
        context.Services.AddScoped<TraceQueries>();
        context.Services.AddPermissionCatalog("auditing", AuditingPermissions.All);
        context.Services.AddPageCatalog("auditing", AuditingPages.All);
        foreach (var permission in AuditingPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        // 排在框架审计中间件里面：本中间件返回时补上的细节，框架随后写进操作审计
        context.AddMiddleware<AuditLogMiddleware>(PipelineStage.Observe);
        context.Pipeline.After<AuditLogMiddleware, AuditMiddleware>();
        context.Services.AddJob<AuditRetentionJob>(AuditRetentionJob.Name, options.RetentionSchedule);
        context.AddGeneratedOperations();
    }

    /// <inheritdoc />
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        _writer = services.GetRequiredService<AuditLogWriter>();
        _stopping = new CancellationTokenSource();
        _loop = _writer.RunAsync(_stopping.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_writer is null || _stopping is null || _loop is null)
        {
            return;
        }

        await _stopping.CancelAsync().ConfigureAwait(false);
        await _loop.ConfigureAwait(false);
        // 先停止接收，再把队列里剩余的刷完；之后到达的按丢弃计数
        _writer.Complete();
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _stopping.Dispose();
        _stopping = null;
        _loop = null;
        _writer = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping?.Dispose();
        _stopping = null;
    }
}
