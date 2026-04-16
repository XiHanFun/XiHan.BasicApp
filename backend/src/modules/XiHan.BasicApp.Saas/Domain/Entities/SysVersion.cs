#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysVersion
// Guid:6a5d1f62-2cfe-40f1-b42c-2f7a9d4a7c2e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统版本实体
/// 平台当前应用版本、数据库版本及升级状态的单行/少量记录，用于启动时版本兼容性校验
/// </summary>
/// <remarks>
/// 关联：
/// - 无 FK；由 SysMigrationHistory 配合形成版本治理闭环
///
/// 写入：
/// - 通常只保留"当前版本"一条记录；升级开始前 IsUpgrading=true，完成后 false
/// - AppVersion 与代码发布版本一致；DbVersion 由迁移脚本末尾更新
/// - MinSupportVersion 用于判断是否需强制升级客户端
///
/// 查询：
/// - 启动自检：读取单行比对 AppVersion vs 已部署二进制版本
/// - 前端兼容性校验：对比客户端版本与 MinSupportVersion
///
/// 删除：
/// - 不删除；升级过程中通过更新字段记录状态
///
/// 场景：
/// - 灰度发布前检查数据库版本是否已迁移到目标版本
/// - 客户端强制升级提示
/// </remarks>
[SugarTable("SysVersion", "系统版本表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_ApVe", nameof(AppVersion), OrderByType.Asc)]
[SugarIndex("IX_{table}_DbVe", nameof(DbVersion), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsUp", nameof(IsUpgrading), OrderByType.Asc)]
public class SysVersion : BasicAppCreationEntity
{
    /// <summary>
    /// 应用版本
    /// </summary>
    [SugarColumn(ColumnDescription = "应用版本", Length = 64)]
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// 数据库版本
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库版本", Length = 64)]
    public string DbVersion { get; set; } = "0.0.0";

    /// <summary>
    /// 最小支持版本
    /// </summary>
    [SugarColumn(ColumnDescription = "最小支持版本", Length = 64, IsNullable = true)]
    public string? MinSupportVersion { get; set; }

    /// <summary>
    /// 是否升级中
    /// </summary>
    [SugarColumn(ColumnDescription = "是否升级中")]
    public bool IsUpgrading { get; set; }

    /// <summary>
    /// 升级节点
    /// </summary>
    [SugarColumn(ColumnDescription = "升级节点", Length = 128, IsNullable = true)]
    public string? UpgradeNode { get; set; }

    /// <summary>
    /// 升级开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "升级开始时间", IsNullable = true)]
    public DateTime? UpgradeStartTime { get; set; }
}
