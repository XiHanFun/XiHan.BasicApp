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
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Infrastructure.Logging;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Data.Extensions.DependencyInjection;
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
}
