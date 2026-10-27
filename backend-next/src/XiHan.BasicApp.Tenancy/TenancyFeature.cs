// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.BasicApp.Tenancy.Queries;
using XiHan.BasicApp.Tenancy.Seeds;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户能力包（Tier 1）：<see cref="Domain.Entities.SysTenant"/> 业务实体、开通 / 启停 / 删除时同步框架租户目录、租户管理员初始化、演示租户种子。
/// 套餐、成员、所有权转移、分库开通随后续批次加入；租户目录与解析本身由框架 <c>MultiTenancy.Database</c> 提供。
/// </summary>
public sealed class TenancyFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "tenancy",
        Package = "XiHan.BasicApp.Tenancy",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(TenancyCapabilities.Tenancy, TenancyCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(IdentityCapabilities.Identity, IdentityCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = TenancyPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(TenancyFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(TenancyMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<TenantService>();
        context.Services.AddScoped<TenantQueries>();
        context.Services.AddSeeder<DemoTenantSeeder>();
        context.Services.AddPermissionCatalog("tenancy", TenancyPermissions.All);
        context.Services.AddPageCatalog("tenancy", TenancyPages.All);
        foreach (var permission in TenancyPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
