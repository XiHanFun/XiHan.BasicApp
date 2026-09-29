// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 内建定时任务：用户统计聚合、日志保留清理
/// </summary>
public sealed class SaasTaskSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasTaskSeeder> logger,
    IServiceProvider serviceProvider)
    : TaskSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData + 4;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]内建定时任务";

    /// <summary>
    /// 本模块的内建任务
    /// </summary>
    public override IReadOnlyList<SysTask> Tasks =>
    [
        new()
        {
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
            Status = EnableStatus.Enabled
        },
        new()
        {
            TaskCode = "log-retention-cleanup",
            TaskName = "日志保留清理",
            TaskDescription = "按保留期（参数 saas.log.retention-days，默认 180 天）删除访问/操作/异常/登录/差异/开放接口/权限变更 7 类按月分表日志的过期行，防止分月表无限增长",
            TaskGroup = "system",
            TaskClass = typeof(LogRetentionCleanupTask).FullName!,
            TaskMethod = nameof(LogRetentionCleanupTask.ExecuteAsync),
            TriggerType = TriggerType.Cron,
            CronExpression = "30 3 * * *",
            TimeoutSeconds = 1800,
            AllowConcurrent = false,
            MaxRetryCount = 1,
            Status = EnableStatus.Enabled
        }
    ];
}
