// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Middleware;
using XiHan.BasicApp.Identity.Queries;
using XiHan.BasicApp.Identity.Seeds;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Caching.Abstractions;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Authentication;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份能力包（Tier 1）：用户、账号安全、登录会话；签发访问令牌（框架 <c>Security.Jwt</c> 校验），会话门禁在 Authenticate 阶段排在 JWT 之后。
/// 装配显式：仓储、服务、种子、权限、操作、中间件都在这里登记，没有约定扫描。
/// </summary>
public sealed class IdentityFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "identity",
        Package = "XiHan.BasicApp.Identity",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(IdentityCapabilities.Identity, IdentityCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authentication, SecurityCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(CachingCapabilities.Distributed, CachingCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Pipeline = [new PipelineContribution(typeof(SessionGateMiddleware).FullName!, PipelineStage.Authenticate, After: [typeof(AuthenticationMiddleware).FullName!])],
            Permissions = IdentityPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(IdentityFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(IdentityMessages.Catalog);
        context.Services.AddSingleton(IdentityOptions.FromConfiguration(context.Configuration));
        // 仓储登记由 Persistence.Generators 从实体类生成
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton<AccessTokenIssuer>();
        context.Services.AddSingleton<SessionGate>();
        context.Services.AddSingleton<LoginAttempts>();
        context.Services.AddScoped<UserAccountService>();
        context.Services.AddScoped<UserQueries>();
        context.Services.AddSeeder<PlatformIdentitySeeder>();
        // 权限目录与页面登记：Access 汇总成权限目录、菜单种子与 auth.permissions 的菜单树
        context.Services.AddPermissionCatalog("identity", IdentityPermissions.All);
        context.Services.AddPageCatalog("identity", IdentityPages.All);
        // 权限码先登记，操作契约才能引用；未登记的权限码在装配时报错
        foreach (var permission in IdentityPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        // 同阶段的基准次序按归属（清单 ID）字符串序排，identity 会排在 security.jwt 之前：显式声明在认证中间件之后，先认出主体，再核对会话
        context.AddMiddleware<SessionGateMiddleware>(PipelineStage.Authenticate);
        context.Pipeline.After<SessionGateMiddleware, AuthenticationMiddleware>();
        // 操作登记由 Kernel.Generators 从 [Operation] 生成（含请求 / 响应 JSON Schema）
        context.AddGeneratedOperations();
    }
}
