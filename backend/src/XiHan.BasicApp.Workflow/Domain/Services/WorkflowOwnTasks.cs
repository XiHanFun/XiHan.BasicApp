// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Users;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Domain.Services;

/// <summary>
/// 调用方名下的一条待办
/// </summary>
/// <param name="Caller">调用方</param>
/// <param name="Task">任务</param>
/// <param name="Instance">所属实例</param>
/// <param name="Snapshot">写审批记录用的快照</param>
public sealed record WorkflowOwnTask(WorkflowCaller Caller, UserTask Task, WorkflowInstance Instance, WorkflowTaskSnapshot Snapshot);

/// <summary>
/// 办理、转办与加签共用的取待办与目标核对（作用域）
/// </summary>
/// <remarks>
/// 只在调用方自己的待办里找，不存在、已办理与不归调用方合成一个 <see cref="WorkflowErrors.TaskNotFound"/>（不透露别人的待办是否存在）
/// </remarks>
/// <param name="tasks">任务服务</param>
/// <param name="engine">引擎</param>
public sealed class WorkflowOwnTasks(IWorkflowTaskService tasks, IWorkflowEngine engine)
{
    /// <summary>
    /// 取调用方名下的一条待办
    /// </summary>
    /// <param name="taskId">任务 ID</param>
    /// <param name="context">调用上下文</param>
    /// <returns>待办、所属实例与快照</returns>
    public async Task<Result<WorkflowOwnTask>> FindAsync(Guid taskId, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var caller = WorkflowCaller.Of(context);
        var notFound = Error.NotFound(WorkflowErrors.TaskNotFound, WorkflowMessages.TaskNotFound, MessageArguments.Of(("id", taskId)));
        if (caller.EngineUser is not { } me)
        {
            return notFound;
        }

        var pending = await tasks.PendingAsync(me, context.CancellationToken).ConfigureAwait(false);
        if (!pending.TryGetValue(out var mine))
        {
            return pending.Error!;
        }

        var engineId = WorkflowIds.ToEngine(taskId);
        if (mine.FirstOrDefault(t => string.Equals(t.Id, engineId, StringComparison.Ordinal)) is not { } task)
        {
            return notFound;
        }

        var found = await engine.FindInstanceAsync(task.InstanceId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var maybe))
        {
            return found.Error!;
        }

        if (!maybe.TryGetValue(out var instance))
        {
            return notFound;
        }

        var snapshot = new WorkflowTaskSnapshot(taskId, WorkflowIds.ToContract(instance.Id), instance.DefinitionId, instance.DefinitionName.Value, instance.Title, task.NodeId, task.Title ?? instance.Title, instance.CorrelationId);
        return new WorkflowOwnTask(caller, task, instance, snapshot);
    }

    /// <summary>
    /// 转办与加签的目标都要是这一侧可见的在用账号
    /// </summary>
    /// <param name="users">用户目录</param>
    /// <param name="side">侧</param>
    /// <param name="targets">目标</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第一个不能作为受理人的目标即失败</returns>
    public static async Task<Result> RequireAssignableAsync(IUserDirectory users, string side, IReadOnlyCollection<Guid> targets, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(targets);
        var visible = await users.ActiveInSideAsync(side, targets, cancellationToken).ConfigureAwait(false);
        var missing = targets.FirstOrDefault(t => !visible.Contains(t));
        return targets.All(visible.Contains)
            ? Result.Success()
            : Error.Validation(WorkflowErrors.AssigneeUnavailable, WorkflowMessages.AssigneeUnavailable, MessageArguments.Of(("id", missing)));
    }
}
