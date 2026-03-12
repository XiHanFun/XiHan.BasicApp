#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationDto
// Guid:fde437f2-9fd8-47c8-9b58-c4f350cae4be
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:49:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 通知 DTO
/// </summary>
public class NotificationDto : BasicAppDto
{
    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long? RecipientUserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SendUserId { get; set; }

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
    public DateTimeOffset SendTime { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }
}

/// <summary>
/// 创建通知 DTO
/// </summary>
public class NotificationCreateDto : BasicAppCDto
{
    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long? RecipientUserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required(ErrorMessage = "通知标题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "通知标题长度必须在 1～200 之间")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    [StringLength(100, ErrorMessage = "通知图标长度不能超过 100")]
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [StringLength(500, ErrorMessage = "跳转链接长度不能超过 500")]
    public string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [StringLength(50, ErrorMessage = "业务类型长度不能超过 50")]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SendTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新通知 DTO
/// </summary>
public class NotificationUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long? RecipientUserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required(ErrorMessage = "通知标题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "通知标题长度必须在 1～200 之间")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    [StringLength(100, ErrorMessage = "通知图标长度不能超过 100")]
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [StringLength(500, ErrorMessage = "跳转链接长度不能超过 500")]
    public string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [StringLength(50, ErrorMessage = "业务类型长度不能超过 50")]
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
    public DateTimeOffset SendTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
