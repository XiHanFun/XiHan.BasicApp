// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 库的版本台账。
/// </summary>
/// <remarks>
/// 每个库一行（<see cref="RowId"/>），由 <see cref="SchemaUpgrader"/> 维护：库的结构版本与升级锁（哪个节点从何时开始升级）。
/// 升级锁靠这一行的条件更新抢占，过了 <see cref="SchemaOptions.LockExpiry"/> 的锁视为已失效（节点中途退出）。
/// </remarks>
public sealed class SysVersion : AppEntity
{
    /// <summary>
    /// 台账行的固定 ID。
    /// </summary>
    public static readonly Guid RowId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// 最近一次升级这个库的应用版本。
    /// </summary>
    [StringLength(64)]
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// 库的结构版本。
    /// </summary>
    /// <remarks>
    /// 已执行完的最高一个脚本目录；还没执行过基线为 <see langword="null"/>。
    /// </remarks>
    [StringLength(32)]
    public string? DbVersion { get; set; }

    /// <summary>
    /// 是否正在升级。
    /// </summary>
    public bool IsUpgrading { get; set; }

    /// <summary>
    /// 持有升级锁的节点。
    /// </summary>
    [StringLength(128)]
    public string? UpgradeNode { get; set; }

    /// <summary>
    /// 这一轮升级的开始时间。
    /// </summary>
    public DateTimeOffset? UpgradeStartedAt { get; set; }

    /// <summary>
    /// 最近一次升级完成的时间。
    /// </summary>
    public DateTimeOffset? UpgradedAt { get; set; }
}
