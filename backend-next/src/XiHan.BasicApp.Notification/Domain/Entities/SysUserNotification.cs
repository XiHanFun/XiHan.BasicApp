// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 收件箱的一条（租户库实体，平台一侧租户为 <c>platform</c>）。
/// </summary>
/// <remarks>
/// 一个接收人对一条已发布通知的阅读、确认与弹窗状态。每个（通知, 用户）至多一条；用户删除即软删除。
/// </remarks>
[ActiveUniqueIndex(nameof(TenantId), nameof(NotificationId), nameof(UserId))]
[SugarIndex("ix_{table}_tenant_id_user_id_status", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysUserNotification : TenantScopedAuditedEntity
{
    /// <summary>
    /// 通知 ID。
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// 接收人。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 阅读状态。
    /// </summary>
    public UserNotificationStatus Status { get; set; } = UserNotificationStatus.Unread;

    /// <summary>
    /// 阅读时间。
    /// </summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>
    /// 确认时间（需要确认的通知）。
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// 登录后弹窗已显示的时间（弹窗通知只弹一次）。
    /// </summary>
    public DateTimeOffset? PopupShownAt { get; set; }
}
