// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.BasicApp.Workflow.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Workflow.Abstractions.Activities;
using XiHan.Framework.Workflow.Abstractions.Assignees;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Tasks;
using XiHan.Framework.Workflow.Abstractions.Values;
using InstanceLookup = XiHan.BasicApp.Workflow.Operations.WorkflowInstances.WorkflowInstances;

namespace XiHan.BasicApp.Workflow.Operations.Approvals;

/// <summary>
/// 我已审批。
/// </summary>
/// <remarks>
/// <c>POST /api/approvals:list-done</c>。照 4.x 只要登录、主体锁定为调用方：办理、转办、加签的记录；关键字匹配任务标题、实例标题与关联 ID，按时间降序。每条带上实例现在的状态与结论。
/// </remarks>
/// <param name="queries">审批记录的分页。</param>
/// <param name="approvals">审批中心共用的补全。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Policy = Policies.Authenticated)]
public sealed class ListDoneApprovals(WorkflowRecordQueries queries, WorkflowApprovals approvals) : IOperation<PageRequest, PageResult<WorkflowApprovalRecordResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "approvals.list-done";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<WorkflowApprovalRecordResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var page = await queries.ListAsync(request, copied: false, context).ConfigureAwait(false);
        return page.TryGetValue(out var records) ? await approvals.RecordsAsync(records, context).ConfigureAwait(false) : page.Error!;
    }
}

/// <summary>
/// 抄送我的。
/// </summary>
/// <remarks>
/// <c>POST /api/approvals:list-cc</c>。照 4.x 只要登录、主体锁定为调用方：抄送给调用方的记录（同一实例同一节点对同一人只记一次）；关键字与顺序同我已审批。
/// </remarks>
/// <param name="queries">审批记录的分页。</param>
/// <param name="approvals">审批中心共用的补全。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Policy = Policies.Authenticated)]
public sealed class ListCcApprovals(WorkflowRecordQueries queries, WorkflowApprovals approvals) : IOperation<PageRequest, PageResult<WorkflowApprovalRecordResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "approvals.list-cc";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<WorkflowApprovalRecordResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var page = await queries.ListAsync(request, copied: true, context).ConfigureAwait(false);
        return page.TryGetValue(out var records) ? await approvals.RecordsAsync(records, context).ConfigureAwait(false) : page.Error!;
    }
}

/// <summary>
/// 我发起的。
/// </summary>
/// <remarks>
/// <c>POST /api/approvals:list-started</c>。照 4.x 只要登录、发起人锁定为调用方、只取顶层实例；关键字匹配标题与关联 ID，筛选 <c>status</c>，按发起时间降序。运行中的带等着办的节点与人。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="tasks">任务服务。</param>
/// <param name="lookup">人名与定义。</param>
/// <param name="registry">审批业务登记表。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, ReadOnly = true, Policy = Policies.Authenticated)]
public sealed class ListStartedApprovals(IWorkflowEngine engine, IWorkflowTaskService tasks, WorkflowLookup lookup, WorkflowBusinessRegistry registry) : IOperation<PageRequest, PageResult<WorkflowApprovalInstanceResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "approvals.list-started";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<WorkflowApprovalInstanceResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (WorkflowCaller.Of(context).EngineUser is not { } me)
        {
            return new PageResult<WorkflowApprovalInstanceResponse>([], 0, request.Page);
        }

        var mapped = WorkflowQueryMapping.Started(request, me);
        if (!mapped.TryGetValue(out var query))
        {
            return mapped.Error!;
        }

        var listed = await engine.ListInstancesAsync(query, context.CancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var instances))
        {
            return listed.Error!;
        }

        var counted = await engine.CountInstancesAsync(query, context.CancellationToken).ConfigureAwait(false);
        if (!counted.TryGetValue(out var total))
        {
            return counted.Error!;
        }

        var pending = new Dictionary<string, IReadOnlyList<UserTask>>(StringComparer.Ordinal);
        foreach (var instance in instances.Where(static i => i.Status == WorkflowInstanceStatus.Running))
        {
            var waiting = await tasks.PendingByInstanceAsync(instance.Id, context.CancellationToken).ConfigureAwait(false);
            if (!waiting.TryGetValue(out var list))
            {
                return waiting.Error!;
            }

            pending[instance.Id] = list;
        }

        var names = await lookup.NamesAsync(pending.Values.SelectMany(static t => t).Select(static t => t.AssigneeId), context.CancellationToken).ConfigureAwait(false);
        var items = new List<WorkflowApprovalInstanceResponse>(instances.Count);
        foreach (var instance in instances)
        {
            var waiting = pending.GetValueOrDefault(instance.Id) ?? [];
            var nodeNames = new List<string>();
            foreach (var nodeId in waiting.Select(static t => t.NodeId).Distinct(StringComparer.Ordinal))
            {
                nodeNames.Add(await lookup.NodeNameAsync(instance.DefinitionId, nodeId, context.CancellationToken).ConfigureAwait(false));
            }

            items.Add(new WorkflowApprovalInstanceResponse(
                WorkflowIds.ToContract(instance.Id),
                instance.DefinitionName.Value,
                instance.DefinitionVersion,
                instance.Title,
                instance.Status,
                instance.Outcome,
                instance.CorrelationId,
                registry.Find(instance.DefinitionName.Value)?.Descriptor.NameKey,
                instance.CreatedAt,
                instance.EndedAt,
                instance.Fault?.Localize(),
                nodeNames,
                waiting.Select(static t => t.AssigneeId).Distinct(StringComparer.Ordinal).Select(id => names.GetValueOrDefault(id) ?? id).ToList()));
        }

        return new PageResult<WorkflowApprovalInstanceResponse>(items, total, request.Page);
    }
}

