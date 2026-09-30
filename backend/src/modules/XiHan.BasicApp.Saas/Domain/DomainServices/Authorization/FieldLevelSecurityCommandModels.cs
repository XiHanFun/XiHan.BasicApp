// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字段级安全创建命令
/// </summary>
public sealed record FieldLevelSecurityCreateCommand(
    FieldSecurityTargetType TargetType,
    long TargetId,
    string EntityName,
    string FieldName,
    FieldMaskStrategy MaskStrategy,
    int? MaskKeepHead,
    int? MaskKeepTail,
    string? MaskReplacement,
    bool IsEditable,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// 字段级安全更新命令
/// </summary>
public sealed record FieldLevelSecurityUpdateCommand(
    long BasicId,
    FieldSecurityTargetType TargetType,
    long TargetId,
    string EntityName,
    string FieldName,
    FieldMaskStrategy MaskStrategy,
    int? MaskKeepHead,
    int? MaskKeepTail,
    string? MaskReplacement,
    bool IsEditable,
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
    string? TargetCode,
    string? TargetName);
