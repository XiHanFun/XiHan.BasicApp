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
using XiHan.BasicApp.Rbac.Application.AppServices;
using XiHan.BasicApp.Rbac.Application.AppServices.Implementations;
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Caching.EventHandlers;
using XiHan.BasicApp.Rbac.Application.Caching.Events;
using XiHan.BasicApp.Rbac.Application.Caching.Implementations;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;
using XiHan.BasicApp.Rbac.Domain.Events;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Infrastructure.Authentication;
using XiHan.BasicApp.Rbac.Infrastructure.Authorization;
using XiHan.BasicApp.Rbac.Infrastructure.Logging;
using XiHan.BasicApp.Rbac.Infrastructure.MultiTenancy;
using XiHan.BasicApp.Rbac.Infrastructure.Repositories;
using XiHan.BasicApp.Rbac.Infrastructure.Settings;
using XiHan.BasicApp.Rbac.Seeders;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Authorization.Policies;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.MultiTenancy.ConfigurationStore;
using XiHan.Framework.Settings.Stores;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Authorization.Roles;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Web.Api.Logging.Writers;

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
        services.AddScoped<IAccessLogAppService, AccessLogAppService>();
        services.AddScoped<IOperationLogAppService, OperationLogAppService>();
        services.AddScoped<IExceptionLogAppService, ExceptionLogAppService>();
        services.AddScoped<IAuditLogAppService, AuditLogAppService>();

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
        services.AddDataSeeder<SysDepartmentHierarchySeeder>(); // Order = 11
        services.AddDataSeeder<SysRoleSeeder>();              // Order = 12
        services.AddDataSeeder<SysRolePermissionSeeder>();    // Order = 13
        services.AddDataSeeder<SysUserSeeder>();              // Order = 14
        services.AddDataSeeder<SysMenuSeeder>();              // Order = 15
        services.AddDataSeeder<SysUserRoleSeeder>();          // Order = 16
        services.AddDataSeeder<SysRoleMenuSeeder>();          // Order = 17
        services.AddDataSeeder<SysDictSeeder>();              // Order = 18
        services.AddDataSeeder<SysDictItemSeeder>();          // Order = 19
        services.AddDataSeeder<SysConfigSeeder>();            // Order = 20
        return services;
    }

    /// <summary>
    /// 添加 RBAC 基础设施适配器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacInfrastructureAdapters(this IServiceCollection services)
    {
        services.AddScoped<IUserStore, RbacUserStore>();
        services.AddScoped<IRoleStore, RbacRoleStore>();
        services.AddScoped<IPermissionStore, RbacPermissionStore>();
        services.AddScoped<IPolicyStore, RbacPolicyStore>();
        services.AddScoped<ITenantStore, RbacTenantStore>();
        services.AddScoped<ISettingStore, RbacSettingStore>();
        services.AddScoped<IAccessLogWriter, RbacAccessLogWriter>();
        services.AddScoped<IOperationLogWriter, RbacOperationLogWriter>();
        services.AddScoped<IExceptionLogWriter, RbacExceptionLogWriter>();
        services.AddScoped<IEntityAuditContextProvider, RbacEntityAuditContextProvider>();
        services.AddScoped<IEntityAuditLogWriter, RbacEntityAuditLogWriter>();
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
