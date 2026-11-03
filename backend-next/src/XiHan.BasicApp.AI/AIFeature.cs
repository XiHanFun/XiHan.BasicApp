// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.BasicApp.AI.Middleware;
using XiHan.BasicApp.AI.Queries;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.AI.Abstractions.Providers;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;
using ModelCapabilities = XiHan.Framework.AI.Abstractions.AICapabilities;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块（Tier 3，可选）
/// </summary>
/// <remarks>
/// 宿主按 <c>BasicApp:Modules:AI:Enabled</c> 装配（缺省开），同时装框架的 OpenAI 兼容 Provider；架在框架的 AI 子系统之上：
/// 模型提供商落库（方案 A1，照 4.x），以 <see cref="AiProviderCatalog"/> 取代框架按配置建的 Provider 目录，宿主启动时装载、提供商写入的调用提交之后重载
/// 提供商与提示词只在平台一侧（照 4.x）
/// </remarks>
public sealed class AIFeature : IHostedFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "ai",
        Package = "XiHan.BasicApp.AI",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(AICapabilities.AI, AICapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(ModelCapabilities.Chat, ModelCapabilities.Version),
            new CapabilityRequirement(ModelCapabilities.Embedding, ModelCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = AIPermissions.All.Select(static p => p.Code).ToList(),
            Pipeline = [new PipelineContribution(typeof(AiProviderCatalogMiddleware).FullName!, PipelineStage.Transaction)],
        },
        Entry = typeof(AIFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(AIMessages.Catalog);
        context.Services.AddGeneratedRepositories();

        // 落库的 Provider 目录取代框架按配置建的（框架以 TryAdd 登记，先后都以这一份为准）
        context.Services.AddSingleton<AiProviderCatalog>();
        context.Services.RemoveAll<IAIProviderCatalog>();
        context.Services.AddSingleton<IAIProviderCatalog>(static services => services.GetRequiredService<AiProviderCatalog>());
        context.Services.AddScoped<AiProviderChanges>();
        context.Services.AddScoped<AiProviderService>();
        context.Services.AddScoped<AiProviderQueries>();
        context.Services.AddScoped<AiPromptService>();
        context.Services.AddScoped<AiPromptQueries>();
        context.Services.AddCacheNamespace("ai", editable: false);

        context.Services.AddPermissionCatalog("ai", AIPermissions.All);
        // 字段安全：实体由响应类型生成，读、建、改的规则由 Access 的中间件统一套用（照 4.x 的登记范围）
        AIFieldSecurity.Register(context.Services);
        context.Services.AddPageCatalog("ai", AIPages.All);
        foreach (var permission in AIPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        // 排在 Transaction 阶段开头、工作单元之外：提供商写入的调用提交之后才重载目录
        context.AddMiddleware<AiProviderCatalogMiddleware>(PipelineStage.Transaction, Placement.Beginning);
        context.AddGeneratedOperations();
    }

    /// <inheritdoc />
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.GetRequiredService<AiProviderCatalog>().ReloadAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
