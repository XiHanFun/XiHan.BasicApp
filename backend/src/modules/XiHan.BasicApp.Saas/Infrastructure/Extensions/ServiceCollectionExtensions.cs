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
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.EventHandlers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Infrastructure.Auth;
using XiHan.BasicApp.Saas.Infrastructure.Logging;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.BasicApp.Saas.Infrastructure.Security;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.BasicApp.Saas.Infrastructure.Tasks.Jobs;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Security.Services;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Web.Api.Logging.Writers;

namespace XiHan.BasicApp.Saas.Infrastructure.Extensions;

/// <summary>
/// SaaS 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 SaaS 领域服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasDomainServices(this IServiceCollection services)
    {
        // 纯逻辑领域服务（无外部依赖，注册为单例）
        services.AddSingleton<IPermissionDecisionDomainService, PermissionDecisionDomainService>();
        services.AddSingleton<IDataScopeDecisionDomainService, DataScopeDecisionDomainService>();
        services.AddSingleton<ITenantAccessDomainService, TenantAccessDomainService>();
        services.AddSingleton<IPasswordPolicyDomainService, PasswordPolicyDomainService>();
        services.AddSingleton<IUserSessionDomainService, UserSessionDomainService>();
        services.AddSingleton<IFileStorageDomainService, FileStorageDomainService>();

        // 依赖仓储的领域服务（跟随仓储生命周期，注册为 Scoped）
        services.AddScoped<ITenantProvisionDomainService, TenantProvisionDomainService>();
        services.AddScoped<IRoleHierarchyDomainService, RoleHierarchyDomainService>();
        services.AddScoped<IPermissionMergeDomainService, PermissionMergeDomainService>();
        services.AddScoped<IDepartmentHierarchyDomainService, DepartmentHierarchyDomainService>();

        return services;
    }

    /// <summary>
    /// 添加 SaaS 应用层内部服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISaasConfigurationService, SaasConfigurationService>();
        services.AddScoped<ISaasCacheInvalidator, SaasCacheInvalidator>();
        return services;
    }

    /// <summary>
    /// 添加 SaaS 领域事件处理器
    /// </summary>
    /// <remarks>
    /// 事件处理器注册为 Transient，由事件总线框架通过 <c>OnRegistered</c> 钩子自动发现并订阅。
    /// Transient 生命周期确保每次事件发布时获取新实例，避免并发冲突。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasEventHandlers(this IServiceCollection services)
    {
        // 租户事件
        services.AddTransient<TenantStatusChangedEventHandler>();
        services.AddTransient<TenantMembershipChangedEventHandler>();

        // 用户会话事件
        services.AddTransient<UserSessionRevokedEventHandler>();

        // 文件事件
        services.AddTransient<FileUploadedEventHandler>();
        services.AddTransient<FileDeletedEventHandler>();
        services.AddTransient<FilePrimaryStorageChangedEventHandler>();

        // 授权事件
        services.AddTransient<AuthorizationChangedEventHandler>();
        services.AddTransient<DataScopeChangedEventHandler>();
        services.AddTransient<FieldLevelSecurityChangedEventHandler>();

        // 组织层级事件
        services.AddTransient<HierarchyChangedEventHandler>();

        return services;
    }

    /// <summary>
    /// 添加 SaaS 种子数据提供者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<SaasIdentitySeeder>();
        services.AddDataSeeder<SaasPermissionSeeder>();
        services.AddDataSeeder<SaasTenantEditionSeeder>();
        services.AddDataSeeder<SaasConfigurationSeeder>();
        services.AddDataSeeder<SaasDictSeeder>();
        services.AddDataSeeder<SaasIdentityPermissionSeeder>();
        services.AddDataSeeder<SaasMenuSeeder>();
        return services;
    }

    /// <summary>
    /// 添加 SaaS 日志写入器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasLogWriters(this IServiceCollection services)
    {
        services.AddScoped<IAccessLogWriter, RbacAccessLogWriter>();
        services.AddScoped<IApiLogWriter, RbacApiLogWriter>();
        services.AddScoped<IOperationLogWriter, RbacOperationLogWriter>();
        services.AddScoped<IExceptionLogWriter, RbacExceptionLogWriter>();
        services.AddScoped<ILoginLogWriter, RbacLoginLogWriter>();
        services.AddScoped<IEntityDiffLogWriter, RbacEntityDiffLogWriter>();
        services.AddScoped<IEntityAuditContextProvider, RbacEntityDiffContextProvider>();
        return services;
    }

    /// <summary>
    /// 添加 SaaS 认证基础设施服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasAuthStores(this IServiceCollection services)
    {
        // 替换框架默认 IUserStore 为数据库实现
        services.Replace(ServiceDescriptor.Scoped<IUserStore, SaasUserStore>());

        // 注册第三方登录存储
        services.Replace(ServiceDescriptor.Scoped<IExternalLoginStore, SaasExternalLoginStore>());

        // 注册密码历史存储
        services.AddScoped<IPasswordHistoryStore, SaasPasswordHistoryStore>();

        return services;
    }

    /// <summary>
    /// 添加 SaaS 消息发送器
    /// </summary>
    /// <remarks>
    /// 注册为 Singleton 以兼容 DefaultMessageDispatcher（Singleton）的依赖链路。
    /// 内部通过 IServiceScopeFactory 创建作用域来解析 Scoped 服务（如 ISqlSugarClientResolver）。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasMessageSenders(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageSender, EmailMessageSender>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageSender, SmsMessageSender>());
        return services;
    }

    /// <summary>
    /// 添加 SaaS 任务调度基础设施
    /// </summary>
    /// <remarks>
    /// 用数据库持久化的 <see cref="SaasJobStore"/> 替换框架默认的内存存储，
    /// 并注册业务层的 <see cref="IJobWorker"/> 实现。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasJobInfrastructure(this IServiceCollection services)
    {
        // 替换框架默认的 InMemoryJobStore 为数据库持久化实现
        services.Replace(ServiceDescriptor.Singleton<IJobStore, SaasJobStore>());

        // 注册动态任务执行器（桥接 SysTask.TaskClass/TaskMethod 反射模型，同时实现 IJobWorker）
        services.AddTransient<DynamicJobWorker>();

        return services;
    }
}
