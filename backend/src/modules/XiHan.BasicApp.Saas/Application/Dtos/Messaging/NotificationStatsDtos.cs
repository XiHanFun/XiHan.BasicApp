#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationStatsDtos
// Guid:b1e3c9a7-2f4d-4c8e-9a1b-7d5e6f0a3c21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/25 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 通知阅读统计 DTO（发 N / 已读 M / 已确认 K）
/// </summary>
public sealed class NotificationReadStatsDto
{
    /// <summary>
    /// 通知主键
    /// </summary>
    public long NotificationId { get; set; }

    /// <summary>
    /// 收件人数（未删除）
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// 已读人数
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// 未读人数
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// 已确认人数（仅 NeedConfirm 时有意义）
    /// </summary>
    public int ConfirmCount { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; }
}

/// <summary>
/// 通知未读人员项 DTO
/// </summary>
public sealed class NotificationUnreadUserDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 收到时间
    /// </summary>
    public DateTimeOffset ReceivedTime { get; set; }
}

/// <summary>
/// 通知未读人员分页查询 DTO
/// </summary>
public sealed class NotificationUnreadUserPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 通知主键
    /// </summary>
    public long NotificationId { get; set; }
}
