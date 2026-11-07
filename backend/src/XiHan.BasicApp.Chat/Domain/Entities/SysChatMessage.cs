// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 聊天消息（租户库实体）
/// </summary>
/// <remarks>
/// 不软删除，按保留期由清理作业物理删除。会话内按 <see cref="Seq"/> 排序（主键是 GUIDv7、同一毫秒内无序）
/// 撤回清空正文、保留行与附件；编辑直接覆盖正文、记编辑时间（审计不另留原文，方案 H4）
/// </remarks>
[SugarIndex("ux_{table}_tenant_id_conversation_id_seq", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc, nameof(Seq), OrderByType.Asc, true)]
[SugarIndex("ix_{table}_tenant_id_sender_user_id", nameof(TenantId), OrderByType.Asc, nameof(SenderUserId), OrderByType.Asc)]
[SugarIndex("ix_{table}_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysChatMessage : TenantScopedRecord
{
    /// <summary>
    /// 会话
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 会话内的序号
    /// </summary>
    public long Seq { get; set; }

    /// <summary>
    /// 发送人
    /// </summary>
    /// <remarks>
    /// 中性的系统提示为空；助手回复是助手 ID；公告以操作人的身份发出
    /// </remarks>
    public Guid? SenderUserId { get; set; }

    /// <summary>
    /// 发送时发送人名字的快照
    /// </summary>
    [StringLength(100)]
    public string? SenderName { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public ChatMessageType Type { get; set; }

    /// <summary>
    /// 正文（撤回后清空）
    /// </summary>
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 附件（JSON 数组：文件、文件名、大小、语音时长）
    /// </summary>
    [LongText]
    public string? Attachments { get; set; }

    /// <summary>
    /// 已撤回
    /// </summary>
    public bool IsRecalled { get; set; }

    /// <summary>
    /// 撤回时间（UTC）
    /// </summary>
    public DateTimeOffset? RecalledAt { get; set; }

    /// <summary>
    /// 客户端消息标识（乐观上屏去重）
    /// </summary>
    [StringLength(50)]
    public string? ClientMessageId { get; set; }

    /// <summary>
    /// 回复的消息
    /// </summary>
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>
    /// 回复快照（「发送人: 内容」，不随原消息撤回或编辑回溯）
    /// </summary>
    [StringLength(300)]
    public string? ReplyPreview { get; set; }

    /// <summary>
    /// 编辑时间（UTC）
    /// </summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>
    /// 提及的用户（逗号分隔）
    /// </summary>
    [StringLength(1000)]
    public string? MentionedUserIds { get; set; }

    /// <summary>
    /// 已置顶
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// 置顶人
    /// </summary>
    public Guid? PinnedByUserId { get; set; }

    /// <summary>
    /// 置顶时间（UTC）
    /// </summary>
    public DateTimeOffset? PinnedAt { get; set; }
}

/// <summary>
/// 消息类型
/// </summary>
public enum ChatMessageType
{
    /// <summary>
    /// 文本
    /// </summary>
    Text = 1,

    /// <summary>
    /// 图片
    /// </summary>
    Image = 2,

    /// <summary>
    /// 语音
    /// </summary>
    Voice = 3,

    /// <summary>
    /// 文件
    /// </summary>
    File = 4,

    /// <summary>
    /// 助手回复
    /// </summary>
    Assistant = 5,

    /// <summary>
    /// 系统提示
    /// </summary>
    System = 99,
}
