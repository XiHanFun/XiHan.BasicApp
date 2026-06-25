#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasNotificationSeeder
// Guid:b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 系统通知种子数据
/// </summary>
public sealed class SaasNotificationSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasNotificationSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 26;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统通知种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;

        var existingCount = await client.Queryable<SysNotification>()
            .Where(notification => notification.TenantId == 0 && !notification.IsDeleted)
            .CountAsync();
        if (existingCount >= 3)
        {
            Logger.LogInformation("平台通知数据已存在，跳过种子数据");
            return;
        }

        var notifications = new List<SysNotification>
        {
            CreateNotification(
                NotificationType.System,
                "欢迎使用 XiHan BasicApp",
                "感谢您选择 XiHan BasicApp 平台。系统已成功初始化，您可以开始使用各项功能。如有任何问题，请联系系统管理员。",
                "lucide:party-popper",
                TargetType: NotificationTargetType.All,
                isPublished: true,
                sort: 10),
            CreateNotification(
                NotificationType.Security,
                "请及时修改默认密码",
                "为了保障您的账户安全，请及时修改默认密码，并设置符合安全策略的强密码。建议密码包含大小写字母、数字和特殊字符，长度不少于8位。",
                "lucide:shield-alert",
                TargetType: NotificationTargetType.All,
                isPublished: true,
                sort: 20),
            CreateNotification(
                NotificationType.System,
                "系统初始化完成",
                "XiHan BasicApp 平台初始化已完成，所有核心模块已就绪。平台提供多租户管理、权限控制、文件存储等功能，请根据业务需求进行配置。",
                "lucide:check-circle",
                TargetType: NotificationTargetType.All,
                isPublished: true,
                sort: 30),
            CreateNotification(
                NotificationType.System,
                "系统使用提示",
                "您可以在「工作台 > 个人中心」中完善个人信息，在「系统管理」中管理组织架构和用户权限。如需帮助，请查看帮助文档或联系技术支持。",
                "lucide:info",
                TargetType: NotificationTargetType.All,
                isPublished: false,
                sort: 40)
        };

        var addCount = 0;
        foreach (var notification in notifications)
        {
            var exists = await client.Queryable<SysNotification>()
                .Where(n => n.TenantId == 0 && n.Title == notification.Title && !n.IsDeleted)
                .AnyAsync();
            if (exists)
            {
                continue;
            }

            _ = await client.Insertable(notification).ExecuteReturnEntityAsync();
            addCount++;
        }

        if (addCount == 0)
        {
            Logger.LogInformation("平台通知数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化平台通知 {Count} 条", addCount);
    }

    private static SysNotification CreateNotification(
        NotificationType type,
        string title,
        string content,
        string icon,
        NotificationTargetType TargetType = NotificationTargetType.All,
        bool isPublished = false,
        int sort = 0)
    {
        return new SysNotification
        {
            TenantId = 0,
            NotificationType = type,
            Title = title,
            Content = content,
            Icon = icon,
            TargetType = TargetType,
            IsPublished = isPublished,
            SendTime = DateTimeOffset.UtcNow,
            Remark = "系统初始化默认通知"
        };
    }
}
