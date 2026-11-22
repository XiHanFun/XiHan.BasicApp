// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Domain.Services;
using XiHan.BasicApp.Plugins.Queries;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块（Tier 3，可选）
/// </summary>
/// <remarks>
/// 平台上传插件包（<c>.xhplugin</c>），核对之后连同批准的授予安装；插件在框架的进程外插件宿主里运行（方案 <c>plans/插件方案.md</c>）
/// 宿主按 <c>BasicApp:Modules:Plugins:Enabled</c> 决定是否装配；关掉后权限目录与操作都不在
/// </remarks>
public sealed class PluginsFeature : IFeature
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
        ],
        Contributes = new Contributions
        {
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
        context.Services.AddSingleton(PluginsOptions.FromConfiguration(context.Configuration));
        context.Services.AddScoped<PluginInspector>();
        context.Services.AddScoped<PluginService>();
        context.Services.AddScoped<PluginQueries>();
        context.Services.AddPermissionCatalog("plugins", PluginsPermissions.All);
        foreach (var permission in PluginsPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