/// <summary>
/// 审批详情。
/// </summary>
/// <remarks>
/// <c>GET /api/approvals/{id}</c>。照 4.x 只要登录、按参与关系鉴权：发起人、当前受理人、办理过或被抄送过的人，或持有 <c>workflow.workflow-instances.read</c>，其余 403（详情带着业务单据）。
/// 返回概要、审批业务的单据（单据组件与数据）、调用方在这个实例里的待办（允许的结论取定义节点的 <c>AllowedOutcomes</c>）与审批轨迹。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="tasks">任务服务。</param>
/// <param name="records">审批记录。</param>
/// <param name="lookup">人名与定义。</param>
/// <param name="registry">审批业务登记表。</param>
/// <param name="permissions">权限判定。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetApproval(IWorkflowEngine engine, IWorkflowTaskService tasks, WorkflowTaskRecords records, WorkflowLookup lookup, WorkflowBusinessRegistry registry, IPermissionChecker permissions) : IOperation<GetApprovalRequest, WorkflowApprovalDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "approvals.get";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowApprovalDetailResponse>> HandleAsync(GetApprovalRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = WorkflowCaller.Of(context);
        var found = await InstanceLookup.FindAsync(engine, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var instance))
        {
            return found.Error!;
        }

        var waiting = await tasks.PendingByInstanceAsync(instance.Id, context.CancellationToken).ConfigureAwait(false);
        if (!waiting.TryGetValue(out var pending))
        {
            return waiting.Error!;
        }

        var history = await records.OfInstanceAsync(caller.Side, request.Id, context.CancellationToken).ConfigureAwait(false);
        var me = caller.EngineUser;
        var participates = me is not null
            && (string.Equals(instance.StarterId, me, StringComparison.Ordinal)
                || pending.Any(t => string.Equals(t.AssigneeId, me, StringComparison.Ordinal))
                || history.Any(r => r.UserId == caller.UserId));
        if (!participates)
        {
            var granted = await permissions.HasPermissionAsync(context.GetSecurity(), WorkflowPermissions.InstancesRead, context.CancellationToken).ConfigureAwait(false);
            if (!granted.TryGetValue(out var reads))
            {
                return granted.Error!;
            }

            if (!reads)
            {
                return Error.Denied(WorkflowErrors.ApprovalHidden, WorkflowMessages.ApprovalHidden);
            }
        }

        var definitionFound = await lookup.DefinitionAsync(instance.DefinitionId, context.CancellationToken).ConfigureAwait(false);
        if (!definitionFound.TryGetValue(out var definition))
        {
            return definitionFound.Error!;
        }

        var business = await BusinessAsync(instance, context).ConfigureAwait(false);
        if (!business.TryGetValue(out var form))
        {
            return business.Error!;
        }

        var nodes = await engine.NodeInstancesAsync(instance.Id, context.CancellationToken).ConfigureAwait(false);
        if (!nodes.TryGetValue(out var nodeInstances))
        {
            return nodes.Error!;
        }

        var names = await lookup.NamesAsync(
            [instance.StarterId, .. history.Select(static r => WorkflowIds.ToEngineUser(r.UserId)), .. history.SelectMany(static r => WorkflowTaskRecords.TargetsOf(r)).Select(WorkflowIds.ToEngineUser), .. pending.Select(static t => t.AssigneeId)],
            context.CancellationToken).ConfigureAwait(false);
        var task = await TaskAsync(request.TaskId, me, pending, definition, instance, context).ConfigureAwait(false);
        var timeline = await TimelineAsync(instance, history, nodeInstances, pending, definition, names, context).ConfigureAwait(false);
        var starter = WorkflowIds.UserOf(instance.StarterId);
        return new WorkflowApprovalDetailResponse(
            request.Id,
            instance.Title,
            instance.DefinitionName.Value,
            definition.TryGetValue(out var used) && !string.IsNullOrWhiteSpace(used.DisplayName) ? used.DisplayName : instance.DefinitionName.Value,
            instance.DefinitionVersion,
            instance.Status,
            instance.Outcome,
            instance.CorrelationId,
            starter,
            instance.StarterId is { } starterId ? names.GetValueOrDefault(starterId) : null,
            instance.CreatedAt,
            instance.StartedAt,
            instance.EndedAt,
            instance.Fault?.Localize(),
            instance.CancellationReason,
            me is not null && string.Equals(instance.StarterId, me, StringComparison.Ordinal) && instance.ParentInstanceId is null
                && instance.Status is WorkflowInstanceStatus.Running or WorkflowInstanceStatus.Suspended or WorkflowInstanceStatus.Faulted,
            form.TryGetValue(out var shown) ? shown : null,
            task,
            timeline);
    }

    /// <summary>
    /// 审批业务的单据（单据已不存在时数据为空）。
    /// </summary>
    private async Task<Result<Maybe<WorkflowApprovalBusinessResponse>>> BusinessAsync(WorkflowInstance instance, InvocationContext context)
    {
        if (instance.ParentInstanceId is not null || string.IsNullOrWhiteSpace(instance.CorrelationId) || registry.Find(instance.DefinitionName.Value) is not { } handler)
        {
            return Maybe.None<WorkflowApprovalBusinessResponse>();
        }

        var data = await handler.GetFormDataAsync(instance.CorrelationId, context).ConfigureAwait(false);
        if (data.IsFailure && !string.Equals(data.Error!.Kind, "NOTFOUND", StringComparison.Ordinal))
        {
            return data.Error!;
        }

        var descriptor = handler.Descriptor;
        return Maybe.Some(new WorkflowApprovalBusinessResponse(descriptor.Code, descriptor.NameKey, descriptor.FormComponent, instance.CorrelationId, data.IsSuccess ? data.ValueOrThrow() : null));
    }

    /// <summary>
    /// 调用方在这个实例里的待办。
    /// </summary>
    /// <remarks>
    /// 给了任务 ID 取那一条，否则取第一条（照 4.x，从任何列表打开都能直接办）。
    /// </remarks>
    private async Task<WorkflowApprovalTaskResponse?> TaskAsync(Guid? taskId, string? me, IReadOnlyList<UserTask> pending, Maybe<WorkflowDefinition> definition, WorkflowInstance instance, InvocationContext context)
    {
        if (me is null)
        {
            return null;
        }

        var engineTaskId = taskId is { } id ? WorkflowIds.ToEngine(id) : null;
        var task = pending.FirstOrDefault(t => string.Equals(t.AssigneeId, me, StringComparison.Ordinal) && (engineTaskId is null || string.Equals(t.Id, engineTaskId, StringComparison.Ordinal)));
        if (task is null)
        {
            return null;
        }

        var nodeName = await lookup.NodeNameAsync(instance.DefinitionId, task.NodeId, context.CancellationToken).ConfigureAwait(false);
        var allowed = definition.TryGetValue(out var used) && used.FindNode(task.NodeId).TryGetValue(out var node) ? WorkflowLookup.AllowedOutcomes(node) : [];
        return new WorkflowApprovalTaskResponse(WorkflowIds.ToContract(task.Id), task.NodeId, nodeName, task.Title ?? nodeName, task.CreatedAt, allowed, task.FormData?.ToJsonString() ?? "{}");
    }

    /// <summary>
    /// 审批轨迹。
    /// </summary>
    /// <remarks>
    /// 发起 → 办理 / 转办 / 加签 / 抄送与无人时的自动处理（按时间）→ 结束或当前待办。
    /// 找不到人时按策略自动通过或驳回：框架 5 不给这种节点写办理历史（4.x 写一条没有办理人的），以「已完成、没有历史的用户任务节点」认出，结论取节点的 <c>EmptyAssigneeStrategy</c>；
    /// 超时完成的历史项没有办理人，同样算自动处理（结论 <c>timeout</c>）。
    /// </remarks>
    private async Task<IReadOnlyList<WorkflowApprovalTimelineItem>> TimelineAsync(
        WorkflowInstance instance,
        IReadOnlyList<SysWorkflowTaskRecord> history,
        IReadOnlyList<WorkflowNodeInstance> nodeInstances,
        IReadOnlyList<UserTask> pending,
        Maybe<WorkflowDefinition> definition,
        IReadOnlyDictionary<string, string> names,
        InvocationContext context)
    {
        string? NameOf(Guid user) => names.GetValueOrDefault(WorkflowIds.ToEngineUser(user)) ?? user.ToString();
        var timeline = new List<WorkflowApprovalTimelineItem>
        {
            new(WorkflowApprovalTimelineKind.Started, null, null, WorkflowIds.UserOf(instance.StarterId), instance.StarterId is { } starter ? names.GetValueOrDefault(starter) : null, null, null, [], null, instance.StartedAt ?? instance.CreatedAt),
        };

        var events = history.Select(r => new WorkflowApprovalTimelineItem(
            r.Action switch
            {
                WorkflowTaskRecordAction.Completed => WorkflowApprovalTimelineKind.Completed,
                WorkflowTaskRecordAction.Transferred => WorkflowApprovalTimelineKind.Transferred,
                WorkflowTaskRecordAction.AddedAssignees => WorkflowApprovalTimelineKind.AddedAssignees,
                _ => WorkflowApprovalTimelineKind.CarbonCopied,
            },
            r.NodeId,
            r.NodeName,
            r.UserId,
            NameOf(r.UserId),
            r.Outcome,
            r.Comment,
            WorkflowTaskRecords.TargetsOf(r).Select(t => NameOf(t)!).ToList(),
            null,
            r.CreatedAt)).ToList();
        foreach (var node in nodeInstances.Where(static n => n.ActivityType.Value == ActivityTypes.UserTask))
        {
            var nodeName = await lookup.NodeNameAsync(instance.DefinitionId, node.NodeId, context.CancellationToken).ConfigureAwait(false);
            if (node.State[UserTaskPayloadKeys.History] is JsonArray entries)
            {
                foreach (var entry in entries.OfType<JsonObject>().Where(static e => WorkflowPayload.Text(e, UserTaskPayloadKeys.ActorId) is null))
                {
                    events.Add(new(WorkflowApprovalTimelineKind.AutoDecided, node.NodeId, nodeName, null, null, WorkflowPayload.Text(entry, UserTaskPayloadKeys.Outcome), WorkflowPayload.Text(entry, UserTaskPayloadKeys.Comment), [], null, node.EndedAt ?? node.StartedAt));
                }
            }
            else if (node.Status == WorkflowNodeInstanceStatus.Completed)
            {
                var strategy = definition.TryGetValue(out var used) && used.FindNode(node.NodeId).TryGetValue(out var declared) && declared.Properties.TryGetPropertyValue(UserTaskActivity.EmptyAssigneeStrategyProperty, out var raw) && raw is JsonValue value
                    ? WorkflowValues.AsString(value)
                    : null;
                var outcome = strategy switch
                {
                    nameof(EmptyAssigneeStrategy.AutoApprove) => UserTaskOutcomes.Approved,
                    nameof(EmptyAssigneeStrategy.AutoReject) => UserTaskOutcomes.Rejected,
                    _ => null,
                };
                events.Add(new(WorkflowApprovalTimelineKind.AutoDecided, node.NodeId, nodeName, null, null, outcome, null, [], null, node.EndedAt ?? node.StartedAt));
            }
        }

        timeline.AddRange(events.OrderBy(static e => e.Time));
        if (instance.IsFinal)
        {
            timeline.Add(new(WorkflowApprovalTimelineKind.Finished, null, null, null, null, instance.Outcome, instance.Status == WorkflowInstanceStatus.Faulted ? instance.Fault?.Localize() : instance.CancellationReason, [], instance.Status, instance.EndedAt));
        }

        foreach (var task in pending.OrderBy(static t => t.CreatedAt))
        {
            var nodeName = await lookup.NodeNameAsync(instance.DefinitionId, task.NodeId, context.CancellationToken).ConfigureAwait(false);
            timeline.Add(new(WorkflowApprovalTimelineKind.Pending, task.NodeId, nodeName, WorkflowIds.UserOf(task.AssigneeId), names.GetValueOrDefault(task.AssigneeId) ?? task.AssigneeId, null, null, [], null, task.CreatedAt));
        }

        return timeline;
    }
}

