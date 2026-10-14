// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;
using XiHan.Framework.Workflow.Abstractions.UserTasks;
using XiHan.Framework.Workflow.UserTasks;

namespace XiHan.BasicApp.Workflow.Application.AppServices;

/// <summary>
/// 我的待办命令应用服务
/// </summary>
/// <remarks>
/// 办理人服务端锁定为当前登录用户；受理人归属与转办/加签合法性由框架任务服务在实例锁内校验。
/// 审批业务流程办理前先回调业务做只读校验（如不能审批自己的申请），校验失败待办保持不变；
/// 每次办理、转办、加签都写一条审批记录，供「我的已办」与审批轨迹查询。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "我的待办")]
public sealed class WorkflowTodoAppService : WorkflowApplicationService, IWorkflowTodoAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkflowUserTaskService _userTaskService;
    private readonly IWorkflowBookmarkStore _bookmarkStore;
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly WorkflowBusinessRegistry _businessRegistry;
    private readonly WorkflowTaskRecordWriter _recordWriter;
    private readonly IUserDirectory _userDirectory;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowTodoAppService(
        IWorkflowUserTaskService userTaskService,
        IWorkflowBookmarkStore bookmarkStore,
        IWorkflowInstanceStore instanceStore,
        WorkflowBusinessRegistry businessRegistry,
        WorkflowTaskRecordWriter recordWriter,
        IUserDirectory userDirectory,
        ICurrentUser currentUser)
    {
        _userTaskService = userTaskService;
        _bookmarkStore = bookmarkStore;
        _instanceStore = instanceStore;
        _businessRegistry = businessRegistry;
        _recordWriter = recordWriter;
        _userDirectory = userDirectory;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 办理待办（同意/拒绝/自定义结果）
    /// </summary>
    public async Task<WorkflowTodoCompleteResultDto> CompleteAsync(WorkflowTodoCompleteDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Outcome);

        var actorId = RequireCurrentUserId();
        var variables = ParseJsonObject(input.VariablesJson);
        var taskId = input.TaskId.Trim();
        var outcome = input.Outcome.Trim();

        var task = await LoadOwnTaskAsync(taskId, actorId, cancellationToken);
        await EnsureBusinessAllowsAsync(task, actorId, outcome, input.Comment, cancellationToken);

        WorkflowInstance instance;
        try
        {
            instance = await _userTaskService.CompleteAsync(taskId, actorId, outcome, input.Comment, variables, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }

        await _recordWriter.WriteAsync(task.Snapshot, [actorId], WorkflowTaskRecordAction.Completed, outcome, input.Comment, cancellationToken: cancellationToken);

        return new WorkflowTodoCompleteResultDto
        {
            InstanceId = instance.Id,
            InstanceStatus = instance.Status,
            FaultMessage = instance.Status == WorkflowInstanceStatus.Faulted ? instance.FaultMessage : null
        };
    }

    /// <summary>
    /// 转办待办（目标须为当前上下文可见的启用用户）
    /// </summary>
    public async Task TransferAsync(WorkflowTodoTransferDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TargetAssigneeId);

        var actorId = RequireCurrentUserId();
        var taskId = input.TaskId.Trim();
        var targetId = input.TargetAssigneeId.Trim();

        var task = await LoadOwnTaskAsync(taskId, actorId, cancellationToken);
        await EnsureAssignableUsersAsync([targetId], cancellationToken);

        try
        {
            await _userTaskService.TransferAsync(taskId, actorId, targetId, input.Comment, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }

        await _recordWriter.WriteAsync(task.Snapshot, [actorId], WorkflowTaskRecordAction.Transferred,
            comment: input.Comment, targetUserIds: [targetId], cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 加签（新增受理人须为当前上下文可见的启用用户）
    /// </summary>
    public async Task AddAssigneesAsync(WorkflowTodoAddAssigneesDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TaskId);
        var assigneeIds = input.AssigneeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (assigneeIds.Count == 0)
        {
            throw new BusinessException(message: "加签受理人不能为空");
        }

        var actorId = RequireCurrentUserId();
        var taskId = input.TaskId.Trim();

        var task = await LoadOwnTaskAsync(taskId, actorId, cancellationToken);
        await EnsureAssignableUsersAsync(assigneeIds, cancellationToken);

        try
        {
            await _userTaskService.AddAssigneesAsync(taskId, actorId, assigneeIds, input.Comment, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }

        await _recordWriter.WriteAsync(task.Snapshot, [actorId], WorkflowTaskRecordAction.AddedAssignees,
            comment: input.Comment, targetUserIds: assigneeIds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 读取当前用户名下的待办（不存在或不属于当前用户时以业务异常拒绝）
    /// </summary>
    private async Task<OwnTask> LoadOwnTaskAsync(string taskId, string actorId, CancellationToken cancellationToken)
    {
        var bookmark = await _bookmarkStore.FindAsync(taskId, cancellationToken);
        if (bookmark is null || bookmark.Kind != WorkflowBookmarkKinds.UserTask)
        {
            throw new BusinessException(message: $"任务 {taskId} 不存在或已被办理");
        }

        if (!string.Equals(bookmark.Key, actorId, StringComparison.Ordinal))
        {
            throw new BusinessException(message: $"您不是任务 {taskId} 的受理人");
        }

        var instance = await _instanceStore.FindAsync(bookmark.InstanceId, cancellationToken)
            ?? throw new BusinessException(message: $"任务 {taskId} 所属的流程实例不存在");

        var userTask = WorkflowUserTaskMapper.ToUserTask(bookmark, instance);
        return new OwnTask(instance, new WorkflowTaskSnapshot(
            ParseLong(userTask.TaskId),
            ParseLong(instance.Id),
            ParseLong(instance.DefinitionId),
            instance.DefinitionCode,
            instance.Name,
            userTask.NodeId,
            userTask.Title,
            userTask.CorrelationId));
    }

    /// <summary>
    /// 审批业务流程：办理前回调业务只读校验
    /// </summary>
    private async Task EnsureBusinessAllowsAsync(OwnTask task, string actorId, string outcome, string? comment, CancellationToken cancellationToken)
    {
        var instance = task.Instance;
        var handler = _businessRegistry.Find(instance.DefinitionCode);
        if (handler is null || !string.IsNullOrEmpty(instance.ParentInstanceId) || string.IsNullOrWhiteSpace(instance.CorrelationId))
        {
            return;
        }

        await handler.OnTaskCompletingAsync(new WorkflowBusinessTaskContext
        {
            BusinessCode = handler.Descriptor.Code,
            BusinessId = instance.CorrelationId,
            InstanceId = task.Snapshot.InstanceId,
            TaskId = task.Snapshot.TaskId.ToString(CultureInfo.InvariantCulture),
            NodeId = task.Snapshot.NodeId,
            ActorUserId = ParseLong(actorId),
            Outcome = outcome,
            Comment = comment
        }, cancellationToken);
    }

    /// <summary>
    /// 转办/加签目标必须是当前上下文可见的启用用户
    /// </summary>
    private async Task EnsureAssignableUsersAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        foreach (var userIdText in userIds)
        {
            var user = long.TryParse(userIdText, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                ? await _userDirectory.FindAsync(userId, cancellationToken)
                : null;
            if (user is null || user.Status != EnableStatus.Enabled)
            {
                throw new BusinessException(message: $"用户 {userIdText} 不存在或已停用，不能作为受理人");
            }
        }
    }

    private string RequireCurrentUserId()
    {
        return _currentUser.UserId?.ToString(CultureInfo.InvariantCulture)
            ?? throw new BusinessException(message: "当前用户未登录，无法办理待办");
    }

    private static long ParseLong(string value)
    {
        return long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
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

    private sealed record OwnTask(WorkflowInstance Instance, WorkflowTaskSnapshot Snapshot);
}
