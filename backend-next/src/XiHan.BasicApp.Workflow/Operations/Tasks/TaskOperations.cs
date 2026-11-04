// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.BasicApp.Workflow.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Storage;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Operations.Tasks;

/// <summary>
/// 我的待办
/// </summary>
/// <remarks>
/// <c>POST /api/tasks:list</c>。照 4.x 只要登录，受理人锁定为调用方；关键字匹配任务标题、实例标题、定义名与关联 ID，按到达时间降序，
/// 过滤与分页在存储里做（4.x 在内存里分页）。每条补上节点名、发起人与审批业务名
/// </remarks>
/// <param name="tasks">任务服务</param>
/// <param name="instanceStore">实例存储（一页的实例一次读完）</param>
/// <param name="lookup">人名与定义</param>
/// <param name="registry">审批业务登记表</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Policy = Policies.Authenticated)]
public sealed class ListTasks(IWorkflowTaskService tasks, IWorkflowInstanceStore instanceStore, WorkflowLookup lookup, WorkflowBusinessRegistry registry) : IOperation<PageRequest, PageResult<WorkflowTaskResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "tasks.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<WorkflowTaskResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (WorkflowCaller.Of(context).EngineUser is not { } me)
        {
            return new PageResult<WorkflowTaskResponse>([], 0, request.Page);
        }

        var mapped = WorkflowQueryMapping.Tasks(request, me);
        if (!mapped.TryGetValue(out var query))
        {
            return mapped.Error!;
        }

        var queried = await tasks.QueryAsync(query, context.CancellationToken).ConfigureAwait(false);
        if (!queried.TryGetValue(out var page))
        {
            return queried.Error!;
        }

        // 分区与引擎读实例同一口径：调用方这一次调用的租户；不存在或在别的分区的不在结果里
        var found = await instanceStore.FindManyAsync(context.GetSecurity().Tenant?.Id, [.. page.Items.Select(static t => t.InstanceId).Distinct(StringComparer.Ordinal)], context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var read))
        {
            return found.Error!;
        }

        var instances = read.ToDictionary(static i => i.Id, StringComparer.Ordinal);

        var starters = await lookup.NamesAsync(page.Items.Select(static t => t.StarterId), context).ConfigureAwait(false);
        var items = new List<WorkflowTaskResponse>(page.Items.Count);
        foreach (var task in page.Items)
        {
            var instance = instances.GetValueOrDefault(task.InstanceId);
            var nodeName = instance is null ? task.NodeId : await lookup.NodeNameAsync(instance.DefinitionId, task.NodeId, context.CancellationToken).ConfigureAwait(false);
            var business = instance is { ParentInstanceId: null } ? registry.Find(task.DefinitionName.Value)?.Descriptor.NameKey : null;
            items.Add(new WorkflowTaskResponse(
                WorkflowIds.ToContract(task.Id),
                WorkflowIds.ToContract(task.InstanceId),
                task.InstanceTitle ?? task.DefinitionName.Value,
                task.DefinitionName.Value,
                task.NodeId,
                nodeName,
                task.Title ?? nodeName,
                task.CorrelationId,
                task.CreatedAt,
                WorkflowIds.UserOf(task.StarterId),
                task.StarterId is { } starter ? starters.GetValueOrDefault(starter) : null,
                business));
        }

        return new PageResult<WorkflowTaskResponse>(items, page.Total, request.Page);
    }
}

/// <summary>
/// 办理待办
/// </summary>
/// <remarks>
/// <c>POST /api/tasks/{id}:complete</c>。照 4.x 只要登录、办理人锁定为调用方；审批业务的顶层实例先回调业务的办理前校验（如不能审批自己的申请），校验不过待办保持不变
/// 办理与审批记录、走到结束节点时业务的结论回写都在同一个事务里；办理后实例故障时照常返回，带上故障原因
/// </remarks>
/// <param name="own">调用方名下的待办</param>
/// <param name="tasks">任务服务</param>
/// <param name="records">审批记录</param>
/// <param name="registry">审批业务登记表</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CompleteTask(WorkflowOwnTasks own, IWorkflowTaskService tasks, WorkflowTaskRecords records, WorkflowBusinessRegistry registry) : IOperation<CompleteTaskRequest, WorkflowTaskActionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "tasks.complete";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowTaskActionResponse>> HandleAsync(CompleteTaskRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var variables = WorkflowJsonText.ReadObject(request.Variables, "variables");
        if (!variables.TryGetValue(out var values))
        {
            return variables.Error!;
        }

        var found = await own.FindAsync(request.Id, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var task))
        {
            return found.Error!;
        }

        var outcome = request.Outcome.Trim();
        var instance = task.Instance;
        if (instance.ParentInstanceId is null && instance.CorrelationId is { Length: > 0 } businessId && registry.Find(instance.DefinitionName.Value) is { } handler)
        {
            var allowed = await handler.OnTaskCompletingAsync(new WorkflowBusinessTask(handler.Descriptor.Code, businessId, task.Snapshot.InstanceId, task.Snapshot.TaskId, task.Snapshot.NodeId, outcome, task.Caller.UserId!.Value), context).ConfigureAwait(false);
            if (allowed.IsFailure)
            {
                return allowed.Error!;
            }
        }

        var completed = await tasks.CompleteAsync([new UserTaskCompletion(task.Task.Id, task.Caller.EngineUser!, outcome, request.Comment, values.TryGetValue(out var given) ? given : null)], context.CancellationToken).ConfigureAwait(false);
        if (!completed[0].TryGetValue(out var after))
        {
            return completed[0].Error!;
        }

        await records.AppendAsync(task.Caller.Side, task.Snapshot, [task.Caller.UserId!.Value], WorkflowTaskRecordAction.Completed, outcome, request.Comment, null, context.CancellationToken).ConfigureAwait(false);
        return new WorkflowTaskActionResponse(WorkflowIds.ToContract(after.Id), after.Status, after.Status == WorkflowInstanceStatus.Faulted ? after.Fault?.Localize() : null);
    }
}

