// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 收件箱的一条（租户库实体，平台一侧租户为 <c>platform</c>）
/// </summary>
/// <remarks>
/// 收件箱每人一行，不在通知上记已读名单：已读、确认与弹窗状态要按人改、按人查，横幅、弹窗与强制阅读也都按本人的状态取
/// 只存状态不复制内容：标题与正文从 <see cref="SysNotification"/> 联表取，两者同在一个租户库（平台一侧租户为 <c>platform</c>）
/// 每个（通知, 用户）至多一行，用部分唯一索引，软删除的行不占位
/// 本人删除即软删除，通知与别人的收件箱不受影响；阅读统计按还留着的行算
/// 状态改得频繁，所以不进差异日志
/// </remarks>
[SugarTable(TableName = "sys_user_notification", TableDescription = "收件箱的一条（租户库实体，平台一侧租户为 platform）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(NotificationId), nameof(UserId), Name = "ux_sys_user_notification_tenant_id_notification_id_user_id")]
[SugarIndex("ix_sys_user_notification_tenant_id_user_id_status", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysUserNotification : TenantScopedAuditedEntity
{
    /// <summary>
    /// 通知 ID
    /// </summary>
    [SugarColumn(ColumnName = "notification_id", IsNullable = false, ColumnDescription = "通知 ID")]
    public Guid NotificationId { get; set; }

    /// <summary>
    /// 接收人
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "接收人")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 阅读状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "阅读状态")]
    public UserNotificationStatus Status { get; set; } = UserNotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    [SugarColumn(ColumnName = "read_at", IsNullable = true, ColumnDescription = "阅读时间")]
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>
    /// 确认时间（需要确认的通知）
    /// </summary>
    [SugarColumn(ColumnName = "confirmed_at", IsNullable = true, ColumnDescription = "确认时间（需要确认的通知）")]
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// 登录后弹窗已显示的时间（弹窗通知只弹一次）
    /// </summary>
    [SugarColumn(ColumnName = "popup_shown_at", IsNullable = true, ColumnDescription = "登录后弹窗已显示的时间（弹窗通知只弹一次）")]
    public DateTimeOffset? PopupShownAt { get; set; }
}

/// <summary>
/// 收件箱一条的阅读状态
/// </summary>
public enum UserNotificationStatus
{
    /// <summary>
    /// 未读
    /// </summary>
    Unread = 0,

    /// <summary>
    /// 已读
    /// </summary>
    Read = 1,
}
