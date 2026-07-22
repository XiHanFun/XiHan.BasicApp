// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 当前用户站内信 DTO
/// </summary>
public sealed class UserInboxItemDto
{
    /// <summary>
    /// 用户通知主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 通知主键
    /// </summary>
    public long NotificationId { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public NotificationPriority Priority { get; set; }

    /// <summary>
    /// 正文格式
    /// </summary>
    public NotificationContentFormat ContentFormat { get; set; }

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
    /// 通知状态（枚举序列化为字符串，与前端契约一致）
    /// </summary>
    public NotificationStatus NotificationStatus { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SendTime { get; set; }

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTimeOffset? ConfirmTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }
}

/// <summary>
/// 用户站内信状态更新 DTO
/// </summary>
public sealed class UserInboxUpdateDto
{
    /// <summary>
    /// 用户通知主键
    /// </summary>
    public long BasicId { get; set; }
}
