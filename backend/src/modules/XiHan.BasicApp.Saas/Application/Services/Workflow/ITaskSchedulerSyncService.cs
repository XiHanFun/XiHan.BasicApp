#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskSchedulerSyncService
// Guid:6406ca58-0a0e-44f9-b0b1-d6efac595089
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
