#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentCommandModels
// Guid:e29d6ff8-9424-4436-a3fd-40a265d37aac
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 部门创建命令
/// </summary>
public sealed record DepartmentCreateCommand(
    long? ParentId,
    string DepartmentName,
    string DepartmentCode,
    DepartmentType DepartmentType,
    long? LeaderId,
    string? Phone,
    string? Email,
    string? Address,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 部门更新命令
/// </summary>
public sealed record DepartmentUpdateCommand(
    long BasicId,
    long? ParentId,
    string DepartmentName,
    DepartmentType DepartmentType,
    long? LeaderId,
    string? Phone,
    string? Email,
    string? Address,
    int Sort,
    string? Remark);

/// <summary>
/// 部门状态变更命令
/// </summary>
public sealed record DepartmentStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 部门命令结果
/// </summary>
public sealed record DepartmentCommandResult(SysDepartment Department);
