// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限申请领域服务
/// </summary>
/// <remarks>
/// 审批走工作流：提交时由应用层发起「权限申请」流程并回填实例主键，
/// 流程结论经 <see cref="ApplyApprovalAsync"/> / <see cref="ApplyRejectionAsync"/> 回写，撤回与中止经 <see cref="MarkAbortedAsync"/> 回写。
/// </remarks>
public interface IPermissionRequestDomainService
{
    /// <summary>
    /// 创建权限申请
    /// </summary>
    Task<PermissionRequestCommandResult> CreatePermissionRequestAsync(PermissionRequestCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 关联审批流程实例
    /// </summary>
    Task AttachWorkflowInstanceAsync(long id, long workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回权限申请（仅申请人本人、仅待审批）
    /// </summary>
    Task WithdrawPermissionRequestAsync(long id, long requestUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限申请
    /// </summary>
    Task<PermissionRequestCommandResult> UpdatePermissionRequestAsync(PermissionRequestUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限申请状态（管理员手工驳回或撤回；不允许直接置为已批准）
    /// </summary>
    Task<PermissionRequestCommandResult> UpdatePermissionRequestStatusAsync(PermissionRequestStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回写审批通过：为申请人自动授予角色/权限并置为已批准（已批准时幂等返回，不重复授权）
    /// </summary>
    Task<PermissionRequestCommandResult> ApplyApprovalAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回写审批驳回：置为已驳回（已驳回时幂等返回）
    /// </summary>
    Task<PermissionRequestCommandResult> ApplyRejectionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回写审批中止（流程被撤回、取消或终止）：待审批的申请置为已撤回，已完结的申请保持不变
    /// </summary>
    Task MarkAbortedAsync(long id, string? reason, CancellationToken cancellationToken = default);
}
