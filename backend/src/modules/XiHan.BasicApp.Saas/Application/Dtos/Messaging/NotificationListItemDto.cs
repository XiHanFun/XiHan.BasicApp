#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationListItemDto
// Guid:819c2524-967f-49f1-9fbc-d180e0934163
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsBroadcast { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// 是否包含正文
    /// </summary>
    public bool HasBody { get; set; }

    /// <summary>
    /// 是否包含视觉标识
    /// </summary>
    public bool HasVisualMark { get; set; }

    /// <summary>
    /// 是否包含跳转动作
    /// </summary>
    public bool HasAction { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
