// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// Telegram 的出站审计（平台库实体）。
/// </summary>
/// <remarks>
/// 发消息、改消息这几种调用在成功或最终失败时各记一条（不按每次重试记），4.x 同字段。
/// 4.x 按月分表；6.0 是普通表，没有页面与操作（4.x 也没有）。
/// </remarks>
[DiffLogIgnored]
public sealed class SysTelegramMessage : AppAuditedEntity
{
    /// <summary>
    /// 机器人名称。
    /// </summary>
    [StringLength(64)]
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// 机器人 ID。
    /// </summary>
    public Guid BotId { get; set; }

    /// <summary>
    /// 目标会话。
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// Bot API 方法名。
    /// </summary>
    /// <remarks>
    /// 如 <c>sendMessage</c>、<c>editMessageText</c> 等。
    /// </remarks>
    [StringLength(50)]
    public string ApiMethod { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型（<c>text</c>、<c>photo</c>、<c>document</c>、<c>reply_markup</c>）。
    /// </summary>
    [StringLength(50)]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// 内容（正文或说明）。
    /// </summary>
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 解析模式（<c>None</c>、<c>Markdown</c>、<c>MarkdownV2</c>、<c>HTML</c>）。
    /// </summary>
    [StringLength(20)]
    public string? ParseMode { get; set; }

    /// <summary>
    /// Telegram 返回的消息 ID。
    /// </summary>
    public long? TelegramMessageId { get; set; }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Bot API 的错误码。
    /// </summary>
    public int? ErrorCode { get; set; }

    /// <summary>
    /// 错误信息（截到 1000 字）。
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒，含重试等待）。
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// 发送时间。
    /// </summary>
    public DateTimeOffset SentAt { get; set; }
}
