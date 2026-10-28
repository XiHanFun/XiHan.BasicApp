// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块（Tier 3，可选）：登记工作台页面（不要求权限、排序 0），菜单种子据此生成登录后的落点。
/// 宿主按 <c>BasicApp:Modules:Workbench:Enabled</c> 决定是否装配；关掉后种子删掉工作台菜单，登录后落到第一个有权限的页面。
/// 小组件数据（<c>workbench.*</c> 操作与 <c>IWorkbenchWidgetProvider</c>）随 N2 加入。
/// </summary>
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
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Entry = typeof(WorkbenchFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddPageCatalog("workbench", WorkbenchPages.All);
    }
}
