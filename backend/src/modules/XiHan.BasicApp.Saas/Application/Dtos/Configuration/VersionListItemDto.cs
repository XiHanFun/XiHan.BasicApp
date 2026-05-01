#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:VersionListItemDto
// Guid:377def95-158f-4856-9e77-d6816c409304
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统版本列表项 DTO
/// </summary>
public class VersionListItemDto : BasicAppDto
{
    /// <summary>
    /// 应用版本
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// 数据库版本
    /// </summary>
    public string DbVersion { get; set; } = string.Empty;

    /// <summary>
    /// 最小支持版本
    /// </summary>
    public string? MinSupportVersion { get; set; }

    /// <summary>
    /// 是否升级中
    /// </summary>
    public bool IsUpgrading { get; set; }

    /// <summary>
    /// 升级节点
    /// </summary>
    public string? UpgradeNode { get; set; }

    /// <summary>
    /// 升级开始时间
    /// </summary>
    public DateTimeOffset? UpgradeStartTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
