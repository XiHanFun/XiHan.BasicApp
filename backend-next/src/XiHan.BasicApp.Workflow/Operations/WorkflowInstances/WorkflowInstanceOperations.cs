// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.BasicApp.Workflow.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Serialization;

namespace XiHan.BasicApp.Workflow.Operations.WorkflowInstances;

/// <summary>
/// 流程实例列表。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:list</c>。当前一侧的实例（含子流程），框架在存储里按关键字（标题、关联 ID）、状态、定义名、关联 ID、发起人与创建区间过滤、
/// 按多列排序（照 4.x）并分页，缺省创建时间降序；筛选与排序见 <see cref="WorkflowQueryMapping.Instances"/>。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="users">用户目录（发起人显示名）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesRead)]
public sealed class ListWorkflowInstances(IWorkflowEngine engine, IUserDirectory users) : IOperation<PageRequest, PageResult<WorkflowInstanceResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.list";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<WorkflowInstanceResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var mapped = WorkflowQueryMapping.Instances(request);
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

        var starters = await WorkflowInstances.StartersAsync(users, instances, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<WorkflowInstanceResponse>(instances.Select(i => WorkflowInstanceResponse.From(i, starters)).ToList(), total, request.Page);
    }
}

/// <summary>
/// 流程实例详情。
/// </summary>
/// <remarks>
/// <c>GET /api/workflow-instances/{id}</c>。带变量、节点执行历史、待恢复的等待与实例所用的定义；别的分区的当作不存在。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="catalog">定义目录。</param>
/// <param name="users">用户目录（发起人显示名）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesRead)]
public sealed class GetWorkflowInstance(IWorkflowEngine engine, IWorkflowDefinitionCatalog catalog, IUserDirectory users) : IOperation<GetWorkflowInstanceRequest, WorkflowInstanceDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.get";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowInstanceDetailResponse>> HandleAsync(GetWorkflowInstanceRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await WorkflowInstances.FindAsync(engine, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var instance))
        {
            return found.Error!;
        }

        var nodes = await engine.NodeInstancesAsync(instance.Id, context.CancellationToken).ConfigureAwait(false);
        if (!nodes.TryGetValue(out var history))
        {
            return nodes.Error!;
        }

        var bookmarks = await engine.BookmarksAsync(instance.Id, context.CancellationToken).ConfigureAwait(false);
        if (!bookmarks.TryGetValue(out var waits))
        {
            return bookmarks.Error!;
        }

        var definition = await catalog.FindAsync(instance.DefinitionId, context.CancellationToken).ConfigureAwait(false);
        if (!definition.TryGetValue(out var maybeDefinition))
        {
            return definition.Error!;
        }

        var starters = await WorkflowInstances.StartersAsync(users, [instance], context.CancellationToken).ConfigureAwait(false);
        return new WorkflowInstanceDetailResponse(
            WorkflowInstanceResponse.From(instance, starters),
            instance.Variables.ToJsonString(),
            instance.CancellationReason,
            instance.FaultNodeId,
            history.Select(WorkflowNodeInstanceResponse.From).ToList(),
            waits.Select(WorkflowWaitResponse.From).ToList(),
            maybeDefinition.TryGetValue(out var used) ? WorkflowJson.Serialize(used) : null);
    }
}

