// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 库的版本台账
/// </summary>
/// <remarks>
/// 每个库固定一行（<see cref="RowId"/>），记结构版本（已执行完的最高版本目录）：升级只执行比它新的目录，库比应用带的脚本还新即拒绝，防止旧应用跑在新结构上
/// 升级锁也放在这一行，靠条件更新抢占、时间取库的 <c>now()</c>：不另需分布式锁组件，各节点的时钟也不必对齐；过了 <see cref="SchemaOptions.LockExpiry"/> 的锁视为已失效，持锁节点中途退出时别的节点能接手
/// 平台库的这一行由 <see cref="SchemaUpgrader"/> 维护，表与行不存在即按实体先建（多节点同时建时后到的再看一眼即可）；脚本只为 PostgreSQL 写，别的库跳过、由 InitTables 建表
/// 租户独立库在自己的库里各有一行：初始化时按当前实体建表、直接登记为最新版本，不重放历史脚本；之后的启动升级在平台库之后、同一把锁里逐个推进（停用的租户也升）
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
