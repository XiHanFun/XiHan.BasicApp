// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Activities;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Serialization;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Domain.Services;

/// <summary>
/// 审批业务服务（<see cref="IWorkflowBusinessService"/> 的实现）
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>WorkflowBusinessService</c>：业务码即流程定义名，单据 ID 写进实例的关联 ID；同一单据同一时刻只许一个进行中的实例（框架的 <see cref="WorkflowReusePolicy.RejectActive"/>，
/// 已有时 <c>WORKFLOW_CONFLICT_005</c>）。引擎写入加入调用方的工作单元：发起与业务写入一起提交或回滚；发起即故障时返回失败，调用方随之回滚，不留半截审批（4.x 在没有事务时另行终止）
/// 当前分区一个这个名字的定义都没有时，按业务描述的缺省定义建并发布（每个分区一份，平台与租户互不可见）
/// </remarks>
/// <param name="registry">审批业务登记表</param>
/// <param name="engine">引擎</param>
/// <param name="catalog">定义目录</param>
/// <param name="tasks">任务服务</param>
/// <param name="lookup">人名与定义</param>
public sealed class WorkflowBusinessService(WorkflowBusinessRegistry registry, IWorkflowEngine engine, IWorkflowDefinitionCatalog catalog, IWorkflowTaskService tasks, WorkflowLookup lookup) : IWorkflowBusinessService
{
    /// <summary>
    /// 一次查进度最多的单据数
    /// </summary>
    public const int MaxProgressIds = WorkflowInstanceQuery.MaxCorrelationIds;

    /// <inheritdoc />
    public async ValueTask<Result<WorkflowBusinessStartResult>> StartAsync(WorkflowBusinessStartRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BusinessId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        var found = Find(request.BusinessCode);
        if (!found.TryGetValue(out var handler))
        {
            return found.Error!;
        }

        var ensured = await EnsureDefinitionAsync(handler.Descriptor, context.CancellationToken).ConfigureAwait(false);
        if (ensured.IsFailure)
        {
            return ensured.Error!;
        }

        var started = await engine.StartAsync(
            new WorkflowStartRequest
            {
                Name = new WorkflowName(handler.Descriptor.Code),
                DisplayName = request.Title.Trim(),
                CorrelationId = request.BusinessId.Trim(),
                StarterId = WorkflowCaller.Of(context).EngineUser,
                Variables = request.Variables,
                ReusePolicy = WorkflowReusePolicy.RejectActive,
            },
            context.CancellationToken).ConfigureAwait(false);
        if (!started.TryGetValue(out var instance))
        {
            return started.Error!;
        }

        // 发起即故障（如首个审批节点解析不到人）：整单失败，调用方随之回滚
        if (instance.Status == WorkflowInstanceStatus.Faulted)
        {
            return Error.Conflict(WorkflowErrors.StartFaulted, WorkflowMessages.StartFaulted, MessageArguments.Of(("reason", instance.Fault?.Localize() ?? string.Empty)));
        }

        return new WorkflowBusinessStartResult(WorkflowIds.ToContract(instance.Id), WorkflowBusinessStatuses.Of(instance.Status), instance.Outcome);
    }

    /// <inheritdoc />
    public async ValueTask<Result<bool>> WithdrawAsync(string businessCode, string businessId, string? reason, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessId);
        var found = Find(businessCode);
        if (!found.TryGetValue(out var handler))
        {
            return found.Error!;
        }

