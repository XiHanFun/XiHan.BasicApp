// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.Mappers;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;
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
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly IWorkflowDefinitionStore _definitionStore;
    private readonly WorkflowBusinessRegistry _businessRegistry;
    private readonly WorkflowUserNameResolver _userNameResolver;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowTodoQueryService(
        IWorkflowUserTaskService userTaskService,
        IWorkflowInstanceStore instanceStore,
        IWorkflowDefinitionStore definitionStore,
        WorkflowBusinessRegistry businessRegistry,
        WorkflowUserNameResolver userNameResolver,
        ICurrentUser currentUser)
    {
        _userTaskService = userTaskService;
        _instanceStore = instanceStore;
        _definitionStore = definitionStore;
        _businessRegistry = businessRegistry;
        _userNameResolver = userNameResolver;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 分页查询当前用户待办（受理人归属服务端锁定为当前用户）
    /// </summary>
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
        var pageTasks = ordered
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var items = await EnrichAsync(pageTasks, cancellationToken);

        return new PageResultDtoBase<WorkflowTodoListItemDto>(items, pageIndex, pageSize, ordered.Count);
    }
    /// <summary>
    /// 补齐节点名称、发起人与业务名称（只对当前页的任务查询）
    /// </summary>
    private async Task<List<WorkflowTodoListItemDto>> EnrichAsync(IReadOnlyList<WorkflowUserTask> tasks, CancellationToken cancellationToken)
    {
        var instances = new Dictionary<string, WorkflowInstance?>(StringComparer.Ordinal);
        foreach (var instanceId in tasks.Select(task => task.InstanceId).Distinct(StringComparer.Ordinal))
        {
            instances[instanceId] = await _instanceStore.FindAsync(instanceId, cancellationToken);
        }

        var starterNames = await _userNameResolver.ResolveAsync(instances.Values.Select(instance => instance?.StarterId), cancellationToken);
        var businessNames = _businessRegistry.GetDescriptors().ToDictionary(descriptor => descriptor.Code, descriptor => descriptor.Name, StringComparer.Ordinal);
        var nodeNames = new WorkflowNodeNameCache(_definitionStore);

        var items = new List<WorkflowTodoListItemDto>(tasks.Count);
        foreach (var task in tasks)
        {
            var item = WorkflowApplicationMapper.ToTodoListItemDto(task);
            var instance = instances.GetValueOrDefault(task.InstanceId);
            if (instance is not null)
            {
                item.NodeName = await nodeNames.GetAsync(WorkflowStoreMapper.ParseId(instance.DefinitionId), task.NodeId, cancellationToken);
                item.StarterId = instance.StarterId;
                item.StarterName = instance.StarterId is null ? null : starterNames.GetValueOrDefault(instance.StarterId);
                item.BusinessName = string.IsNullOrEmpty(instance.ParentInstanceId) ? businessNames.GetValueOrDefault(instance.DefinitionCode) : null;
            }

            items.Add(item);
        }

        return items;
    }
}
