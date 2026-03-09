#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskAppService
// Guid:01199742-9462-4057-869d-59f41d37d1ad
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:53:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 任务应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class TaskAppService
    : CrudApplicationServiceBase<SysTask, TaskDto, long, TaskCreateDto, TaskUpdateDto, BasicAppPRDto>,
        ITaskAppService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IRbacLookupCacheService _lookupCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskAppService(ITaskRepository taskRepository, IRbacLookupCacheService lookupCacheService)
        : base(taskRepository)
    {
        _taskRepository = taskRepository;
        _lookupCacheService = lookupCacheService;
    }

    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    public async Task<TaskDto?> GetByTaskCodeAsync(string taskCode, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskCode);
        var normalizedCode = taskCode.Trim();

        return await _lookupCacheService.GetTaskByCodeAsync(
            normalizedCode,
            tenantId,
            async token =>
            {
                var entity = await _taskRepository.GetByTaskCodeAsync(normalizedCode, tenantId, token);
                return entity?.Adapt<TaskDto>();
            });
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    public override async Task<TaskDto> CreateAsync(TaskCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.TaskCode.Trim();
        var exists = await _taskRepository.IsTaskCodeExistsAsync(normalizedCode, input.TenantId);
        if (exists)
        {
            throw new BusinessException(message: $"任务编码 '{normalizedCode}' 已存在");
        }

        var dto = await base.CreateAsync(input);
        await _lookupCacheService.InvalidateTaskLookupAsync(input.TenantId);
        return dto;
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    public override async Task<TaskDto> UpdateAsync(long id, TaskUpdateDto input)
    {
        input.ValidateAnnotations();
        var entity = await _taskRepository.GetByIdAsync(id)
                     ?? throw new KeyNotFoundException($"未找到任务: {id}");
        var dto = await base.UpdateAsync(id, input);
        await _lookupCacheService.InvalidateTaskLookupAsync(entity.TenantId);
        return dto;
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override async Task<bool> DeleteAsync(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("任务 ID 无效", nameof(id));
        }

        var entity = await _taskRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        var deleted = await base.DeleteAsync(id);
        if (deleted)
        {
            await _lookupCacheService.InvalidateTaskLookupAsync(entity.TenantId);
        }

        return deleted;
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysTask> MapDtoToEntityAsync(TaskCreateDto createDto)
    {
        var entity = new SysTask
        {
            TenantId = createDto.TenantId,
            TaskCode = createDto.TaskCode.Trim(),
            TaskName = createDto.TaskName.Trim(),
            TaskDescription = createDto.TaskDescription,
            TaskGroup = createDto.TaskGroup,
            TaskClass = createDto.TaskClass.Trim(),
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
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(TaskUpdateDto updateDto, SysTask entity)
    {
        entity.TaskName = updateDto.TaskName.Trim();
        entity.TaskDescription = updateDto.TaskDescription;
        entity.TaskGroup = updateDto.TaskGroup;
        entity.TaskClass = updateDto.TaskClass.Trim();
        entity.TaskMethod = updateDto.TaskMethod;
        entity.TaskParams = updateDto.TaskParams;
        entity.TriggerType = updateDto.TriggerType;
        entity.CronExpression = updateDto.CronExpression;
        entity.StartTime = updateDto.StartTime;
        entity.EndTime = updateDto.EndTime;
        entity.NextRunTime = updateDto.NextRunTime;
        entity.IntervalSeconds = updateDto.IntervalSeconds;
        entity.RepeatCount = updateDto.RepeatCount;
        entity.TimeoutSeconds = updateDto.TimeoutSeconds;
        entity.RunTaskStatus = updateDto.RunTaskStatus;
        entity.Priority = updateDto.Priority;
        entity.AllowConcurrent = updateDto.AllowConcurrent;
        entity.MaxRetryCount = updateDto.MaxRetryCount;
        entity.Status = updateDto.Status;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }
}
