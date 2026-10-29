// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 短信发送记录，也是短信的发件箱（平台库实体，随所属侧）：写法与 <see cref="SysEmail"/> 相同。
/// 服务商发的是它那边的模板，记录里的正文是本应用模板渲染出的文字（便于查看），参数另存一份交给服务商。
/// </summary>
[DiffLogIgnored]
public sealed class SysSms : AppAuditedEntity, IOwnedByTenant, IMessageRecord
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 发起人（系统发出的为空）。
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// 接收人（不是本应用用户的号码为空）。
    /// </summary>
    public Guid? ReceiverId { get; set; }

    /// <summary>
    /// 手机号。
    /// </summary>
    [StringLength(32)]
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 正文（本应用模板渲染出的文字）。
    /// </summary>
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 用的模板编码（按短信配置的模板映射换成服务商的模板 ID）。
    /// </summary>
    [StringLength(64)]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 模板参数（JSON 对象，按写入顺序交给服务商）。
    /// </summary>
    [LongText]
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 发出时用的服务商。
    /// </summary>
    public SmsProvider? Provider { get; set; }

    /// <summary>
    /// 服务商返回的消息 ID。
    /// </summary>
    [StringLength(128)]
    public string? ProviderMessageId { get; set; }

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
    /// 下次可以尝试的时间；为空即立即。
    /// </summary>
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
    /// 发出时用的短信配置。
    /// </summary>
    public Guid? ConfigId { get; set; }

    /// <summary>
    /// 关联的业务类型。
    /// </summary>
    [StringLength(100)]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID。
    /// </summary>
    [StringLength(100)]
    public string? BusinessId { get; set; }
}
