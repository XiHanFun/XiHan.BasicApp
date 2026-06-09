#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionCommandModels
// Guid:9a3fa6bd-8c6c-4c70-a863-79b90317699f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
