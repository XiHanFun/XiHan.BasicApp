#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUpgradeLock
// Guid:0c1c58da-7d03-4b1c-8f5a-6d8e2a1c0f0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:22:10
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统升级分布式锁实体
/// </summary>
[SugarTable("Sys_Upgrade_Lock", "系统升级分布式锁表")]
public class SysUpgradeLock : BasicAppCreationEntity
{
    /// <summary>
    /// 资源键
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnDescription = "资源键", Length = 128)]
    public string ResourceKey { get; set; } = string.Empty;

    /// <summary>
    /// 锁标识
    /// </summary>
    [SugarColumn(ColumnDescription = "锁标识", Length = 64)]
    public string LockId { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间")]
    public DateTime ExpiryTime { get; set; }

    /// <summary>
    /// 所属节点
    /// </summary>
    [SugarColumn(ColumnDescription = "所属节点", Length = 128, IsNullable = true)]
    public string? OwnerNode { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnDescription = "更新时间", IsNullable = true)]
    public DateTime? UpdatedTime { get; set; }
}
