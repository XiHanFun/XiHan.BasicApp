// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.BasicApp.AI.Middleware;
using XiHan.BasicApp.AI.Queries;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Assistants;
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
using XiHan.Framework.AI.OpenAI;
using XiHan.Framework.Kernel.Security;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块（Tier 3，可选）
/// </summary>
/// <remarks>
/// 宿主按 <c>BasicApp:Modules:AI:Enabled</c> 装配（缺省开），同时装框架的 OpenAI 兼容 Provider；架在框架的 AI 子系统之上：
/// 模型提供商落库（方案 A1，照 4.x），以 <see cref="AiProviderCatalog"/> 取代框架按配置建的 Provider 目录，宿主启动时装载、提供商写入的调用提交之后重载
/// 提供商、提示词与助手的配置只在平台一侧（照 4.x）；知识文档平台与租户各自维护（方案 A2），向量库由宿主按配置装，没配即知识库不可用
/// 助手经 Contracts 的 <see cref="IChatAssistants"/> 给聊天应答，聊天不引用本包
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
        // 提供商地址是平台管理员填的，模型调用过出站地址策略（框架自己的端点来自配置，不过这一道）
        context.Services.AddHttpClient(OpenAIFeature.HttpClientName).UseSocketsHttpHandler(static (handler, services) => services.GetRequiredService<OutboundAddressPolicy>().Apply(handler));
        context.Services.AddScoped<AiProviderChanges>();
        context.Services.AddScoped<AiProviderService>();
        context.Services.AddScoped<AiProviderQueries>();
        context.Services.AddScoped<AiPromptService>();
        context.Services.AddScoped<AiPromptQueries>();
        context.Services.AddScoped<AiAssistantService>();
        context.Services.AddScoped<AiAssistantQueries>();
        // 聊天的助手应答：聊天不引用 AI，经 Contracts 的接口拿到它，AI 关掉时没有实现
        context.Services.AddScoped<IChatAssistants, AiChatAssistants>();
        context.Services.AddScoped<KnowledgeDocumentService>();
        context.Services.AddScoped<KnowledgeDocumentQueries>();
        context.Services.AddScoped<AiProviderReferences>();
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
