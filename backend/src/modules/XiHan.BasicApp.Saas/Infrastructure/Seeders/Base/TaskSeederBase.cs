// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 内建定时任务种子基类：按任务编码写入模块自带的后台任务（平台任务，TenantId = 0）
/// </summary>
/// <remarks>
/// 模块启动时调度同步会把启用的任务注册进调度器。Cron、超时、启停归运营：只在首次创建时写入，之后不再覆盖，运营删掉的也不补回。
/// </remarks>
public abstract class TaskSeederBase(
    ISqlSugarClientResolver clientResolver,
    ILogger logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 本模块的内建任务
    /// </summary>
    public abstract IReadOnlyList<SysTask> Tasks { get; }

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var codes = Tasks.Select(static task => task.TaskCode).ToList();
        var existingCodes = (await DbClient.Queryable<SysTask>()
                .IncludingDeleted()
                .Where(task => task.TenantId == 0 && codes.Contains(task.TaskCode))
                .Select(task => task.TaskCode)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var adding = Tasks.Where(task => !existingCodes.Contains(task.TaskCode)).ToList();
        foreach (var task in adding)
        {
            task.TenantId = 0;
            task.Remark = "系统初始化内建任务";
        }

        await BulkInsertAsync(adding);
        Logger.LogInformation("{Seeder}：新增 {AddCount} 个，已有的不覆盖", Name, adding.Count);
    }
}
