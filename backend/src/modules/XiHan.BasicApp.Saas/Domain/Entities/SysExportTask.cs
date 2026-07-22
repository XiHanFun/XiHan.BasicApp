// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 导出任务实体
/// 记录异步导出任务的状态机（待执行/执行中/成功/失败）、进度、查询/列快照与产物文件引用。
/// </summary>
/// <remarks>
/// 职责边界：
/// - 后台 worker 轮询 Status=Pending 的任务，重建发起人（CreatedId/TenantId）上下文后，
///   经资源对应 IExportProvider 调既有 QueryService 流式拉取 → 写出 CSV → 落 SysFile → 回写状态。
/// - 不承载导出数据本身；数据的权限/数据范围/字段脱敏均在被调 QueryService 内生效（见导出引擎）。
/// - CreatedId = 发起人；TenantId = 发起租户。导出中心列表按 CreatedId 自鉴权。
///
/// 查询：
/// - 导出中心「我的任务」：IX_TeId_CrTi + WHERE CreatedId=? ORDER BY CreatedTime DESC 分页
/// - 后台扫描待执行：IX_St + WHERE Status=Pending（无租户上下文跨租户扫描）
/// </remarks>
[SugarTable(TableName = "Sys_Export_Task", TableDescription = "导出任务表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_St", nameof(Status), OrderByType.Asc)]
public partial class SysExportTask : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 业务类型（= 前端 pageCode，匹配 IExportProvider.BusinessType）
    /// </summary>
    [SugarColumn(ColumnName = "Business_Type", ColumnDescription = "业务类型", Length = 100, IsNullable = false)]
    public virtual string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称（用于展示与下载文件名，如「操作日志_20260614120000」）
    /// </summary>
    [SugarColumn(ColumnName = "Task_Name", ColumnDescription = "任务名称", Length = 256, IsNullable = false)]
    public virtual string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 导出范围
    /// </summary>
    [SugarColumn(ColumnName = "Scope", ColumnDescription = "导出范围", IsNullable = false)]
    public virtual ExportScope Scope { get; set; } = ExportScope.SearchResult;

    /// <summary>
    /// 导出格式
    /// </summary>
    [SugarColumn(ColumnName = "Format", ColumnDescription = "导出格式", IsNullable = false)]
    public virtual ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>
    /// 任务状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "任务状态", IsNullable = false)]
    public virtual ExportTaskStatus Status { get; set; } = ExportTaskStatus.Pending;

    /// <summary>
    /// 进度（0-100）
    /// </summary>
    [SugarColumn(ColumnName = "Progress", ColumnDescription = "进度", IsNullable = false)]
    public virtual int Progress { get; set; }

    /// <summary>
    /// 数据总行数（首页查询得到的 TotalCount）
    /// </summary>
    [SugarColumn(ColumnName = "Total_Count", ColumnDescription = "数据总行数", IsNullable = false)]
    public virtual int TotalCount { get; set; }

    /// <summary>
    /// 已处理行数
    /// </summary>
    [SugarColumn(ColumnName = "Processed_Count", ColumnDescription = "已处理行数", IsNullable = false)]
    public virtual int ProcessedCount { get; set; }

    /// <summary>
    /// 查询条件快照（JSON：资源自身的分页查询 DTO，由 Provider 反序列化为具体类型）
    /// </summary>
    [SugarColumn(ColumnName = "Query_Snapshot", ColumnDescription = "查询条件快照(JSON)", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? QuerySnapshot { get; set; }

    /// <summary>
    /// 导出列快照（JSON：[{ key, title, valueMap? }]，按顺序写出表头与单元格）
    /// </summary>
    [SugarColumn(ColumnName = "Fields_Snapshot", ColumnDescription = "导出列快照(JSON)", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string FieldsSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 产物文件ID（成功后关联 SysFile.BasicId）
    /// </summary>
    [SugarColumn(ColumnName = "File_Id", ColumnDescription = "产物文件ID", IsNullable = true)]
    public virtual long? FileId { get; set; }

    /// <summary>
    /// 产物文件名（下载展示名）
    /// </summary>
    [SugarColumn(ColumnName = "File_Name", ColumnDescription = "产物文件名", Length = 256, IsNullable = true)]
    public virtual string? FileName { get; set; }

    /// <summary>
    /// 产物文件大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "File_Size", ColumnDescription = "产物文件大小", IsNullable = false)]
    public virtual long FileSize { get; set; }

    /// <summary>
    /// 错误信息（失败时填充）
    /// </summary>
    [SugarColumn(ColumnName = "Error_Message", ColumnDescription = "错误信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 开始执行时间
    /// </summary>
    [SugarColumn(ColumnName = "Started_Time", ColumnDescription = "开始执行时间", IsNullable = true)]
    public virtual DateTimeOffset? StartedTime { get; set; }

    /// <summary>
    /// 完成时间（成功或失败）
    /// </summary>
    [SugarColumn(ColumnName = "Finished_Time", ColumnDescription = "完成时间", IsNullable = true)]
    public virtual DateTimeOffset? FinishedTime { get; set; }
}
