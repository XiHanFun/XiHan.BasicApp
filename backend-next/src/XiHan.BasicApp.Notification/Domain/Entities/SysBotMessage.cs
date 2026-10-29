// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 机器人消息的发件箱（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// 发布时按本侧每个服务商的缺省机器人各写一条（平台一侧另按每个 Telegram 机器人的每个广播群各一条），与业务同一事务提交，由发送循环发出、失败按次数重试
/// （4.x 在提交后直发、失败只记日志）。没有操作与页面，只供发送循环与排查。
/// </remarks>
[DiffLogIgnored]
public sealed class SysBotMessage : AppAuditedEntity, IOwnedByTenant, IMessageRecord
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 发起人（系统发出的为空）。
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// 通道。
    /// </summary>
    public BotChannel Channel { get; set; }

    /// <summary>
    /// 目标会话（Telegram 的 chat ID）。
    /// </summary>
    /// <remarks>
    /// 群机器人为空。
    /// </remarks>
    [StringLength(64)]
    public string? ChatId { get; set; }

    /// <summary>
    /// 纯文本正文（标题、正文与链接各一段）。
    /// </summary>
    [LongText]
    public string Content { get; set; } = string.Empty;

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
    /// 下次可以尝试的时间。
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
    /// 要发到的机器人配置或 Telegram 机器人。
    /// </summary>
    /// <remarks>
    /// 写入时选定；之后被停用或删除即失败。
    /// </remarks>
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
