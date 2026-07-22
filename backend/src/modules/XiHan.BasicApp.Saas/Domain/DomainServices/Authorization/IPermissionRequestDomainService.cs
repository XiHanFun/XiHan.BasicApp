// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限申请领域服务
/// </summary>
public interface IPermissionRequestDomainService
{
    /// <summary>
    /// 创建权限申请
    /// </summary>
    Task<PermissionRequestCommandResult> CreatePermissionRequestAsync(PermissionRequestCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回权限申请
    /// </summary>
    Task WithdrawPermissionRequestAsync(long id, long requestUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限申请
    /// </summary>
    Task<PermissionRequestCommandResult> UpdatePermissionRequestAsync(PermissionRequestUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限申请状态
    /// </summary>
    Task<PermissionRequestCommandResult> UpdatePermissionRequestStatusAsync(PermissionRequestStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 审批通过权限申请：审批单留痕通过 + 为申请人自动授予角色/权限 + 申请置为已通过。
    /// </summary>
    Task<PermissionRequestCommandResult> ApprovePermissionRequestAsync(PermissionRequestApprovalCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 驳回权限申请：审批单留痕驳回 + 申请置为已驳回。
    /// </summary>
    Task<PermissionRequestCommandResult> RejectPermissionRequestAsync(PermissionRequestApprovalCommand command, CancellationToken cancellationToken = default);
}
