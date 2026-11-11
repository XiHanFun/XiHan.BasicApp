// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Domain.Services;

/// <summary>
/// 某人或某实例的全部待办
/// </summary>
/// <remarks>
/// 框架的待办只经分页查询读取（ADR-0149），这里按最大页长翻页读完
/// </remarks>
internal static class PendingTasks
{
    /// <summary>
    /// 某人的全部待办
    /// </summary>
    /// <param name="tasks">待办服务</param>
    /// <param name="assigneeId">引擎里的用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task<Result<IReadOnlyList<UserTask>>> OfAssigneeAsync(IWorkflowTaskService tasks, string assigneeId, CancellationToken cancellationToken)
        => AllAsync(tasks, new UserTaskQuery { AssigneeId = assigneeId }, cancellationToken);

    /// <summary>
    /// 某实例的全部待办
    /// </summary>
    /// <param name="tasks">待办服务</param>
    /// <param name="instanceId">实例 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task<Result<IReadOnlyList<UserTask>>> OfInstanceAsync(IWorkflowTaskService tasks, string instanceId, CancellationToken cancellationToken)
        => AllAsync(tasks, new UserTaskQuery { InstanceId = instanceId }, cancellationToken);

    /// <summary>
    /// 按查询翻页读完
    /// </summary>
    private static async Task<Result<IReadOnlyList<UserTask>>> AllAsync(IWorkflowTaskService tasks, UserTaskQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        var all = new List<UserTask>();
        for (var skip = 0; ; skip += UserTaskQuery.MaxTake)
        {
            var page = await tasks.QueryAsync(query with { Skip = skip, Take = UserTaskQuery.MaxTake }, cancellationToken).ConfigureAwait(false);
            if (!page.TryGetValue(out var value))
            {
                return page.Error!;
            }

            all.AddRange(value.Items);
            if (value.Items.Count < UserTaskQuery.MaxTake || all.Count >= value.Total)
            {
                return all;
            }
        }
    }
}
