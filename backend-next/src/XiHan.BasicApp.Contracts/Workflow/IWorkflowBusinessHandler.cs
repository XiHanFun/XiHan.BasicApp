// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Workflow;

/// <summary>
/// 审批业务（业务包实现）。
/// </summary>
/// <remarks>
/// 业务包把自己的单据接进审批流：描述（业务码即流程定义名、单据组件、允许的结论、缺省定义），办理前校验、改动前校验、结论回写、中止回写与单据数据。
/// 业务包在 <c>Configure</c> 里以 <see cref="WorkflowBusinessExtensions.AddWorkflowBusiness{THandler}"/> 登记，同一业务码登记两次时 Workflow 启动即失败；
/// Workflow 没装时没有消费方。各回调都在引擎这次操作的调用里执行，与流程写入同一个事务。
/// </remarks>
public interface IWorkflowBusinessHandler
{
    /// <summary>
    /// 业务描述。
    /// </summary>
    WorkflowBusinessDescriptor Descriptor { get; }

    /// <summary>
    /// 办理前校验。
    /// </summary>
    /// <remarks>
    /// 只读校验：失败即拒绝这次办理，待办保留。
    /// </remarks>
    /// <param name="task">办理的任务。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>是否允许办理。</returns>
    ValueTask<Result> OnTaskCompletingAsync(WorkflowBusinessTask task, InvocationContext context);

    /// <summary>
    /// 改动前校验。
    /// </summary>
    /// <remarks>
    /// 办理之外的改动（转办、加签、撤回，管理员的取消、终止、重试、挂起与恢复）之前调用；只读校验，失败即拒绝这次改动，批量操作里那一项记失败、不发起。
    /// </remarks>
    /// <param name="change">改动。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>是否允许改动。</returns>
    ValueTask<Result> OnChangingAsync(WorkflowBusinessChange change, InvocationContext context);

    /// <summary>
    /// 结论回写。
    /// </summary>
    /// <remarks>
    /// 顶层实例走到带结论的结束节点时调用；失败即该结束节点故障、实例停在故障态，重试时再调（要幂等）。
    /// </remarks>
    /// <param name="outcome">结论。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>回写结果。</returns>
    ValueTask<Result> OnOutcomeAsync(WorkflowBusinessOutcome outcome, InvocationContext context);

    /// <summary>
    /// 中止回写。
    /// </summary>
    /// <remarks>
    /// 管理员取消、终止或发起人在审批中心撤回之后调用（业务自己经 <see cref="IWorkflowBusinessService.WithdrawAsync"/> 撤回的不回调）。
    /// </remarks>
    /// <param name="aborted">中止的实例。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>回写结果。</returns>
    ValueTask<Result> OnAbortedAsync(WorkflowBusinessAborted aborted, InvocationContext context);

    /// <summary>
    /// 单据数据。
    /// </summary>
    /// <remarks>
    /// 审批详情里给单据组件看的数据（JSON 文本）；单据不存在时返回 NotFound。
    /// </remarks>
    /// <param name="businessId">单据 ID。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>JSON 文本。</returns>
    ValueTask<Result<string>> GetFormDataAsync(string businessId, InvocationContext context);
}

/// <summary>
/// 审批业务的登记。
/// </summary>
public static class WorkflowBusinessExtensions
{
    /// <summary>
    /// 登记一个审批业务。
    /// </summary>
    /// <typeparam name="THandler">业务处理器。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <returns>同一个服务集合。</returns>
    public static IServiceCollection AddWorkflowBusiness<THandler>(this IServiceCollection services)
        where THandler : class, IWorkflowBusinessHandler
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IWorkflowBusinessHandler, THandler>());
        return services;
    }
}
