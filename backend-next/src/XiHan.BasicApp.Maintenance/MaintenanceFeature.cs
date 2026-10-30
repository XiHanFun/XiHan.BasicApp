// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Maintenance.Queries;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Scheduling.Abstractions;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维能力包的特性。
/// </summary>
/// <remarks>
/// 作业的管理面架在框架调度之上（宿主选 <c>Scheduling.Database</c>，多实例共用作业表），本包没有自己的调度表；
/// 版本台账与升级记录读 Runtime 升级执行器写的两张表。
/// </remarks>
public sealed class MaintenanceFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "maintenance",
        Package = "XiHan.BasicApp.Maintenance",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(MaintenanceCapabilities.Maintenance, MaintenanceCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(SchedulingCapabilities.Jobs, SchedulingCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = MaintenancePermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(MaintenanceFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(MaintenanceMessages.Catalog);
        context.Services.AddScoped<SchemaQueries>();
        context.Services.AddPermissionCatalog("maintenance", MaintenancePermissions.All);
        context.Services.AddPageCatalog("maintenance", MaintenancePages.All);
        foreach (var permission in MaintenancePermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
