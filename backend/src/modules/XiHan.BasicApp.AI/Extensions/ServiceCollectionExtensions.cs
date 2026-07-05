#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:a11c0de0-0002-4a10-9a00-00000000ai02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.DomainServices.Implementations;
using XiHan.BasicApp.AI.Infrastructure.Configuration;
using XiHan.BasicApp.AI.Infrastructure.Security;
using XiHan.BasicApp.AI.Infrastructure.Seeders.System;
using XiHan.BasicApp.AI.Infrastructure.Skills;
using XiHan.Framework.AI.Abstractions.Configuration;
using XiHan.Framework.AI.Abstractions.Skills;
using XiHan.Framework.AI.Extensions.DependencyInjection;
using XiHan.Framework.Data.Extensions.DependencyInjection;

namespace XiHan.BasicApp.AI.Extensions;

/// <summary>
/// AI 模块服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 AI 模块种子数据提供者
    /// </summary>
    /// <remarks>
    /// AI 种子独立使用 Order 200+ 段，晚于 Saas（10-37）与代码生成（100-105），互不交叠。
    /// 链内顺序：操作字典 → 资源 → 权限(资源×操作) → 菜单 → 角色授权。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddAIDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<SysOperationSeeder>();       // Order = 200（操作字典，权限派生前置）
        services.AddDataSeeder<SysResourceSeeder>();       // Order = 201（资源，权限派生前置）
        services.AddDataSeeder<SysPermissionSeeder>();     // Order = 202（资源 × 操作 → ai:* 权限）
        services.AddDataSeeder<SysMenuSeeder>();           // Order = 203（菜单，建即绑 ai:read）
        services.AddDataSeeder<SysRolePermissionSeeder>(); // Order = 204（仅授超管）
        return services;
    }

    /// <summary>
    /// 添加 AI 领域服务与密钥保护器
    /// </summary>
    /// <remarks>
    /// 领域服务接口未携带 DI 标记接口（IScopedDependency/IDomainService），框架不会按约定自动注册，
    /// 故在此显式登记为 Scoped。密钥保护器为无状态、依赖单例 IDataProtectionProvider，注册为 Singleton。
    /// 仓储（SaasRepository → IScopedDependency）与应用/查询服务（IApplicationService → 瞬时）由框架约定自动注册。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddAIDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAiProviderDomainService, AiProviderDomainService>();
        services.AddSingleton<IAiProviderSecretProtector, DataProtectionAiProviderSecretProtector>();
        return services;
    }

    /// <summary>
    /// 覆盖框架默认 provider 配置源为 DB 存储实现
    /// </summary>
    /// <remarks>
    /// 框架 <c>AddXiHanAI</c> 已 <c>TryAddSingleton</c> 默认 Options 配置源（Core 依赖 XiHanAIModule 触发），
    /// 故此处必须用 <c>Replace</c> 覆盖，<c>TryAdd</c> 会被静默忽略导致 DB 配置永不生效。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddAIConfigStore(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IAiProviderConfigStore, SaasAiProviderConfigStore>());
        return services;
    }

    /// <summary>
    /// 添加知识库（RAG）种子数据提供者
    /// </summary>
    /// <remarks>
    /// RAG 种子用 Order 205+ 段（AI provider 段 200-204 之后）；操作字典复用 AI 段的 <see cref="SysOperationSeeder"/>(200)。
    /// 链内顺序：资源 → 权限(资源×操作) → 菜单 → 角色授权。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRAGDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<KnowledgeResourceSeeder>();       // Order = 205
        services.AddDataSeeder<KnowledgePermissionSeeder>();     // Order = 206
        services.AddDataSeeder<KnowledgeMenuSeeder>();           // Order = 207（建即绑 knowledge_base:read）
        services.AddDataSeeder<KnowledgeRolePermissionSeeder>(); // Order = 208（仅授超管）
        return services;
    }

    /// <summary>
    /// 添加知识库（RAG）领域服务
    /// </summary>
    /// <remarks>领域服务无 DI 标记接口，显式登记 Scoped；仓储/应用服务由框架约定自动注册。</remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRAGDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IKnowledgeDocumentDomainService, KnowledgeDocumentDomainService>();
        return services;
    }

    /// <summary>
    /// 添加 RAG 能力与向量库连接器
    /// </summary>
    /// <remarks>
    /// 框架 <c>AddXiHanRAG</c> 给切片/摄取/检索/增强默认实现（不含具体 VectorStore）；此处登记 Qdrant 连接器，
    /// 连接参数取 <see cref="XiHanRagOptions"/>（appsettings 的 XiHan:AI:Rag 节，属部署级基础设施配置）。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns></returns>
    public static IServiceCollection AddRAG(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // 框架 RAG 默认实现（切片/摄取/检索/增强）
        services.AddXiHanRAG();

        // RAG 配置（检索 topK 等）
        services.AddOptions<XiHanRagOptions>().BindConfiguration(XiHanRagOptions.SectionName);

        // Qdrant 向量库连接器（gRPC；启动期读连接参数）
        var ragOptions = configuration.GetSection(XiHanRagOptions.SectionName).Get<XiHanRagOptions>() ?? new XiHanRagOptions();
        services.AddQdrantVectorStore(ragOptions.QdrantHost, ragOptions.QdrantPort, ragOptions.QdrantHttps, ragOptions.QdrantApiKey);

        return services;
    }

    /// <summary>
    /// 添加应用层 AI 技能
    /// </summary>
    /// <remarks>
    /// 技能注册为 <see cref="IAiSkill"/>,框架 <c>DefaultAiSkillRegistry</c> 构造时收纳,进而暴露为对话工具 / MCP tools。
    /// 单例(依赖单例 IKnowledgeRetriever)。新增技能在此追加一行。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddAISkills(this IServiceCollection services)
    {
        services.AddSingleton<IAiSkill, KnowledgeRetrieveSkill>();
        return services;
    }
}
