#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:637ed309-a5cf-46ee-88e4-88baf409e54a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Saas.Infrastructure.Extensions;

/// <summary>
/// SaaS 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 SaaS 种子数据提供者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<SaasIdentitySeeder>();
        services.AddDataSeeder<SaasPermissionSeeder>();
        services.AddDataSeeder<SaasIdentityPermissionSeeder>();
        return services;
    }
}
