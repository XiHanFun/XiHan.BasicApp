// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户的通知偏好
/// </summary>
/// <remarks>
/// 平台库，跟着账号走，不分租户；每人至多一行，第一次保存时建。没有行即全收
/// 只存真正起作用的开关：站内信 / 邮件 / 短信三个渠道与公告 / 待办 / 安全三类
/// </remarks>
[SugarTable(TableName = "sys_user_notification_preference", TableDescription = "用户的通知偏好")]
[ActiveUniqueIndex(nameof(UserId), Name = "ux_sys_user_notification_preference_user_id")]
public sealed class SysUserNotificationPreference : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 收站内信
    /// </summary>
    [SugarColumn(ColumnName = "channel_in_app", IsNullable = false, ColumnDescription = "收站内信")]
    public bool ChannelInApp { get; set; } = true;

    /// <summary>
    /// 收邮件
    /// </summary>
    [SugarColumn(ColumnName = "channel_email", IsNullable = false, ColumnDescription = "收邮件")]
    public bool ChannelEmail { get; set; } = true;

    /// <summary>
    /// 收短信
    /// </summary>
    [SugarColumn(ColumnName = "channel_sms", IsNullable = false, ColumnDescription = "收短信")]
    public bool ChannelSms { get; set; } = true;

    /// <summary>
    /// 收公告（系统、业务等）
    /// </summary>
    [SugarColumn(ColumnName = "type_announcement", IsNullable = false, ColumnDescription = "收公告（系统、业务等）")]
    public bool TypeAnnouncement { get; set; } = true;

    /// <summary>
    /// 收待办
    /// </summary>
    [SugarColumn(ColumnName = "type_task", IsNullable = false, ColumnDescription = "收待办")]
    public bool TypeTask { get; set; } = true;

    /// <summary>
    /// 收安全通知
    /// </summary>
    [SugarColumn(ColumnName = "type_security", IsNullable = false, ColumnDescription = "收安全通知")]
    public bool TypeSecurity { get; set; } = true;
}
