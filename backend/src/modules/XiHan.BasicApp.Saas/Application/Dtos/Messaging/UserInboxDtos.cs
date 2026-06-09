#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxDtos
// Guid:f7edb76f-2704-4b44-8635-3df1acec2134
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    public int NotificationType { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    public int NotificationStatus { get; set; }

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
