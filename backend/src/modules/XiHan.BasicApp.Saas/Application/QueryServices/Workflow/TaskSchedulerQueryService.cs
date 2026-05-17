#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskSchedulerQueryService
// Guid:442bb0c8-3570-4718-b32f-9eff5a6556d1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务调度查询服务实现
/// </summary>
public sealed class TaskSchedulerQueryService
    : ITaskSchedulerQueryService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskSchedulerQueryService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    private readonly ITaskRepository _taskRepository;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysTask>> GetEnabledTasksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _taskRepository.GetListAsync(task => task.Status == EnableStatus.Enabled, cancellationToken);
    }
}
