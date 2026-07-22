// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字典创建命令
/// </summary>
public sealed record DictCreateCommand(
    string DictCode,
    string DictName,
    string DictType,
    string? DictDescription,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 字典更新命令
/// </summary>
public sealed record DictUpdateCommand(
    long BasicId,
    string DictName,
    string DictType,
    string? DictDescription,
    int Sort,
    string? Remark);

/// <summary>
/// 字典状态变更命令
/// </summary>
public sealed record DictStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 字典项创建命令
/// </summary>
public sealed record DictItemCreateCommand(
    long DictId,
    long? ParentId,
    string ItemCode,
    string ItemName,
    string? ItemValue,
    string? ItemDescription,
    string? Metadata,
    bool IsDefault,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 字典项更新命令
/// </summary>
public sealed record DictItemUpdateCommand(
    long BasicId,
    long? ParentId,
    string ItemName,
    string? ItemValue,
    string? ItemDescription,
    string? Metadata,
    bool IsDefault,
    int Sort,
    string? Remark);

/// <summary>
/// 字典项状态变更命令
/// </summary>
public sealed record DictItemStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 字典命令结果
/// </summary>
public sealed record DictCommandResult(SysDict Dict);

/// <summary>
/// 字典项命令结果
/// </summary>
public sealed record DictItemCommandResult(SysDictItem DictItem);
