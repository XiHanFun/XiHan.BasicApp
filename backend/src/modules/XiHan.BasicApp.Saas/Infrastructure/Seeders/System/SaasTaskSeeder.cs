#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasTaskSeeder
// Guid:a2c8f5d1-4b97-4e63-8a0d-7f3b6c9e2d54
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 内建定时任务种子数据
/// </summary>
/// <remarks>
/// 落地系统自带的后台任务（如用户统计聚合）；模块启动时 TaskSchedulerSync 会把启用任务注册进调度器。
/// 已存在不覆盖：Cron/启停等允许运营调整，种子只负责首次落地。
/// </remarks>
public sealed class SaasTaskSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasTaskSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级（晚于配置/模板，无依赖）
    /// </summary>
    public override int Order => 29;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]内建定时任务种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;

        const string taskCode = "user-statistics-aggregation";
        var existing = await client.Queryable<SysTask>()
            .FirstAsync(task => task.TaskCode == taskCode);
        if (existing is not null)
        {
            Logger.LogInformation("内建定时任务 {TaskCode} 已存在，跳过种子数据", taskCode);
            return;
        }

        var task = new SysTask
        {
            TenantId = 0,
            TaskCode = taskCode,
            TaskName = "用户统计聚合",
            TaskDescription = "按今日/本周/本月周期把登录、访问、操作日志与会话在线时长聚合为用户统计快照（个人中心数据统计/工作台数据源）",
            TaskGroup = "system",
            TaskClass = typeof(UserStatisticsAggregationTask).FullName!,
            TaskMethod = nameof(UserStatisticsAggregationTask.ExecuteAsync),
            TriggerType = TriggerType.Cron,
            CronExpression = "*/10 * * * *",
            TimeoutSeconds = 600,
            AllowConcurrent = false,
            MaxRetryCount = 3,
            Status = EnableStatus.Enabled,
            Remark = "系统初始化内建任务"
        };

        _ = await client.Insertable(task).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化内建定时任务 {TaskCode}", taskCode);
    }
}
