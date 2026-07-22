// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Inference;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders.System;
using XiHan.Framework.Data.Extensions.DependencyInjection;

namespace XiHan.BasicApp.CodeGeneration.Extensions;

/// <summary>
/// 代码生成服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 CodeGeneration 种子数据提供者
    /// </summary>
    /// <remarks>
    /// 代码生成种子独立使用 Order 100+ 段，整体晚于 Saas 全部种子执行，与 Saas 互不交叠、互不影响。
    /// 链内顺序：操作字典 → 资源 → 权限(资源×操作) → 菜单 → 角色授权 → 模板。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddCodeGenerationDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<SysOperationSeeder>();        // Order = 100（操作字典，权限派生前置）
        services.AddDataSeeder<SysResourceSeeder>();        // Order = 101（资源，权限派生前置）
        services.AddDataSeeder<SysPermissionSeeder>();      // Order = 102（资源 × 操作 → code_gen:* 权限）
        services.AddDataSeeder<SysMenuSeeder>();            // Order = 103
        services.AddDataSeeder<SysRolePermissionSeeder>();  // Order = 104
        services.AddDataSeeder<SysCodeGenTemplateSeeder>(); // Order = 105
        return services;
    }

    /// <summary>
    /// 添加代码生成引擎（渲染器/解析器/类型映射/打包/扫描/编排）
    /// </summary>
    /// <remarks>
    /// 仓储（SaasRepository → IScopedDependency）与应用服务（IApplicationService → 瞬时）由框架约定自动注册，此处无需登记。
    /// 引擎组件无 DI 标记接口，统一在此显式注册。多渲染器以 IEnumerable&lt;ITemplateRenderer&gt; 注入解析器。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddCodeGenerationEngine(this IServiceCollection services)
    {
        // 模板渲染器（多引擎抽象；当前仅 Scriban 落地，Razor 已移除）
        services.AddTransient<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddTransient<ITemplateRendererResolver, TemplateRendererResolver>();

        // 落盘选项 + 类型映射 + 产物打包 + 受控落盘写入器
        services.AddOptions<CodeGenerationOptions>().BindConfiguration(CodeGenerationOptions.SectionName);
        services.AddTransient<ITypeMappingProvider, DefaultTypeMappingProvider>();
        services.AddTransient<IGeneratedArtifactPackager, ZipArtifactPackager>();
        services.AddTransient<IGeneratedArtifactWriter, FileSystemArtifactWriter>();

        // 实体元数据目录（反射一次、进程内缓存）+ 表配置推断引擎
        services.AddSingleton<IEntityMetadataCatalog, EntityMetadataCatalog>();
        services.AddTransient<ITableConfigInferrer, TableConfigInferenceEngine>();

        // DbFirst 元数据扫描与生成编排（依赖 Scoped 仓储/元数据提供器）
        services.AddScoped<IDatabaseSchemaImporter, DatabaseSchemaImporter>();
        services.AddScoped<ICodeGenerationEngine, CodeGenerationEngine>();

        return services;
    }

    /// <summary>
    /// 添加代码生成领域服务
    /// </summary>
    /// <remarks>
    /// 领域服务接口未携带 DI 标记接口（IScopedDependency/IDomainService），框架不会按约定自动注册，
    /// 故在此显式登记为 Scoped（与仓储/工作单元生命周期一致）。新增领域服务请同步在此登记。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddCodeGenerationDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ICodeGenDataSourceDomainService, CodeGenDataSourceDomainService>();
        services.AddScoped<ICodeGenTableDomainService, CodeGenTableDomainService>();
        services.AddScoped<ICodeGenTableColumnDomainService, CodeGenTableColumnDomainService>();
        services.AddScoped<ICodeGenTemplateDomainService, CodeGenTemplateDomainService>();

        return services;
    }
}
