#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserNotificationListItemDto
// Guid:1b7f9336-afa8-44d2-94cc-44631661d36e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户通知列表项 DTO
/// </summary>
public class UserNotificationListItemDto : BasicAppDto
{
    /// <summary>
    /// 通知主键
    /// </summary>
    public long NotificationId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    public NotificationStatus NotificationStatus { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType? NotificationType { get; set; }

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
    /// 发送时间
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTimeOffset? ConfirmTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
