// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的历史记录
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧，只增不改。每次生成（成功、失败）记一条，预览不记
/// 产物包存成 Files 的临时文件、这里记文件 ID（方案 C2）
/// </remarks>
[SugarTable(TableName = "sys_code_gen_history", TableDescription = "代码生成的历史记录")]
[SugarIndex("ix_sys_code_gen_history_generated_at", nameof(GeneratedAt), OrderByType.Desc)]
[SugarIndex("ix_sys_code_gen_history_table_id", nameof(TableId), OrderByType.Asc)]
public sealed class SysCodeGenHistory : AppRecord
{
    /// <summary>
    /// 表配置 ID
    /// </summary>
    [SugarColumn(ColumnName = "table_id", IsNullable = false, ColumnDescription = "表配置 ID")]
    public Guid TableId { get; set; }

    /// <summary>
    /// 表名
    /// </summary>
    [SugarColumn(ColumnName = "table_name", Length = 200, IsNullable = false, ColumnDescription = "表名")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(ColumnName = "batch_number", Length = 50, IsNullable = true, ColumnDescription = "批次号")]
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 生成结果
    /// </summary>
    [SugarColumn(ColumnName = "generation_status", IsNullable = false, ColumnDescription = "生成结果")]
    public CodeGenGenerationStatus GenerationStatus { get; set; }

    /// <summary>
    /// 交付方式
    /// </summary>
    [SugarColumn(ColumnName = "delivery_type", IsNullable = false, ColumnDescription = "交付方式")]
    public CodeGenDeliveryType DeliveryType { get; set; }

    /// <summary>
    /// 生成时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "generated_at", IsNullable = false, ColumnDescription = "生成时间（UTC）")]
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "duration", IsNullable = false, ColumnDescription = "耗时（毫秒）")]
    public long Duration { get; set; }

    /// <summary>
    /// 产物文件数
    /// </summary>
    [SugarColumn(ColumnName = "file_count", IsNullable = false, ColumnDescription = "产物文件数")]
    public int FileCount { get; set; }

    /// <summary>
    /// 产物总字节数
    /// </summary>
    [SugarColumn(ColumnName = "total_size", IsNullable = false, ColumnDescription = "产物总字节数")]
    public long TotalSize { get; set; }

    /// <summary>
    /// 写进项目时的目标根目录
    /// </summary>
    [SugarColumn(ColumnName = "gen_path", Length = 1000, IsNullable = true, ColumnDescription = "写进项目时的目标根目录")]
    public string? GenPath { get; set; }

    /// <summary>
    /// 产物包的文件 ID
    /// </summary>
    /// <remarks>
    /// Files 的临时文件，保留 7 天
    /// </remarks>
    [SugarColumn(ColumnName = "file_id", IsNullable = true, ColumnDescription = "产物包的文件 ID")]
    public Guid? FileId { get; set; }

    /// <summary>
    /// 产物文件清单（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "generated_files", IsNullable = true, ColumnDescription = "产物文件清单（JSON）")]
    [LongText]
    public string? GeneratedFiles { get; set; }

    /// <summary>
    /// 用到的模板（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "used_templates", IsNullable = true, ColumnDescription = "用到的模板（JSON）")]
    [LongText]
    public string? UsedTemplates { get; set; }

    /// <summary>
    /// 表配置快照（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "table_snapshot", IsNullable = true, ColumnDescription = "表配置快照（JSON）")]
    [LongText]
    public string? TableSnapshot { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    [SugarColumn(ColumnName = "error_message", IsNullable = true, ColumnDescription = "失败原因")]
    [LongText]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作人 ID
    /// </summary>
    [SugarColumn(ColumnName = "operator_id", Length = 64, IsNullable = true, ColumnDescription = "操作人 ID")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// 操作人名称
    /// </summary>
    [SugarColumn(ColumnName = "operator_name", Length = 100, IsNullable = true, ColumnDescription = "操作人名称")]
    public string? OperatorName { get; set; }

    /// <summary>
    /// 操作人 IP
    /// </summary>
    [SugarColumn(ColumnName = "operator_ip", Length = 64, IsNullable = true, ColumnDescription = "操作人 IP")]
    public string? OperatorIp { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
