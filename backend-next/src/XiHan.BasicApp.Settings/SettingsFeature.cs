// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Queries;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置能力包（Tier 2）。
/// </summary>
/// <remarks>
/// 系统参数（平台值与租户覆盖、加密值）、字典与树形字典项、业务编号规则与永久发号记录。
/// 全部是平台库实体，按 <c>OwnerTenantId</c> 分平台与租户两侧：读共享、写只本侧。对外经 Contracts 的 <see cref="IConfigValueReader"/> 与 <see cref="INumberGenerator"/>。
/// </remarks>
public sealed class SettingsFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "settings",
        Package = "XiHan.BasicApp.Settings",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(SettingsCapabilities.Settings, SettingsCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = SettingsPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(SettingsFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(SettingsMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<ConfigValues>();
        context.Services.AddScoped<IConfigValueReader, ConfigValueReader>();
        context.Services.AddScoped<DictService>();
        context.Services.AddScoped<NumberingRuleService>();
        context.Services.AddScoped<INumberGenerator, NumberGenerator>();
        context.Services.AddScoped<ConfigQueries>();
        context.Services.AddScoped<DictQueries>();
        context.Services.AddScoped<NumberingRuleQueries>();
        context.Services.AddScoped<NumberingAllocationQueries>();
        context.Services.AddPermissionCatalog("settings", SettingsPermissions.All);
        // 字段安全：实体由响应类型生成，读、建、改的规则由 Access 的中间件统一套用
        SettingsFieldSecurity.Register(context.Services);
        context.Services.AddPageCatalog("settings", SettingsPages.All);
        foreach (var permission in SettingsPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
