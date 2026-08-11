// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Services;
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
}
