#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserCommandModels
// Guid:62a78087-365b-49e2-a141-0d89a9861ea8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户创建命令
/// </summary>
public sealed record UserCreateCommand(
    string UserName,
    string InitialPassword,
    string? RealName,
    string? NickName,
    string? Avatar,
    string? Email,
    string? Phone,
    UserGender Gender,
    DateTimeOffset? Birthday,
    EnableStatus Status,
    string? TimeZone,
    string? Language,
    string? Country,
    TenantMemberType MemberType,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? DisplayName,
    string? InviteRemark,
    string? Remark,
    long? OperatorUserId);

/// <summary>
/// 用户更新命令
/// </summary>
public sealed record UserUpdateCommand(
    long BasicId,
    string? RealName,
    string? NickName,
    string? Avatar,
    string? Email,
    string? Phone,
    UserGender Gender,
    DateTimeOffset? Birthday,
    string? TimeZone,
    string? Language,
    string? Country,
    string? Remark);

/// <summary>
/// 用户状态变更命令
/// </summary>
public sealed record UserStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 用户密码重置命令
/// </summary>
public sealed record UserPasswordResetCommand(long UserId, string NewPassword, DateTimeOffset? PasswordExpirationTime, string? Remark);

/// <summary>
/// 用户锁定状态变更命令
/// </summary>
public sealed record UserLockChangeCommand(long UserId, bool IsLocked, DateTimeOffset? LockoutEndTime, string? Remark);

/// <summary>
/// 用户登录策略更新命令
/// </summary>
public sealed record UserLoginPolicyUpdateCommand(long UserId, bool AllowMultiLogin, int MaxLoginDevices, string? Remark);

/// <summary>
/// 用户角色授权命令
/// </summary>
public sealed record UserRoleGrantCommand(
    long UserId,
    long RoleId,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? GrantReason,
    string? Remark);

/// <summary>
/// 用户角色更新命令
/// </summary>
public sealed record UserRoleUpdateCommand(
    long BasicId,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? GrantReason,
    string? Remark);

/// <summary>
/// 用户角色状态变更命令
/// </summary>
public sealed record UserRoleStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 用户直授权限授权命令
/// </summary>
public sealed record UserPermissionGrantCommand(
    long UserId,
    long PermissionId,
    PermissionAction PermissionAction,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? GrantReason,
    string? Remark);

/// <summary>
/// 用户直授权限更新命令
/// </summary>
public sealed record UserPermissionUpdateCommand(
    long BasicId,
    PermissionAction PermissionAction,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? GrantReason,
    string? Remark);

/// <summary>
/// 用户直授权限状态变更命令
/// </summary>
public sealed record UserPermissionStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 用户数据范围授权命令
/// </summary>
public sealed record UserDataScopeGrantCommand(
    long UserId,
    long DepartmentId,
    bool IncludeChildren,
    string? Remark);

/// <summary>
/// 用户数据范围更新命令
/// </summary>
public sealed record UserDataScopeUpdateCommand(
    long BasicId,
    bool IncludeChildren,
    string? Remark);

/// <summary>
/// 用户数据范围状态变更命令
/// </summary>
public sealed record UserDataScopeStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 用户部门归属分配命令
/// </summary>
public sealed record UserDepartmentAssignCommand(long UserId, long DepartmentId, bool IsMain, string? Remark);

/// <summary>
/// 用户部门归属更新命令
/// </summary>
public sealed record UserDepartmentUpdateCommand(long BasicId, bool IsMain, string? Remark);

/// <summary>
/// 用户部门归属状态变更命令
/// </summary>
public sealed record UserDepartmentStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 用户会话撤销命令
/// </summary>
public sealed record UserSessionRevokeCommand(long BasicId, string Reason, long? OperatorUserId);

/// <summary>
/// 用户全部会话撤销命令
/// </summary>
public sealed record UserSessionsRevokeCommand(long UserId, string Reason, long? OperatorUserId);

/// <summary>
/// 用户命令结果
/// </summary>
public sealed record UserCommandResult(SysUser User);

/// <summary>
/// 用户安全命令结果
/// </summary>
public sealed record UserSecurityCommandResult(SysUserSecurity Security, SysUser User, DateTimeOffset Now);

/// <summary>
/// 用户角色命令结果
/// </summary>
public sealed record UserRoleCommandResult(SysUserRole UserRole, SysRole? Role, SysTenantUser? TenantMember, DateTimeOffset Now);

/// <summary>
/// 用户直授权限命令结果
/// </summary>
public sealed record UserPermissionCommandResult(SysUserPermission UserPermission, SysPermission? Permission, SysTenantUser? TenantMember, DateTimeOffset Now);

/// <summary>
/// 用户数据范围命令结果
/// </summary>
public sealed record UserDataScopeCommandResult(SysUserDataScope DataScope, SysDepartment? Department, SysTenantUser? TenantMember);

/// <summary>
/// 用户部门归属命令结果
/// </summary>
public sealed record UserDepartmentCommandResult(SysUserDepartment UserDepartment, SysDepartment? Department);

/// <summary>
/// 用户会话命令结果
/// </summary>
public sealed record UserSessionCommandResult(SysUserSession Session, SysUser? User, DateTimeOffset Now, UserSessionRevokedDomainEvent? DomainEvent);

/// <summary>
/// 用户全部会话撤销结果
/// </summary>
public sealed record UserSessionsRevokeResult(int Count, UserSessionRevokedDomainEvent? DomainEvent);
