// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户版本创建命令
/// </summary>
public sealed record TenantEditionCreateCommand(
    string EditionCode,
    string EditionName,
    string? Description,
    int? UserLimit,
    long? StorageLimit,
    decimal? Price,
    int? BillingPeriodMonths,
    bool IsFree,
    bool IsDefault,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 租户版本更新命令
/// </summary>
public sealed record TenantEditionUpdateCommand(
    long BasicId,
    string EditionName,
    string? Description,
    int? UserLimit,
    long? StorageLimit,
    decimal? Price,
    int? BillingPeriodMonths,
    bool IsFree,
    bool IsDefault,
    int Sort,
    string? Remark);

/// <summary>
/// 租户版本状态变更命令
/// </summary>
public sealed record TenantEditionStatusChangeCommand(long BasicId, EnableStatus Status);

/// <summary>
/// 默认租户版本变更命令
/// </summary>
public sealed record TenantEditionDefaultChangeCommand(long BasicId);

/// <summary>
/// 租户版本权限授权命令
/// </summary>
public sealed record TenantEditionPermissionGrantCommand(long EditionId, long PermissionId, string? Remark);

/// <summary>
/// 租户版本权限状态变更命令
/// </summary>
public sealed record TenantEditionPermissionStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 租户版本命令结果
/// </summary>
public sealed record TenantEditionCommandResult(SysTenantEdition Edition);

/// <summary>
/// 租户版本权限命令结果
/// </summary>
public sealed record TenantEditionPermissionCommandResult(SysTenantEditionPermission EditionPermission, SysPermission? Permission);
