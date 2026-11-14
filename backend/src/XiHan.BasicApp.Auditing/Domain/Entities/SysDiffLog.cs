// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 数据变更日志：实体的新增、修改、删除（含软删除）与字段级前后值
/// </summary>
/// <remarks>
/// 一个实体行的一次变更一行，带字段级前后值：操作日志只知道调了哪个操作，回答不了「这行数据在哪次调用里被谁从什么改成了什么」
/// 来源是 SqlSugar 的差异日志回调而不是各操作手写：凡经仓储写全审计实体（<c>AppAuditedEntity</c>，标了 <c>[DiffLogIgnored]</c> 的除外）都自动记，不会漏；日志、关联表这类非全审计实体不记
/// Runtime 的 <c>DiffLogBuffer</c> 按调用缓冲，调用成功才落库：失败的调用已回滚，记下来就是没发生过的变更；作业与种子的写入不记
/// 敏感列（password、hash、secret、token 等）的值打码，只动了并发戳、修改时间与修改人这类随写入就变的列的修改不记：日志给审计看，不能变成泄露渠道，也不能被噪音淹没
/// 被改的行只按实体类型、表名与字符串化的主键值指向，不建外键：任意实体、任意主键类型都能记，被改的行删了日志也要留；另按主键值建索引，查一行数据的完整变更历史
/// 软删除本是修改，<c>is_deleted</c> 由真变假单独记为恢复，误删后的恢复一眼能找到
/// 分表、异步批量写入与只追加同其他分表日志（见 <see cref="AuditLogRecord"/>）
/// </remarks>
[SugarTable(TableName = "sys_diff_log_{year}{month}{day}", TableDescription = "数据变更日志：实体的新增、修改、删除（含软删除）与字段级前后值")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_entity_id", nameof(EntityId), OrderByType.Asc)]
public sealed class SysDiffLog : AuditLogRecord
{
    /// <summary>
    /// 触发变更的操作 ID
    /// </summary>
    [SugarColumn(ColumnName = "operation_id", Length = 128, IsNullable = true, ColumnDescription = "触发变更的操作 ID")]
    public string? OperationId { get; set; }

    /// <summary>
    /// 变更种类
    /// </summary>
    [SugarColumn(ColumnName = "kind", IsNullable = false, ColumnDescription = "变更种类")]
    public DiffKind Kind { get; set; }

    /// <summary>
    /// 实体类型名
    /// </summary>
    [SugarColumn(ColumnName = "entity_type", Length = 256, IsNullable = false, ColumnDescription = "实体类型名")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 表名
    /// </summary>
    [SugarColumn(ColumnName = "table_name", Length = 128, IsNullable = true, ColumnDescription = "表名")]
    public string? TableName { get; set; }

    /// <summary>
    /// 主键值
    /// </summary>
    [SugarColumn(ColumnName = "entity_id", Length = 128, IsNullable = true, ColumnDescription = "主键值")]
    public string? EntityId { get; set; }

    /// <summary>
    /// 变更前的值（JSON 对象，新增为空）
    /// </summary>
    [SugarColumn(ColumnName = "before", IsNullable = true, ColumnDescription = "变更前的值（JSON 对象，新增为空）")]
    [LongText]
    public string? Before { get; set; }

    /// <summary>
    /// 变更后的值（JSON 对象，删除为空）
    /// </summary>
    [SugarColumn(ColumnName = "after", IsNullable = true, ColumnDescription = "变更后的值（JSON 对象，删除为空）")]
    [LongText]
    public string? After { get; set; }

    /// <summary>
    /// 有变化的列，逗号分隔
    /// </summary>
    [SugarColumn(ColumnName = "changed_fields", Length = 2048, IsNullable = true, ColumnDescription = "有变化的列，逗号分隔")]
    public string? ChangedFields { get; set; }
}

/// <summary>
/// 数据变更的种类
/// </summary>
public enum DiffKind
{
    /// <summary>
    /// 新增
    /// </summary>
    Insert = 0,

    /// <summary>
    /// 修改（软删除也是修改）
    /// </summary>
    Update = 1,

    /// <summary>
    /// 物理删除
    /// </summary>
    Delete = 2,

    /// <summary>
    /// 恢复（软删除的数据 IsDeleted 由真变假）
    /// </summary>
    /// <remarks>
    /// 从修改里区分出来
    /// </remarks>
    Restore = 3,
}
