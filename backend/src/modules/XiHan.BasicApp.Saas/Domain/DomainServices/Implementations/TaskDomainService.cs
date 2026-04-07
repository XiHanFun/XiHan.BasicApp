#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskDomainService
// Guid:4a5b6c7d-8e9f-4123-def0-440000000004
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 任务领域服务
/// </summary>
public class TaskDomainService : ITaskDomainService, ITransientDependency
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskDomainService(ITaskRepository taskRepository, ILocalEventBus localEventBus)
    {
        _taskRepository = taskRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysTask> CreateAsync(SysTask task)
    {
        var created = await _taskRepository.AddAsync(task);
        await _localEventBus.PublishAsync(new TaskChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysTask> UpdateAsync(SysTask task)
    {
        var updated = await _taskRepository.UpdateAsync(task);
        await _localEventBus.PublishAsync(new TaskChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null) return false;

        var result = await _taskRepository.DeleteAsync(task);
        if (result)
        {
            await _localEventBus.PublishAsync(new TaskChangedDomainEvent(id));
        }
        return result;
    }
}
