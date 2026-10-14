// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 工作流模块未加载时的审批业务服务（每个调用都明确失败，业务不会绕过审批直接生效）
/// </summary>
/// <remarks>
/// 工作流模块加载后以其实现 Replace 本注册。
/// </remarks>
public sealed class UnavailableWorkflowBusinessService : IWorkflowBusinessService
{
    private const string Message = "工作流模块未启用，无法处理审批流程";

    /// <summary>
    /// 发起审批流（始终失败）
    /// </summary>
    public Task<WorkflowBusinessStartResult> StartAsync(WorkflowBusinessStartRequest request, CancellationToken cancellationToken = default)
    {
        throw new BusinessException(message: Message);
    }

    /// <summary>
    /// 撤回审批流（始终失败）
    /// </summary>
    public Task<bool> WithdrawAsync(string businessCode, string businessId, string? reason, CancellationToken cancellationToken = default)
    {
        throw new BusinessException(message: Message);
    }

    /// <summary>
    /// 查询审批进度（始终失败）
    /// </summary>
    public Task<IReadOnlyDictionary<string, WorkflowBusinessProgress>> GetProgressAsync(
        string businessCode,
        IReadOnlyCollection<string> businessIds,
        CancellationToken cancellationToken = default)
    {
        throw new BusinessException(message: Message);
    }
}