/// <summary>
/// 转办
/// </summary>
/// <remarks>
/// <c>POST /api/tasks/{id}:transfer</c>。照 4.x 只要登录；目标要是当前一侧可见的在用账号，已经是这个节点的受理人时 <c>WORKFLOW_CONFLICT_004</c>。审批业务的顶层实例先问业务的改动前校验。写一条转办记录
/// </remarks>
/// <param name="own">调用方名下的待办</param>
/// <param name="tasks">任务服务</param>
/// <param name="records">审批记录</param>
/// <param name="users">用户目录</param>
/// <param name="changes">审批业务的改动前校验</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class TransferTask(WorkflowOwnTasks own, IWorkflowTaskService tasks, WorkflowTaskRecords records, IUserDirectory users, WorkflowBusinessChanges changes) : IOperation<TransferTaskRequest, WorkflowTaskActionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "tasks.transfer";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowTaskActionResponse>> HandleAsync(TransferTaskRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await own.FindAsync(request.Id, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var task))
        {
            return found.Error!;
        }

        var allowed = await changes.CheckAsync(task.Instance, WorkflowBusinessChangeKind.Transfer, context).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var assignable = await WorkflowOwnTasks.RequireAssignableAsync(users, task.Caller.Side, [request.TargetUserId], context.CancellationToken).ConfigureAwait(false);
        if (assignable.IsFailure)
        {
            return assignable.Error!;
        }

        var transferred = await tasks.TransferAsync([new UserTaskTransfer(task.Task.Id, task.Caller.EngineUser!, WorkflowIds.ToEngineUser(request.TargetUserId), request.Comment)], context.CancellationToken).ConfigureAwait(false);
        if (transferred[0].IsFailure)
        {
            return transferred[0].Error!;
        }

        await records.AppendAsync(task.Caller.Side, task.Snapshot, [task.Caller.UserId!.Value], WorkflowTaskRecordAction.Transferred, null, request.Comment, [request.TargetUserId], context.CancellationToken).ConfigureAwait(false);
        return new WorkflowTaskActionResponse(task.Snapshot.InstanceId, task.Instance.Status, null);
    }
}

/// <summary>
/// 加签
/// </summary>
/// <remarks>
/// <c>POST /api/tasks/{id}:add-assignees</c>。照 4.x 只要登录；加的人都要是当前一侧可见的在用账号，按节点的完成策略一起办（或签任一人、会签全部、依次排在后面）。审批业务的顶层实例先问业务的改动前校验。写一条加签记录
/// </remarks>
/// <param name="own">调用方名下的待办</param>
/// <param name="tasks">任务服务</param>
/// <param name="records">审批记录</param>
/// <param name="users">用户目录</param>
/// <param name="changes">审批业务的改动前校验</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class AddTaskAssignees(WorkflowOwnTasks own, IWorkflowTaskService tasks, WorkflowTaskRecords records, IUserDirectory users, WorkflowBusinessChanges changes) : IOperation<AddTaskAssigneesRequest, WorkflowTaskActionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "tasks.add-assignees";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowTaskActionResponse>> HandleAsync(AddTaskAssigneesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await own.FindAsync(request.Id, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var task))
        {
            return found.Error!;
        }

        var allowed = await changes.CheckAsync(task.Instance, WorkflowBusinessChangeKind.AddAssignees, context).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var targets = request.UserIds.Distinct().ToList();
        var assignable = await WorkflowOwnTasks.RequireAssignableAsync(users, task.Caller.Side, targets, context.CancellationToken).ConfigureAwait(false);
        if (assignable.IsFailure)
        {
            return assignable.Error!;
        }

        var added = await tasks.AddAssigneesAsync([new UserTaskAddition(task.Task.Id, task.Caller.EngineUser!, targets.Select(WorkflowIds.ToEngineUser).ToList(), request.Comment)], context.CancellationToken).ConfigureAwait(false);
        if (added[0].IsFailure)
        {
            return added[0].Error!;
        }

        await records.AppendAsync(task.Caller.Side, task.Snapshot, [task.Caller.UserId!.Value], WorkflowTaskRecordAction.AddedAssignees, null, request.Comment, targets, context.CancellationToken).ConfigureAwait(false);
        return new WorkflowTaskActionResponse(task.Snapshot.InstanceId, task.Instance.Status, null);
    }
}