/// <summary>
/// 撤回。
/// </summary>
/// <remarks>
/// <c>POST /api/approvals/{id}:withdraw</c>。照 4.x 只要登录：只有发起人能撤回自己运行中、挂起或故障的顶层实例（子流程随父流程结束），即取消实例，之后回调审批业务的中止回写；
/// 回写在同一个事务里，失败即整个撤回回滚、实例照旧（4.x 的实例已取消、只报回写失败）。撤回之前先问审批业务的改动前校验。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="aborts">中止回写。</param>
/// <param name="texts">文案。</param>
/// <param name="changes">审批业务的改动前校验。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class WithdrawApproval(IWorkflowEngine engine, WorkflowBusinessAborts aborts, WorkflowTexts texts, WorkflowBusinessChanges changes) : IOperation<WithdrawApprovalRequest, WithdrawApprovalResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "approvals.withdraw";

    /// <inheritdoc />
    public async ValueTask<Result<WithdrawApprovalResponse>> HandleAsync(WithdrawApprovalRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await InstanceLookup.FindAsync(engine, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var instance))
        {
            return found.Error!;
        }

        var me = WorkflowCaller.Of(context).EngineUser;
        if (me is null || !string.Equals(instance.StarterId, me, StringComparison.Ordinal))
        {
            return Error.Denied(WorkflowErrors.WithdrawNotStarter, WorkflowMessages.WithdrawNotStarter);
        }

        if (instance.ParentInstanceId is not null)
        {
            return Error.Validation(WorkflowErrors.WithdrawSubWorkflow, WorkflowMessages.WithdrawSubWorkflow);
        }

        if (instance.IsFinal && instance.Status != WorkflowInstanceStatus.Faulted)
        {
            return Error.Conflict(WorkflowErrors.WithdrawFinished, WorkflowMessages.WithdrawFinished);
        }

        var allowed = await changes.CheckAsync(instance, WorkflowBusinessChangeKind.Withdraw, context).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? texts.Render(context, WorkflowMessages.WithdrawReason) : request.Reason.Trim();
        var canceled = await engine.CancelAsync([instance.Id], reason, context.CancellationToken).ConfigureAwait(false);
        if (!canceled[0].TryGetValue(out var after))
        {
            return canceled[0].Error!;
        }

        var notified = await aborts.NotifyAsync(after, reason, context).ConfigureAwait(false);
        return notified.IsSuccess ? new WithdrawApprovalResponse(request.Id, after.Status) : notified.Error!;
    }
}

