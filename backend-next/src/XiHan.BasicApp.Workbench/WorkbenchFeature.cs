// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Workbench.Operations.Workbench;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块（Tier 3，可选）。
/// </summary>
/// <remarks>
/// 登记工作台页面（不要求权限、排序 0），菜单种子据此生成登录后的落点；
/// 汇总各包经 <c>IWorkbenchWidgetProvider</c> 登记的小组件，按调用方权限列出并取数（<c>workbench.*</c> 操作）。
/// 看板布局照 4.x 存在 Identity 的用户设置里（<c>workbench:widgets</c>），本包不建表。
/// 宿主按 <c>BasicApp:Modules:Workbench:Enabled</c> 决定是否装配；关掉后种子删掉工作台菜单，登录后落到第一个有权限的页面。
/// </remarks>
public sealed class WorkbenchFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "workbench",
        Package = "XiHan.BasicApp.Workbench",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(WorkbenchCapabilities.Workbench, WorkbenchCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Entry = typeof(WorkbenchFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(WorkbenchMessages.Catalog);
        context.Services.AddScoped<WidgetCatalog>();
        context.Services.AddPageCatalog("workbench", WorkbenchPages.All);
        context.AddGeneratedOperations();
    }
}
