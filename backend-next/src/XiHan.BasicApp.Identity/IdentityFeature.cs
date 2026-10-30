// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Infrastructure.External;
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
/// 身份能力包：用户、账号安全与登录会话。
/// </summary>
/// <remarks>
/// 属于 Tier 1。签发访问令牌（框架 <c>Security.Jwt</c> 校验），会话门禁在 Authenticate 阶段排在 JWT 之后。
/// 装配显式：仓储、服务、种子、权限、操作、中间件都在这里登记，没有约定扫描。
/// </remarks>
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
        context.Services.TryAddSingleton<ICaptchaCodeSource, RandomCaptchaCodes>();
        context.Services.AddSingleton<Captchas>();
        context.Services.AddSingleton<LoginThrottle>();
        context.Services.AddSingleton<TwoFactorTickets>();
        context.Services.AddSingleton<VerificationCodes>();
        context.Services.AddScoped<ContactCodes>();
        context.Services.AddSingleton<TotpEnrollments>();
        context.Services.AddScoped<LoginSessions>();
        context.Services.AddScoped<UserAccountService>();
        context.Services.AddScoped<UserQueries>();
        context.Services.AddScoped<SessionQueries>();
        // 绕过内核管道的实时 Hub 用同一份会话判定
        context.Services.AddSingleton<ISessionStatus, SessionStatus>();
        // 通知找人：一侧的全部账号与指定用户（指定用户也是其他来源的最后一道筛）
        context.Services.AddScoped<IRecipientSource, AllUsersRecipientSource>();
        context.Services.AddScoped<IRecipientSource, UserRecipientSource>();
        context.Services.AddScoped<IUserDirectory, UserDirectory>();
        // 通知偏好：Notification 发布与扇出时按它过滤接收人
        context.Services.AddScoped<INotificationPreferences, NotificationPreferences>();
        context.Services.AddScoped<ApiCredentialService>();
        context.Services.AddScoped<ProfileAvatars>();
        context.Services.AddSingleton(ExternalLoginOptions.FromConfiguration(context.Configuration));
        context.Services.AddHttpClient(ExternalLoginFlow.HttpClientName, static client =>
        {
            // GitHub 不带 User-Agent 即 403（4.x 用 ASP.NET 认证处理器的缺省 UA）
            client.DefaultRequestHeaders.UserAgent.ParseAdd("XiHan.BasicApp");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        foreach (var provider in (IExternalLoginProvider[])[new GoogleProvider(), new GitHubProvider(), new GiteeProvider(), new QqProvider(), new WeChatProvider(), new WeComProvider(), new LarkProvider(), new DingTalkProvider()])
        {
            context.Services.AddSingleton(provider);
        }

        context.Services.AddSingleton<ExternalLoginProviders>();
        context.Services.AddSingleton<IApiCredentialDirectory, ApiCredentialDirectory>();
        context.Services.AddSeeder<PlatformIdentitySeeder>();
        // 权限目录与页面登记：Access 汇总成权限目录、菜单种子与 auth.permissions 的菜单树
        context.Services.AddPermissionCatalog("identity", IdentityPermissions.All);
        // 缓存管理只开放登记过的命名空间；验证码、会话与两步验证的缓存，属鉴权数据，只许删、不许改写
        context.Services.AddCacheNamespace("identity", editable: false);
        context.Services.AddPageCatalog("identity", IdentityPages.All);
        context.Services.AddFieldSecurityCatalog("identity", IdentityFieldSecurity.All);
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
