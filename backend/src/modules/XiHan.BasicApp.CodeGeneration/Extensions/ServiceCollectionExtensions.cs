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
        services.AddDataSeeder<SysResourceSeeder>();       // Order = 20
        services.AddDataSeeder<SysMenuSeeder>();           // Order = 21
        services.AddDataSeeder<SysPermissionSeeder>();     // Order = 22
        services.AddDataSeeder<SysRolePermissionSeeder>(); // Order = 23
        return services;
    }
}
