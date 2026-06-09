#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:VersionCommandModels
// Guid:15512cec-c490-438c-862c-693a44a48f0c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 版本创建命令
/// </summary>
public sealed record VersionCreateCommand(
    string AppVersion,
    string DbVersion,
    string? MinSupportVersion,
    bool IsUpgrading,
    string? UpgradeNode,
    DateTimeOffset? UpgradeStartTime);

/// <summary>
/// 版本更新命令
/// </summary>
public sealed record VersionUpdateCommand(
    long BasicId,
    string AppVersion,
    string DbVersion,
    string? MinSupportVersion,
    bool IsUpgrading,
    string? UpgradeNode,
    DateTimeOffset? UpgradeStartTime);

/// <summary>
/// 版本升级开始命令
/// </summary>
public sealed record VersionUpgradeStartCommand(long BasicId, string? UpgradeNode, DateTimeOffset? UpgradeStartTime);

/// <summary>
/// 版本升级完成命令
/// </summary>
public sealed record VersionUpgradeFinishCommand(long BasicId, string? AppVersion, string? DbVersion, string? MinSupportVersion);

/// <summary>
/// 版本命令结果
/// </summary>
public sealed record VersionCommandResult(SysVersion Version);
