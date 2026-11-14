// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// Telegram 的出站审计（平台库实体）
/// </summary>
/// <remarks>
/// 发消息、改消息这几种调用在成功或最终失败时各记一条（不按每次重试记）
/// 普通表，没有页面与操作
/// </remarks>
[SugarTable(TableName = "sys_telegram_message", TableDescription = "Telegram 的出站审计（平台库实体）")]
[DiffLogIgnored]
public sealed class SysTelegramMessage : AppAuditedEntity
{
    /// <summary>
    /// 机器人名称
    /// </summary>
    [SugarColumn(ColumnName = "bot_name", Length = 64, IsNullable = false, ColumnDescription = "机器人名称")]
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// 机器人 ID
    /// </summary>
    [SugarColumn(ColumnName = "bot_id", IsNullable = false, ColumnDescription = "机器人 ID")]
    public Guid BotId { get; set; }

    /// <summary>
    /// 目标会话
    /// </summary>
    [SugarColumn(ColumnName = "chat_id", IsNullable = false, ColumnDescription = "目标会话")]
    public long ChatId { get; set; }

    /// <summary>
    /// Bot API 方法名
    /// </summary>
    /// <remarks>
    /// 如 <c>sendMessage</c>、<c>editMessageText</c> 等
    /// </remarks>
    [SugarColumn(ColumnName = "api_method", Length = 50, IsNullable = false, ColumnDescription = "Bot API 方法名")]
    public string ApiMethod { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型（<c>text</c>、<c>photo</c>、<c>document</c>、<c>reply_markup</c>）
    /// </summary>
    [SugarColumn(ColumnName = "message_type", Length = 50, IsNullable = false, ColumnDescription = "消息类型（text、photo、document、reply_markup）")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// 内容（正文或说明）
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = true, ColumnDescription = "内容（正文或说明）")]
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 解析模式（<c>None</c>、<c>Markdown</c>、<c>MarkdownV2</c>、<c>HTML</c>）
    /// </summary>
    [SugarColumn(ColumnName = "parse_mode", Length = 20, IsNullable = true, ColumnDescription = "解析模式（None、Markdown、MarkdownV2、HTML）")]
    public string? ParseMode { get; set; }

    /// <summary>
    /// Telegram 返回的消息 ID
    /// </summary>
    [SugarColumn(ColumnName = "telegram_message_id", IsNullable = true, ColumnDescription = "Telegram 返回的消息 ID")]
    public long? TelegramMessageId { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnName = "success", IsNullable = false, ColumnDescription = "是否成功")]
    public bool Success { get; set; }

    /// <summary>
    /// Bot API 的错误码
    /// </summary>
    [SugarColumn(ColumnName = "error_code", IsNullable = true, ColumnDescription = "Bot API 的错误码")]
    public int? ErrorCode { get; set; }

    /// <summary>
    /// 错误信息（截到 1000 字）
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 1000, IsNullable = true, ColumnDescription = "错误信息（截到 1000 字）")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒，含重试等待）
    /// </summary>
    [SugarColumn(ColumnName = "elapsed_ms", IsNullable = false, ColumnDescription = "耗时（毫秒，含重试等待）")]
    public long ElapsedMs { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnName = "sent_at", IsNullable = false, ColumnDescription = "发送时间")]
    public DateTimeOffset SentAt { get; set; }
}
