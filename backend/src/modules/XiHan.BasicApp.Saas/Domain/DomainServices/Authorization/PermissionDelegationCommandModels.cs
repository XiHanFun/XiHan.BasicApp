#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationCommandModels
// Guid:27de28c2-96ca-4b9d-92bf-5e5874a795ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限委托创建命令
/// </summary>
public sealed record PermissionDelegationCreateCommand(
    long DelegatorUserId,
    long DelegateeUserId,
    long? PermissionId,
    long? RoleId,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset ExpirationTime,
    string? DelegationReason,
    string? Remark);

/// <summary>
/// 权限委托更新命令
/// </summary>
public sealed record PermissionDelegationUpdateCommand(
    long BasicId,
    long DelegatorUserId,
    long DelegateeUserId,
    long? PermissionId,
    long? RoleId,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset ExpirationTime,
    string? DelegationReason,
    string? Remark);

/// <summary>
/// 权限委托状态命令
/// </summary>
public sealed record PermissionDelegationStatusCommand(long BasicId, DelegationStatus DelegationStatus, string? Remark);

/// <summary>
/// 权限委托命令结果
/// </summary>
public sealed record PermissionDelegationCommandResult(long DelegationId);
