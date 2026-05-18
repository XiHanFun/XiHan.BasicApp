#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestCommandModels
// Guid:b35de6e7-1da0-4707-8a98-76d4d6e75d25
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 权限申请命令结果
/// </summary>
public sealed record PermissionRequestCommandResult(long RequestId);
