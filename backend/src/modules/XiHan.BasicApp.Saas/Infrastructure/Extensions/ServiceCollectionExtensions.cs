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
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Infrastructure.Auth;
using XiHan.BasicApp.Saas.Infrastructure.Exporting;
using XiHan.BasicApp.Saas.Infrastructure.Logging;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.BasicApp.Saas.Infrastructure.Security;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Local;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Security.Services;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Utils.Collections;
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
        services.AddSingleton<ITaskScheduleDomainService, TaskScheduleDomainService>();

        // 依赖仓储的领域服务（跟随仓储生命周期，注册为 Scoped）
        services.AddScoped<IAuthenticationDomainService, AuthenticationDomainService>();
        services.AddScoped<ILoginSessionDomainService, LoginSessionDomainService>();
        services.AddScoped<IMenuDomainService, MenuDomainService>();
        services.AddScoped<IRoleDomainService, RoleDomainService>();
        services.AddScoped<IUserDomainService, UserDomainService>();
        services.AddScoped<IConstraintRuleDomainService, ConstraintRuleDomainService>();
        services.AddScoped<IFieldLevelSecurityDomainService, FieldLevelSecurityDomainService>();
        services.AddScoped<IFileDomainService, FileDomainService>();
        services.AddScoped<IStorageConfigDomainService, StorageConfigDomainService>();
        services.AddScoped<IConfigDomainService, ConfigDomainService>();
        services.AddScoped<IDictDomainService, DictDomainService>();
        services.AddScoped<IVersionDomainService, VersionDomainService>();
        services.AddScoped<ITenantProvisionDomainService, TenantProvisionDomainService>();
        services.AddScoped<IRoleHierarchyDomainService, RoleHierarchyDomainService>();
        services.AddScoped<IPermissionMergeDomainService, PermissionMergeDomainService>();
        services.AddScoped<IPermissionCatalogDomainService, PermissionCatalogDomainService>();
        services.AddScoped<IPermissionConditionDomainService, PermissionConditionDomainService>();
        services.AddScoped<IPermissionDelegationDomainService, PermissionDelegationDomainService>();
        services.AddScoped<IPermissionRequestDomainService, PermissionRequestDomainService>();
        services.AddScoped<IProfileDomainService, ProfileDomainService>();
        services.AddScoped<IDepartmentDomainService, DepartmentDomainService>();
        services.AddScoped<IDepartmentHierarchyDomainService, DepartmentHierarchyDomainService>();
        services.AddScoped<ITaskDomainService, TaskDomainService>();
        services.AddScoped<IReviewDomainService, ReviewDomainService>();
        services.AddScoped<IOAuthAppDomainService, OAuthAppDomainService>();
        services.AddScoped<IMessageDomainService, MessageDomainService>();
        services.AddScoped<IMessageTemplateDomainService, MessageTemplateDomainService>();
        services.AddScoped<INotificationDomainService, NotificationDomainService>();
        services.AddScoped<IUserInboxDomainService, UserInboxDomainService>();
        services.AddScoped<ITenantDomainService, TenantDomainService>();
        services.AddScoped<ITenantEditionDomainService, TenantEditionDomainService>();

        return services;
    }

    /// <summary>
    /// 添加 SaaS 应用层内部服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthContextQueryService, AuthContextQueryService>();
        services.AddScoped<IAuthorizationSnapshotQueryService, AuthorizationSnapshotQueryService>();
        // 请求期鉴权改用授权快照（替换框架内存版 DefaultPermissionChecker），使授权变更无需重新登录即生效
        services.Replace(ServiceDescriptor.Scoped<IPermissionChecker, SaasPermissionChecker>());
        services.AddScoped<IMenuRouteQueryService, MenuRouteQueryService>();
        services.AddScoped<IUserDataScopeFilterService, UserDataScopeFilterService>();
        services.AddScoped<IFileRecordQueryService, FileRecordQueryService>();
        services.AddScoped<ITaskSchedulerQueryService, TaskSchedulerQueryService>();
        services.AddScoped<IMessageRecordQueryService, MessageRecordQueryService>();
        services.AddScoped<IUserInboxQueryService, UserInboxQueryService>();
        services.AddScoped<ISaasConfigValueQueryService, SaasConfigValueQueryService>();
        services.AddScoped<IProfileQueryService, ProfileQueryService>();
        services.AddScoped<IEnumMetadataQueryService, EnumMetadataQueryService>();
        services.AddScoped<IServerInfoQueryService, ServerInfoQueryService>();
        services.AddScoped<IMessageDeliveryService, MessageDeliveryService>();
        services.AddScoped<IMessageTemplateRenderer, MessageTemplateRenderer>();
        services.AddScoped<ITaskSchedulerSyncService, TaskSchedulerSyncService>();
        services.AddScoped<IFileTransferService, FileTransferService>();
        services.AddScoped<IAuthTokenIssueService, AuthTokenIssueService>();
        services.AddSingleton<IAuthEmailLoginCodeService, AuthEmailLoginCodeService>();
        services.AddScoped<IProfileVerificationService, ProfileVerificationService>();
        services.AddScoped<IFieldSecurityService, FieldSecurityService>();
        services.AddScoped<ICacheManagementService, CacheManagementService>();
        services.AddScoped<ISaasConfigurationService, SaasConfigurationService>();
        services.AddScoped<ISaasCacheInvalidator, SaasCacheInvalidator>();
        return services;
    }

    /// <summary>
    /// 添加 SaaS 领域事件处理器
    /// </summary>
    /// <remarks>
    /// 事件总线的 <c>OnRegistered</c> 自动发现仅覆盖以接口为服务类型的注册（由 Castle 动态代理扫描触发），
    /// 具体类注册不会被发现。因此这里在注册的同时显式将处理器加入
    /// <see cref="XiHanLocalEventBusOptions.Handlers"/>，确保 LocalEventBus 完成订阅。
    /// Transient 生命周期确保每次事件发布时获取新实例，避免并发冲突。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasEventHandlers(this IServiceCollection services)
    {
        // 租户事件
        services.AddSaasLocalEventHandler<TenantStatusChangedEventHandler>();
        services.AddSaasLocalEventHandler<TenantMembershipChangedEventHandler>();

        // 用户会话事件
        services.AddSaasLocalEventHandler<UserSessionRevokedEventHandler>();

        // 认证事件
        services.AddSaasLocalEventHandler<AuthLoginEventHandler>();

        // 文件事件
        services.AddSaasLocalEventHandler<FileUploadedEventHandler>();
        services.AddSaasLocalEventHandler<FileDeletedEventHandler>();
        services.AddSaasLocalEventHandler<FilePrimaryStorageChangedEventHandler>();

        // 授权事件
        services.AddSaasLocalEventHandler<AuthorizationChangedEventHandler>();
        services.AddSaasLocalEventHandler<DataScopeChangedEventHandler>();
        services.AddSaasLocalEventHandler<FieldLevelSecurityChangedEventHandler>();

        // 组织层级事件
        services.AddSaasLocalEventHandler<HierarchyChangedEventHandler>();

        return services;
    }

    /// <summary>
    /// 注册本地事件处理器并加入事件总线订阅列表
    /// </summary>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection AddSaasLocalEventHandler<THandler>(this IServiceCollection services)
        where THandler : class
    {
        services.AddTransient<THandler>();
        services.Configure<XiHanLocalEventBusOptions>(options => options.Handlers.AddIfNotContains(typeof(THandler)));
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
        services.AddDataSeeder<SaasMenuSeeder>();
        services.AddDataSeeder<SaasMessageTemplateSeeder>();
        services.AddDataSeeder<SaasOAuthAppSeeder>();
        services.AddDataSeeder<SaasOrganizationSeeder>();
        services.AddDataSeeder<SaasSampleIdentitySeeder>();
        services.AddDataSeeder<SaasNotificationSeeder>();
        services.AddDataSeeder<SaasStorageConfigSeeder>();
        services.AddDataSeeder<SaasTaskSeeder>();
        return services;
    }

    /// <summary>
    /// 添加 SaaS 日志写入器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasLogWriters(this IServiceCollection services)
    {
        services.AddScoped<IAccessLogWriter, SaasAccessLogWriter>();
        services.AddScoped<IApiLogWriter, SaasApiLogWriter>();
        services.AddScoped<IOperationLogWriter, SaasOperationLogWriter>();
        services.AddScoped<IExceptionLogWriter, SaasExceptionLogWriter>();
        services.AddScoped<ILoginLogWriter, SaasLoginLogWriter>();
        services.AddScoped<IEntityDiffLogWriter, SaasEntityDiffLogWriter>();
        services.AddScoped<IEntityAuditContextProvider, SaasEntityDiffContextProvider>();
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
        services.AddOptions<EmailSenderOptions>().BindConfiguration(EmailSenderOptions.SectionName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageSender, EmailMessageSender>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageSender, SmsMessageSender>());

        // 业务层发件箱（框架 Messaging 仅负责路由，发件箱在业务层）：先落 SysEmail/SysSms 为 Pending，
        // 入 Redis 延迟队列后由后台服务拉取发送（拉不到等待、拉到消费、可并发；失败延迟重投）。
        services.AddSingleton<DbMessageOutbox>();
        // 注意：MessageOutboxHostedService 继承 XiHanBackgroundServiceBase（IBackgroundWorker:ISingletonDependency）
        // 且类名以 HostedService 结尾，已被约定注册自动暴露为 IHostedService 托管。切勿再 AddHostedService（否则重复托管、重复消费）。

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

    /// <summary>
    /// 添加 SaaS 导出中心基础设施
    /// </summary>
    /// <remarks>
    /// 导出引擎：执行器 + CSV 写出器 + 逐资源登记的 <see cref="IExportProvider"/>（首版 system.user / log.operation）；
    /// 后台 <see cref="ExportTaskHostedService"/> 轮询 Pending 任务异步执行。
    /// 导出任务仓储（<c>IExportTaskRepository</c>）随 <c>SaasRepository</c> 的 <c>IScopedDependency</c> 自动注册。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSaasExportInfrastructure(this IServiceCollection services)
    {
        // 导出引擎
        services.AddScoped<IExportExecutor, ExportExecutor>();
        services.AddSingleton<IExportWriter, CsvExportWriter>();

        // 导出 Provider（逐资源登记；新增资源在此追加一行）
        services.AddScoped<IExportProvider, UserExportProvider>();
        services.AddScoped<IExportProvider, OperationLogExportProvider>();
        services.AddScoped<IExportProvider, AccessLogExportProvider>();
        services.AddScoped<IExportProvider, ApiLogExportProvider>();
        services.AddScoped<IExportProvider, LoginLogExportProvider>();
        services.AddScoped<IExportProvider, ExceptionLogExportProvider>();
        services.AddScoped<IExportProvider, DiffLogExportProvider>();

        // 后台执行 worker：从 Redis 延迟队列拉取任务执行（拉不到等待、拉到消费、可并发）。
        // 注意：ExportTaskHostedService 继承 XiHanBackgroundServiceBase（IBackgroundWorker:ISingletonDependency）
        // 且类名以 HostedService 结尾，已被约定注册自动暴露为 IHostedService 托管。切勿再 AddHostedService（否则重复托管、重复消费）。

        return services;
    }
}
