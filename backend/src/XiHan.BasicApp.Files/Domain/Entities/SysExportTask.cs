// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 导出任务：把某个列表操作的查询结果导成 CSV 或 XLSX 文件
/// </summary>
/// <remarks>
/// 租户库实体，平台一侧租户为 <c>platform</c>
/// 按发起人的身份导出，结果存成临时文件
/// </remarks>
[SugarTable(TableName = "sys_export_task", TableDescription = "导出任务：把某个列表操作的查询结果导成 CSV 或 XLSX 文件")]
[SugarIndex("ix_sys_export_task_tenant_id_requester_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(RequesterId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_export_task_status", nameof(Status), OrderByType.Asc)]
public sealed class SysExportTask : TenantScopedAuditedEntity
{
    /// <summary>
    /// 要导出的列表操作 ID（如 <c>users.list</c>）
    /// </summary>
    [SugarColumn(ColumnName = "business_type", Length = 128, IsNullable = false, ColumnDescription = "要导出的列表操作 ID（如 users.list）")]
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(ColumnName = "task_name", Length = 256, IsNullable = false, ColumnDescription = "任务名称")]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 导出范围
    /// </summary>
    [SugarColumn(ColumnName = "scope", IsNullable = false, ColumnDescription = "导出范围")]
    public ExportScope Scope { get; set; }

    /// <summary>
    /// 文件格式
    /// </summary>
    [SugarColumn(ColumnName = "format", IsNullable = false, ColumnDescription = "文件格式")]
    public ExportFormat Format { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public ExportTaskStatus Status { get; set; }

    /// <summary>
    /// 进度（0–100）
    /// </summary>
    [SugarColumn(ColumnName = "progress", IsNullable = false, ColumnDescription = "进度（0–100）")]
    public int Progress { get; set; }

    /// <summary>
    /// 要导出的总行数（第一页返回后才知道）
    /// </summary>
    [SugarColumn(ColumnName = "total_count", IsNullable = false, ColumnDescription = "要导出的总行数（第一页返回后才知道）")]
    public long TotalCount { get; set; }

    /// <summary>
    /// 已写出的行数
    /// </summary>
    [SugarColumn(ColumnName = "processed_count", IsNullable = false, ColumnDescription = "已写出的行数")]
    public long ProcessedCount { get; set; }

    /// <summary>
    /// 查询快照（列表操作的分页请求 JSON）
    /// </summary>
    [SugarColumn(ColumnName = "query_snapshot", IsNullable = true, ColumnDescription = "查询快照（列表操作的分页请求 JSON）")]
    [LongText]
    public string? QuerySnapshot { get; set; }

    /// <summary>
    /// 列快照（JSON：<c>[{ key, title, valueMap? }]</c>）
    /// </summary>
    [SugarColumn(ColumnName = "fields_snapshot", IsNullable = false, ColumnDescription = "列快照（JSON：[{ key, title, valueMap? }]）")]
    [LongText]
    public string FieldsSnapshot { get; set; } = "[]";

    /// <summary>
    /// 结果文件 ID
    /// </summary>
    [SugarColumn(ColumnName = "file_id", IsNullable = true, ColumnDescription = "结果文件 ID")]
    public Guid? FileId { get; set; }

    /// <summary>
    /// 结果文件名
    /// </summary>
    [SugarColumn(ColumnName = "file_name", Length = 256, IsNullable = true, ColumnDescription = "结果文件名")]
    public string? FileName { get; set; }

    /// <summary>
    /// 结果文件大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "file_size", IsNullable = true, ColumnDescription = "结果文件大小（字节）")]
    public long? FileSize { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 1000, IsNullable = true, ColumnDescription = "失败原因")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 开始执行时间
    /// </summary>
    [SugarColumn(ColumnName = "started_at", IsNullable = true, ColumnDescription = "开始执行时间")]
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// 结束时间（成功或失败）
    /// </summary>
    [SugarColumn(ColumnName = "finished_at", IsNullable = true, ColumnDescription = "结束时间（成功或失败）")]
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// 发起人（用户 ID）
    /// </summary>
    [SugarColumn(ColumnName = "requester_id", IsNullable = false, ColumnDescription = "发起人（用户 ID）")]
    public Guid RequesterId { get; set; }
}

/// <summary>
/// 导出范围
/// </summary>
public enum ExportScope
{
    /// <summary>
    /// 当前页：按快照里的页码与页大小导一页
    /// </summary>
    CurrentPage = 0,

    /// <summary>
    /// 搜索结果：按快照里的筛选与关键字导全部页
    /// </summary>
    SearchResult = 1,

    /// <summary>
    /// 全部：去掉筛选与关键字导全部页
    /// </summary>
    All = 2,
}

/// <summary>
/// 导出格式
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// CSV（UTF-8 带 BOM，CRLF 换行）
    /// </summary>
    Csv = 0,

    /// <summary>
    /// Excel 工作簿（单个工作表，内联字符串）
    /// </summary>
    Xlsx = 1,
}

/// <summary>
/// 导出任务状态
/// </summary>
/// <remarks>
/// 取消记为失败并写明已取消
/// </remarks>
public enum ExportTaskStatus
{
    /// <summary>
    /// 排队中
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Success = 2,

    /// <summary>
    /// 失败（含取消与进程重启中断）
    /// </summary>
    Failed = 3,
}
