// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 消息模板（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 邮件、短信、站内通知与机器人的主题和正文，按 Scriban 语法渲染（双花括号包住变量名）
/// 租户读得到平台的模板、只改自己的，建同编码的模板即覆盖平台的；取模板时先看当前一侧启用的同编码模板，没有再取平台的
/// </remarks>
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Channel), nameof(Code))]
public sealed class SysMessageTemplate : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧、同一渠道内唯一，如 <c>auth-email-login-code</c>）
    /// </summary>
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 渠道
    /// </summary>
    public TemplateChannel Channel { get; set; } = TemplateChannel.Email;

    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 主题（邮件）
    /// </summary>
    [StringLength(256)]
    public string? Subject { get; set; }

    /// <summary>
    /// 正文
    /// </summary>
    [LongText]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 正文是 HTML（邮件）
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 说明（可用变量等）
    /// </summary>
    [StringLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 消息模板的渠道
/// </summary>
/// <remarks>
/// 与 <see cref="MessageChannels"/> 同值
/// </remarks>
public enum TemplateChannel
{
    /// <summary>
    /// 站内通知（主题即通知标题，正文是纯文本）
    /// </summary>
    /// <remarks>
    /// 内置的发送都不用它
    /// </remarks>
    InApp = 1,

    /// <summary>
    /// 邮件（有主题，正文可以是 HTML）
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信（纯文本）
    /// </summary>
    /// <remarks>
    /// 云厂商发送要在短信配置里把模板编码映射到服务商的模板 ID
    /// </remarks>
    Sms = 4,

    /// <summary>
    /// 机器人（纯文本，不用主题）
    /// </summary>
    /// <remarks>
    /// 通知发到机器人时按 <c>notification-bot</c> 渲染
    /// </remarks>
    Bot = 8,
}
