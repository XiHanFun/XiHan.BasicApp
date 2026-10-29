// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户的通知偏好。
/// </summary>
/// <remarks>
/// 平台库，跟着账号走，不分租户；每人至多一行，第一次保存时建。没有行即全收（4.x 同）。
/// 只存在 6.0 里真正起作用的开关：站内信 / 邮件 / 短信三个渠道与公告 / 待办 / 安全三类（4.x 的推送、机器人、审批、营销四项没有效果，不存）。
/// </remarks>
public sealed class SysUserNotificationPreference : AppAuditedEntity
{
    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 收站内信。
    /// </summary>
    public bool ChannelInApp { get; set; } = true;

    /// <summary>
    /// 收邮件。
    /// </summary>
    public bool ChannelEmail { get; set; } = true;

    /// <summary>
    /// 收短信。
    /// </summary>
    public bool ChannelSms { get; set; } = true;

    /// <summary>
    /// 收公告（系统、业务等）。
    /// </summary>
    public bool TypeAnnouncement { get; set; } = true;

    /// <summary>
    /// 收待办。
    /// </summary>
    public bool TypeTask { get; set; } = true;

    /// <summary>
    /// 收安全通知。
    /// </summary>
    public bool TypeSecurity { get; set; } = true;
}
