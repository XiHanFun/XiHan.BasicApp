#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityCommandModels
// Guid:8129657d-f79b-4741-a07c-e69b17810f68
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字段级安全创建命令
/// </summary>
public sealed record FieldLevelSecurityCreateCommand(
    FieldSecurityTargetType TargetType,
    long TargetId,
    long ResourceId,
    string FieldName,
    bool IsReadable,
    bool IsEditable,
    FieldMaskStrategy MaskStrategy,
    string? MaskPattern,
    int Priority,
    string? Description,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// 字段级安全更新命令
/// </summary>
public sealed record FieldLevelSecurityUpdateCommand(
    long BasicId,
    FieldSecurityTargetType TargetType,
    long TargetId,
    long ResourceId,
    string FieldName,
    bool IsReadable,
    bool IsEditable,
    FieldMaskStrategy MaskStrategy,
    string? MaskPattern,
    int Priority,
    string? Description,
    string? Remark);

/// <summary>
/// 字段级安全状态变更命令
/// </summary>
public sealed record FieldLevelSecurityStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 字段级安全命令结果
/// </summary>
public sealed record FieldLevelSecurityCommandResult(
    SysFieldLevelSecurity Policy,
    SysResource? Resource,
    string? TargetCode,
    string? TargetName);
