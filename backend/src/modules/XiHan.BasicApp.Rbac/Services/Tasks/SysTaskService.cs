#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskService
// Guid:d1e2f3g4-h5i6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Tasks;
using XiHan.BasicApp.Rbac.Services.Tasks.Dtos;
using XiHan.Framework.Application.Services;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Services.Tasks;

/// <summary>
/// 系统任务服务实现
/// </summary>
public class SysTaskService : CrudApplicationServiceBase<SysTask, TaskDto, XiHanBasicAppIdType, CreateTaskDto, UpdateTaskDto>, ISysTaskService
{
    private readonly ISysTaskRepository _taskRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTaskService(ISysTaskRepository taskRepository) : base(taskRepository)
    {
        _taskRepository = taskRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    public async Task<TaskDto?> GetByTaskCodeAsync(string taskCode)
    {
        var task = await _taskRepository.GetByTaskCodeAsync(taskCode);
        return task?.ToDto();
    }

    /// <summary>
    /// 根据任务状态获取任务列表
    /// </summary>
    public async Task<List<TaskDto>> GetByStatusAsync(TaskStatus taskStatus)
    {
        var tasks = await _taskRepository.GetByStatusAsync(taskStatus);
        return tasks.ToDto();
    }

    /// <summary>
    /// 获取待执行的任务列表
    /// </summary>
    public async Task<List<TaskDto>> GetPendingTasksAsync(int count = 100)
    {
        var tasks = await _taskRepository.GetPendingTasksAsync(count);
        return tasks.ToDto();
    }

    /// <summary>
    /// 根据任务分组获取任务列表
    /// </summary>
    public async Task<List<TaskDto>> GetByTaskGroupAsync(string taskGroup)
    {
        var tasks = await _taskRepository.GetByTaskGroupAsync(taskGroup);
        return tasks.ToDto();
    }

    /// <summary>
    /// 检查任务编码是否存在
    /// </summary>
    public async Task<bool> ExistsByTaskCodeAsync(string taskCode, XiHanBasicAppIdType? excludeId = null)
    {
        return await _taskRepository.ExistsByTaskCodeAsync(taskCode, excludeId);
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<TaskDto> MapToEntityDtoAsync(SysTask entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 TaskDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysTask> MapToEntityAsync(TaskDto dto)
    {
        var entity = new SysTask
        {
            TenantId = dto.TenantId,
            TaskCode = dto.TaskCode,
            TaskName = dto.TaskName,
            TaskDescription = dto.TaskDescription,
            TaskGroup = dto.TaskGroup,
            TaskClass = dto.TaskClass,
            TaskMethod = dto.TaskMethod,
            TaskParams = dto.TaskParams,
            TriggerType = dto.TriggerType,
            CronExpression = dto.CronExpression,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            NextRunTime = dto.NextRunTime,
            LastRunTime = dto.LastRunTime,
            IntervalSeconds = dto.IntervalSeconds,
            RepeatCount = dto.RepeatCount,
            ExecutedCount = dto.ExecutedCount,
            TimeoutSeconds = dto.TimeoutSeconds,
            TaskStatus = dto.TaskStatus,
            Priority = dto.Priority,
            AllowConcurrent = dto.AllowConcurrent,
            RetryCount = dto.RetryCount,
            MaxRetryCount = dto.MaxRetryCount,
            Status = dto.Status,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 TaskDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(TaskDto dto, SysTask entity)
    {
        entity.TaskName = dto.TaskName;
        entity.TaskDescription = dto.TaskDescription;
        entity.TaskGroup = dto.TaskGroup;
        entity.TaskClass = dto.TaskClass;
        entity.TaskMethod = dto.TaskMethod;
        entity.TaskParams = dto.TaskParams;
        entity.TriggerType = dto.TriggerType;
        entity.CronExpression = dto.CronExpression;
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        entity.NextRunTime = dto.NextRunTime;
        entity.LastRunTime = dto.LastRunTime;
        entity.IntervalSeconds = dto.IntervalSeconds;
        entity.RepeatCount = dto.RepeatCount;
        entity.ExecutedCount = dto.ExecutedCount;
        entity.TimeoutSeconds = dto.TimeoutSeconds;
        entity.TaskStatus = dto.TaskStatus;
        entity.Priority = dto.Priority;
        entity.AllowConcurrent = dto.AllowConcurrent;
        entity.RetryCount = dto.RetryCount;
        entity.MaxRetryCount = dto.MaxRetryCount;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysTask> MapToEntityAsync(CreateTaskDto createDto)
    {
        var entity = new SysTask
        {
            TenantId = createDto.TenantId,
            TaskCode = createDto.TaskCode,
            TaskName = createDto.TaskName,
            TaskDescription = createDto.TaskDescription,
            TaskGroup = createDto.TaskGroup,
            TaskClass = createDto.TaskClass,
            TaskMethod = createDto.TaskMethod,
            TaskParams = createDto.TaskParams,
            TriggerType = createDto.TriggerType,
            CronExpression = createDto.CronExpression,
            StartTime = createDto.StartTime,
            EndTime = createDto.EndTime,
            IntervalSeconds = createDto.IntervalSeconds,
            RepeatCount = createDto.RepeatCount,
            TimeoutSeconds = createDto.TimeoutSeconds,
            Priority = createDto.Priority,
            AllowConcurrent = createDto.AllowConcurrent,
            MaxRetryCount = createDto.MaxRetryCount,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateTaskDto updateDto, SysTask entity)
    {
        if (updateDto.TaskName != null) entity.TaskName = updateDto.TaskName;
        if (updateDto.TaskDescription != null) entity.TaskDescription = updateDto.TaskDescription;
        if (updateDto.TaskGroup != null) entity.TaskGroup = updateDto.TaskGroup;
        if (updateDto.TaskClass != null) entity.TaskClass = updateDto.TaskClass;
        if (updateDto.TaskMethod != null) entity.TaskMethod = updateDto.TaskMethod;
        if (updateDto.TaskParams != null) entity.TaskParams = updateDto.TaskParams;
        if (updateDto.TriggerType.HasValue) entity.TriggerType = updateDto.TriggerType.Value;
        if (updateDto.CronExpression != null) entity.CronExpression = updateDto.CronExpression;
        if (updateDto.StartTime.HasValue) entity.StartTime = updateDto.StartTime;
        if (updateDto.EndTime.HasValue) entity.EndTime = updateDto.EndTime;
        if (updateDto.IntervalSeconds.HasValue) entity.IntervalSeconds = updateDto.IntervalSeconds;
        if (updateDto.RepeatCount.HasValue) entity.RepeatCount = updateDto.RepeatCount.Value;
        if (updateDto.TimeoutSeconds.HasValue) entity.TimeoutSeconds = updateDto.TimeoutSeconds.Value;
        if (updateDto.TaskStatus.HasValue) entity.TaskStatus = updateDto.TaskStatus.Value;
        if (updateDto.Priority.HasValue) entity.Priority = updateDto.Priority.Value;
        if (updateDto.AllowConcurrent.HasValue) entity.AllowConcurrent = updateDto.AllowConcurrent.Value;
        if (updateDto.MaxRetryCount.HasValue) entity.MaxRetryCount = updateDto.MaxRetryCount.Value;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
