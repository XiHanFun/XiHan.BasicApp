#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UpgradeStartResultDto
// Guid:5d6d7f1b-8e8a-4d2c-8c96-0a3e1e1c8b11
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:20
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Upgrade.Enums;

namespace XiHan.BasicApp.Upgrade.Application.Dtos;

/// <summary>
/// 升级启动结果 DTO
/// </summary>
public sealed record UpgradeStartResultDto
{
    /// <summary>
    /// 是否已启动
    /// </summary>
    public bool Started { get; init; }

    /// <summary>
    /// 升级状态
    /// </summary>
    public UpgradeStatus Status { get; init; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
