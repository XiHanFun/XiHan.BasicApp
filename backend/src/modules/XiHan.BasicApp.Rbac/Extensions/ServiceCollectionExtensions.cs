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
using XiHan.BasicApp.Rbac.Repositories.AccessLogs;
using XiHan.BasicApp.Rbac.Repositories.ApiLogs;
using XiHan.BasicApp.Rbac.Repositories.AuditLogs;
using XiHan.BasicApp.Rbac.Repositories.Audits;
using XiHan.BasicApp.Rbac.Repositories.Configs;
using XiHan.BasicApp.Rbac.Repositories.Departments;
using XiHan.BasicApp.Rbac.Repositories.DictItems;
using XiHan.BasicApp.Rbac.Repositories.Dicts;
using XiHan.BasicApp.Rbac.Repositories.Emails;
using XiHan.BasicApp.Rbac.Repositories.Files;
using XiHan.BasicApp.Rbac.Repositories.LoginLogs;
using XiHan.BasicApp.Rbac.Repositories.Menus;
using XiHan.BasicApp.Rbac.Repositories.Notifications;
using XiHan.BasicApp.Rbac.Repositories.OAuthApps;
using XiHan.BasicApp.Rbac.Repositories.OAuthCodes;
using XiHan.BasicApp.Rbac.Repositories.OAuthTokens;
using XiHan.BasicApp.Rbac.Repositories.OperationLogs;
using XiHan.BasicApp.Rbac.Repositories.Permissions;
using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.BasicApp.Rbac.Repositories.Sms;
using XiHan.BasicApp.Rbac.Repositories.TaskLogs;
using XiHan.BasicApp.Rbac.Repositories.Tasks;
using XiHan.BasicApp.Rbac.Repositories.Tenants;
using XiHan.BasicApp.Rbac.Repositories.UserPermissions;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.BasicApp.Rbac.Repositories.UserSecurities;
using XiHan.BasicApp.Rbac.Repositories.UserSessions;
using XiHan.BasicApp.Rbac.Services.AccessLogs;
using XiHan.BasicApp.Rbac.Services.ApiLogs;
using XiHan.BasicApp.Rbac.Services.AuditLogs;
using XiHan.BasicApp.Rbac.Services.Audits;
using XiHan.BasicApp.Rbac.Services.Configs;
using XiHan.BasicApp.Rbac.Services.Departments;
using XiHan.BasicApp.Rbac.Services.DictItems;
using XiHan.BasicApp.Rbac.Services.Dicts;
using XiHan.BasicApp.Rbac.Services.Emails;
using XiHan.BasicApp.Rbac.Services.Files;
using XiHan.BasicApp.Rbac.Services.LoginLogs;
using XiHan.BasicApp.Rbac.Services.Menus;
using XiHan.BasicApp.Rbac.Services.Notifications;
using XiHan.BasicApp.Rbac.Services.OAuthApps;
using XiHan.BasicApp.Rbac.Services.OAuthCodes;
using XiHan.BasicApp.Rbac.Services.OAuthTokens;
using XiHan.BasicApp.Rbac.Services.OperationLogs;
using XiHan.BasicApp.Rbac.Services.Permissions;
using XiHan.BasicApp.Rbac.Services.Roles;
using XiHan.BasicApp.Rbac.Services.Sms;
using XiHan.BasicApp.Rbac.Services.TaskLogs;
using XiHan.BasicApp.Rbac.Services.Tasks;
using XiHan.BasicApp.Rbac.Services.Tenants;
using XiHan.BasicApp.Rbac.Services.UserPermissions;
using XiHan.BasicApp.Rbac.Services.Users;
using XiHan.BasicApp.Rbac.Services.UserSecurities;
using XiHan.BasicApp.Rbac.Services.UserSessions;

