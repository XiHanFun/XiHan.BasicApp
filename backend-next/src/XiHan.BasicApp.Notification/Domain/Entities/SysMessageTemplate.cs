// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 消息模板（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// 邮件与短信的主题和正文，按 Scriban 语法渲染（双花括号包住变量名）。
/// 取模板时先看当前一侧启用的同编码模板，没有再取平台的（4.x 同租户优先、平台兜底）。
/// </remarks>
public sealed class SysMessageTemplate : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧、同一渠道内唯一，如 <c>auth-email-login-code</c>）。
    /// </summary>
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 渠道。
    /// </summary>
    public TemplateChannel Channel { get; set; } = TemplateChannel.Email;

    /// <summary>
    /// 名称。
    /// </summary>
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 主题（邮件）。
    /// </summary>
    [StringLength(256)]
    public string? Subject { get; set; }

    /// <summary>
    /// 正文。
    /// </summary>
    [LongText]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 正文是 HTML（邮件）。
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 说明（可用变量等）。
    /// </summary>
    [StringLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
