// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.UserTasks;

namespace XiHan.BasicApp.Workflow.Application.AppServices;

/// <summary>
/// 我的待办命令应用服务
/// </summary>
/// <remarks>
/// 办理人服务端锁定为当前登录用户；受理人归属与转办/加签合法性由框架任务服务在实例锁内校验。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "我的待办")]
public sealed class WorkflowTodoAppService : WorkflowApplicationService, IWorkflowTodoAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkflowUserTaskService _userTaskService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowTodoAppService(IWorkflowUserTaskService userTaskService, ICurrentUser currentUser)
    {
        _userTaskService = userTaskService;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<WorkflowTodoCompleteResultDto> CompleteAsync(WorkflowTodoCompleteDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Outcome);

        var actorId = RequireCurrentUserId();
        var variables = ParseJsonObject(input.VariablesJson);

        try
        {
            var instance = await _userTaskService.CompleteAsync(
                input.TaskId.Trim(), actorId, input.Outcome.Trim(), input.Comment, variables, cancellationToken);
            return new WorkflowTodoCompleteResultDto
            {
                InstanceId = instance.Id,
                InstanceStatus = instance.Status
            };
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task TransferAsync(WorkflowTodoTransferDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TargetAssigneeId);

        var actorId = RequireCurrentUserId();

        try
        {
            await _userTaskService.TransferAsync(
                input.TaskId.Trim(), actorId, input.TargetAssigneeId.Trim(), input.Comment, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task AddAssigneesAsync(WorkflowTodoAddAssigneesDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TaskId);
        if (input.AssigneeIds.Count == 0)
        {
            throw new BusinessException(message: "加签受理人不能为空");
        }

        var actorId = RequireCurrentUserId();

        try
        {
            await _userTaskService.AddAssigneesAsync(
                input.TaskId.Trim(), actorId, input.AssigneeIds, input.Comment, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }
    }

    private string RequireCurrentUserId()
    {
        return _currentUser.UserId?.ToString(CultureInfo.InvariantCulture)
            ?? throw new BusinessException(message: "当前用户未登录，无法办理待办");
    }

    private static Dictionary<string, object?>? ParseJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new BusinessException(message: $"附加变量 JSON 非法：{ex.Message}");
        }
    }
}
