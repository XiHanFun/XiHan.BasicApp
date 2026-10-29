// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 邮件发送记录，也是邮件的发件箱（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// 发送记录即队列（4.x 同）。业务写入时只记一条排队中的记录、随业务一起提交，
/// 由后台的发送循环按所属侧的缺省邮件配置发出，失败按退避重试，用完重试即失败；重发即改回排队中。
/// 主题与正文在写入时已按模板渲染好，记录的就是实际发出的内容。
/// </remarks>
[DiffLogIgnored]
public sealed class SysEmail : AppAuditedEntity, IOwnedByTenant, IMessageRecord
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 发起人（系统发出的为空）。
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// 接收人（不是本应用用户的地址为空）。
    /// </summary>
    public Guid? ReceiverId { get; set; }

    /// <summary>
    /// 收件地址。
    /// </summary>
    [StringLength(256)]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 主题。
    /// </summary>
    [StringLength(512)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 正文。
    /// </summary>
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 正文是 HTML。
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 用的模板编码。
    /// </summary>
    [StringLength(64)]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public MessageSendStatus Status { get; set; } = MessageSendStatus.Pending;

    /// <summary>
    /// 已尝试的次数。
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// 最多尝试的次数。
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// 下次可以尝试的时间（失败后按退避推迟）。
    /// </summary>
    /// <remarks>
    /// 为空即立即。
    /// </remarks>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// 发出时间。
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// 最近一次失败的原因。
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 发出时用的邮件配置。
    /// </summary>
    public Guid? ConfigId { get; set; }

    /// <summary>
    /// 关联的业务类型（如 <c>notification</c>、<c>auth.login-code</c>）。
    /// </summary>
    [StringLength(100)]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID。
    /// </summary>
    [StringLength(100)]
    public string? BusinessId { get; set; }
}
