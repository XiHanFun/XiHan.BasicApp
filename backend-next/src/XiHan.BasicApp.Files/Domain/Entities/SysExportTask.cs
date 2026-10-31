// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 导出任务：把某个列表操作的查询结果导成 CSV 或 XLSX 文件。
/// </summary>
/// <remarks>
/// 租户库实体，平台一侧租户为 <c>platform</c>；4.x 同。
/// 按发起人的身份导出，结果存成临时文件。
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_requester_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(RequesterId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_{table}_status", nameof(Status), OrderByType.Asc)]
public sealed class SysExportTask : TenantScopedAuditedEntity
{
    /// <summary>
    /// 要导出的列表操作 ID（如 <c>users.list</c>）。
    /// </summary>
    [StringLength(128)]
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称。
    /// </summary>
    [StringLength(256)]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 导出范围。
    /// </summary>
    public ExportScope Scope { get; set; }

    /// <summary>
    /// 文件格式。
    /// </summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public ExportTaskStatus Status { get; set; }

    /// <summary>
    /// 进度（0–100）。
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 要导出的总行数（第一页返回后才知道）。
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 已写出的行数。
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// 查询快照（列表操作的分页请求 JSON）。
    /// </summary>
    [LongText]
    public string? QuerySnapshot { get; set; }

    /// <summary>
    /// 列快照（JSON：<c>[{ key, title, valueMap? }]</c>）。
    /// </summary>
    [LongText]
    public string FieldsSnapshot { get; set; } = "[]";

    /// <summary>
    /// 结果文件 ID。
    /// </summary>
    public Guid? FileId { get; set; }

    /// <summary>
    /// 结果文件名。
    /// </summary>
    [StringLength(256)]
    public string? FileName { get; set; }

    /// <summary>
    /// 结果文件大小（字节）。
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 失败原因。
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 开始执行时间。
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// 结束时间（成功或失败）。
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// 发起人（用户 ID）。
    /// </summary>
    public Guid RequesterId { get; set; }
}

/// <summary>
/// 导出范围。
/// </summary>
/// <remarks>
/// 4.x 同。
/// </remarks>
public enum ExportScope
{
    /// <summary>
    /// 当前页：按快照里的页码与页大小导一页。
    /// </summary>
    CurrentPage = 0,

    /// <summary>
    /// 搜索结果：按快照里的筛选与关键字导全部页。
    /// </summary>
    SearchResult = 1,

    /// <summary>
    /// 全部：去掉筛选与关键字导全部页。
    /// </summary>
    All = 2,
}

/// <summary>
/// 导出格式。
/// </summary>
/// <remarks>
/// 4.x 同。
/// </remarks>
public enum ExportFormat
{
    /// <summary>
    /// CSV（UTF-8 带 BOM，CRLF 换行）。
    /// </summary>
    Csv = 0,

    /// <summary>
    /// Excel 工作簿（单个工作表，内联字符串）。
    /// </summary>
    Xlsx = 1,
}

/// <summary>
/// 导出任务状态。
/// </summary>
/// <remarks>
/// 4.x 同；取消记为失败并写明已取消。
/// </remarks>
public enum ExportTaskStatus
{
    /// <summary>
    /// 排队中。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中。
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 已完成。
    /// </summary>
    Success = 2,

    /// <summary>
    /// 失败（含取消与进程重启中断）。
    /// </summary>
    Failed = 3,
}
