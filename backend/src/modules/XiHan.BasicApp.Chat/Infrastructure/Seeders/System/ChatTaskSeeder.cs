// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Infrastructure.Tasks;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders.System;

/// <summary>
/// 聊天模块定时任务种子数据
/// </summary>
public sealed class ChatTaskSeeder : PlatformDataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatTaskSeeder(ISqlSugarClientResolver clientResolver, ILogger<ChatTaskSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 403;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]内建定时任务种子数据";

    /// <summary>
    /// 种子数据实现（已存在不覆盖：Cron/启停等允许运营调整）
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var task = new SysTask
        {
            TenantId = 0,
            TaskCode = "chat-retention-cleanup",
            TaskName = "聊天消息保留清理",
            TaskDescription = "按保留期（全局配置 chat:retention-days，默认 365 天）跨租户物理删除过期聊天消息，防止消息表无限增长",
            TaskGroup = "chat",
            TaskClass = typeof(ChatRetentionCleanupTask).FullName!,
            TaskMethod = nameof(ChatRetentionCleanupTask.ExecuteAsync),
            TriggerType = TriggerType.Cron,
            CronExpression = "0 4 * * *",
            TimeoutSeconds = 1800,
            AllowConcurrent = false,
            MaxRetryCount = 1,
            Status = EnableStatus.Enabled,
            Remark = "系统初始化内建任务"
        };

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