namespace XiHan.BasicApp.Rbac.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 RBAC 服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacServices(this IServiceCollection services)
    {
        // 核心 RBAC 服务
        services.AddScoped<ISysUserService, SysUserService>();
        services.AddScoped<ISysRoleService, SysRoleService>();
        services.AddScoped<ISysPermissionService, SysPermissionService>();
        services.AddScoped<ISysMenuService, SysMenuService>();
        services.AddScoped<ISysDepartmentService, SysDepartmentService>();
        services.AddScoped<ISysTenantService, SysTenantService>();

        // 日志服务
        services.AddScoped<ISysAccessLogService, SysAccessLogService>();
        services.AddScoped<ISysApiLogService, SysApiLogService>();
        services.AddScoped<ISysAuditService, SysAuditService>();
        services.AddScoped<ISysAuditLogService, SysAuditLogService>();
        services.AddScoped<ISysLoginLogService, SysLoginLogService>();
        services.AddScoped<ISysOperationLogService, SysOperationLogService>();

        // OAuth 服务
        services.AddScoped<ISysOAuthAppService, SysOAuthAppService>();
        services.AddScoped<ISysOAuthCodeService, SysOAuthCodeService>();
        services.AddScoped<ISysOAuthTokenService, SysOAuthTokenService>();

        // 配置和字典服务
        services.AddScoped<ISysConfigService, SysConfigService>();
        services.AddScoped<ISysDictService, SysDictService>();
        services.AddScoped<ISysDictItemService, SysDictItemService>();

        // 通知和消息服务
        services.AddScoped<ISysEmailService, SysEmailService>();
        services.AddScoped<ISysSmsService, SysSmsService>();
        services.AddScoped<ISysNotificationService, SysNotificationService>();

        // 文件服务
        services.AddScoped<ISysFileService, SysFileService>();

        // 任务服务
        services.AddScoped<ISysTaskService, SysTaskService>();
        services.AddScoped<ISysTaskLogService, SysTaskLogService>();

        // 用户扩展服务
        services.AddScoped<ISysUserSessionService, SysUserSessionService>();
        services.AddScoped<ISysUserSecurityService, SysUserSecurityService>();
        services.AddScoped<ISysUserPermissionService, SysUserPermissionService>();

        return services;
    }

    /// <summary>
    /// 添加 RBAC 仓储
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddRbacRepositories(this IServiceCollection services)
    {
        // 核心 RBAC 仓储
        services.AddScoped<ISysUserRepository, SysUserRepository>();
        services.AddScoped<ISysRoleRepository, SysRoleRepository>();
        services.AddScoped<ISysPermissionRepository, SysPermissionRepository>();
        services.AddScoped<ISysMenuRepository, SysMenuRepository>();
        services.AddScoped<ISysDepartmentRepository, SysDepartmentRepository>();
        services.AddScoped<ISysTenantRepository, SysTenantRepository>();

        // 日志仓储
        services.AddScoped<ISysAccessLogRepository, SysAccessLogRepository>();
        services.AddScoped<ISysApiLogRepository, SysApiLogRepository>();
        services.AddScoped<ISysAuditRepository, SysAuditRepository>();
        services.AddScoped<ISysAuditLogRepository, SysAuditLogRepository>();
        services.AddScoped<ISysLoginLogRepository, SysLoginLogRepository>();
        services.AddScoped<ISysOperationLogRepository, SysOperationLogRepository>();

        // OAuth 仓储
        services.AddScoped<ISysOAuthAppRepository, SysOAuthAppRepository>();
        services.AddScoped<ISysOAuthCodeRepository, SysOAuthCodeRepository>();
        services.AddScoped<ISysOAuthTokenRepository, SysOAuthTokenRepository>();

        // 配置和字典仓储
        services.AddScoped<ISysConfigRepository, SysConfigRepository>();
        services.AddScoped<ISysDictRepository, SysDictRepository>();
        services.AddScoped<ISysDictItemRepository, SysDictItemRepository>();

        // 通知和消息仓储
        services.AddScoped<ISysEmailRepository, SysEmailRepository>();
        services.AddScoped<ISysSmsRepository, SysSmsRepository>();
        services.AddScoped<ISysNotificationRepository, SysNotificationRepository>();

        // 文件仓储
        services.AddScoped<ISysFileRepository, SysFileRepository>();

        // 任务仓储
        services.AddScoped<ISysTaskRepository, SysTaskRepository>();
        services.AddScoped<ISysTaskLogRepository, SysTaskLogRepository>();

        // 用户扩展仓储
        services.AddScoped<ISysUserSessionRepository, SysUserSessionRepository>();
        services.AddScoped<ISysUserSecurityRepository, SysUserSecurityRepository>();
        services.AddScoped<ISysUserPermissionRepository, SysUserPermissionRepository>();

        return services;
    }
}
