#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:6b2b3c4d-5e6f-7890-abcd-ef12345678ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;
using XiHan.BasicApp.Rbac.Domain.Services;
using XiHan.BasicApp.Rbac.Domain.Services.Implementations;
using XiHan.BasicApp.Rbac.Seeders;
using XiHan.Framework.Data.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Rbac.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 RBAC 仓储
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacRepositories(this IServiceCollection services)
    {
        // 聚合根仓储
        services.AddScoped<ISysUserRepository, SysUserRepository>();
        services.AddScoped<ISysRoleRepository, SysRoleRepository>();
        services.AddScoped<ISysPermissionRepository, SysPermissionRepository>();
        services.AddScoped<ISysTenantRepository, SysTenantRepository>();
        services.AddScoped<ISysMenuRepository, SysMenuRepository>();
        services.AddScoped<ISysDepartmentRepository, SysDepartmentRepository>();
        services.AddScoped<ISysConfigRepository, SysConfigRepository>();
        services.AddScoped<ISysDictRepository, SysDictRepository>();

        return services;
    }

    /// <summary>
    /// 添加 RBAC 领域服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacDomainServices(this IServiceCollection services)
    {
        // Domain Services
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<IPermissionAuthorizationService, PermissionAuthorizationService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();
        services.AddScoped<IMenuNavigationService, MenuNavigationService>();
        services.AddScoped<IDepartmentHierarchyService, DepartmentHierarchyService>();

        return services;
    }

    /// <summary>
    /// 添加 RBAC 应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacApplicationServices(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// 添加 RBAC 种子数据提供者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacDataSeeders(this IServiceCollection services)
    {
        // 按执行顺序注册
        services.AddDataSeeder<SysOperationSeeder>();        // Order = 0
        services.AddDataSeeder<SysResourceSeeder>();          // Order = 1
        services.AddDataSeeder<SysPermissionSeeder>();        // Order = 2

        services.AddDataSeeder<SysDepartmentSeeder>();        // Order = 10
        services.AddDataSeeder<SysRoleSeeder>();              // Order = 11
        services.AddDataSeeder<SysRolePermissionSeeder>();    // Order = 12
        services.AddDataSeeder<SysUserSeeder>();              // Order = 13
        services.AddDataSeeder<SysMenuSeeder>();              // Order = 14
        services.AddDataSeeder<SysUserRoleSeeder>();          // Order = 15
        return services;
    }

    /// <summary>
    /// 添加 RBAC 基础设施适配器
    /// </summary>
    /// <remarks>
    /// Adapters 属于基础设施层，作为防腐层（Anti-Corruption Layer）隔离框架接口和领域模型
    /// 所有适配器只做接口转换，不包含业务逻辑，完全委托给 Domain Services 和 Application Services
    /// </remarks>
    /// <param name="services"></param>
    public static IServiceCollection AddRbacInfrastructureAdapters(this IServiceCollection services)
    {
        // 暂时注释掉，稍后创建极简实现
        // 认证适配器
        // services.AddScoped<IAuthenticationService, RbacAuthenticationService>();

        // 授权适配器
        // services.AddScoped<IPermissionStore, RbacPermissionStore>();
        // services.AddScoped<IRoleStore, RbacRoleStore>();
        // services.AddScoped<IRoleManager, RbacRoleManager>();
        // services.AddSingleton<IPolicyStore, RbacPolicyStore>();
        // services.AddScoped<IPolicyEvaluator, RbacPolicyEvaluator>();

        return services;
    }
}
