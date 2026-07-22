// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统任务命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统任务")]
public sealed class TaskAppService : SaasApplicationService, ITaskAppService
{
    private readonly ITaskDomainService _taskDomainService;
    private readonly ITaskSchedulerSyncService _taskSchedulerSyncService;
    private readonly ITaskRepository _taskRepository;
    private readonly IJobScheduler _jobScheduler;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskAppService(
        ITaskDomainService taskDomainService,
        ITaskSchedulerSyncService taskSchedulerSyncService,
        ITaskRepository taskRepository,
        IJobScheduler jobScheduler)
    {
        _taskDomainService = taskDomainService;
        _taskSchedulerSyncService = taskSchedulerSyncService;
        _taskRepository = taskRepository;
        _jobScheduler = jobScheduler;
    }

    /// <summary>
    /// 创建系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Create)]
    public async Task<TaskDetailDto> CreateTaskAsync(TaskCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _taskDomainService.CreateTaskAsync(TaskApplicationMapper.ToCreateCommand(input), cancellationToken);
        _taskSchedulerSyncService.Apply(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
    }

    /// <summary>
    /// 删除系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Delete)]
    public async Task DeleteTaskAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _taskDomainService.DeleteTaskAsync(id, cancellationToken);
        _taskSchedulerSyncService.Apply(result.Task, result.SchedulerSyncAction);
    }

    /// <summary>
    /// 同步所有活跃的 SysTask 到框架调度器（用于应用启动时初始化）
    /// </summary>
    public async Task SyncAllActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        await _taskSchedulerSyncService.SyncAllActiveJobsAsync(cancellationToken);
    }

    /// <summary>
    /// 更新系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Update)]
    public async Task<TaskDetailDto> UpdateTaskAsync(TaskUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _taskDomainService.UpdateTaskAsync(TaskApplicationMapper.ToUpdateCommand(input), cancellationToken);
        _taskSchedulerSyncService.Apply(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
    }

    /// <summary>
    /// 更新系统任务运行状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.RunStatus)]
    public async Task<TaskDetailDto> UpdateTaskRunStatusAsync(TaskRunStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _taskDomainService.UpdateTaskRunStatusAsync(TaskApplicationMapper.ToRunStatusCommand(input), cancellationToken);
        _taskSchedulerSyncService.Apply(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
    }

    /// <summary>
    /// 更新系统任务启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Status)]
    public async Task<TaskDetailDto> UpdateTaskStatusAsync(TaskStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _taskDomainService.UpdateTaskStatusAsync(TaskApplicationMapper.ToStatusCommand(input), cancellationToken);
        _taskSchedulerSyncService.Apply(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
    }

    /// <summary>
    /// 立即执行一次系统任务（不影响既有调度计划，执行结果见任务日志）
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Task.RunStatus)]
    public async Task<TaskRunResultDto> RunTaskAsync(TaskRunDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new UserFriendlyException("任务主键无效。");
        }

        var task = await _taskRepository.GetByIdAsync(input.BasicId, cancellationToken)
            ?? throw new UserFriendlyException("任务不存在。");
        if (task.Status != EnableStatus.Enabled)
        {
            throw new UserFriendlyException("任务已禁用，请先启用后再执行。");
        }

        // 调度器未注册（如重启后异常/刚启用）时先补注册，保证手动触发可用
        if (_jobScheduler.GetAllJobs().All(job => job.JobName != task.TaskCode))
        {
            _taskSchedulerSyncService.Apply(task, TaskSchedulerSyncAction.Register);
        }

        var instanceId = await _jobScheduler.TriggerJobAsync(task.TaskCode);
        return new TaskRunResultDto { InstanceId = instanceId };
    }
}
