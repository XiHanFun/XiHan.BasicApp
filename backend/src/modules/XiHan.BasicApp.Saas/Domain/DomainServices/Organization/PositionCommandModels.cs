#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PositionCommandModels
// Guid:6d9fc50e-b178-4c4d-d0a5-5e6f70819204
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
