// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.OpenPlatform.Infrastructure;
using XiHan.BasicApp.OpenPlatform.Middleware;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Caching.Abstractions;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Authentication;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台能力包（Tier 2）：签名开放接口（<c>open.*</c> 操作）。宿主在 <c>UseXiHan()</c> 之前装 <see cref="OpenApiRequestBuffer.UseOpenApiRequestBuffer"/> 留下请求体原文，
/// Authenticate 阶段的 <see cref="RequestSigningMiddleware"/> 验签、解密与加密响应，凭证经 Contracts 的 <see cref="Contracts.Security.IApiCredentialDirectory"/> 取（Identity 实现），
/// 开放接口日志经 <see cref="Contracts.Auditing.OpenApiLogEntry"/> 交给 Auditing 落库。
/// </summary>
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
            new CapabilityRequirement(SecurityCapabilities.Authentication, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(CachingCapabilities.Distributed, CachingCapabilities.Version),
        ],
        Contributes = new Contributions
        {
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
        context.AddMiddleware<RequestSigningMiddleware>(PipelineStage.Authenticate);
        context.Pipeline.After<RequestSigningMiddleware, AuthenticationMiddleware>();
        context.AddGeneratedOperations();
    }
}