/// <summary>
/// 发起流程。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:start</c>。发起人是调用方；按名字取已发布的定义（给了版本取那个版本）。缺必填变量、定义不存在或没有已发布版本由引擎报（<c>WORKFLOW_*</c>）。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="users">用户目录（发起人显示名）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesExecute)]
public sealed class StartWorkflowInstance(IWorkflowEngine engine, IUserDirectory users) : IOperation<StartWorkflowInstanceRequest, WorkflowInstanceResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.start";

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowInstanceResponse>> HandleAsync(StartWorkflowInstanceRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var variables = WorkflowJsonText.ReadObject(request.Variables, "variables");
        if (!variables.TryGetValue(out var values))
        {
            return variables.Error!;
        }

        var started = await engine.StartAsync(
            new WorkflowStartRequest
            {
                Name = new WorkflowName(request.Name),
                Version = request.Version,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
                CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? null : request.CorrelationId.Trim(),
                Variables = values.TryGetValue(out var given) ? given : null,
                StarterId = WorkflowCaller.Of(context).EngineUser,
            },
            context.CancellationToken).ConfigureAwait(false);
        if (!started.TryGetValue(out var instance))
        {
            return started.Error!;
        }

        return WorkflowInstanceResponse.From(instance, await WorkflowInstances.StartersAsync(users, [instance], context.CancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// 取消流程实例。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:cancel</c>。批量；运行中、挂起与故障的实例可取消（启用了补偿的逆序补偿），已结束的算成功。
/// 审批业务的顶层实例取消前先问业务的改动前校验，不过的那一项记失败、不取消；取消后回调业务的中止回写，回写失败记为那一项失败（实例照样已取消，同 4.x）。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="aborts">中止回写。</param>
/// <param name="changes">审批业务的改动前校验。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesExecute)]
public sealed class CancelWorkflowInstances(IWorkflowEngine engine, WorkflowBusinessAborts aborts, WorkflowBusinessChanges changes) : IOperation<WorkflowInstanceControlRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.cancel";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowInstanceControlRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        var canceled = await changes.RunAsync(ids, WorkflowBusinessChangeKind.Cancel, passed => engine.CancelAsync(passed, request.Reason, context.CancellationToken), context).ConfigureAwait(false);
        return await WorkflowInstances.AbortedAsync(ids, canceled, request.Reason, aborts, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 终止流程实例。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:terminate</c>。批量；同取消但不补偿，审批业务同样先问改动前校验、之后回调中止回写。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="aborts">中止回写。</param>
/// <param name="changes">审批业务的改动前校验。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesExecute)]
public sealed class TerminateWorkflowInstances(IWorkflowEngine engine, WorkflowBusinessAborts aborts, WorkflowBusinessChanges changes) : IOperation<WorkflowInstanceControlRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.terminate";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowInstanceControlRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        var terminated = await changes.RunAsync(ids, WorkflowBusinessChangeKind.Terminate, passed => engine.TerminateAsync(passed, request.Reason, context.CancellationToken), context).ConfigureAwait(false);
        return await WorkflowInstances.AbortedAsync(ids, terminated, request.Reason, aborts, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 重试故障的流程实例。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:retry</c>。批量；只对有故障节点的故障实例，从故障节点重跑；审批业务的顶层实例先问改动前校验。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="changes">审批业务的改动前校验。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesExecute)]
public sealed class RetryWorkflowInstances(IWorkflowEngine engine, WorkflowBusinessChanges changes) : IOperation<WorkflowInstanceIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.retry";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowInstanceIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        return WorkflowInstances.Batch(ids, await changes.RunAsync(ids, WorkflowBusinessChangeKind.Retry, passed => engine.RetryAsync(passed, context.CancellationToken), context).ConfigureAwait(false));
    }
}

/// <summary>
/// 挂起流程实例。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:suspend</c>。批量；只对运行中的；审批业务的顶层实例先问改动前校验。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="changes">审批业务的改动前校验。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesUpdate)]
public sealed class SuspendWorkflowInstances(IWorkflowEngine engine, WorkflowBusinessChanges changes) : IOperation<WorkflowInstanceControlRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.suspend";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowInstanceControlRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        return WorkflowInstances.Batch(ids, await changes.RunAsync(ids, WorkflowBusinessChangeKind.Suspend, passed => engine.SuspendAsync(passed, request.Reason, context.CancellationToken), context).ConfigureAwait(false));
    }
}

