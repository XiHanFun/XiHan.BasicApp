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
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 任务应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class TaskAppService
    : CrudApplicationServiceBase<SysTask, TaskDto, long, TaskCreateDto, TaskUpdateDto, BasicAppPRDto>,
        ITaskAppService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskQueryService _queryService;
    private readonly ITaskDomainService _domainService;
    private readonly IRbacLookupCacheService _lookupCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskAppService(
        ITaskRepository taskRepository,
        ITaskQueryService queryService,
        ITaskDomainService domainService,
        IRbacLookupCacheService lookupCacheService)
        : base(taskRepository)
    {
        _taskRepository = taskRepository;
        _queryService = queryService;
        _domainService = domainService;
        _lookupCacheService = lookupCacheService;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    public override async Task<TaskDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
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
    /// 创建任务（委托 DomainService）
    /// </summary>
    public override async Task<TaskDto> CreateAsync(TaskCreateDto input)
    {
        input.ValidateAnnotations();
        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        return created.Adapt<TaskDto>()!;
    }

    /// <summary>
    /// 更新任务（委托 DomainService）
    /// </summary>
    public override async Task<TaskDto> UpdateAsync(TaskUpdateDto input)
    {
        input.ValidateAnnotations();
        var entity = await Repository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到任务: {input.BasicId}");
        await MapDtoToEntityAsync(input, entity);
        var updated = await _domainService.UpdateAsync(entity);
        return updated.Adapt<TaskDto>()!;
    }

    /// <summary>
    /// 删除任务（委托 DomainService）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        return await _domainService.DeleteAsync(id);
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
