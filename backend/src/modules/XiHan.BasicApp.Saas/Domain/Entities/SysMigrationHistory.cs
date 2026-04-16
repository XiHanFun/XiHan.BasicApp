#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMigrationHistory
// Guid:9d3f3f89-4c9a-41dd-8f69-2a8e8c0b1c7a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:22:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统迁移历史实体
/// </summary>
[SugarTable("SysMigrationHistory", "系统迁移历史表")]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CrTi", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_Ve", nameof(Version), OrderByType.Asc)]
public class SysMigrationHistory : BasicAppCreationEntity
{
    /// <summary>
    /// 版本
    /// </summary>
    [SugarColumn(ColumnDescription = "版本", Length = 64)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 脚本名称
    /// </summary>
    [SugarColumn(ColumnDescription = "脚本名称", Length = 256)]
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>
    /// 执行时间
    /// </summary>
    [SugarColumn(ColumnDescription = "执行时间")]
    public DateTime ExecutedTime { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功")]
    public bool Success { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [SugarColumn(ColumnDescription = "节点名称", Length = 128, IsNullable = true)]
    public string? NodeName { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 1024, IsNullable = true)]
    public string? ErrorMessage { get; set; }
}
