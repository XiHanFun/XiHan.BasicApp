#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:2b78b40b-8fe5-4e80-a688-c6deefda5a7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 12:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;
using XiHan.BasicApp.CodeGeneration.Seeders;
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
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddCodeGenerationDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<SysOperationSeeder>();        // Order = 18（操作字典，权限派生前置）
        services.AddDataSeeder<SysResourceSeeder>();        // Order = 19（资源，权限派生前置）
        services.AddDataSeeder<SysPermissionSeeder>();      // Order = 31（资源 × 操作 → code_gen:* 权限）
        services.AddDataSeeder<SysMenuSeeder>();            // Order = 32
        services.AddDataSeeder<SysRolePermissionSeeder>();  // Order = 33
        services.AddDataSeeder<SysCodeGenTemplateSeeder>(); // Order = 34
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
        // 模板渲染器（多引擎抽象；当前 Scriban 可用，Razor 占位）
        services.AddTransient<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddTransient<ITemplateRenderer, RazorTemplateRenderer>();
        services.AddTransient<ITemplateRendererResolver, TemplateRendererResolver>();

        // 类型映射与产物打包
        services.AddTransient<ITypeMappingProvider, DefaultTypeMappingProvider>();
        services.AddTransient<IGeneratedArtifactPackager, ZipArtifactPackager>();

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
