#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskAppService
// Guid:2a47620b-74e8-4f4e-bcad-7a0e7f2db207
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
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

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskAppService(
        ITaskDomainService taskDomainService,
        ITaskSchedulerSyncService taskSchedulerSyncService)
    {
        _taskDomainService = taskDomainService;
        _taskSchedulerSyncService = taskSchedulerSyncService;
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

}
