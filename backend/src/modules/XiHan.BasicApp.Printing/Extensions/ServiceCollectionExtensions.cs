// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Services;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Printing.Infrastructure.Seeders.System;
using XiHan.Framework.Data.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Printing.Extensions;

/// <summary>
/// 打印模块服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加打印模块种子数据（权限 → 菜单 → 角色授权）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPrintingDataSeeders(this IServiceCollection services)
    {
        _ = services.AddDataSeeder<PrintingPermissionSeeder>();       // 500
        _ = services.AddDataSeeder<PrintingMenuSeeder>();             // 501
        _ = services.AddDataSeeder<PrintingRolePermissionSeeder>();   // 502
        return services;
    }

    /// <summary>
    /// 添加打印模块领域服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPrintingDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPrintTemplateDomainService, PrintTemplateDomainService>();
        return services;
    }

    /// <summary>
    /// 添加打印模块应用层内部服务（解析器与缓存失效器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPrintingApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPrintTemplateResolver, PrintTemplateResolver>();
        services.AddScoped<IPrintingCacheInvalidator, PrintingCacheInvalidator>();
        return services;
    }

    /// <summary>
    /// 添加打印数据源注册表（单例）并登记内置示例数据源；可重复调用，内置示例只登记一次。
    /// 业务模块在自己的 ConfigureServices 中经 <see cref="RegisterPrintDataSource"/> 追加数据源。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPrintingDataSources(this IServiceCollection services)
    {
        services.TryAddSingleton<IPrintDataSourceRegistry, PrintDataSourceRegistry>();
        var demoRegistered = services.Any(descriptor =>
            descriptor.ImplementationInstance is PrintDataSourceRegistration registration
            && registration.Definition.Code == BuiltInPrintDataSources.SystemPrintDemo.Code);
        if (!demoRegistered)
        {
            services.RegisterPrintDataSource(BuiltInPrintDataSources.SystemPrintDemo);
        }

        return services;
    }

    /// <summary>
    /// 登记一个打印数据源定义（应用启动完成后由注册表统一收纳；重复编码在收纳时抛出）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="definition">数据源定义</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection RegisterPrintDataSource(this IServiceCollection services, PrintDataSourceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        services.AddSingleton(new PrintDataSourceRegistration(definition));
        return services;
    }
}
