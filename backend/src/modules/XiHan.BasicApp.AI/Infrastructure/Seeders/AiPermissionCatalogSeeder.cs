// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders;

/// <summary>
/// AI 权限目录：模型服务、提示词、助手、知识库四个资源的资源型权限
/// </summary>
/// <remarks>
/// 全部是平台侧权限：模型提供方、提示词与助手由平台配置后给租户用，知识库是平台私有数据，租户里改不了也看不到管理入口。
/// </remarks>
public sealed class AiPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<AiPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    private static readonly ResourceSeed Ai = new(AiPermissionCodes.Resource, "AI 服务", "/api/ai", "模型提供方配置与对话服务接口", 200);

    private static readonly ResourceSeed Prompt = new(AiPromptPermissionCodes.Resource, "AI 提示词", "/api/ai-prompt", "提示词库接口", 210);

    private static readonly ResourceSeed Assistant = new(AiAssistantPermissionCodes.Resource, "AI 助手", "/api/ai-assistant", "助手配置接口", 220);

    private static readonly ResourceSeed Knowledge = new(KnowledgePermissionCodes.Resource, "知识库", "/api/knowledge-document", "RAG 知识库接口", 230);

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog + 20;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[AI]权限目录";

    /// <summary>
    /// 模块编码
    /// </summary>
    public override string ModuleCode => AiPermissionCodes.Module;

    /// <summary>
    /// 本模块的资源
    /// </summary>
    public override IReadOnlyList<ResourceSeed> Resources { get; } = [Ai, Prompt, Assistant, Knowledge];

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } =
    [
        .. PermissionSeed.Of(Ai, PermissionSide.Platform, 3100, OperationSeeds.Read, OperationSeeds.Create, OperationSeeds.Update, OperationSeeds.Delete, OperationSeeds.Execute),
        .. PermissionSeed.Of(Prompt, PermissionSide.Platform, 3110, OperationSeeds.Read, OperationSeeds.Create, OperationSeeds.Update, OperationSeeds.Delete),
        .. PermissionSeed.Of(Assistant, PermissionSide.Platform, 3120, OperationSeeds.Read, OperationSeeds.Create, OperationSeeds.Update, OperationSeeds.Delete),
        .. PermissionSeed.Of(Knowledge, PermissionSide.Platform, 3130, OperationSeeds.Read, OperationSeeds.Create, OperationSeeds.Update, OperationSeeds.Delete, OperationSeeds.Execute),
    ];
}