/// <summary>
/// 撤回之后的实例。
/// </summary>
/// <param name="InstanceId">流程实例。</param>
/// <param name="Status">实例状态。</param>
public sealed record WithdrawApprovalResponse(Guid InstanceId, WorkflowInstanceStatus Status);

/// <summary>
/// 审批中心的列表补全（作用域）。
/// </summary>
/// <param name="engine">引擎。</param>
/// <param name="lookup">人名与定义。</param>
/// <param name="registry">审批业务登记表。</param>
public sealed class WorkflowApprovals(IWorkflowEngine engine, WorkflowLookup lookup, WorkflowBusinessRegistry registry)
{
    /// <summary>
    /// 给一页审批记录补上实例的状态、结论、发起人与审批业务名。
    /// </summary>
    public async Task<Result<PageResult<WorkflowApprovalRecordResponse>>> RecordsAsync(PageResult<SysWorkflowTaskRecord> page, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);
        var instances = new Dictionary<Guid, WorkflowInstance>();
        foreach (var id in page.Items.Select(static r => r.InstanceId).Distinct())
        {
            var found = await engine.FindInstanceAsync(WorkflowIds.ToEngine(id), context.CancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var maybe))
            {
                return found.Error!;
            }

            if (maybe.TryGetValue(out var instance))
            {
                instances[id] = instance;
            }
        }

        var starters = await lookup.NamesAsync(instances.Values.Select(static i => i.StarterId), context.CancellationToken).ConfigureAwait(false);
        var items = page.Items.Select(r =>
        {
            var instance = instances.GetValueOrDefault(r.InstanceId);
            return new WorkflowApprovalRecordResponse(
                r.Id,
                r.InstanceId,
                r.TaskId,
                r.InstanceTitle,
                r.DefinitionName,
                r.NodeId,
                r.NodeName,
                r.Title,
                r.CorrelationId,
                r.Action,
                r.Outcome,
                r.Comment,
                r.CreatedAt,
                instance is { ParentInstanceId: null } ? registry.Find(r.DefinitionName)?.Descriptor.NameKey : null,
                WorkflowIds.UserOf(instance?.StarterId),
                instance?.StarterId is { } starter ? starters.GetValueOrDefault(starter) : null,
                instance?.Status,
                instance?.Outcome);
        }).ToList();
        return new PageResult<WorkflowApprovalRecordResponse>(items, page.Total, page.Page);
    }
}
