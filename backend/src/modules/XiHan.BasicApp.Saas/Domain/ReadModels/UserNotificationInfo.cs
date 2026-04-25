#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserNotificationInfo
// Guid:e2b4c891-7d3f-4a5e-b8c1-9f6e3d2a1b04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.ReadModels;

/// <summary>
/// 用户通知读模型 —— JOIN SysNotification + SysUserNotification 的投影结果，
/// 不继承任何实体/DTO 基类，不污染领域模型
/// </summary>
public class UserNotificationInfo
{
    // ===== SysNotification 字段 =====

    public long BasicId { get; set; }
    public long? TenantId { get; set; }
    public long? SendUserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Icon { get; set; }
    public string? Link { get; set; }
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public DateTimeOffset SendTime { get; set; }
    public DateTimeOffset? ExpireTime { get; set; }
    public bool IsBroadcast { get; set; }
    public bool NeedConfirm { get; set; }
    public bool IsPublished { get; set; }
    public YesOrNo Status { get; set; }
    public string? Remark { get; set; }

    // ===== SysUserNotification 字段 =====

    public NotificationStatus NotificationStatus { get; set; }
    public DateTimeOffset? ReadTime { get; set; }
    public DateTimeOffset? ConfirmTime { get; set; }
}
