// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限 ABAC 条件创建命令
/// </summary>
public sealed record PermissionConditionCreateCommand(
    long? RolePermissionId,
    long? UserPermissionId,
    int ConditionGroup,
    string AttributeName,
    ConditionOperator Operator,
    bool IsNegated,
    ConfigDataType ValueType,
    string ConditionValue,
    string? Description,
    ValidityStatus Status,
    string? Remark);

/// <summary>
/// 权限 ABAC 条件更新命令
/// </summary>
public sealed record PermissionConditionUpdateCommand(
    long BasicId,
    long? RolePermissionId,
    long? UserPermissionId,
    int ConditionGroup,
    string AttributeName,
    ConditionOperator Operator,
    bool IsNegated,
    ConfigDataType ValueType,
    string ConditionValue,
    string? Description,
    string? Remark);

/// <summary>
/// 权限 ABAC 条件状态命令
/// </summary>
public sealed record PermissionConditionStatusCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 权限 ABAC 条件命令结果
/// </summary>
/// <param name="ConditionId">条件主键</param>
/// <param name="TargetRoleId">绑定在角色权限上时为该角色</param>
/// <param name="TargetUserId">绑定在用户直授上时为该用户</param>
/// <param name="PermissionId">所绑权限</param>
public sealed record PermissionConditionCommandResult(long ConditionId, long? TargetRoleId = null, long? TargetUserId = null, long? PermissionId = null);
