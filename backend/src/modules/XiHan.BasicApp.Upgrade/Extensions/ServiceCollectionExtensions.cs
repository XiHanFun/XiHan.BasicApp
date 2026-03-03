#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:7a5f6d1a-5a7d-4c3a-9b6c-9e5f0b0b8d6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:22:20
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Upgrade.Application.AppServices;
using XiHan.BasicApp.Upgrade.Application.AppServices.Implementations;
using XiHan.BasicApp.Upgrade.Infrastructure.Adapters;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Upgrade.Extensions;

/// <summary>
/// 升级服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加升级应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddUpgradeApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUpgradeAppService, UpgradeAppService>();
        return services;
    }

    /// <summary>
    /// 添加升级基础设施适配器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddUpgradeInfrastructureAdapters(this IServiceCollection services)
    {
        services.AddScoped<IUpgradeVersionStore, SqlSugarUpgradeVersionStore>();
        services.AddScoped<IUpgradeLockProvider, SqlSugarUpgradeLockProvider>();
        services.AddScoped<IUpgradeMigrationExecutor, SqlSugarUpgradeMigrationExecutor>();
        services.AddSingleton<IUpgradeTenantProvider, SqlSugarUpgradeTenantProvider>();
        return services;
    }
}
