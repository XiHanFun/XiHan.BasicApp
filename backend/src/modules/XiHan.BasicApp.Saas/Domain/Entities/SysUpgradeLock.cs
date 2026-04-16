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
/// 多实例环境下基于数据库唯一索引实现的互斥锁，保证升级/迁移任务仅被单实例执行
/// </summary>
/// <remarks>
/// 关联：
/// - 无 FK
///
/// 写入：
/// - ResourceKey 全局唯一（UX_ReKe）；INSERT 成功即获锁，唯一索引冲突即抢锁失败
/// - LockId 使用 UUID 标识持锁方，释放锁时需校验避免误释放
/// - ExpiryTime 防止死锁：即使持锁方崩溃，超时后其它实例可抢占
/// - OwnerNode 记录占用节点便于排查
///
/// 查询：
/// - 获取当前锁状态：按 ResourceKey 精确查询
///
/// 删除：
/// - 硬删（释放锁）；必须先校验 LockId 匹配
/// - 清理任务扫描 ExpiryTime < now 的僵尸锁强制删除
///
/// 场景：
/// - 启动时多实例争抢执行一次性迁移脚本
/// - 定时任务分布式互斥（避免 Cron 重复执行）
/// </remarks>
[SugarTable("SysUpgradeLock", "系统升级分布式锁表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_ReKe", nameof(ResourceKey), OrderByType.Asc, true)]
public class SysUpgradeLock : BasicAppCreationEntity
{
    /// <summary>
    /// 资源键（分布式锁标识，唯一索引保证互斥）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源键", Length = 128, IsNullable = false)]
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
