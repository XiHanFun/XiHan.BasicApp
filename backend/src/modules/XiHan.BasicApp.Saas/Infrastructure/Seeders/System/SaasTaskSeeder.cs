// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

        // 用户统计聚合
        await SeedTaskAsync(new SysTask
        {
            TenantId = 0,
            TaskCode = "user-statistics-aggregation",
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
        });

        // 日志保留清理
        await SeedTaskAsync(new SysTask
        {
            TenantId = 0,
            TaskCode = "log-retention-cleanup",
            TaskName = "日志保留清理",
            TaskDescription = "按保留期（全局配置 saas:log:retention-days，默认 180 天）删除访问/操作/异常/登录/差异/开放接口/权限变更 7 类按月分表日志的过期行，防止分月表无限增长",
            TaskGroup = "system",
            TaskClass = typeof(LogRetentionCleanupTask).FullName!,
            TaskMethod = nameof(LogRetentionCleanupTask.ExecuteAsync),
            TriggerType = TriggerType.Cron,
            CronExpression = "30 3 * * *",
            TimeoutSeconds = 1800,
            AllowConcurrent = false,
            MaxRetryCount = 1,
            Status = EnableStatus.Enabled,
            Remark = "系统初始化内建任务"
        });
    }

    /// <summary>
    /// 落地单个内建任务（已存在不覆盖：Cron/启停等允许运营调整）
    /// </summary>
    private async Task SeedTaskAsync(SysTask task)
    {
        var existing = await DbClient.Queryable<SysTask>()
            .FirstAsync(item => item.TaskCode == task.TaskCode);
        if (existing is not null)
        {
            Logger.LogInformation("内建定时任务 {TaskCode} 已存在，跳过种子数据", task.TaskCode);
            return;
        }

        _ = await DbClient.Insertable(task).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化内建定时任务 {TaskCode}", task.TaskCode);
    }
}
