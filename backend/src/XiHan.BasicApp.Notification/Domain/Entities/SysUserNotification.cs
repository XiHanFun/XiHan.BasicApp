// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 收件箱的一条（租户库实体，平台一侧租户为 <c>platform</c>）
/// </summary>
/// <remarks>
/// 一个接收人对一条已发布通知的阅读、确认与弹窗状态。每个（通知, 用户）至多一条；用户删除即软删除
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
