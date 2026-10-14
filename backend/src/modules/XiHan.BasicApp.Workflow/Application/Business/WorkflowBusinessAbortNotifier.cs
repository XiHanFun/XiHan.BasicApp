// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 流程中止回写（审批中心撤回、管理员取消/终止实例之后通知业务）
/// </summary>
/// <remarks>
/// 业务自己发起的撤回（<see cref="IWorkflowBusinessService.WithdrawAsync"/>）已自行更新单据，不经过这里。
/// 回写失败时实例已经中止（引擎状态独立落库），以业务异常告知操作人，由其核对单据状态。
/// </remarks>
public sealed class WorkflowBusinessAbortNotifier
{
    private readonly WorkflowBusinessRegistry _registry;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowBusinessAbortNotifier(WorkflowBusinessRegistry registry, IUnitOfWorkManager unitOfWorkManager, ICurrentUser currentUser)
    {
        _registry = registry;
        _unitOfWorkManager = unitOfWorkManager;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 通知业务流程已中止（非业务流程、子流程或实例未处于取消/终止态时不做任何事）
    /// </summary>
    /// <param name="instance">已中止的实例</param>
    /// <param name="reason">中止原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task NotifyAsync(WorkflowInstance instance, string? reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var handler = _registry.Find(instance.DefinitionCode);
        if (handler is null
            || !string.IsNullOrEmpty(instance.ParentInstanceId)
            || string.IsNullOrWhiteSpace(instance.CorrelationId)
            || instance.Status is not (WorkflowInstanceStatus.Canceled or WorkflowInstanceStatus.Terminated))
        {
            return;
        }

        var context = new WorkflowBusinessAbortedContext
        {
            BusinessCode = handler.Descriptor.Code,
            BusinessId = instance.CorrelationId,
            InstanceId = long.Parse(instance.Id, NumberStyles.None, CultureInfo.InvariantCulture),
            Status = instance.Status,
            OperatorUserId = _currentUser.UserId,
            Reason = reason ?? instance.CancellationReason
        };

        try
        {
            var ambient = _unitOfWorkManager.Current;
            if (ambient is { IsCompleted: false, IsDisposed: false } && ambient.Options.IsTransactional)
            {
                await handler.OnAbortedAsync(context, cancellationToken);
                return;
            }

            using var unitOfWork = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(isTransactional: true), requiresNew: true);
            await handler.OnAbortedAsync(context, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var action = instance.Status == WorkflowInstanceStatus.Canceled ? "撤回" : "终止";
            throw new BusinessException(message: $"流程已{action}，但{handler.Descriptor.Name}状态回写失败：{ex.Message}");
        }
    }
}
