// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Timing;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;
using XiHan.Framework.Workflow.Abstractions.UserTasks;
using XiHan.Framework.Workflow.UserTasks;

namespace XiHan.BasicApp.Workflow.Application.QueryServices;

/// <summary>
/// 审批中心查询应用服务
/// </summary>
/// <remarks>
/// 列表的主体服务端锁定为当前登录用户，不接受前端指定；登录即可访问（与待办一致）。
/// 详情按参与关系鉴权：发起人、当前或曾经的审批人、被抄送人，或拥有流程查看权限者，其余一律拒绝——
/// 详情里带着业务单据数据，不能凭实例主键随意读取。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "审批中心")]
public sealed class WorkflowApprovalQueryService : WorkflowApplicationService, IWorkflowApprovalQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly WorkflowTaskRecordAction[] DoneActions =
    [
        WorkflowTaskRecordAction.Completed,
        WorkflowTaskRecordAction.Transferred,
        WorkflowTaskRecordAction.AddedAssignees
    ];

    private readonly IWorkflowTaskRecordRepository _recordRepository;
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly IWorkflowBookmarkStore _bookmarkStore;
    private readonly IWorkflowDefinitionStore _definitionStore;
    private readonly IWorkflowUserTaskService _userTaskService;
    private readonly WorkflowBusinessRegistry _businessRegistry;
    private readonly WorkflowUserNameResolver _userNameResolver;
    private readonly IPermissionChecker _permissionChecker;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowApprovalQueryService(
        IWorkflowTaskRecordRepository recordRepository,
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowInstanceStore instanceStore,
        IWorkflowBookmarkStore bookmarkStore,
        IWorkflowDefinitionStore definitionStore,
        IWorkflowUserTaskService userTaskService,
        WorkflowBusinessRegistry businessRegistry,
        WorkflowUserNameResolver userNameResolver,
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser,
        IClock clock)
    {
        _recordRepository = recordRepository;
        _instanceRepository = instanceRepository;
        _instanceStore = instanceStore;
        _bookmarkStore = bookmarkStore;
        _definitionStore = definitionStore;
        _userTaskService = userTaskService;
        _businessRegistry = businessRegistry;
        _userNameResolver = userNameResolver;
        _permissionChecker = permissionChecker;
        _currentUser = currentUser;
        _clock = clock;
    }

    /// <summary>
    /// 分页查询我的已办
    /// </summary>
    [HttpPost]
    public Task<PageResultDtoBase<WorkflowApprovalRecordItemDto>> GetDonePageAsync(WorkflowApprovalPageQueryDto input, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        return GetRecordPageAsync(input, record => record.UserId == userId && DoneActions.Contains(record.Action), cancellationToken);
    }

    /// <summary>
    /// 分页查询抄送我的
    /// </summary>
    [HttpPost]
    public Task<PageResultDtoBase<WorkflowApprovalRecordItemDto>> GetCcPageAsync(WorkflowApprovalPageQueryDto input, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        return GetRecordPageAsync(input, record => record.UserId == userId && record.Action == WorkflowTaskRecordAction.CarbonCopied, cancellationToken);
    }

    /// <summary>
    /// 分页查询我发起的（只含顶层实例）
    /// </summary>
    [HttpPost]
    public async Task<PageResultDtoBase<WorkflowApprovalInstanceItemDto>> GetStartedPageAsync(WorkflowApprovalPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var starterId = RequireCurrentUserId().ToString(CultureInfo.InvariantCulture);
        var request = new BasicAppPRDto { Page = input.Page, Conditions = new QueryConditions() };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysWorkflowInstance>(input.Keyword.Trim(), instance => instance.Name, instance => instance.CorrelationId!);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysWorkflowInstance instance) => instance.Status, input.Status.Value);
        }

        request.Conditions.AddSort((SysWorkflowInstance instance) => instance.CreationTime, SortDirection.Descending, 0);

        var page = await _instanceRepository.GetPagedAsync(
            request,
            instance => instance.StarterId == starterId && instance.ParentInstanceId == null,
            cancellationToken);

        var businessNames = GetBusinessNames();
        var nodeNames = new WorkflowNodeNameCache(_definitionStore);
        var pendingByInstance = new Dictionary<long, List<WorkflowUserTask>>();
        foreach (var instance in page.Items.Where(item => item.Status == WorkflowInstanceStatus.Running))
        {
            pendingByInstance[instance.BasicId] = await _userTaskService.GetPendingByInstanceAsync(
                instance.BasicId.ToString(CultureInfo.InvariantCulture), cancellationToken);
        }

        var assigneeNames = await _userNameResolver.ResolveAsync(
            pendingByInstance.Values.SelectMany(tasks => tasks).Select(task => task.AssigneeId), cancellationToken);

        var items = new List<WorkflowApprovalInstanceItemDto>(page.Items.Count);
        foreach (var instance in page.Items)
        {
            var tasks = pendingByInstance.GetValueOrDefault(instance.BasicId) ?? [];
            var pendingNodeNames = new List<string>();
            foreach (var nodeId in tasks.Select(task => task.NodeId).Distinct(StringComparer.Ordinal))
            {
                pendingNodeNames.Add(await nodeNames.GetAsync(instance.DefinitionId, nodeId, cancellationToken));
            }

            items.Add(new WorkflowApprovalInstanceItemDto
            {
                BasicId = instance.BasicId,
                DefinitionCode = instance.DefinitionCode,
                DefinitionVersion = instance.DefinitionVersion,
                Name = instance.Name,
                Status = instance.Status,
                Outcome = instance.Outcome,
                CorrelationId = instance.CorrelationId,
                BusinessName = businessNames.GetValueOrDefault(instance.DefinitionCode),
                CreationTime = instance.CreationTime,
                EndTime = instance.EndTime,
                FaultMessage = instance.FaultMessage,
                PendingNodeNames = pendingNodeNames,
                PendingAssigneeNames = [.. tasks
                    .Select(task => task.AssigneeId)
                    .Distinct(StringComparer.Ordinal)
                    .Select(id => assigneeNames.GetValueOrDefault(id) ?? id)]
            });
        }

        return new PageResultDtoBase<WorkflowApprovalInstanceItemDto>(items, page.Page);
    }

    /// <summary>
    /// 审批详情（单据 + 当前任务 + 审批轨迹）
    /// </summary>
    public async Task<WorkflowApprovalDetailDto> GetDetailAsync(long instanceId, string? taskId = null, CancellationToken cancellationToken = default)
    {
        if (instanceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(instanceId), "实例主键必须大于 0。");
        }

        var userId = RequireCurrentUserId();
        var userIdText = userId.ToString(CultureInfo.InvariantCulture);
        var instanceIdText = instanceId.ToString(CultureInfo.InvariantCulture);

        var instance = await _instanceStore.FindAsync(instanceIdText, cancellationToken)
            ?? throw new UserFriendlyException("流程实例不存在。");
        var bookmarks = await _bookmarkStore.GetByInstanceAsync(instanceIdText, cancellationToken);
        var pendingTasks = bookmarks
            .Where(bookmark => bookmark.Kind == WorkflowBookmarkKinds.UserTask)
            .Select(bookmark => WorkflowUserTaskMapper.ToUserTask(bookmark, instance))
            .ToList();
        var records = await _recordRepository.GetByInstanceIdAsync(instanceId, cancellationToken);

        await EnsureCanViewAsync(instance, userIdText, pendingTasks, records, cancellationToken);

        var nodeNames = new WorkflowNodeNameCache(_definitionStore);
        var definitionId = WorkflowStoreMapper.ParseId(instance.DefinitionId);
        var definition = await nodeNames.GetDefinitionAsync(definitionId, cancellationToken);

        var detail = new WorkflowApprovalDetailDto
        {
            InstanceId = instanceId,
            InstanceName = instance.Name,
            DefinitionCode = instance.DefinitionCode,
            DefinitionName = definition?.Name ?? instance.DefinitionCode,
            DefinitionVersion = instance.DefinitionVersion,
            Status = instance.Status,
            Outcome = instance.Outcome,
            CorrelationId = instance.CorrelationId,
            StarterId = instance.StarterId,
            CreationTime = instance.CreationTime,
            StartTime = instance.StartTime,
            EndTime = instance.EndTime,
            FaultMessage = instance.FaultMessage,
            CancellationReason = instance.CancellationReason,
            CanWithdraw = string.Equals(instance.StarterId, userIdText, StringComparison.Ordinal)
                && instance.Status is WorkflowInstanceStatus.Running or WorkflowInstanceStatus.Suspended or WorkflowInstanceStatus.Faulted
        };

        detail.Business = await BuildBusinessAsync(instance, cancellationToken);
        detail.Task = await BuildTaskAsync(taskId, instanceId, userIdText, pendingTasks, nodeNames, definitionId, cancellationToken);
        detail.Timeline = await BuildTimelineAsync(instance, definitionId, records, pendingTasks, nodeNames, cancellationToken);
        detail.StarterName = detail.Timeline.FirstOrDefault(item => item.Kind == WorkflowApprovalTimelineKind.Started)?.UserName;

        return detail;
    }

    private async Task<PageResultDtoBase<WorkflowApprovalRecordItemDto>> GetRecordPageAsync(
        WorkflowApprovalPageQueryDto input,
        Expression<Func<SysWorkflowTaskRecord, bool>> predicate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = new BasicAppPRDto { Page = input.Page, Conditions = new QueryConditions() };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysWorkflowTaskRecord>(
                input.Keyword.Trim(),
                record => record.Title,
                record => record.InstanceName,
                record => record.CorrelationId!);
        }

        request.Conditions.AddSort((SysWorkflowTaskRecord record) => record.CreatedTime, SortDirection.Descending, 0);

        var page = await _recordRepository.GetPagedAsync(request, predicate, cancellationToken);
        var instances = (await _instanceRepository.GetByIdsAsync(page.Items.Select(record => record.InstanceId).Distinct(), cancellationToken))
            .ToDictionary(instance => instance.BasicId);
        var starterNames = await _userNameResolver.ResolveAsync(instances.Values.Select(instance => instance.StarterId), cancellationToken);
        var businessNames = GetBusinessNames();

        var items = page.Items.Select(record =>
        {
            var instance = instances.GetValueOrDefault(record.InstanceId);
            return new WorkflowApprovalRecordItemDto
            {
                BasicId = record.BasicId,
                InstanceId = record.InstanceId,
                TaskId = record.TaskId.ToString(CultureInfo.InvariantCulture),
                InstanceName = record.InstanceName,
                DefinitionCode = record.DefinitionCode,
                NodeId = record.NodeId,
                NodeName = record.NodeName,
                Title = record.Title,
                CorrelationId = record.CorrelationId,
                Action = record.Action,
                Outcome = record.Outcome,
                Comment = record.Comment,
                CreatedTime = record.CreatedTime,
                BusinessName = instance is { ParentInstanceId: null } ? businessNames.GetValueOrDefault(record.DefinitionCode) : null,
                StarterId = instance?.StarterId,
                StarterName = instance?.StarterId is { } starterId ? starterNames.GetValueOrDefault(starterId) : null,
                InstanceStatus = instance?.Status ?? WorkflowInstanceStatus.Terminated,
                InstanceOutcome = instance?.Outcome
            };
        }).ToList();

        return new PageResultDtoBase<WorkflowApprovalRecordItemDto>(items, page.Page);
    }

    /// <summary>
    /// 详情可见性：发起人、当前审批人、参与过的人（办理/转办/加签/抄送），或拥有流程查看权限
    /// </summary>
    private async Task EnsureCanViewAsync(
        WorkflowInstance instance,
        string userIdText,
        IReadOnlyList<WorkflowUserTask> pendingTasks,
        IReadOnlyList<SysWorkflowTaskRecord> records,
        CancellationToken cancellationToken)
    {
        var userId = long.Parse(userIdText, NumberStyles.None, CultureInfo.InvariantCulture);
        if (string.Equals(instance.StarterId, userIdText, StringComparison.Ordinal)
            || pendingTasks.Any(task => string.Equals(task.AssigneeId, userIdText, StringComparison.Ordinal))
            || records.Any(record => record.UserId == userId)
            || await _permissionChecker.IsGrantedAsync(userIdText, WorkflowPermissionCodes.Read, cancellationToken))
        {
            return;
        }

        throw new UserFriendlyException("您没有查看该审批的权限。");
    }

    private async Task<WorkflowApprovalBusinessDto?> BuildBusinessAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        var handler = _businessRegistry.Find(instance.DefinitionCode);
        if (handler is null || !string.IsNullOrEmpty(instance.ParentInstanceId) || string.IsNullOrWhiteSpace(instance.CorrelationId))
        {
            return null;
        }

        var data = await handler.GetFormDataAsync(instance.CorrelationId, cancellationToken);
        return new WorkflowApprovalBusinessDto
        {
            Code = handler.Descriptor.Code,
            Name = handler.Descriptor.Name,
            FormComponent = handler.Descriptor.FormComponent,
            BusinessId = instance.CorrelationId,
            DataJson = data is null ? null : JsonSerializer.Serialize(data, data.GetType(), JsonOptions)
        };
    }

    private static async Task<WorkflowApprovalTaskDto?> BuildTaskAsync(
        string? taskId,
        long instanceId,
        string userIdText,
        IReadOnlyList<WorkflowUserTask> pendingTasks,
        WorkflowNodeNameCache nodeNames,
        long definitionId,
        CancellationToken cancellationToken)
    {
        // 未指定任务时取当前用户在该实例里的第一条待办，审批中心从任意列表打开都能直接办理
        var task = string.IsNullOrWhiteSpace(taskId)
            ? pendingTasks.FirstOrDefault(item => string.Equals(item.AssigneeId, userIdText, StringComparison.Ordinal))
            : pendingTasks.FirstOrDefault(item => string.Equals(item.TaskId, taskId.Trim(), StringComparison.Ordinal)
                && string.Equals(item.AssigneeId, userIdText, StringComparison.Ordinal));
        if (task is null || task.InstanceId != instanceId.ToString(CultureInfo.InvariantCulture))
        {
            return null;
        }

        var definition = await nodeNames.GetDefinitionAsync(definitionId, cancellationToken);
        var node = definition?.Nodes.FirstOrDefault(item => item.Id == task.NodeId);
        var allowedOutcomes = node is not null && node.Properties.TryGetValue("AllowedOutcomes", out var raw)
            ? WorkflowValueConverter.ConvertTo<List<string>>(raw) ?? []
            : [];

        return new WorkflowApprovalTaskDto
        {
            TaskId = task.TaskId,
            NodeId = task.NodeId,
            NodeName = await nodeNames.GetAsync(definitionId, task.NodeId, cancellationToken),
            Title = task.Title,
            CreationTime = task.CreationTime,
            AllowedOutcomes = allowedOutcomes,
            FormDataJson = JsonSerializer.Serialize(task.FormData, JsonOptions)
        };
    }

    /// <summary>
    /// 审批轨迹：发起 → 办理/转办/加签/抄送 与无人自动处理（按时间）→ 结束或当前待办
    /// </summary>
    private async Task<List<WorkflowApprovalTimelineItemDto>> BuildTimelineAsync(
        WorkflowInstance instance,
        long definitionId,
        IReadOnlyList<SysWorkflowTaskRecord> records,
        IReadOnlyList<WorkflowUserTask> pendingTasks,
        WorkflowNodeNameCache nodeNames,
        CancellationToken cancellationToken)
    {
        var nodeInstances = await _instanceStore.GetNodeInstancesAsync(instance.Id, cancellationToken);

        var userIds = new List<string?> { instance.StarterId };
        userIds.AddRange(records.Select(record => record.UserId.ToString(CultureInfo.InvariantCulture)));
        userIds.AddRange(records.SelectMany(record => SplitTargets(record.TargetUserIds)));
        userIds.AddRange(pendingTasks.Select(task => task.AssigneeId));
        var names = await _userNameResolver.ResolveAsync(userIds, cancellationToken);

        var timeline = new List<WorkflowApprovalTimelineItemDto>
        {
            new()
            {
                Kind = WorkflowApprovalTimelineKind.Started,
                UserId = instance.StarterId,
                UserName = instance.StarterId is null ? null : names.GetValueOrDefault(instance.StarterId),
                Time = ToOffset(instance.StartTime ?? instance.CreationTime)
            }
        };

        var events = new List<WorkflowApprovalTimelineItemDto>();
        foreach (var record in records)
        {
            var userIdText = record.UserId.ToString(CultureInfo.InvariantCulture);
            events.Add(new WorkflowApprovalTimelineItemDto
            {
                Kind = record.Action switch
                {
                    WorkflowTaskRecordAction.Completed => WorkflowApprovalTimelineKind.Completed,
                    WorkflowTaskRecordAction.Transferred => WorkflowApprovalTimelineKind.Transferred,
                    WorkflowTaskRecordAction.AddedAssignees => WorkflowApprovalTimelineKind.AddedAssignees,
                    _ => WorkflowApprovalTimelineKind.CarbonCopied
                },
                NodeId = record.NodeId,
                NodeName = record.NodeName,
                UserId = userIdText,
                UserName = names.GetValueOrDefault(userIdText) ?? userIdText,
                Outcome = record.Outcome,
                Comment = record.Comment,
                TargetUserNames = [.. SplitTargets(record.TargetUserIds).Select(id => names.GetValueOrDefault(id) ?? id)],
                Time = record.CreatedTime
            });
        }

        // 无审批人时自动通过/驳回：只存在于节点私有轨迹里（办理人为空）
        foreach (var nodeInstance in nodeInstances.Where(item => item.ActivityType == WorkflowActivityTypes.UserTask))
        {
            var history = WorkflowValueConverter.ConvertTo<List<Dictionary<string, object?>>>(nodeInstance.State.GetValueOrDefault("history")) ?? [];
            foreach (var entry in history.Where(item => item.GetValueOrDefault("actorId") is null))
            {
                events.Add(new WorkflowApprovalTimelineItemDto
                {
                    Kind = WorkflowApprovalTimelineKind.AutoDecided,
                    NodeId = nodeInstance.NodeId,
                    NodeName = await nodeNames.GetAsync(definitionId, nodeInstance.NodeId, cancellationToken),
                    Outcome = WorkflowValueConverter.ConvertTo<string>(entry.GetValueOrDefault("outcome")),
                    Comment = WorkflowValueConverter.ConvertTo<string>(entry.GetValueOrDefault("comment")),
                    Time = ToOffset(nodeInstance.EndTime ?? nodeInstance.StartTime)
                });
            }
        }

        timeline.AddRange(events.OrderBy(item => item.Time));

        if (instance.IsFinalStatus())
        {
            timeline.Add(new WorkflowApprovalTimelineItemDto
            {
                Kind = WorkflowApprovalTimelineKind.Finished,
                InstanceStatus = instance.Status,
                Outcome = instance.Outcome,
                Comment = instance.Status == WorkflowInstanceStatus.Faulted ? instance.FaultMessage : instance.CancellationReason,
                Time = instance.EndTime is { } endTime ? ToOffset(endTime) : null
            });
        }

        foreach (var task in pendingTasks.OrderBy(item => item.CreationTime))
        {
            timeline.Add(new WorkflowApprovalTimelineItemDto
            {
                Kind = WorkflowApprovalTimelineKind.Pending,
                NodeId = task.NodeId,
                NodeName = await nodeNames.GetAsync(definitionId, task.NodeId, cancellationToken),
                UserId = task.AssigneeId,
                UserName = names.GetValueOrDefault(task.AssigneeId) ?? task.AssigneeId,
                Time = ToOffset(task.CreationTime)
            });
        }

        return timeline;
    }

    private Dictionary<string, string> GetBusinessNames()
    {
        return _businessRegistry.GetDescriptors().ToDictionary(descriptor => descriptor.Code, descriptor => descriptor.Name, StringComparer.Ordinal);
    }

    private long RequireCurrentUserId()
    {
        return _currentUser.UserId ?? throw new BusinessException(message: "当前用户未登录，无法查询审批");
    }

    private static IEnumerable<string> SplitTargets(string? targets)
    {
        return string.IsNullOrWhiteSpace(targets)
            ? []
            : targets.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// 引擎时间取自框架时钟，落库后丢失 Kind：按时钟的时间类型还原（UTC 时钟按 UTC，否则按服务器本地时间）
    /// </summary>
    private DateTimeOffset ToOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, _clock.Kind == DateTimeKind.Utc ? DateTimeKind.Utc : DateTimeKind.Local));
    }
}
