// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 岗位创建命令
/// </summary>
public sealed record PositionCreateCommand(
    string PositionCode,
    string PositionName,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 岗位更新命令
/// </summary>
public sealed record PositionUpdateCommand(
    long BasicId,
    string PositionName,
    int Sort,
    string? Remark);

/// <summary>
/// 岗位状态变更命令
/// </summary>
public sealed record PositionStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 岗位命令结果
/// </summary>
public sealed record PositionCommandResult(SysPosition Position);
