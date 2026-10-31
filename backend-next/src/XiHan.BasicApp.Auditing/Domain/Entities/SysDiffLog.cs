// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 数据变更日志：实体的新增、修改、删除（含软删除）与字段级前后值。
/// </summary>
/// <remarks>
/// 来源是 SqlSugar 的差异日志回调（方案 D6），按调用缓冲，调用成功才落库。
/// </remarks>
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_entity_id", nameof(EntityId), OrderByType.Asc)]
[SugarTable("sys_diff_log_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysDiffLog : AuditLogRecord
{
    /// <summary>
    /// 触发变更的操作 ID。
    /// </summary>
    [StringLength(128)]
    public string? OperationId { get; set; }

    /// <summary>
    /// 变更种类。
    /// </summary>
    public DiffKind Kind { get; set; }

    /// <summary>
    /// 实体类型名。
    /// </summary>
    [StringLength(256)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 表名。
    /// </summary>
    [StringLength(128)]
    public string? TableName { get; set; }

    /// <summary>
    /// 主键值。
    /// </summary>
    [StringLength(128)]
    public string? EntityId { get; set; }

    /// <summary>
    /// 变更前的值（JSON 对象，新增为空）。
    /// </summary>
    [LongText]
    public string? Before { get; set; }

    /// <summary>
    /// 变更后的值（JSON 对象，删除为空）。
    /// </summary>
    [LongText]
    public string? After { get; set; }

    /// <summary>
    /// 有变化的列，逗号分隔。
    /// </summary>
    [StringLength(2048)]
    public string? ChangedFields { get; set; }
}
