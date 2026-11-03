// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Printing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Printing.Infrastructure;
using XiHan.BasicApp.Printing.Queries;
using XiHan.BasicApp.Printing.Seeds;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块（Tier 3，可选）
/// </summary>
/// <remarks>
/// 打印模板（平台库实体，平台全局模板与租户私有模板）的维护与按编码解析，打印数据源的注册表与目录；设计、样例数据、预览与打印都在浏览器里完成，本包没有作业与普通端点（照 4.x）
/// 数据源由各包经 Contracts 的 <see cref="PrintDataSourceServiceCollectionExtensions.AddPrintDataSource"/> 登记，注册表的构造登记为启动核对：编码重复或字段契约不合法即启动失败
/// 内置数据源 <c>system.print-demo</c> 总会登记，五个演示数据源只在演示开关打开时登记（4.x 同）
/// 宿主按 <c>BasicApp:Modules:Printing:Enabled</c> 决定是否装配；关掉后权限目录与页面登记都不在，菜单种子删掉打印模板页，前端模块随菜单不再出现
/// </remarks>
public sealed class PrintingFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "printing",
        Package = "XiHan.BasicApp.Printing",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(PrintingCapabilities.Printing, PrintingCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = PrintingPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(PrintingFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(PrintingMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton<PrintDataSourceRegistry>();
        context.Services.AddPrintDataSource(BuiltInPrintDataSources.SystemPrintDemo);
        if (SeedOptions.FromConfiguration(context.Configuration).EnableDemoData)
        {
            foreach (var definition in PrintingDemoCatalog.LoadDataSources())
            {
                context.Services.AddPrintDataSource(definition);
            }
        }

        // 构造一次注册表：编码重复或字段契约不合法在启动阶段就失败，而不是等到第一个请求（照 4.x 的应用初始化）
        context.AddStartupCheck(static services => _ = services.GetRequiredService<PrintDataSourceRegistry>());
        context.Services.AddScoped<PrintTemplateService>();
        context.Services.AddScoped<PrintTemplateResolver>();
        context.Services.AddScoped<PrintTemplateQueries>();
        context.Services.AddSingleton<IPolicy, PrintDataSourceCatalogPolicy>();
        context.Services.AddCacheNamespace("printing", editable: true);
        context.Services.AddPermissionCatalog("printing", PrintingPermissions.All);
        // 字段安全：实体由响应类型生成，读、建、改的规则由 Access 的中间件统一套用
        PrintingFieldSecurity.Register(context.Services);
        context.Services.AddPageCatalog("printing", PrintingPages.All);
        foreach (var permission in PrintingPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.Services.AddSeeder<DemoPrintTemplateSeeder>();
        context.AddGeneratedOperations();
    }
}
