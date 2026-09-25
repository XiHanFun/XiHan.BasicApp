// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Infrastructure.Tasks;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders;

/// <summary>
/// 聊天内建定时任务：聊天消息保留清理
/// </summary>
public sealed class ChatTaskSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<ChatTaskSeeder> logger,
    IServiceProvider serviceProvider)
    : TaskSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData + 41;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]内建定时任务";

    /// <summary>
    /// 本模块的内建任务
    /// </summary>
    public override IReadOnlyList<SysTask> Tasks =>
    [
        new()
        {
            TaskCode = "chat-retention-cleanup",
            TaskName = "聊天消息保留清理",
            TaskDescription = "按保留期（聊天策略 chat.policy 的 retentionDays，默认 365 天）跨租户物理删除过期聊天消息，防止消息表无限增长",
            TaskGroup = "chat",
            TaskClass = typeof(ChatRetentionCleanupTask).FullName!,
            TaskMethod = nameof(ChatRetentionCleanupTask.ExecuteAsync),
            TriggerType = TriggerType.Cron,
            CronExpression = "0 4 * * *",
            TimeoutSeconds = 1800,
            AllowConcurrent = false,
            MaxRetryCount = 1,
            Status = EnableStatus.Enabled
        }
    ];
}