        var listed = await engine.ListInstancesAsync(
            new WorkflowInstanceQuery
            {
                DefinitionName = new WorkflowName(handler.Descriptor.Code),
                CorrelationIds = [businessId.Trim()],
                TopLevelOnly = true,
                Statuses = [WorkflowInstanceStatus.Running, WorkflowInstanceStatus.Suspended, WorkflowInstanceStatus.Faulted],
                Limit = WorkflowInstanceQuery.MaxLimit,
            },
            context.CancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var active))
        {
            return listed.Error!;
        }

        if (active.Count == 0)
        {
            return false;
        }

        var canceled = await engine.CancelAsync(active.Select(static i => i.Id).ToList(), reason, context.CancellationToken).ConfigureAwait(false);
        foreach (var outcome in canceled)
        {
            if (outcome.IsFailure)
            {
                return outcome.Error!;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyDictionary<string, WorkflowBusinessProgress>>> ProgressAsync(string businessCode, IReadOnlyCollection<string> businessIds, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(businessIds);
        ArgumentNullException.ThrowIfNull(context);
        var found = Find(businessCode);
        if (!found.TryGetValue(out var handler))
        {
            return found.Error!;
        }

        var ids = businessIds.Where(static id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count > MaxProgressIds)
        {
            return Error.Validation(WorkflowErrors.ProgressTooMany, WorkflowMessages.ProgressTooMany, MessageArguments.Of(("max", MaxProgressIds)));
        }

        var progress = new Dictionary<string, WorkflowBusinessProgress>(StringComparer.Ordinal);
        if (ids.Count == 0)
        {
            return progress;
        }

        // 框架按创建时间降序给：每张单据取第一个见到的（最近一次的顶层实例）
        var latest = new Dictionary<string, WorkflowInstance>(StringComparer.Ordinal);
        for (var skip = 0; ; skip += WorkflowInstanceQuery.MaxLimit)
        {
            var listed = await engine.ListInstancesAsync(
                new WorkflowInstanceQuery { DefinitionName = new WorkflowName(handler.Descriptor.Code), CorrelationIds = ids, TopLevelOnly = true, Skip = skip, Limit = WorkflowInstanceQuery.MaxLimit },
                context.CancellationToken).ConfigureAwait(false);
            if (!listed.TryGetValue(out var page))
            {
                return listed.Error!;
            }

            foreach (var instance in page)
            {
                latest.TryAdd(instance.CorrelationId!, instance);
            }

            if (page.Count < WorkflowInstanceQuery.MaxLimit)
            {
                break;
            }
        }

        // 只有运行中的实例才有待办，按实例取一次
        var pending = new Dictionary<string, IReadOnlyList<UserTask>>(StringComparer.Ordinal);
        foreach (var instance in latest.Values.Where(static i => i.Status == WorkflowInstanceStatus.Running))
        {
            var waiting = await tasks.PendingByInstanceAsync(instance.Id, context.CancellationToken).ConfigureAwait(false);
            if (!waiting.TryGetValue(out var list))
            {
                return waiting.Error!;
            }

            pending[instance.Id] = list;
        }

        var names = await lookup.NamesAsync(pending.Values.SelectMany(static t => t).Select(static t => t.AssigneeId), context.CancellationToken).ConfigureAwait(false);
        foreach (var (businessId, instance) in latest)
        {
            var waiting = pending.GetValueOrDefault(instance.Id) ?? [];
            var nodeNames = new List<string>();
            foreach (var nodeId in waiting.Select(static t => t.NodeId).Distinct(StringComparer.Ordinal))
            {
                nodeNames.Add(await lookup.NodeNameAsync(instance.DefinitionId, nodeId, context.CancellationToken).ConfigureAwait(false));
            }

            var assignees = waiting.Select(static t => t.AssigneeId).Distinct(StringComparer.Ordinal)
                .Select(id => (Id: WorkflowIds.UserOf(id), Name: names.GetValueOrDefault(id) ?? id))
                .Where(static a => a.Id is not null)
                .Select(static a => new WorkflowBusinessAssignee(a.Id!.Value, a.Name))
                .ToList();
            progress[businessId] = new WorkflowBusinessProgress(
                WorkflowIds.ToContract(instance.Id),
                WorkflowBusinessStatuses.Of(instance.Status),
                instance.Outcome,
                nodeNames,
                assignees,
                instance.Fault?.Localize(),
                instance.StartedAt ?? instance.CreatedAt,
                instance.EndedAt);
        }

        return progress;
    }

    private Result<IWorkflowBusinessHandler> Find(string businessCode)
    {
        if (registry.Find(businessCode) is { } handler)
        {
            return Result.Success(handler);
        }

        return Error.Validation(WorkflowErrors.BusinessNotRegistered, WorkflowMessages.BusinessNotRegistered, MessageArguments.Of(("code", businessCode)));
    }

    /// <summary>
    /// 当前分区还没有这个名字的定义时，按缺省定义建并发布
    /// </summary>
    private async Task<Result> EnsureDefinitionAsync(WorkflowBusinessDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (descriptor.DefaultDefinition is null)
        {
            return Result.Success();
        }

        var existing = await catalog.QueryAsync(new WorkflowDefinitionQuery { Name = new WorkflowName(descriptor.Code), Limit = 1 }, cancellationToken).ConfigureAwait(false);
        if (!existing.TryGetValue(out var page))
        {
            return existing.Error!;
        }

        if (page.Total > 0)
        {
            return Result.Success();
        }

        // 缺省定义是业务包自己的资源：名字对不上、不是合法的 WorkflowJson 都是业务包的缺陷
        var json = JsonNode.Parse(descriptor.DefaultDefinition) as JsonObject
            ?? throw new InvalidOperationException($"The default definition of the approval business '{descriptor.Code}' is not a JSON object.");
        if (!string.Equals((string?)json["name"], descriptor.Code, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The default definition of the approval business '{descriptor.Code}' must be named '{descriptor.Code}'.");
        }

        json["id"] = WorkflowIds.ToEngine(Guid.CreateVersion7());
        json["version"] ??= 1;
        var definition = WorkflowJson.DeserializeDefinition(json.ToJsonString()).ValueOrThrow();
        var guarded = WorkflowDefinitionGuard.Check(definition, descriptor);
        if (guarded.IsFailure)
        {
            return guarded;
        }

        var created = await catalog.CreateAsync(definition, cancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var draft))
        {
            return created.Error!;
        }

        var published = await catalog.PublishAsync(draft.Id, cancellationToken).ConfigureAwait(false);
        return published.ToResult();
    }
}

/// <summary>
/// 结论回写（结束节点 → 审批业务）
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>WorkflowBusinessOutcomeHandler</c>：只分发登记过的业务的顶层实例；结论不在业务允许的范围里、实例没有单据 ID 即返回失败，结束节点随之故障、实例停在故障态，修好后重试再分发
/// 在实例锁内、与引擎同一个调用和事务里执行（框架 <see cref="IWorkflowOutcomeHandler"/>），业务的写入与实例完成一起提交
/// </remarks>
/// <param name="registry">审批业务登记表</param>
public sealed class WorkflowBusinessOutcomeDispatcher(WorkflowBusinessRegistry registry) : IWorkflowOutcomeHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(WorkflowOutcomeContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var instance = context.Instance;
        if (instance.ParentInstanceId is not null || registry.Find(instance.DefinitionName.Value) is not { } handler)
        {
            return Result.Success();
        }

        var allowed = WorkflowBusinessRegistry.OutcomesOf(handler.Descriptor);
        if (!allowed.Contains(context.Outcome, StringComparer.Ordinal))
        {
            return Error.Validation(WorkflowErrors.BusinessOutcomeInvalid, WorkflowMessages.BusinessOutcomeInvalid, MessageArguments.Of(("node", context.Node.Id), ("outcome", context.Outcome), ("allowed", string.Join(", ", allowed))));
        }

        if (string.IsNullOrWhiteSpace(instance.CorrelationId))
        {
            return Error.Validation(WorkflowErrors.BusinessIdMissing, WorkflowMessages.BusinessIdMissing, MessageArguments.Of(("instance", instance.Id)));
        }

        return await handler.OnOutcomeAsync(new WorkflowBusinessOutcome(handler.Descriptor.Code, instance.CorrelationId, WorkflowIds.ToContract(instance.Id), context.Outcome), context.Activity.Invocation).ConfigureAwait(false);
    }
}

