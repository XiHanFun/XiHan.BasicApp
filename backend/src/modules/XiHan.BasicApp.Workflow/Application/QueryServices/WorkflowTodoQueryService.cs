// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.Mappers;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.UserTasks;

namespace XiHan.BasicApp.Workflow.Application.QueryServices;

/// <summary>
/// 我的待办查询应用服务
/// </summary>
/// <remarks>
/// 受理人服务端锁定为当前登录用户；单人待办量有限，关键字过滤与分页在内存完成。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "我的待办")]
public sealed class WorkflowTodoQueryService : WorkflowApplicationService, IWorkflowTodoQueryService
{
    private readonly IWorkflowUserTaskService _userTaskService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowTodoQueryService(IWorkflowUserTaskService userTaskService, ICurrentUser currentUser)
    {
        _userTaskService = userTaskService;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    [HttpPost]
    public async Task<PageResultDtoBase<WorkflowTodoListItemDto>> GetPageAsync(WorkflowTodoPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = _currentUser.UserId
            ?? throw new BusinessException(message: "当前用户未登录，无法查询待办");

        var tasks = await _userTaskService.GetPendingAsync(userId.ToString(CultureInfo.InvariantCulture), cancellationToken);

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            tasks = [.. tasks.Where(task =>
                task.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || task.InstanceName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || task.DefinitionCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (task.CorrelationId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))];
        }

        var ordered = tasks.OrderByDescending(task => task.CreationTime).ToList();
        var pageIndex = Math.Max(1, input.Page.PageIndex);
        var pageSize = Math.Clamp(input.Page.PageSize, 1, 500);
        var items = ordered
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(WorkflowApplicationMapper.ToTodoListItemDto)
            .ToList();

        return new PageResultDtoBase<WorkflowTodoListItemDto>(items, pageIndex, pageSize, ordered.Count);
    }
}
