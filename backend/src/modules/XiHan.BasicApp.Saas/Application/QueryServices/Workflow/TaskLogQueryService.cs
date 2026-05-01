#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogQueryService
// Guid:d23d62ce-586a-4ee6-a0a0-5587733bce5c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务日志查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "任务日志")]
public sealed class TaskLogQueryService(ITaskLogRepository taskLogRepository)
    : SaasApplicationService, ITaskLogQueryService
{
    /// <summary>
    /// 任务日志仓储
    /// </summary>
    private readonly ITaskLogRepository _taskLogRepository = taskLogRepository;

    /// <summary>
    /// 获取任务日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务日志分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TaskLog.Read)]
    public async Task<PageResultDtoBase<TaskLogListItemDto>> GetTaskLogPageAsync(TaskLogPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var beginTime = input.StartTimeStart!.Value;
        var endTime = input.StartTimeEnd!.Value;
        var predicate = BuildTaskLogPredicate(input, beginTime, endTime);
        var taskLogPage = await _taskLogRepository.GetPagedByTimeRangeAsync(
            input.Page.PageIndex,
            input.Page.PageSize,
            beginTime,
            endTime,
            predicate,
            taskLog => taskLog.StartTime,
            false,
            cancellationToken);

        if (taskLogPage.Items.Count == 0)
        {
            return new PageResultDtoBase<TaskLogListItemDto>([], taskLogPage.Page);
        }

        var items = taskLogPage.Items
            .Select(TaskLogApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<TaskLogListItemDto>(items, taskLogPage.Page);
    }

    /// <summary>
    /// 获取任务日志详情
    /// </summary>
    /// <param name="id">任务日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务日志详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.TaskLog.Read)]
    public async Task<TaskLogDetailDto?> GetTaskLogDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "任务日志主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var taskLog = await _taskLogRepository.GetByIdAsync(id, cancellationToken);
        return taskLog is null ? null : TaskLogApplicationMapper.ToDetailDto(taskLog);
    }

    /// <summary>
    /// 构建任务日志查询表达式
    /// </summary>
    private static Expression<Func<SysTaskLog, bool>> BuildTaskLogPredicate(TaskLogPageQueryDto input, DateTimeOffset beginTime, DateTimeOffset endTime)
    {
        Expression<Func<SysTaskLog, bool>> predicate = taskLog => taskLog.StartTime >= beginTime && taskLog.StartTime <= endTime;

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            predicate = And(predicate, taskLog =>
                taskLog.TaskCode.Contains(keyword) ||
                taskLog.TaskName.Contains(keyword) ||
                (taskLog.BatchNumber != null && taskLog.BatchNumber.Contains(keyword)) ||
                (taskLog.TriggerMode != null && taskLog.TriggerMode.Contains(keyword)));
        }

        if (input.TaskId.HasValue)
        {
            var taskId = input.TaskId.Value;
            predicate = And(predicate, taskLog => taskLog.TaskId == taskId);
        }

        if (!string.IsNullOrWhiteSpace(input.TaskCode))
        {
            var taskCode = input.TaskCode.Trim();
            predicate = And(predicate, taskLog => taskLog.TaskCode == taskCode);
        }

        if (!string.IsNullOrWhiteSpace(input.TaskName))
        {
            var taskName = input.TaskName.Trim();
            predicate = And(predicate, taskLog => taskLog.TaskName.Contains(taskName));
        }

        if (!string.IsNullOrWhiteSpace(input.BatchNumber))
        {
            var batchNumber = input.BatchNumber.Trim();
            predicate = And(predicate, taskLog => taskLog.BatchNumber == batchNumber);
        }

        if (input.TaskStatus.HasValue)
        {
            var taskStatus = input.TaskStatus.Value;
            predicate = And(predicate, taskLog => taskLog.TaskStatus == taskStatus);
        }

        if (!string.IsNullOrWhiteSpace(input.TriggerMode))
        {
            var triggerMode = input.TriggerMode.Trim();
            predicate = And(predicate, taskLog => taskLog.TriggerMode == triggerMode);
        }

        if (input.MinExecutionTime.HasValue)
        {
            var minExecutionTime = input.MinExecutionTime.Value;
            predicate = And(predicate, taskLog => taskLog.ExecutionTime >= minExecutionTime);
        }

        if (input.MaxExecutionTime.HasValue)
        {
            var maxExecutionTime = input.MaxExecutionTime.Value;
            predicate = And(predicate, taskLog => taskLog.ExecutionTime <= maxExecutionTime);
        }

        if (input.MinRetryCount.HasValue)
        {
            var minRetryCount = input.MinRetryCount.Value;
            predicate = And(predicate, taskLog => taskLog.RetryCount >= minRetryCount);
        }

        if (input.MaxRetryCount.HasValue)
        {
            var maxRetryCount = input.MaxRetryCount.Value;
            predicate = And(predicate, taskLog => taskLog.RetryCount <= maxRetryCount);
        }

        return predicate;
    }

    /// <summary>
    /// 合并查询表达式
    /// </summary>
    private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(TaskLogPageQueryDto input)
    {
        if (!input.StartTimeStart.HasValue || !input.StartTimeEnd.HasValue)
        {
            throw new ArgumentException("任务日志分表查询必须提供开始时间起点和开始时间终点。", nameof(input));
        }

        if (input.StartTimeStart.Value > input.StartTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.StartTimeStart), "开始时间起点不能晚于终点。");
        }

        ValidateRequiredId(input.TaskId, nameof(input.TaskId), "任务主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.TaskCode, 100, nameof(input.TaskCode), "任务编码长度不能超过 100。");
        ValidateMaxLength(input.TaskName, 200, nameof(input.TaskName), "任务名称长度不能超过 200。");
        ValidateMaxLength(input.BatchNumber, 50, nameof(input.BatchNumber), "执行批次号长度不能超过 50。");
        ValidateMaxLength(input.TriggerMode, 50, nameof(input.TriggerMode), "触发方式长度不能超过 50。");
        ValidateExecutionTime(input.MinExecutionTime, nameof(input.MinExecutionTime), "最小执行耗时不能小于 0。");
        ValidateExecutionTime(input.MaxExecutionTime, nameof(input.MaxExecutionTime), "最大执行耗时不能小于 0。");
        ValidateRetryCount(input.MinRetryCount, nameof(input.MinRetryCount), "最小重试次数不能小于 0。");
        ValidateRetryCount(input.MaxRetryCount, nameof(input.MaxRetryCount), "最大重试次数不能小于 0。");

        if (input.TaskStatus.HasValue && !Enum.IsDefined(input.TaskStatus.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.TaskStatus), "任务状态无效。");
        }

        if (input.MinExecutionTime.HasValue &&
            input.MaxExecutionTime.HasValue &&
            input.MinExecutionTime.Value > input.MaxExecutionTime.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.MinExecutionTime), "最小执行耗时不能大于最大执行耗时。");
        }

        if (input.MinRetryCount.HasValue &&
            input.MaxRetryCount.HasValue &&
            input.MinRetryCount.Value > input.MaxRetryCount.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.MinRetryCount), "最小重试次数不能大于最大重试次数。");
        }
    }

    /// <summary>
    /// 校验可空主键
    /// </summary>
    private static void ValidateRequiredId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateMaxLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验执行耗时
    /// </summary>
    private static void ValidateExecutionTime(long? executionTime, string paramName, string message)
    {
        if (executionTime is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验重试次数
    /// </summary>
    private static void ValidateRetryCount(int? retryCount, string paramName, string message)
    {
        if (retryCount is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 替换表达式参数
    /// </summary>
    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : node;
        }
    }
}