/// <summary>
/// 改动前校验（转办、加签、撤回与管理员的实例控制之前问业务）
/// </summary>
/// <remarks>
/// 只对登记过的业务、有单据 ID 的顶层实例回调 <see cref="IWorkflowBusinessHandler.OnChangingAsync"/>（如授权申请在模仿会话里一律拒绝）。批量控制先逐个校验，
/// 不过的那一项记失败、不交给引擎，免得流程已中止而单据还待审批；找不到的实例不在这里报，交给引擎照常报
/// </remarks>
/// <param name="registry">审批业务登记表</param>
/// <param name="engine">引擎</param>
public sealed class WorkflowBusinessChanges(WorkflowBusinessRegistry registry, IWorkflowEngine engine)
{
    /// <summary>
    /// 校验一个实例
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="kind">改动的种类</param>
    /// <param name="context">调用上下文</param>
    /// <returns>业务是否允许；不需要回调时成功</returns>
    public async ValueTask<Result> CheckAsync(WorkflowInstance instance, WorkflowBusinessChangeKind kind, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(context);
        if (instance.ParentInstanceId is not null
            || string.IsNullOrWhiteSpace(instance.CorrelationId)
            || registry.Find(instance.DefinitionName.Value) is not { } handler)
        {
            return Result.Success();
        }

        return await handler.OnChangingAsync(new WorkflowBusinessChange(handler.Descriptor.Code, instance.CorrelationId, WorkflowIds.ToContract(instance.Id), kind), context).ConfigureAwait(false);
    }

