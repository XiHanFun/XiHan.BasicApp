// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的历史记录
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧，只增不改。每次生成（成功、失败）记一条，预览不记（照 4.x）
/// 产物包存成 Files 的临时文件、这里记文件 ID（4.x 的 <c>DownloadPath</c> 从没写过，方案 C2）
/// </remarks>
[SugarIndex("ix_{table}_generated_at", nameof(GeneratedAt), OrderByType.Desc)]
[SugarIndex("ix_{table}_table_id", nameof(TableId), OrderByType.Asc)]
public sealed class SysCodeGenHistory : AppRecord
{
    /// <summary>
    /// 表配置 ID
    /// </summary>
    public Guid TableId { get; set; }

    /// <summary>
    /// 表名
    /// </summary>
    [StringLength(200)]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    [StringLength(50)]
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 生成结果
    /// </summary>
    public CodeGenGenerationStatus GenerationStatus { get; set; }

    /// <summary>
    /// 交付方式
    /// </summary>
    public CodeGenDeliveryType DeliveryType { get; set; }

    /// <summary>
    /// 生成时间（UTC）
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 产物文件数
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// 产物总字节数
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 写进项目时的目标根目录
    /// </summary>
    [StringLength(1000)]
    public string? GenPath { get; set; }

    /// <summary>
    /// 产物包的文件 ID
    /// </summary>
    /// <remarks>
    /// Files 的临时文件，保留 7 天
    /// </remarks>
    public Guid? FileId { get; set; }

    /// <summary>
    /// 产物文件清单（JSON）
    /// </summary>
    [LongText]
    public string? GeneratedFiles { get; set; }

    /// <summary>
    /// 用到的模板（JSON）
    /// </summary>
    [LongText]
    public string? UsedTemplates { get; set; }

    /// <summary>
    /// 表配置快照（JSON）
    /// </summary>
    [LongText]
    public string? TableSnapshot { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    [LongText]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作人 ID
    /// </summary>
    [StringLength(64)]
    public string? OperatorId { get; set; }

    /// <summary>
    /// 操作人名称
    /// </summary>
    [StringLength(100)]
    public string? OperatorName { get; set; }

    /// <summary>
    /// 操作人 IP
    /// </summary>
    [StringLength(64)]
    public string? OperatorIp { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
