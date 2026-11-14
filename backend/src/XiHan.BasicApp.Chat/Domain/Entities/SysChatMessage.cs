// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 聊天消息（租户库实体）
/// </summary>
/// <remarks>
/// 只追加的租户库记录，不软删除：消息只增长，撤回、编辑与置顶只改它自己的列；保留期（平台参数 <c>chat.policy</c>）到了由作业 <c>chat.messages.cleanup</c> 在平台与每个租户里逐个物理删除，不删附件文件
/// 会话内按 <see cref="Seq"/> 排序而不是按主键：主键是 GUIDv7、同一毫秒内无序；序号由会话行的 <see cref="SysChatConversation.LastSeq"/> 分配，会话内唯一
/// 撤回清空正文、保留行与附件：时间线与引用它的回复不断开；编辑直接覆盖正文、记编辑时间，审计不另留原文（方案 H4）
/// 发送人名字与回复内容存快照：用户在平台库、不能联表；回复快照也不随原消息撤回或编辑回溯
/// 附件存 JSON 列，文件名与大小以文件记录为准；附件以会话为访问范围（方案 H2），会话成员凭会话换下载地址
/// 系统提示与助手回复也是消息、由服务端追加：发送人为空即中性的系统提示，助手回复以助手 ID 作发送人
/// </remarks>
[SugarTable(TableName = "sys_chat_message", TableDescription = "聊天消息（租户库实体）")]
[SugarIndex("ux_sys_chat_message_tenant_id_conversation_id_seq", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc, nameof(Seq), OrderByType.Asc, true)]
[SugarIndex("ix_sys_chat_message_tenant_id_sender_user_id", nameof(TenantId), OrderByType.Asc, nameof(SenderUserId), OrderByType.Asc)]
[SugarIndex("ix_sys_chat_message_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysChatMessage : TenantScopedRecord
{
    /// <summary>
    /// 会话
    /// </summary>
    [SugarColumn(ColumnName = "conversation_id", IsNullable = false, ColumnDescription = "会话")]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 会话内的序号
    /// </summary>
    [SugarColumn(ColumnName = "seq", IsNullable = false, ColumnDescription = "会话内的序号")]
    public long Seq { get; set; }

    /// <summary>
    /// 发送人
    /// </summary>
    /// <remarks>
    /// 中性的系统提示为空；助手回复是助手 ID；公告以操作人的身份发出
    /// </remarks>
    [SugarColumn(ColumnName = "sender_user_id", IsNullable = true, ColumnDescription = "发送人")]
    public Guid? SenderUserId { get; set; }

    /// <summary>
    /// 发送时发送人名字的快照
    /// </summary>
    [SugarColumn(ColumnName = "sender_name", Length = 100, IsNullable = true, ColumnDescription = "发送时发送人名字的快照")]
    public string? SenderName { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    [SugarColumn(ColumnName = "type", IsNullable = false, ColumnDescription = "消息类型")]
    public ChatMessageType Type { get; set; }

    /// <summary>
    /// 正文（撤回后清空）
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = true, ColumnDescription = "正文（撤回后清空）")]
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 附件（JSON 数组：文件、文件名、大小、语音时长）
    /// </summary>
    [SugarColumn(ColumnName = "attachments", IsNullable = true, ColumnDescription = "附件（JSON 数组：文件、文件名、大小、语音时长）")]
    [LongText]
    public string? Attachments { get; set; }

    /// <summary>
    /// 已撤回
    /// </summary>
    [SugarColumn(ColumnName = "is_recalled", IsNullable = false, ColumnDescription = "已撤回")]
    public bool IsRecalled { get; set; }

    /// <summary>
    /// 撤回时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "recalled_at", IsNullable = true, ColumnDescription = "撤回时间（UTC）")]
    public DateTimeOffset? RecalledAt { get; set; }

    /// <summary>
    /// 客户端消息标识（乐观上屏去重）
    /// </summary>
    [SugarColumn(ColumnName = "client_message_id", Length = 50, IsNullable = true, ColumnDescription = "客户端消息标识（乐观上屏去重）")]
    public string? ClientMessageId { get; set; }

    /// <summary>
    /// 回复的消息
    /// </summary>
    [SugarColumn(ColumnName = "reply_to_message_id", IsNullable = true, ColumnDescription = "回复的消息")]
    public Guid? ReplyToMessageId { get; set; }

    /// <summary>
    /// 回复快照（「发送人: 内容」，不随原消息撤回或编辑回溯）
    /// </summary>
    [SugarColumn(ColumnName = "reply_preview", Length = 300, IsNullable = true, ColumnDescription = "回复快照（「发送人: 内容」，不随原消息撤回或编辑回溯）")]
    public string? ReplyPreview { get; set; }

    /// <summary>
    /// 编辑时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "edited_at", IsNullable = true, ColumnDescription = "编辑时间（UTC）")]
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>
    /// 提及的用户（逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "mentioned_user_ids", Length = 1000, IsNullable = true, ColumnDescription = "提及的用户（逗号分隔）")]
    public string? MentionedUserIds { get; set; }

    /// <summary>
    /// 已置顶
    /// </summary>
    [SugarColumn(ColumnName = "is_pinned", IsNullable = false, ColumnDescription = "已置顶")]
    public bool IsPinned { get; set; }

    /// <summary>
    /// 置顶人
    /// </summary>
    [SugarColumn(ColumnName = "pinned_by_user_id", IsNullable = true, ColumnDescription = "置顶人")]
    public Guid? PinnedByUserId { get; set; }

    /// <summary>
    /// 置顶时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "pinned_at", IsNullable = true, ColumnDescription = "置顶时间（UTC）")]
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
