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
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.AppServices.Implementations;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Caching.EventHandlers;
using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.BasicApp.Saas.Application.Caching.Implementations;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Authentication;
using XiHan.BasicApp.Saas.Infrastructure.Authorization;
using XiHan.BasicApp.Saas.Infrastructure.Logging;
using XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Settings;
using XiHan.BasicApp.Saas.Seeders;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authorization.Abac;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Authorization.Policies;
using XiHan.Framework.Authorization.Roles;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.MultiTenancy.ConfigurationStore;
using XiHan.Framework.Settings.Stores;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Web.Api.Logging.Writers;
using XiHan.Framework.Web.Api.Security.OpenApi;
using XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

namespace XiHan.BasicApp.Saas.Extensions;

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
        services.AddScoped<IRoleHierarchyRepository, RoleHierarchyRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IConfigRepository, ConfigRepository>();
        services.AddScoped<IConstraintRuleRepository, ConstraintRuleRepository>();
        services.AddScoped<IDictRepository, DictRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<ISmsRepository, SmsRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskLogRepository, TaskLogRepository>();
        services.AddScoped<IOAuthAppRepository, OAuthAppRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // 日志仓储
        services.AddScoped<ILoginLogRepository, LoginLogRepository>();

        // 分表日志仓储
        services.AddScoped<IAccessLogSplitRepository, AccessLogSplitRepository>();
        services.AddScoped<IAuditLogSplitRepository, AuditLogSplitRepository>();
        services.AddScoped<IExceptionLogSplitRepository, ExceptionLogSplitRepository>();
        services.AddScoped<ILoginLogSplitRepository, LoginLogSplitRepository>();
        services.AddScoped<IOperationLogSplitRepository, OperationLogSplitRepository>();

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
        services.AddScoped<IRoleResolutionDomainService, RoleResolutionDomainService>();
        services.AddScoped<IConfigDomainService, ConfigDomainService>();
        services.AddScoped<IDictDomainService, DictDomainService>();
        services.AddScoped<IDepartmentDomainService, DepartmentDomainService>();
        services.AddScoped<IMenuDomainService, MenuDomainService>();
        services.AddScoped<IFileDomainService, FileDomainService>();
        services.AddScoped<INotificationDomainService, NotificationDomainService>();
        services.AddScoped<IOAuthAppDomainService, OAuthAppDomainService>();
        services.AddScoped<IReviewDomainService, ReviewDomainService>();
        services.AddScoped<ITaskDomainService, TaskDomainService>();

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
        services.AddScoped<IAuthSessionManager, AuthSessionManager>();
        services.AddScoped<IAuthNotificationService, AuthNotificationService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IProfileAppService, ProfileAppService>();
        services.AddScoped<IAuthTokenCacheHelper, AuthTokenCacheHelper>();
        services.AddScoped<IPermissionAppService, PermissionAppService>();
        services.AddScoped<IMenuAppService, MenuAppService>();
        services.AddScoped<IDepartmentAppService, DepartmentAppService>();
        services.AddScoped<ITenantAppService, TenantAppService>();
        services.AddScoped<IConfigAppService, ConfigAppService>();
        services.AddScoped<IConstraintRuleAppService, ConstraintRuleAppService>();
        services.AddScoped<IDictAppService, DictAppService>();
        services.AddScoped<IEnumAppService, EnumAppService>();
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
        services.AddScoped<IUpgradeAppService, UpgradeAppService>();
        services.AddScoped<IAccessLogAppService, AccessLogAppService>();
        services.AddScoped<IOperationLogAppService, OperationLogAppService>();
        services.AddScoped<IExceptionLogAppService, ExceptionLogAppService>();
        services.AddScoped<IAuditLogAppService, AuditLogAppService>();
        services.AddScoped<ILoginLogAppService, LoginLogAppService>();
        services.AddScoped<ITaskLogAppService, TaskLogAppService>();

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

        services.AddDataSeeder<SysTenantSeeder>();            // Order = 9
        services.AddDataSeeder<SysDepartmentSeeder>();        // Order = 10
        services.AddDataSeeder<SysDepartmentHierarchySeeder>(); // Order = 11
        services.AddDataSeeder<SysRoleSeeder>();              // Order = 12
        services.AddDataSeeder<SysRolePermissionSeeder>();    // Order = 13
        services.AddDataSeeder<SysUserSeeder>();              // Order = 14
        services.AddDataSeeder<SysMenuSeeder>();              // Order = 15
        services.AddDataSeeder<SysUserRoleSeeder>();          // Order = 16
        services.AddDataSeeder<SysDictSeeder>();              // Order = 17
        services.AddDataSeeder<SysDictItemSeeder>();          // Order = 18
        services.AddDataSeeder<SysConfigSeeder>();            // Order = 19
        services.AddDataSeeder<SysConstraintRuleFeatureSeeder>(); // Order = 20
        return services;
    }

    /// <summary>
    /// 添加 RBAC 基础设施适配器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacInfrastructureAdapters(this IServiceCollection services)
    {
        services.AddScoped<IExternalLoginStore, RbacExternalLoginStore>();
        services.AddScoped<IUserStore, RbacUserStore>();
        services.AddScoped<IRoleStore, RbacRoleStore>();
        services.AddScoped<IPermissionStore, RbacPermissionStore>();
        services.AddScoped<IAbacEvaluator, RbacAbacEvaluator>();
        services.AddScoped<IPolicyStore, RbacPolicyStore>();
        services.AddScoped<IOpenApiSecurityClientStore, RbacOpenApiSecurityClientStore>();
        services.AddScoped<ITenantStore, RbacTenantStore>();
        services.AddScoped<ISettingStore, RbacSettingStore>();
        services.AddScoped<IAccessLogWriter, RbacAccessLogWriter>();
        services.AddScoped<IOperationLogWriter, RbacOperationLogWriter>();
        services.AddScoped<IExceptionLogWriter, RbacExceptionLogWriter>();
        services.AddScoped<IApiLogWriter, RbacApiLogWriter>();
        services.AddScoped<IEntityAuditContextProvider, RbacEntityAuditContextProvider>();
        services.AddScoped<IEntityAuditLogWriter, RbacEntityAuditLogWriter>();
        services.AddScoped<IRbacAuthorizationCacheService, RbacAuthorizationCacheService>();
        services.AddScoped<IRbacLookupCacheService, RbacLookupCacheService>();
        services.AddScoped<IMessageCacheService, MessageCacheService>();
        services.AddScoped<IUpgradeVersionStore, SqlSugarUpgradeVersionStore>();
        services.AddScoped<IUpgradeLockProvider, RedisUpgradeLockProvider>();
        services.AddScoped<IUpgradeMigrationExecutor, SqlSugarUpgradeMigrationExecutor>();
        services.AddSingleton<IUpgradeTenantProvider, SqlSugarUpgradeTenantProvider>();

        // 本地事件总线 IocEventHandlerFactory 按「具体处理器类型」GetRequiredService，须先注册具体类再转发接口
        services.AddScoped<RbacAuthorizationChangedEventHandler>();
        services.AddScoped<ILocalEventHandler<RbacAuthorizationChangedEvent>>(static sp =>
            sp.GetRequiredService<RbacAuthorizationChangedEventHandler>());
        services.AddScoped<UserRolesChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<UserRolesChangedDomainEvent>>(static sp =>
            sp.GetRequiredService<UserRolesChangedDomainEventHandler>());
        services.AddScoped<UserPermissionsChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<UserPermissionsChangedDomainEvent>>(static sp =>
            sp.GetRequiredService<UserPermissionsChangedDomainEventHandler>());
        services.AddScoped<UserDepartmentsChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<UserDepartmentsChangedDomainEvent>>(static sp =>
            sp.GetRequiredService<UserDepartmentsChangedDomainEventHandler>());
        services.AddScoped<RolePermissionsChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<RolePermissionsChangedDomainEvent>>(static sp =>
            sp.GetRequiredService<RolePermissionsChangedDomainEventHandler>());
        services.AddScoped<RoleDataScopeChangedDomainEventHandler>();
        services.AddScoped<ILocalEventHandler<RoleDataScopeChangedDomainEvent>>(static sp =>
            sp.GetRequiredService<RoleDataScopeChangedDomainEventHandler>());

        return services;
    }
}
