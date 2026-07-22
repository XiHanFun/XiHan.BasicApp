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
public sealed record PermissionConditionCommandResult(long ConditionId);