/// <summary>
/// 恢复挂起的流程实例。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:resume</c>。批量；只对挂起的；审批业务的顶层实例先问改动前校验。
/// </remarks>
/// <param name="engine">引擎。</param>
/// <param name="changes">审批业务的改动前校验。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesUpdate)]
public sealed class ResumeWorkflowInstances(IWorkflowEngine engine, WorkflowBusinessChanges changes) : IOperation<WorkflowInstanceIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.resume";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(WorkflowInstanceIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ids = request.Ids.Distinct().ToList();
        return WorkflowInstances.Batch(ids, await changes.RunAsync(ids, WorkflowBusinessChangeKind.Resume, passed => engine.ResumeInstancesAsync(passed, context.CancellationToken), context).ConfigureAwait(false));
    }
}

/// <summary>
/// 发信号。
/// </summary>
/// <remarks>
/// <c>POST /api/workflow-instances:signal</c>。恢复当前一侧在等这个信号的实例（给了关联 ID 只恢复关联上的），返回恢复数。
/// </remarks>
/// <param name="engine">引擎。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = WorkflowPermissions.InstancesExecute)]
public sealed class SignalWorkflowInstances(IWorkflowEngine engine) : IOperation<SignalWorkflowInstancesRequest, SignalWorkflowInstancesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workflow-instances.signal";

    /// <inheritdoc />
    public async ValueTask<Result<SignalWorkflowInstancesResponse>> HandleAsync(SignalWorkflowInstancesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var payload = WorkflowJsonText.ReadObject(request.Payload, "payload");
        if (!payload.TryGetValue(out var values))
        {
            return payload.Error!;
        }

        var resumed = await engine.SignalAsync(request.Name.Trim(), values.TryGetValue(out var given) ? given : null, string.IsNullOrWhiteSpace(request.CorrelationId) ? null : request.CorrelationId.Trim(), context.CancellationToken).ConfigureAwait(false);
        return resumed.TryGetValue(out var count) ? new SignalWorkflowInstancesResponse(count) : resumed.Error!;
    }
}

/// <summary>
/// 实例操作共用的读取与换算。
/// </summary>
internal static class WorkflowInstances
{
    public static async Task<Result<WorkflowInstance>> FindAsync(IWorkflowEngine engine, Guid id, CancellationToken cancellationToken)
    {
        var found = await engine.FindInstanceAsync(WorkflowIds.ToEngine(id), cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var maybe))
        {
            return found.Error!;
        }

        return maybe.TryGetValue(out var instance)
            ? instance
            : Error.NotFound(WorkflowErrors.InstanceNotFound, WorkflowMessages.InstanceNotFound, MessageArguments.Of(("id", id)));
    }

    public static async Task<IReadOnlyDictionary<Guid, UserSummary>> StartersAsync(IUserDirectory users, IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken)
    {
        var ids = instances.Select(static i => WorkflowIds.UserOf(i.StarterId)).OfType<Guid>().Distinct().ToList();
        return ids.Count == 0 ? new Dictionary<Guid, UserSummary>() : await users.FindAsync(ids, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<BatchResult> AbortedAsync(IReadOnlyList<Guid> ids, IReadOnlyList<Result<WorkflowInstance>> outcomes, string? reason, WorkflowBusinessAborts aborts, InvocationContext context)
    {
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        for (var i = 0; i < ids.Count; i++)
        {
            if (!outcomes[i].TryGetValue(out var instance))
            {
                failed.Add(new BatchFailure(ids[i], outcomes[i].Error!.Code));
                continue;
            }

            var notified = await aborts.NotifyAsync(instance, reason, context).ConfigureAwait(false);
            if (notified.IsSuccess)
            {
                succeeded.Add(ids[i]);
            }
            else
            {
                failed.Add(new BatchFailure(ids[i], notified.Error!.Code));
            }
        }

        return new BatchResult(succeeded, failed);
    }

    public static BatchResult Batch(IReadOnlyList<Guid> ids, IReadOnlyList<Result<WorkflowInstance>> outcomes)
    {
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        for (var i = 0; i < ids.Count; i++)
        {
            if (outcomes[i].IsSuccess)
            {
                succeeded.Add(ids[i]);
            }
            else
            {
                failed.Add(new BatchFailure(ids[i], outcomes[i].Error!.Code));
            }
        }

        return new BatchResult(succeeded, failed);
    }
}
