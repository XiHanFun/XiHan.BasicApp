// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限申请创建命令
/// </summary>
public sealed record PermissionRequestCreateCommand(
    long RequestUserId,
    long? PermissionId,
    long? RoleId,
    string RequestReason,
    DateTimeOffset? ExpectedEffectiveTime,
    DateTimeOffset? ExpectedExpirationTime,
    string? Remark);

/// <summary>
/// 权限申请更新命令
/// </summary>
public sealed record PermissionRequestUpdateCommand(
    long BasicId,
    long RequestUserId,
    long? PermissionId,
    long? RoleId,
    string RequestReason,
    DateTimeOffset? ExpectedEffectiveTime,
    DateTimeOffset? ExpectedExpirationTime,
    string? Remark);

/// <summary>
/// 权限申请状态命令
/// </summary>
public sealed record PermissionRequestStatusCommand(
    long BasicId,
    long OperatorUserId,
    PermissionRequestStatus RequestStatus,
    long? ReviewId,
    string? Remark);

/// <summary>
/// 权限申请审批命令（通过 / 驳回）
/// </summary>
public sealed record PermissionRequestApprovalCommand(
    long BasicId,
    long OperatorUserId,
    string? Remark);

/// <summary>
/// 权限申请命令结果
/// </summary>
/// <param name="RequestId">权限申请主键</param>
/// <param name="GrantedUserId">审批通过自动授权时的被授用户ID（仅审批通过场景填写）</param>
/// <param name="GrantedRoleId">审批通过自动授予的角色ID（申请角色时填写）</param>
/// <param name="GrantedPermissionId">审批通过自动直授的权限ID（申请权限时填写）</param>
public sealed record PermissionRequestCommandResult(
    long RequestId,
    long? GrantedUserId = null,
    long? GrantedRoleId = null,
    long? GrantedPermissionId = null);
