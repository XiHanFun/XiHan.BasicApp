// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Workflow;

/// <summary>
/// 审批业务服务（Workflow 实现）。
/// </summary>
/// <remarks>
/// 业务包经它发起、撤回自己单据的审批流，查一批单据的审批进度。都在调用方的事务里：发起与业务写入一起提交或回滚。
/// Workflow 没装时没有实现，调用方以 <c>IEnumerable&lt;IWorkflowBusinessService&gt;</c> 注入，自己处理「审批未启用」。
/// </remarks>
public interface IWorkflowBusinessService
{
    /// <summary>
    /// 发起审批。
    /// </summary>
    /// <remarks>
    /// 业务码即流程定义名；当前分区没有这个名字的定义时按业务描述的缺省定义建并发布。同一单据已有进行中的实例即冲突；发起即故障（如首个审批节点找不到人）时整单失败。
    /// </remarks>
    /// <param name="request">业务码、单据、标题与变量。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>新实例。</returns>
    ValueTask<Result<WorkflowBusinessStartResult>> StartAsync(WorkflowBusinessStartRequest request, InvocationContext context);

    /// <summary>
    /// 撤回单据进行中的审批。
    /// </summary>
    /// <remarks>
    /// 取消该单据进行中的顶层实例（不回调业务的中止回写）；没有进行中的返回 <c>false</c>。
    /// </remarks>
    /// <param name="businessCode">业务码。</param>
    /// <param name="businessId">单据 ID。</param>
    /// <param name="reason">原因。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>是否撤回了实例。</returns>
    ValueTask<Result<bool>> WithdrawAsync(string businessCode, string businessId, string? reason, InvocationContext context);

    /// <summary>
    /// 一批单据的审批进度。
    /// </summary>
    /// <remarks>
    /// 每张单据取最近一次的顶层实例；没有发起过的不在结果里。
    /// </remarks>
    /// <param name="businessCode">业务码。</param>
    /// <param name="businessIds">单据 ID（一次最多 1000 个）。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>按单据 ID 索引的进度。</returns>
    ValueTask<Result<IReadOnlyDictionary<string, WorkflowBusinessProgress>>> ProgressAsync(string businessCode, IReadOnlyCollection<string> businessIds, InvocationContext context);
}
