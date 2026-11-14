// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 库的版本台账
/// </summary>
/// <remarks>
/// 每个库一行（<see cref="RowId"/>），由 <see cref="SchemaUpgrader"/> 维护：库的结构版本与升级锁（哪个节点从何时开始升级）
/// 升级锁靠这一行的条件更新抢占，过了 <see cref="SchemaOptions.LockExpiry"/> 的锁视为已失效（节点中途退出）
/// </remarks>
[SugarTable(TableName = "sys_version", TableDescription = "库的版本台账")]
public sealed class SysVersion : AppEntity
{
    /// <summary>
    /// 台账行的固定 ID
    /// </summary>
    public static readonly Guid RowId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// 最近一次升级这个库的应用版本
    /// </summary>
    [SugarColumn(ColumnName = "app_version", Length = 64, IsNullable = false, ColumnDescription = "最近一次升级这个库的应用版本")]
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// 库的结构版本
    /// </summary>
    /// <remarks>
    /// 已执行完的最高一个脚本目录；还没执行过基线为 <see langword="null"/>
    /// </remarks>
    [SugarColumn(ColumnName = "db_version", Length = 32, IsNullable = true, ColumnDescription = "库的结构版本")]
    public string? DbVersion { get; set; }

    /// <summary>
    /// 是否正在升级
    /// </summary>
    [SugarColumn(ColumnName = "is_upgrading", IsNullable = false, ColumnDescription = "是否正在升级")]
    public bool IsUpgrading { get; set; }

    /// <summary>
    /// 持有升级锁的节点
    /// </summary>
    [SugarColumn(ColumnName = "upgrade_node", Length = 128, IsNullable = true, ColumnDescription = "持有升级锁的节点")]
    public string? UpgradeNode { get; set; }

    /// <summary>
    /// 这一轮升级的开始时间
    /// </summary>
    [SugarColumn(ColumnName = "upgrade_started_at", IsNullable = true, ColumnDescription = "这一轮升级的开始时间")]
    public DateTimeOffset? UpgradeStartedAt { get; set; }

    /// <summary>
    /// 最近一次升级完成的时间
    /// </summary>
    [SugarColumn(ColumnName = "upgraded_at", IsNullable = true, ColumnDescription = "最近一次升级完成的时间")]
    public DateTimeOffset? UpgradedAt { get; set; }
}
