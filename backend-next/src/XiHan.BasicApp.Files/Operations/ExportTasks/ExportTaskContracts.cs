// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;

namespace XiHan.BasicApp.Files.Operations.ExportTasks;

/// <summary>
/// 导出的一列（请求）。
/// </summary>
/// <param name="Key">字段名（列表响应里的属性名）。</param>
/// <param name="Title">表头。</param>
/// <param name="ValueMap">原值到显示文字的映射。</param>
public sealed record ExportColumnRequest(
    [Required, StringLength(128, MinimumLength = 1)] string Key,
    [Required, StringLength(256, MinimumLength = 1)] string Title,
    IReadOnlyDictionary<string, string>? ValueMap = null);

/// <summary>
/// <c>export-tasks.submit</c> 的请求。
/// </summary>
/// <param name="BusinessType">要导出的列表操作 ID（如 <c>users.list</c>）。</param>
/// <param name="Columns">导出的列（顺序即表头顺序）。</param>
/// <param name="TaskName">任务名称；不填按「操作 ID_时间」起名。</param>
/// <param name="Scope">范围：当前页、搜索结果或全部。</param>
/// <param name="Format">格式。</param>
/// <param name="QuerySnapshot">查询快照（列表操作的分页请求 JSON）。</param>
public sealed record SubmitExportTaskRequest(
    [Required, StringLength(128, MinimumLength = 1)] string BusinessType,
    [Required, MinLength(1), MaxLength(200)] IReadOnlyList<ExportColumnRequest> Columns,
    [StringLength(256)] string? TaskName = null,
    ExportScope Scope = ExportScope.SearchResult,
    ExportFormat Format = ExportFormat.Csv,
    [StringLength(20000)] string? QuerySnapshot = null)
{
    /// <summary>
    /// 转成提交内容（列序列化成快照）。
    /// </summary>
    /// <returns>提交内容。</returns>
    public ExportSubmission ToSubmission()
    {
        var columns = Columns.Select(static c => new ExportColumnSnapshot(c.Key, c.Title, c.ValueMap)).ToList();
        return new ExportSubmission(BusinessType, TaskName, Scope, Format, QuerySnapshot, JsonSerializer.Serialize(columns, FilesJsonContext.Default.ListExportColumnSnapshot));
    }
}

/// <summary>
/// 列快照的一项（存进任务的 JSON 形状）。
/// </summary>
/// <param name="Key">字段名。</param>
/// <param name="Title">表头。</param>
/// <param name="ValueMap">映射。</param>
public sealed record ExportColumnSnapshot(string Key, string Title, IReadOnlyDictionary<string, string>? ValueMap);

/// <summary>
/// 一个导出任务。
/// </summary>
/// <param name="Id">任务 ID。</param>
/// <param name="BusinessType">导出的列表操作 ID。</param>
/// <param name="TaskName">任务名称。</param>
/// <param name="Scope">范围。</param>
/// <param name="Format">格式。</param>
/// <param name="Status">状态。</param>
/// <param name="Progress">进度（0–100）。</param>
/// <param name="TotalCount">总行数。</param>
/// <param name="ProcessedCount">已写出的行数。</param>
/// <param name="FileId">结果文件 ID（下载经 <c>files.presign</c>）。</param>
/// <param name="FileName">结果文件名。</param>
/// <param name="FileSize">结果文件大小（字节）。</param>
/// <param name="ErrorMessage">失败原因（取消、中断与执行错误）。</param>
/// <param name="StartedAt">开始时间。</param>
/// <param name="FinishedAt">结束时间。</param>
/// <param name="CreatedAt">提交时间。</param>
public sealed record ExportTaskResponse(
    Guid Id,
    string BusinessType,
    string TaskName,
    ExportScope Scope,
    ExportFormat Format,
    ExportTaskStatus Status,
    int Progress,
    long TotalCount,
    long ProcessedCount,
    Guid? FileId,
    string? FileName,
    long? FileSize,
    string? ErrorMessage,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由任务投影。
    /// </summary>
    /// <param name="task">任务。</param>
    /// <returns>响应。</returns>
    public static ExportTaskResponse From(SysExportTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return new(task.Id, task.BusinessType, task.TaskName, task.Scope, task.Format, task.Status, task.Progress, task.TotalCount, task.ProcessedCount, task.FileId, task.FileName, task.FileSize, task.ErrorMessage, task.StartedAt, task.FinishedAt, task.CreatedAt);
    }
}

/// <summary>
/// 针对一个任务的请求（<c>export-tasks.get</c>、<c>export-tasks.cancel</c>）。
/// </summary>
/// <param name="Id">任务 ID。</param>
public sealed record ExportTaskIdRequest([Required] Guid Id);

/// <summary>
/// <c>export-tasks.delete</c> 的请求。
/// </summary>
/// <param name="Ids">任务 ID。</param>
public sealed record ExportTaskIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
