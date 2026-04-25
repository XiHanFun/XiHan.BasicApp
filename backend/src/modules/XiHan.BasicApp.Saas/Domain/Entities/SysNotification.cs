#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification
// Guid:ac28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统通知实体
/// 站内通知内容主表：承载通知正文、发布状态、目标范围；每用户读取/确认状态由 SysUserNotification 独立维护
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表只有"一条通知内容"；"谁看过/是否已读"交由 SysUserNotification
/// - 避免循环："通知表 × 用户表"直接笛卡尔积（大租户会爆）
///
/// 关联：
/// - 反向：SysUserNotification.NotificationId
///
/// 写入：
/// - 编辑阶段 IsPublished=false；发布后 IsPublished=true + SendTime
/// - 发布时按 TargetType（All/Role/Department/User）展开生成 SysUserNotification 记录
/// - 可选 ExpiresAt 控制消息过期自动隐藏
///
/// 查询：
/// - 租户最近发布：IX_TeId_SeTi
/// - 按类型过滤：IX_NoTy
///
/// 删除：
/// - 仅软删；删除时级联软删所有 SysUserNotification
///
/// 状态：
/// - IsPublished: false=草稿 / true=已发布
/// - NotificationType: System/Announcement/Activity/Personal 等
///
/// 场景：
/// - 系统公告、版本升级通知、营销活动、告警推送
/// </remarks>
[SugarTable("SysNotification", "系统通知表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_NoTy", nameof(NotificationType), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsPu", nameof(IsPublished), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_SeTi", nameof(TenantId), OrderByType.Asc, nameof(SendTime), OrderByType.Desc)]
public partial class SysNotification : BasicAppAggregateRoot
{
    /// <summary>
    /// 发送用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "发送用户ID", IsNullable = true)]
    public virtual long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    [SugarColumn(ColumnDescription = "通知类型")]
    public virtual NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    [SugarColumn(ColumnDescription = "通知标题", Length = 200, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(ColumnDescription = "通知内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    [SugarColumn(ColumnDescription = "通知图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [SugarColumn(ColumnDescription = "跳转链接", Length = 500, IsNullable = true)]
    public virtual string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnDescription = "发送时间")]
    public virtual DateTimeOffset SendTime { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知（注意：此字段表示"广播给所有用户"，与权限体系中 IsGlobal 表示"平台级 TenantId=0"的语义不同）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全员通知")]
    public virtual bool IsBroadcast { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要确认")]
    public virtual bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 是否已发布（发布后不可编辑/删除）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已发布")]
    public virtual bool IsPublished { get; set; } = false;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
