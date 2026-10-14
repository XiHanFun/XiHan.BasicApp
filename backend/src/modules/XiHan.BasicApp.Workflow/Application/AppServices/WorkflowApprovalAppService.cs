// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Application.Contracts;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Application.AppServices;

/// <summary>
/// 审批中心命令应用服务
/// </summary>
/// <remarks>
/// 撤回只允许发起人对自己进行中的流程操作（登录即可，不设权限码）；审批业务流程撤回后回写单据状态。
/// </remarks>
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务", Tag = "审批中心")]
public sealed class WorkflowApprovalAppService : WorkflowApplicationService, IWorkflowApprovalAppService
{
    private readonly IWorkflowEngine _engine;
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly WorkflowBusinessAbortNotifier _abortNotifier;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowApprovalAppService(
        IWorkflowEngine engine,
        IWorkflowInstanceStore instanceStore,
        WorkflowBusinessAbortNotifier abortNotifier,
        ICurrentUser currentUser)
    {
        _engine = engine;
        _instanceStore = instanceStore;
        _abortNotifier = abortNotifier;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 发起人撤回进行中的流程
    /// </summary>
    public async Task WithdrawAsync(WorkflowApprovalWithdrawDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.InstanceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "实例主键必须大于 0。");
        }

        var userId = _currentUser.UserId?.ToString(CultureInfo.InvariantCulture)
            ?? throw new BusinessException(message: "当前用户未登录，无法撤回");
        var instanceId = input.InstanceId.ToString(CultureInfo.InvariantCulture);

        var instance = await _instanceStore.FindAsync(instanceId, cancellationToken)
            ?? throw new UserFriendlyException("流程实例不存在。");
        if (!string.Equals(instance.StarterId, userId, StringComparison.Ordinal))
        {
            throw new UserFriendlyException("只能撤回自己发起的流程。");
        }

        if (!string.IsNullOrEmpty(instance.ParentInstanceId))
        {
            throw new UserFriendlyException("子流程随父流程结束，不能单独撤回。");
        }

        if (instance.IsFinalStatus() && instance.Status != WorkflowInstanceStatus.Faulted)
        {
            throw new UserFriendlyException("流程已结束，不能撤回。");
        }

        var reason = string.IsNullOrWhiteSpace(input.Reason) ? "发起人撤回" : input.Reason.Trim();
        WorkflowInstance canceled;
        try
        {
            canceled = await _engine.CancelAsync(instanceId, reason, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: ex.Message);
        }

        await _abortNotifier.NotifyAsync(canceled, reason, cancellationToken);
    }
}
