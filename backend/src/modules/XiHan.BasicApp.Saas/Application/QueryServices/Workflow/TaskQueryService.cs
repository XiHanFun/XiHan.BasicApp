#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskQueryService
// Guid:bc595df2-23c0-4eaa-9bc1-543fb448929e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统任务查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统任务")]
public sealed class TaskQueryService(ITaskRepository taskRepository)
    : SaasApplicationService, ITaskQueryService
{
    /// <summary>
    /// 系统任务仓储
    /// </summary>
    private readonly ITaskRepository _taskRepository = taskRepository;

    /// <summary>
    /// 获取系统任务分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统任务分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Task.Read)]
    public async Task<PageResultDtoBase<TaskListItemDto>> GetTaskPageAsync(TaskPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTaskPageRequest(input);
        var tasks = await _taskRepository.GetPagedAsync(request, cancellationToken);
        return tasks.Map(TaskApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取系统任务详情
    /// </summary>
    /// <param name="id">系统任务主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统任务详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Task.Read)]
    public async Task<TaskDetailDto?> GetTaskDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统任务主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task is null ? null : TaskApplicationMapper.ToDetailDto(task);
    }

    /// <summary>
    /// 构建系统任务分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统任务分页请求</returns>
    private static BasicAppPRDto BuildTaskPageRequest(TaskPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysTask.TaskCode),
                nameof(SysTask.TaskName),
                nameof(SysTask.TaskDescription),
                nameof(SysTask.TaskGroup));
        }

        if (!string.IsNullOrWhiteSpace(input.TaskCode))
        {
            request.Conditions.AddFilter(nameof(SysTask.TaskCode), input.TaskCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.TaskGroup))
        {
            request.Conditions.AddFilter(nameof(SysTask.TaskGroup), input.TaskGroup.Trim());
        }

        if (input.TriggerType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTask.TriggerType), input.TriggerType.Value);
        }

        if (input.RunTaskStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTask.RunTaskStatus), input.RunTaskStatus.Value);
        }

        if (input.AllowConcurrent.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTask.AllowConcurrent), input.AllowConcurrent.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysTask.Status), input.Status.Value);
        }

        AddTimeRange(request, nameof(SysTask.NextRunTime), input.NextRunTimeStart, input.NextRunTimeEnd);
        AddTimeRange(request, nameof(SysTask.LastRunTime), input.LastRunTimeStart, input.LastRunTimeEnd);
        request.Conditions.AddSort(nameof(SysTask.Priority), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysTask.NextRunTime), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysTask.CreatedTime), SortDirection.Descending, 2);
        return request;
    }

    /// <summary>
    /// 添加时间范围筛选
    /// </summary>
    private static void AddTimeRange(BasicAppPRDto request, string fieldName, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue)
        {
            request.Conditions.AddFilter(fieldName, start.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (end.HasValue)
        {
            request.Conditions.AddFilter(fieldName, end.Value, QueryOperator.LessThanOrEqual);
        }
    }
}
