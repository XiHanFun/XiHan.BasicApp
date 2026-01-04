#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationDto
// Guid:z1a2b3c4-d5e6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Notifications.Dtos;

/// <summary>
/// 通知 DTO
/// </summary>
public class NotificationDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 接收用户ID（为空表示全体用户）
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    public NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SendTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建通知 DTO
/// </summary>
public class CreateNotificationDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 接收用户ID（为空表示全体用户）
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新通知 DTO
/// </summary>
public class UpdateNotificationDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 通知标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    public NotificationStatus? NotificationStatus { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 标记已读 DTO
/// </summary>
public class MarkReadDto
{
    /// <summary>
    /// 通知ID列表
    /// </summary>
    public List<long> NotificationIds { get; set; } = [];

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }
}
