// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统通知列表项 DTO
/// </summary>
public class NotificationListItemDto : BasicAppDto
{
    /// <summary>
    /// 发送用户主键
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; }

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
    /// 业务主键
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SendTime { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 通知目标类型
    /// </summary>
    public NotificationTargetType TargetType { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public NotificationPriority Priority { get; set; }

    /// <summary>
    /// 正文格式
    /// </summary>
    public NotificationContentFormat ContentFormat { get; set; }

    /// <summary>
    /// 投递渠道（[Flags] 按位组合；必含站内信，可叠加邮箱/短信/机器人）
    /// </summary>
    public MessageChannel DeliveryChannels { get; set; } = MessageChannel.SiteNotification;

    /// <summary>
    /// 生效开始时间
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 是否强制阅读
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// 是否顶部横幅
    /// </summary>
    public bool IsBanner { get; set; }

    /// <summary>
    /// 是否登录后弹窗
    /// </summary>
    public bool IsPopup { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
