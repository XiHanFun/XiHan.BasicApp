#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UpgradeStatusDto
// Guid:9c89c7b5-0f69-4c1e-8b64-9a0f9a1d3e0b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:10
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Upgrade.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 升级状态 DTO
/// </summary>
public sealed record UpgradeStatusDto
{
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
    public DateTime? UpgradeStartTime { get; init; }
}
