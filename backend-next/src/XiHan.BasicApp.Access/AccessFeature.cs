// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Access.Seeds;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权能力包（Tier 1）：角色与成员、角色授予（写框架 <c>PermissionGrantRecord</c>，判定仍由框架 <c>PermissionChecker</c> 完成）、权限目录（汇总各包登记）、菜单（种子汇总各包页面登记，<c>menus.*</c> 调展示、建目录与外链）、<c>auth.permissions</c>。
/// 给 Identity 提供 <see cref="IUserRoleResolver"/>：登录 / 刷新把用户的角色 ID 放进令牌。套餐门控：<see cref="EditionPermissionFilter"/> 接进框架的权限判定，<c>auth.permissions</c> 与权限目录同样按套餐白名单收窄。
/// Deny、授予与指派的有效期、用户直接授权与用户级数据范围由 <see cref="AccessPermissionFilter"/> 与 <see cref="DataScopeResolver"/> 接进判定；角色继承、委托、ABAC 随后续批次加入。
/// </summary>
public sealed class AccessFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "access",
        Package = "XiHan.BasicApp.Access",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(AccessCapabilities.Access, AccessCapabilities.Version)],
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
            Permissions = AccessPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(AccessFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(AccessMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton<PermissionCatalog>();
        context.Services.AddScoped<RoleService>();
        context.Services.AddScoped<MenuService>();
        context.Services.AddScoped<RoleGrants>();
        context.Services.AddScoped<UserGrants>();
        context.Services.AddScoped<PermissionRuleWriter>();
        context.Services.AddScoped<UserDataScopeService>();
        context.Services.AddScoped<EffectivePermissions>();
        context.Services.AddSingleton<PermissionRules>();
        context.Services.AddSingleton<RoleAssignments>();
        context.Services.AddSingleton<GrantEvaluator>();
        // 先按本包规则收窄（Deny、有效期、此刻算数的角色），再按套餐白名单收窄
        context.Services.AddSingleton<IPermissionFilter, AccessPermissionFilter>();
        context.Services.AddSingleton<IPermissionFilter, EditionPermissionFilter>();
        context.Services.AddScoped<RoleQueries>();
        context.Services.AddScoped<IUserRoleResolver, UserRoleResolver>();
        context.Services.AddScoped<RoleDataScopeService>();
        context.Services.AddScoped<IDataScopeResolver, DataScopeResolver>();
        context.Services.AddScoped<ITenantInitializer, TenantRoleInitializer>();
        context.Services.AddSeeder<PlatformRoleSeeder>();
        context.Services.AddSeeder<MenuSeeder>();
        context.Services.AddPermissionCatalog("access", AccessPermissions.All);
        context.Services.AddPageCatalog("access", AccessPages.All);
        foreach (var permission in AccessPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
