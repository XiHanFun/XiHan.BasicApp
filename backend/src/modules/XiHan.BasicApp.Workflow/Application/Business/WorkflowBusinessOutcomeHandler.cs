// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Workflow.Abstractions.Outcomes;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 流程结论分发器（结果结束节点 → 审批业务处理器）
/// </summary>
/// <remarks>
/// 只分发登记过的业务流程的顶层实例；结论不在业务允许的结果里视为定义错误、节点故障。
/// 事务边界：调用方已在事务型工作单元里（业务发起时流程直接走完）就在其中执行，处理器能看到同一事务里刚写入的单据，
/// 失败随业务一起回滚；否则（审批人办理、定时器、重试）为处理器单开事务型工作单元，失败即回滚处理器的全部写入，
/// 结束节点随之故障，实例停在故障态，修正后重试再次分发。
/// </remarks>
public sealed class WorkflowBusinessOutcomeHandler : IWorkflowOutcomeHandler
{
    private readonly WorkflowBusinessRegistry _registry;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowBusinessOutcomeHandler(WorkflowBusinessRegistry registry, IUnitOfWorkManager unitOfWorkManager)
    {
        _registry = registry;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 处理流程结论
    /// </summary>
    /// <param name="context">结论上下文</param>
    /// <returns>任务</returns>
    public async Task HandleAsync(WorkflowOutcomeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handler = _registry.Find(context.Definition.Code);
        if (handler is null || !string.IsNullOrEmpty(context.Instance.ParentInstanceId))
        {
            return;
        }

        var descriptor = handler.Descriptor;
        if (!descriptor.Outcomes.Contains(context.Outcome, StringComparer.Ordinal))
        {
            throw new BusinessException(
                message: $"{descriptor.Name}流程的结束节点声明了不支持的结果 {context.Outcome}（允许：{string.Join("、", descriptor.Outcomes)}）");
        }

        if (string.IsNullOrWhiteSpace(context.Instance.CorrelationId))
        {
            throw new BusinessException(message: $"{descriptor.Name}流程实例 {context.Instance.Id} 缺少业务单据标识，无法回写结论");
        }

        var businessContext = new WorkflowBusinessOutcomeContext
        {
            BusinessCode = descriptor.Code,
            BusinessId = context.Instance.CorrelationId,
            InstanceId = long.Parse(context.Instance.Id, NumberStyles.None, CultureInfo.InvariantCulture),
            Outcome = context.Outcome,
            StarterUserId = long.TryParse(context.Instance.StarterId, NumberStyles.None, CultureInfo.InvariantCulture, out var starterId) ? starterId : null,
            Variables = context.Variables
        };

        var ambient = _unitOfWorkManager.Current;
        if (ambient is { IsCompleted: false, IsDisposed: false } && ambient.Options.IsTransactional)
        {
            await handler.OnOutcomeAsync(businessContext, context.CancellationToken);
            return;
        }

        using var unitOfWork = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(isTransactional: true), requiresNew: true);
        await handler.OnOutcomeAsync(businessContext, context.CancellationToken);
        await unitOfWork.CompleteAsync(context.CancellationToken);
    }
}
