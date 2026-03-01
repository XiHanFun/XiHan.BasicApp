#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysVersionEntity
// Guid:6a5d1f62-2cfe-40f1-b42c-2f7a9d4a7c2e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Upgrade.Domain.Entities;

/// <summary>
/// 系统版本实体
/// </summary>
[SugarTable("Sys_Version", "系统版本表")]
public class SysVersionEntity : BasicAppCreationEntity
{
    /// <summary>
    /// 应用版本
    /// </summary>
    [SugarColumn(ColumnDescription = "应用版本", Length = 64)]
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// 数据库版本
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库版本", Length = 64)]
    public string DbVersion { get; set; } = "0.0.0";

    /// <summary>
    /// 最小支持版本
    /// </summary>
    [SugarColumn(ColumnDescription = "最小支持版本", Length = 64, IsNullable = true)]
    public string? MinSupportVersion { get; set; }

    /// <summary>
    /// 是否升级中
    /// </summary>
    [SugarColumn(ColumnDescription = "是否升级中")]
    public bool IsUpgrading { get; set; }

    /// <summary>
    /// 升级节点
    /// </summary>
    [SugarColumn(ColumnDescription = "升级节点", Length = 128, IsNullable = true)]
    public string? UpgradeNode { get; set; }

    /// <summary>
    /// 升级开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "升级开始时间", IsNullable = true)]
    public DateTime? UpgradeStartTime { get; set; }
}
