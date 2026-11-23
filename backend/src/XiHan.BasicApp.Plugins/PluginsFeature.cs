// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Domain.Services;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.BasicApp.Plugins.Middleware;
using XiHan.BasicApp.Plugins.Queries;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Plugins.OutOfProcess;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块（Tier 3，可选）
/// </summary>
/// <remarks>
/// 平台上传插件包（<c>.xhplugin</c>），核对之后连同批准的授予安装；插件在框架的进程外插件宿主里运行（方案 <c>plans/插件方案.md</c>）
/// 启动时（结构升级之后、种子之前）按库把启用的插件加载到本节点，对齐完成之前节点不就绪；管理操作提交后本节点立即再对齐（<see cref="PluginReconciler"/>）
/// 宿主按 <c>BasicApp:Modules:Plugins:Enabled</c> 决定是否装配；关掉后权限目录与操作都不在
/// </remarks>
public sealed class PluginsFeature : IHostedFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "plugins",
        Package = "XiHan.BasicApp.Plugins",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(PluginsCapabilities.Plugins, PluginsCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(OutOfProcessPluginsFeature.Capability, 1),
        ],
        Contributes = new Contributions
        {
            Pipeline = [new PipelineContribution(typeof(PluginReconcileMiddleware).FullName!, PipelineStage.Transaction) { Before = [typeof(UnitOfWorkMiddleware).FullName!] }],
            Permissions = PluginsPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(PluginsFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(PluginsMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton(static services => PluginsOptions.FromConfiguration(services.GetRequiredService<IConfiguration>(), services.GetService<IHostEnvironment>()));
        context.Services.AddScoped<PluginInspector>();
        context.Services.AddScoped<PluginService>();
        context.Services.AddScoped<PluginQueries>();

        // 已加载插件的权限码进已登记的权限，回调权限码给插件主体，数据范围按批准的授予
        context.Services.AddSingleton<PluginRuntimeCatalog>();
        context.Services.AddSingleton<IRuntimePermissionSource>(static sp => sp.GetRequiredService<PluginRuntimeCatalog>());
        context.Services.AddSingleton<IPluginDataScopes>(static sp => sp.GetRequiredService<PluginRuntimeCatalog>());
        context.Services.AddSingleton<IPermissionGrantSource, PluginGrantSource>();

        // 插件宿主按隔离级别取键控服务；测试可以先登记替身，这里只在没登记时补上框架的进程外宿主
        context.Services.TryAddKeyedSingleton<IPluginHost>(PluginIsolation.OutOfProcess, static (services, _) => services.GetRequiredService<OutOfProcessPluginHost>());
        context.Services.AddSingleton<PluginPages>();
        context.Services.AddSingleton<IRuntimePageSource>(static sp => sp.GetRequiredService<PluginPages>());
        context.Services.AddSingleton<PluginCache>();
        context.Services.AddSingleton<PluginReconciler>();
        context.Services.AddScoped<PluginChanges>();
        context.Services.AddSingleton<IHealthContributor, PluginsHealthContributor>();
        context.Services.AddPermissionCatalog("plugins", PluginsPermissions.All);
        foreach (var permission in PluginsPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        // 对齐要落在工作单元提交之后：排在 Transaction 阶段开头，并显式排在框架工作单元中间件之前
        context.AddMiddleware<PluginReconcileMiddleware>(PipelineStage.Transaction, Placement.Beginning);
        context.Pipeline.Before<PluginReconcileMiddleware, UnitOfWorkMiddleware>();
        context.AddGeneratedOperations();
    }

    /// <inheritdoc />
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.GetRequiredService<PluginReconciler>().ReconcileAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
