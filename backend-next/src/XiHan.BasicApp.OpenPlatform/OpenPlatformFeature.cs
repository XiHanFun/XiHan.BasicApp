// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.BasicApp.OpenPlatform.Infrastructure;
using XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;
using XiHan.BasicApp.OpenPlatform.Jobs;
using XiHan.BasicApp.OpenPlatform.Middleware;
using XiHan.BasicApp.OpenPlatform.Queries;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Caching.Abstractions;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Scheduling.Abstractions;
using XiHan.Framework.Scheduling.Abstractions.Schedules;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Authentication;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台能力包：签名开放接口与 OAuth / OIDC 服务端。
/// </summary>
/// <remarks>
/// Tier 2：签名开放接口（<c>open.*</c> 操作）。宿主在 <c>UseXiHan()</c> 之前装 <see cref="OpenApiRequestBuffer.UseOpenApiRequestBuffer"/> 留下请求体原文，
/// Authenticate 阶段的 <see cref="RequestSigningMiddleware"/> 验签、解密与加密响应，凭证经 Contracts 的 <see cref="Contracts.Security.IApiCredentialDirectory"/> 取（Identity 实现），
/// 开放接口日志经 <see cref="Contracts.Auditing.OpenApiLogEntry"/> 交给 Auditing 落库。OAuth 应用（平台库实体）：平台的「应用管理」与个人中心的「我的应用」；OAuth 2.0 / OIDC 服务端（宿主映射 <see cref="OAuthEndpoints.MapOAuthServer"/>，同意经 <c>oauth-consents.*</c>，令牌只用于身份）。
/// </remarks>
public sealed class OpenPlatformFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "openplatform",
        Package = "XiHan.BasicApp.OpenPlatform",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(OpenPlatformCapabilities.OpenPlatform, OpenPlatformCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authentication, SecurityCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(CachingCapabilities.Distributed, CachingCapabilities.Version),
            new CapabilityRequirement(SchedulingCapabilities.Jobs, SchedulingCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = OpenPlatformPermissions.All.Select(static p => p.Code).ToList(),
            Pipeline = [new PipelineContribution(typeof(RequestSigningMiddleware).FullName!, PipelineStage.Authenticate, After: [typeof(AuthenticationMiddleware).FullName!])],
        },
        Entry = typeof(OpenPlatformFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(OpenPlatformMessages.Catalog);
        context.Services.AddSingleton(static services => OpenPlatformOptions.FromConfiguration(services.GetRequiredService<IConfiguration>()));
        context.Services.AddScoped<OpenApiRequest>();
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<OAuthAppService>();
        context.Services.AddScoped<OAuthAppQueries>();
        context.Services.AddScoped<OAuthAuthorizer>();
        context.Services.AddScoped<OAuthTokenService>();
        context.Services.AddSingleton<OAuthSigningKeys>();
        context.Services.AddJob<OAuthCleanupJob>(OAuthCleanupJob.Name, JobSchedule.FromCron("30 3 * * *"));
        context.Services.AddPermissionCatalog("openplatform", OpenPlatformPermissions.All);
        context.Services.AddPageCatalog("openplatform", OpenPlatformPages.All);
        foreach (var permission in OpenPlatformPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddMiddleware<RequestSigningMiddleware>(PipelineStage.Authenticate);
        context.Pipeline.After<RequestSigningMiddleware, AuthenticationMiddleware>();
        context.AddGeneratedOperations();
    }
}
