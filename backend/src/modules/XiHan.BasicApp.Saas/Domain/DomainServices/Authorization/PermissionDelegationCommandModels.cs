// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// <param name="DelegationId">权限委托主键</param>
/// <param name="DelegateeUserId">被委托人用户ID（审计留痕的目标用户）</param>
/// <param name="PermissionId">被委托的权限ID（委托权限时填写）</param>
/// <param name="RoleId">被委托的角色ID（委托角色时填写）</param>
/// <param name="IsActive">委托是否处于生效/待生效（用于审计判定授予 vs 收回）</param>
public sealed record PermissionDelegationCommandResult(
    long DelegationId,
    long DelegateeUserId = 0,
    long? PermissionId = null,
    long? RoleId = null,
    bool IsActive = true);