    /// <summary>
    /// 批量控制：校验过的交给引擎，不过的记失败
    /// </summary>
    /// <param name="ids">实例 ID（已去重）</param>
    /// <param name="kind">改动的种类</param>
    /// <param name="run">对校验过的那些调引擎</param>
    /// <param name="context">调用上下文</param>
    /// <returns>与 <paramref name="ids"/> 一一对应的结果</returns>
    public async Task<IReadOnlyList<Result<WorkflowInstance>>> RunAsync(IReadOnlyList<Guid> ids, WorkflowBusinessChangeKind kind, Func<IReadOnlyList<string>, ValueTask<IReadOnlyList<Result<WorkflowInstance>>>> run, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var denied = new Dictionary<Guid, Error>();
        foreach (var id in ids)
        {
            var found = await engine.FindInstanceAsync(WorkflowIds.ToEngine(id), context.CancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var maybe) || !maybe.TryGetValue(out var instance))
            {
                continue;
            }

            var allowed = await CheckAsync(instance, kind, context).ConfigureAwait(false);
            if (allowed.IsFailure)
            {
                denied[id] = allowed.Error!;
            }
        }

        var passed = ids.Where(id => !denied.ContainsKey(id)).ToList();
        IReadOnlyList<Result<WorkflowInstance>> outcomes = passed.Count == 0 ? [] : await run(passed.Select(WorkflowIds.ToEngine).ToList()).ConfigureAwait(false);
        var merged = new List<Result<WorkflowInstance>>(ids.Count);
        var next = 0;
        foreach (var id in ids)
        {
            merged.Add(denied.TryGetValue(id, out var error) ? error : outcomes[next++]);
        }

        return merged;
    }
}

/// <summary>
/// 中止回写（管理员取消、终止，发起人撤回之后通知业务）
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>WorkflowBusinessAbortNotifier</c>：只对登记过的业务、有单据 ID、已取消或已终止的顶层实例回调；业务自己经 <see cref="IWorkflowBusinessService.WithdrawAsync"/> 撤回的不经过这里
/// 回写在操作的同一个事务里：批量取消、终止时回写失败记为那一项失败（实例照样已中止，同 4.x）；发起人撤回是单条，回写失败即整个撤回回滚
/// </remarks>
/// <param name="registry">审批业务登记表</param>
public sealed class WorkflowBusinessAborts(WorkflowBusinessRegistry registry)
{
    /// <summary>
    /// 通知业务
    /// </summary>
    /// <param name="instance">已中止的实例</param>
    /// <param name="reason">原因</param>
    /// <param name="context">调用上下文</param>
    /// <returns>业务的回写结果；不需要回调时成功</returns>
    public async ValueTask<Result> NotifyAsync(WorkflowInstance instance, string? reason, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(context);
        if (instance.ParentInstanceId is not null
            || string.IsNullOrWhiteSpace(instance.CorrelationId)
            || instance.Status is not (WorkflowInstanceStatus.Canceled or WorkflowInstanceStatus.Terminated)
            || registry.Find(instance.DefinitionName.Value) is not { } handler)
        {
            return Result.Success();
        }

        var aborted = new WorkflowBusinessAborted(handler.Descriptor.Code, instance.CorrelationId, WorkflowIds.ToContract(instance.Id), WorkflowBusinessStatuses.Of(instance.Status), reason ?? instance.CancellationReason);
        return await handler.OnAbortedAsync(aborted, context).ConfigureAwait(false);
    }
}

/// <summary>
/// 实例状态到审批业务状态的换算
/// </summary>
public static class WorkflowBusinessStatuses
{
    /// <summary>
    /// 换算
    /// </summary>
    public static WorkflowBusinessStatus Of(WorkflowInstanceStatus status) => status switch
    {
        WorkflowInstanceStatus.Running => WorkflowBusinessStatus.Running,
        WorkflowInstanceStatus.Suspended => WorkflowBusinessStatus.Suspended,
        WorkflowInstanceStatus.Completed => WorkflowBusinessStatus.Completed,
        WorkflowInstanceStatus.Canceled => WorkflowBusinessStatus.Canceled,
        WorkflowInstanceStatus.Faulted => WorkflowBusinessStatus.Faulted,
        WorkflowInstanceStatus.Terminated => WorkflowBusinessStatus.Terminated,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
