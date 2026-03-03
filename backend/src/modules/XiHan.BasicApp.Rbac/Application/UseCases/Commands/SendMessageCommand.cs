#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SendMessageCommand
// Guid:e527cb8d-f8cd-49a9-b244-b452708eb4b7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:28:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.UseCases.Commands;

/// <summary>
/// 发送统一消息命令
/// </summary>
public class SendMessageCommand
{
    /// <summary>
    /// 消息通道
    /// </summary>
    public MessageChannel Channels { get; set; } = MessageChannel.SiteNotification;

    /// <summary>
    /// 接收用户ID集合（全员站内通知可为空）
    /// </summary>
    public IReadOnlyCollection<long> RecipientUserIds { get; set; } = [];

    /// <summary>
    /// 是否全员通知（仅站内通知支持）
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SendUserId { get; set; }

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
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 是否需要确认（站内通知）
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 邮件主题（为空时使用 Title）
    /// </summary>
    public string? EmailSubject { get; set; }

    /// <summary>
    /// 邮件是否 Html
    /// </summary>
    public bool EmailIsHtml { get; set; } = true;

    /// <summary>
    /// 邮件模板ID
    /// </summary>
    public long? EmailTemplateId { get; set; }

    /// <summary>
    /// 邮件模板参数
    /// </summary>
    public string? EmailTemplateParams { get; set; }

    /// <summary>
    /// 短信模板ID
    /// </summary>
    public string? SmsTemplateId { get; set; }

    /// <summary>
    /// 短信模板参数
    /// </summary>
    public string? SmsTemplateParams { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
