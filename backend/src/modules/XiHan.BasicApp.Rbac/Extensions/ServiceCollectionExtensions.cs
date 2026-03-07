#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:6b2b3c4d-5e6f-7890-abcd-ef12345678ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 05:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Caching.EventHandlers;
using XiHan.BasicApp.Rbac.Application.Caching.Events;
using XiHan.BasicApp.Rbac.Application.Caching.Implementations;
using XiHan.BasicApp.Rbac.Domain.Events;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Infrastructure.Repositories;
using XiHan.BasicApp.Rbac.Seeders;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.BasicApp.Rbac.Application.AppServices.Implementations;
using XiHan.BasicApp.Rbac.Application.AppServices;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

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
        // 聚合仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IConfigRepository, ConfigRepository>();
        services.AddScoped<IDictRepository, DictRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<ISmsRepository, SmsRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IOAuthAppRepository, OAuthAppRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // 日志仓储
        services.AddScoped<ILoginLogRepository, LoginLogRepository>();

        return services;
    }

    /// <summary>
    /// 添加 RBAC 领域服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IRoleManager, RoleManager>();
        services.AddScoped<ITenantManager, TenantManager>();
        services.AddScoped<IOrganizationDomainService, OrganizationDomainService>();
        services.AddScoped<IPermissionDomainService, PermissionDomainService>();
        services.AddScoped<IAuthorizationDomainService, AuthorizationDomainService>();

        return services;
    }

    /// <summary>
    /// 添加 RBAC 应用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IRoleAppService, RoleAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IPermissionAppService, PermissionAppService>();
        services.AddScoped<IMenuAppService, MenuAppService>();
        services.AddScoped<IDepartmentAppService, DepartmentAppService>();
        services.AddScoped<ITenantAppService, TenantAppService>();
        services.AddScoped<IConfigAppService, ConfigAppService>();
        services.AddScoped<IDictAppService, DictAppService>();
        services.AddScoped<IFileAppService, FileAppService>();
        services.AddScoped<IEmailAppService, EmailAppService>();
        services.AddScoped<ISmsAppService, SmsAppService>();
        services.AddScoped<ITaskAppService, TaskAppService>();
        services.AddScoped<IOAuthAppService, OAuthAppService>();
        services.AddScoped<IReviewAppService, ReviewAppService>();
        services.AddScoped<IUserSessionAppService, UserSessionAppService>();
        services.AddScoped<INotificationAppService, NotificationAppService>();
        services.AddScoped<IMessageAppService, MessageAppService>();
        services.AddScoped<ICacheAppService, CacheAppService>();
        services.AddScoped<IServerService, ServerService>();

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
        services.AddDataSeeder<SysDepartmentHierarchySeeder>(); // Order = 10
        services.AddDataSeeder<SysRoleSeeder>();              // Order = 11
        services.AddDataSeeder<SysRolePermissionSeeder>();    // Order = 12
        services.AddDataSeeder<SysUserSeeder>();              // Order = 13
        services.AddDataSeeder<SysMenuSeeder>();              // Order = 14
        services.AddDataSeeder<SysUserRoleSeeder>();          // Order = 15
        services.AddDataSeeder<SysDictSeeder>();              // Order = 16
        services.AddDataSeeder<SysDictItemSeeder>();          // Order = 17
        services.AddDataSeeder<SysConfigSeeder>();            // Order = 18
        return services;
    }

    /// <summary>
    /// 添加 RBAC 基础设施适配器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacInfrastructureAdapters(this IServiceCollection services)
    {
        services.AddScoped<IRbacAuthorizationCacheService, RbacAuthorizationCacheService>();
        services.AddScoped<IRbacLookupCacheService, RbacLookupCacheService>();
        services.AddScoped<IMessageCacheService, MessageCacheService>();
        services.AddScoped<ILocalEventHandler<RbacAuthorizationChangedEvent>, RbacAuthorizationChangedEventHandler>();
        services.AddScoped<ILocalEventHandler<UserRolesChangedDomainEvent>, UserRolesChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<UserPermissionsChangedDomainEvent>, UserPermissionsChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<UserDepartmentsChangedDomainEvent>, UserDepartmentsChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<RolePermissionsChangedDomainEvent>, RolePermissionsChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<RoleDataScopeChangedDomainEvent>, RoleDataScopeChangedDomainEventHandler>();

        return services;
    }
}
