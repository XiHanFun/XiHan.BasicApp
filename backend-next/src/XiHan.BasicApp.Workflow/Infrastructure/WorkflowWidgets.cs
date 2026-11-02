// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Infrastructure;

/// <summary>
/// 工作流的工作台小组件。
/// </summary>
/// <remarks>
/// 方案 W7（4.x 没有）：「我的待办」，值是调用方的待办数，条目是最近到达的几条，点开到审批中心直接办理。只要登录，Workflow 关掉时不登记。
/// </remarks>
/// <param name="tasks">任务服务。</param>
public sealed class WorkflowWidgets(IWorkflowTaskService tasks) : IWorkbenchWidgetProvider
{
    /// <summary>
    /// 我的待办。
    /// </summary>
    public const string Todo = "workflow.todo";

    /// <summary>
    /// 条目最多几条。
    /// </summary>
    private const int TodoLimit = 5;

    /// <inheritdoc />
    public IReadOnlyList<WorkbenchWidget> Widgets { get; } = [new(Todo, WorkbenchWidgetKind.List, null)];

    /// <inheritdoc />
    public async ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(key, Todo, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        if (WorkflowCaller.Of(context).EngineUser is not { } me)
        {
            return new WorkbenchWidgetData(Value: 0, Items: []);
        }

        var queried = await tasks.QueryAsync(new UserTaskQuery { AssigneeId = me, Descending = true, Limit = TodoLimit }, context.CancellationToken).ConfigureAwait(false);
        if (!queried.TryGetValue(out var page))
        {
            return queried.Error!;
        }

        return new WorkbenchWidgetData(
            Value: page.Total,
            Items: page.Items.Select(static t => new WorkbenchItem(
                WorkflowIds.ToContract(t.Id).ToString(),
                t.Title ?? t.InstanceTitle ?? t.DefinitionName.Value,
                t.DefinitionName.Value,
                t.CreatedAt,
                $"/workflow/approvals?tab=todo&instanceId={WorkflowIds.ToContract(t.InstanceId)}&taskId={WorkflowIds.ToContract(t.Id)}")).ToList());
    }
}
