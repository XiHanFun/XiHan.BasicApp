// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.BasicApp.Organization.Queries;
using XiHan.BasicApp.Organization.Seeds;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Tenancy;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织能力包（Tier 1）：部门树（闭包表）、岗位、用户部门归属；全部是租户库实体（<c>IMultiTenant</c>），只在租户侧管理，平台主体的菜单与操作里都没有。
/// 数据范围「本部门及子部门」由 Access 后续批次用闭包表联查。
/// </summary>
public sealed class OrganizationFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "organization",
        Package = "XiHan.BasicApp.Organization",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(OrganizationCapabilities.Organization, OrganizationCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(IdentityCapabilities.Identity, IdentityCapabilities.Version),
            new CapabilityRequirement(TenancyCapabilities.Tenancy, TenancyCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = OrganizationPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(OrganizationFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(OrganizationMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<DepartmentService>();
        context.Services.AddScoped<MembershipService>();
        context.Services.AddScoped<PositionQueries>();
        context.Services.AddScoped<MembershipQueries>();
        context.Services.AddSeeder<DemoOrganizationSeeder>();
        context.Services.AddPermissionCatalog("organization", OrganizationPermissions.All);
        context.Services.AddPageCatalog("organization", OrganizationPages.All);
        foreach (var permission in OrganizationPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
