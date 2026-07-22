// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 任务调度器同步服务
/// </summary>
public interface ITaskSchedulerSyncService
{
    /// <summary>
    /// 应用单个任务调度同步动作
    /// </summary>
    void Apply(SysTask task, TaskSchedulerSyncAction syncAction);

    /// <summary>
    /// 同步所有启用任务到调度器
    /// </summary>
    Task SyncAllActiveJobsAsync(CancellationToken cancellationToken = default);
}
