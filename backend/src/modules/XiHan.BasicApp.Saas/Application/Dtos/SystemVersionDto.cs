#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SystemVersionDto
// Guid:db0e9d9c-4d80-4c8e-a4be-3d96a9f8d1c1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Upgrade.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统版本信息 DTO
/// </summary>
public sealed record SystemVersionDto
{
    /// <summary>
    /// 当前应用版本
    /// </summary>
    public string CurrentAppVersion { get; init; } = string.Empty;

    /// <summary>
    /// 当前数据库版本
    /// </summary>
    public string CurrentDbVersion { get; init; } = string.Empty;

    /// <summary>
    /// 最小支持版本
    /// </summary>
    public string MinSupportVersion { get; init; } = string.Empty;

    /// <summary>
    /// 已记录的应用版本
    /// </summary>
    public string RecordedAppVersion { get; init; } = string.Empty;

    /// <summary>
    /// 是否需要升级
    /// </summary>
    public bool NeedUpgrade { get; init; }

    /// <summary>
    /// 是否强制升级
    /// </summary>
    public bool ForceUpgrade { get; init; }

    /// <summary>
    /// 是否兼容
    /// </summary>
    public bool IsCompatible { get; init; } = true;

    /// <summary>
    /// 升级状态
    /// </summary>
    public UpgradeStatus Status { get; init; }

    /// <summary>
    /// 是否升级中
    /// </summary>
    public bool IsUpgrading { get; init; }

    /// <summary>
    /// 升级节点
    /// </summary>
    public string? UpgradeNode { get; init; }

    /// <summary>
    /// 升级开始时间
    /// </summary>
    public DateTimeOffset? UpgradeStartTime { get; init; }
}
