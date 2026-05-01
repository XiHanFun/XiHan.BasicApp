#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationPageQueryDto
// Guid:efd8ec0d-b3f7-4c00-a146-acb74d42ef1d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统通知分页查询 DTO
/// </summary>
public sealed class NotificationPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 发送用户主键
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType? NotificationType { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool? IsBroadcast { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool? NeedConfirm { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    public bool? IsPublished { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务主键
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 发送时间起始
    /// </summary>
    public DateTimeOffset? SendTimeStart { get; set; }

    /// <summary>
    /// 发送时间结束
    /// </summary>
    public DateTimeOffset? SendTimeEnd { get; set; }

    /// <summary>
    /// 过期时间起始
    /// </summary>
    public DateTimeOffset? ExpireTimeStart { get; set; }

    /// <summary>
    /// 过期时间结束
    /// </summary>
    public DateTimeOffset? ExpireTimeEnd { get; set; }
}
