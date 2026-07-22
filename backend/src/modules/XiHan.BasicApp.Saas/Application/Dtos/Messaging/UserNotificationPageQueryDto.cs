// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户通知分页查询 DTO
/// </summary>
public sealed class UserNotificationPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 通知主键
    /// </summary>
    public long? NotificationId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    public NotificationStatus? NotificationStatus { get; set; }

    /// <summary>
    /// 阅读时间起始
    /// </summary>
    public DateTimeOffset? ReadTimeStart { get; set; }

    /// <summary>
    /// 阅读时间结束
    /// </summary>
    public DateTimeOffset? ReadTimeEnd { get; set; }

    /// <summary>
    /// 确认时间起始
    /// </summary>
    public DateTimeOffset? ConfirmTimeStart { get; set; }

    /// <summary>
    /// 确认时间结束
    /// </summary>
    public DateTimeOffset? ConfirmTimeEnd { get; set; }
}
