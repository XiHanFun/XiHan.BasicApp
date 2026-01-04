#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysGenHistory
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567013
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities.Base;

namespace XiHan.BasicApp.CodeGeneration.Entities;

/// <summary>
/// 系统代码生成历史记录实体
/// </summary>
[SugarTable("Sys_Gen_History", "系统代码生成历史记录表")]
[SugarIndex("IX_SysGenHistory_TableId", nameof(TableId), OrderByType.Asc)]
[SugarIndex("IX_SysGenHistory_GenTime", nameof(GenTime), OrderByType.Desc)]
[SugarIndex("IX_SysGenHistory_GenStatus", nameof(GenStatus), OrderByType.Asc)]
public partial class SysGenHistory : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 所属表ID
    /// </summary>
    [SugarColumn(ColumnDescription = "所属表ID", IsNullable = false)]
    public virtual long TableId { get; set; }

    /// <summary>
    /// 表名称
    /// </summary>
    [SugarColumn(ColumnDescription = "表名称", Length = 200, IsNullable = false)]
    public virtual string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 生成批次号
    /// </summary>
    [SugarColumn(ColumnDescription = "生成批次号", Length = 50, IsNullable = true)]
    public virtual string? BatchNumber { get; set; }

    /// <summary>
    /// 生成状态
    /// </summary>
    [SugarColumn(ColumnDescription = "生成状态")]
    public virtual GenStatus GenStatus { get; set; } = GenStatus.NotGenerated;

    /// <summary>
    /// 生成方式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成方式")]
    public virtual GenType GenType { get; set; } = GenType.Zip;

    /// <summary>
    /// 生成时间
    /// </summary>
    [SugarColumn(ColumnDescription = "生成时间")]
    public virtual DateTimeOffset GenTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 生成耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "生成耗时（毫秒）")]
    public virtual long Duration { get; set; } = 0;

    /// <summary>
    /// 生成文件数量
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件数量")]
    public virtual int FileCount { get; set; } = 0;

    /// <summary>
    /// 生成文件总大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件总大小（字节）")]
    public virtual long TotalSize { get; set; } = 0;

    /// <summary>
    /// 生成路径
    /// </summary>
    [SugarColumn(ColumnDescription = "生成路径", Length = 500, IsNullable = true)]
    public virtual string? GenPath { get; set; }

    /// <summary>
    /// 下载路径
    /// </summary>
    [SugarColumn(ColumnDescription = "下载路径", Length = 500, IsNullable = true)]
    public virtual string? DownloadPath { get; set; }

    /// <summary>
    /// 生成文件列表（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件列表", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? GeneratedFiles { get; set; }

    /// <summary>
    /// 使用的模板列表（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "使用的模板列表", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? UsedTemplates { get; set; }

    /// <summary>
    /// 表配置快照（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "表配置快照", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TableSnapshot { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "操作用户ID", IsNullable = true)]
    public virtual long? OperatorId { get; set; }

    /// <summary>
    /// 操作用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "操作用户名", Length = 100, IsNullable = true)]
    public virtual string? OperatorName { get; set; }

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnDescription = "操作IP", Length = 50, IsNullable = true)]
    public virtual string? OperatorIp { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
