// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    private readonly ITaskRepository _taskRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskSchedulerQueryService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysTask>> GetEnabledTasksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _taskRepository.GetListAsync(task => task.Status == EnableStatus.Enabled, cancellationToken);
    }
}
