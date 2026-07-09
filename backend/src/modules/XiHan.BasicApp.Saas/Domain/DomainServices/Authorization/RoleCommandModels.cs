#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleCommandModels
// Guid:32ef29be-4b67-4ba7-b7db-c0133faac425
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色创建命令
/// </summary>
public sealed record RoleCreateCommand(
    string RoleCode,
    string RoleName,
    string? RoleDescription,
    RoleType RoleType,
    DataPermissionScope DataScope,
    int MaxMembers,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 角色更新命令
/// </summary>
public sealed record RoleUpdateCommand(
    long BasicId,
    string RoleName,
    string? RoleDescription,
    RoleType RoleType,
    DataPermissionScope DataScope,
    int MaxMembers,
    int Sort,
    string? Remark);

/// <summary>
/// 角色状态变更命令
/// </summary>
public sealed record RoleStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 角色权限授权命令
/// </summary>
public sealed record RolePermissionGrantCommand(
    long RoleId,
    long PermissionId,
    PermissionAction PermissionAction,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? GrantReason,
    string? Remark);

/// <summary>
/// 角色权限批量变更命令（一次性提交授予与撤销）
/// </summary>
public sealed record RolePermissionBatchUpdateCommand(
    long RoleId,
    IReadOnlyList<long> GrantPermissionIds,
    IReadOnlyList<long> RevokeRolePermissionIds);

/// <summary>
/// 角色权限更新命令
/// </summary>
public sealed record RolePermissionUpdateCommand(
    long BasicId,
    PermissionAction PermissionAction,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? GrantReason,
    string? Remark);

/// <summary>
/// 角色权限状态变更命令
/// </summary>
public sealed record RolePermissionStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 角色数据范围授权命令
/// </summary>
public sealed record RoleDataScopeGrantCommand(
    long RoleId,
    long DepartmentId,
    bool IncludeChildren,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? Remark);

/// <summary>
/// 角色数据范围更新命令
/// </summary>
public sealed record RoleDataScopeUpdateCommand(
    long BasicId,
    bool IncludeChildren,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? Remark);

/// <summary>
/// 角色数据范围状态变更命令
/// </summary>
public sealed record RoleDataScopeStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 角色继承创建命令
/// </summary>
public sealed record RoleHierarchyCreateCommand(long AncestorId, long DescendantId, string? Remark);

/// <summary>
/// 角色命令结果
/// </summary>
public sealed record RoleCommandResult(SysRole Role);

/// <summary>
/// 角色权限命令结果
/// </summary>
public sealed record RolePermissionCommandResult(SysRolePermission RolePermission, SysPermission? Permission);

/// <summary>
/// 角色权限批量变更结果（本次实际发生变化的权限，用于审计发事件）
/// </summary>
/// <param name="GrantedPermissionIds">实际授予/复活的权限ID（不含本已有效的重复授予）</param>
/// <param name="RevokedPermissionIds">实际撤销的权限ID</param>
public sealed record RolePermissionBatchUpdateResult(
    IReadOnlyList<long> GrantedPermissionIds,
    IReadOnlyList<long> RevokedPermissionIds);

/// <summary>
/// 角色数据范围命令结果
/// </summary>
public sealed record RoleDataScopeCommandResult(SysRoleDataScope DataScope, SysDepartment? Department);

/// <summary>
/// 角色继承命令结果
/// </summary>
public sealed record RoleHierarchyCommandResult(SysRoleHierarchy Hierarchy, SysRole Ancestor, SysRole Descendant);
