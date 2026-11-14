// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 机器人消息的发件箱（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 机器人消息另有发件箱，不并进邮件与短信的记录：目标是机器人或广播群而不是某个人，没有收件地址；一条通知给本侧每个服务商的缺省机器人各写一条，平台一侧另给每个 Telegram 广播群各写一条，每条各自重试、互不拖累
/// 只存目标的 <see cref="ConfigId"/>（Telegram 另存 <see cref="ChatId"/>），不复制 Webhook 地址与令牌：密文只留在配置里，发送时按 ID 重读，停用或删除后排队中的消息即发送失败，不会再发到撤下的群
/// 只发本侧的机器人：租户的通知不进平台的群，Telegram 只在平台一侧
/// 与邮件、短信同一个发送循环、同一套领取与重试，随发布一起提交；正文写入时已渲染成纯文本
/// 没有操作与页面，只供发送循环与排查；发送循环频繁改状态，所以不进差异日志；没有清理作业
/// </remarks>
[SugarTable(TableName = "sys_bot_message", TableDescription = "机器人消息的发件箱（平台库实体，随所属侧）")]
[SugarIndex("ix_sys_bot_message_status_created_at", nameof(Status), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysBotMessage : AppAuditedEntity, IOwnedByTenant, IMessageRecord
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 发起人（系统发出的为空）
    /// </summary>
    [SugarColumn(ColumnName = "sender_id", IsNullable = true, ColumnDescription = "发起人（系统发出的为空）")]
    public Guid? SenderId { get; set; }

    /// <summary>
    /// 通道
    /// </summary>
    [SugarColumn(ColumnName = "channel", IsNullable = false, ColumnDescription = "通道")]
    public BotChannel Channel { get; set; }

    /// <summary>
    /// 目标会话（Telegram 的 chat ID）
    /// </summary>
    /// <remarks>
    /// 群机器人为空
    /// </remarks>
    [SugarColumn(ColumnName = "chat_id", Length = 64, IsNullable = true, ColumnDescription = "目标会话（Telegram 的 chat ID）")]
    public string? ChatId { get; set; }

    /// <summary>
    /// 纯文本正文（标题、正文与链接各一段）
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = false, ColumnDescription = "纯文本正文（标题、正文与链接各一段）")]
    [LongText]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public MessageSendStatus Status { get; set; } = MessageSendStatus.Pending;

    /// <summary>
    /// 已尝试的次数
    /// </summary>
    [SugarColumn(ColumnName = "attempts", IsNullable = false, ColumnDescription = "已尝试的次数")]
    public int Attempts { get; set; }

    /// <summary>
    /// 最多尝试的次数
    /// </summary>
    [SugarColumn(ColumnName = "max_attempts", IsNullable = false, ColumnDescription = "最多尝试的次数")]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// 下次可以尝试的时间
    /// </summary>
    /// <remarks>
    /// 为空即立即
    /// </remarks>
    [SugarColumn(ColumnName = "next_attempt_at", IsNullable = true, ColumnDescription = "下次可以尝试的时间")]
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// 发出时间
    /// </summary>
    [SugarColumn(ColumnName = "sent_at", IsNullable = true, ColumnDescription = "发出时间")]
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// 最近一次失败的原因
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 2000, IsNullable = true, ColumnDescription = "最近一次失败的原因")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 要发到的机器人配置或 Telegram 机器人
    /// </summary>
    /// <remarks>
    /// 写入时选定；之后被停用或删除即失败
    /// </remarks>
    [SugarColumn(ColumnName = "config_id", IsNullable = true, ColumnDescription = "要发到的机器人配置或 Telegram 机器人")]
    public Guid? ConfigId { get; set; }

    /// <summary>
    /// 关联的业务类型
    /// </summary>
    [SugarColumn(ColumnName = "business_type", Length = 100, IsNullable = true, ColumnDescription = "关联的业务类型")]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID
    /// </summary>
    [SugarColumn(ColumnName = "business_id", Length = 100, IsNullable = true, ColumnDescription = "关联的业务 ID")]
    public string? BusinessId { get; set; }
}

/// <summary>
/// 机器人消息走的通道
/// </summary>
/// <remarks>
/// 三家群机器人（与 <see cref="BotProvider"/> 同值）与 Telegram（平台托管）
/// </remarks>
public enum BotChannel
{
    /// <summary>
    /// 钉钉群机器人
    /// </summary>
    DingTalk = 1,

    /// <summary>
    /// 飞书群机器人
    /// </summary>
    Lark = 2,

    /// <summary>
    /// 企业微信群机器人
    /// </summary>
    WeCom = 3,

    /// <summary>
    /// Telegram 机器人（只在平台一侧广播）
    /// </summary>
    Telegram = 4,
}
