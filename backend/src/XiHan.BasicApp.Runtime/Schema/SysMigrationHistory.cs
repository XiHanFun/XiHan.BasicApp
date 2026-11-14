// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个升级脚本的执行记录
/// </summary>
/// <remarks>
/// 每执行一个脚本记一行，成功与失败都记；同一脚本失败后修好重试会再记一行
/// </remarks>
[SugarTable(TableName = "sys_migration_history", TableDescription = "一个升级脚本的执行记录")]
[SugarIndex("ix_sys_migration_history_executed_at", nameof(ExecutedAt), OrderByType.Asc)]
public sealed class SysMigrationHistory : AppEntity
{
    /// <summary>
    /// 脚本所在的版本目录
    /// </summary>
    [SugarColumn(ColumnName = "version", Length = 32, IsNullable = false, ColumnDescription = "脚本所在的版本目录")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 脚本文件名
    /// </summary>
    [SugarColumn(ColumnName = "script_name", Length = 256, IsNullable = false, ColumnDescription = "脚本文件名")]
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>
    /// 开始执行的时间
    /// </summary>
    [SugarColumn(ColumnName = "executed_at", IsNullable = false, ColumnDescription = "开始执行的时间")]
    public DateTimeOffset ExecutedAt { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "duration_ms", IsNullable = false, ColumnDescription = "耗时（毫秒）")]
    public long DurationMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnName = "success", IsNullable = false, ColumnDescription = "是否成功")]
    public bool Success { get; set; }

    /// <summary>
    /// 执行的节点
    /// </summary>
    [SugarColumn(ColumnName = "node_name", Length = 128, IsNullable = true, ColumnDescription = "执行的节点")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 失败的原因
    /// </summary>
    [SugarColumn(ColumnName = "error_message", IsNullable = true, ColumnDescription = "失败的原因")]
    [LongText]
    public string? ErrorMessage { get; set; }
}
