// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 消息的表情回应（租户库实体）
/// </summary>
/// <remarks>
/// 同一消息、同一用户、同一表情只有一条，再点即取消（物理删除）；存活期随所属消息，保留清理先删回应再删消息
/// </remarks>
[SugarTable(TableName = "sys_chat_message_reaction", TableDescription = "消息的表情回应（租户库实体）")]
[SugarIndex("ux_sys_chat_message_reaction_message_user_emoji", nameof(TenantId), OrderByType.Asc, nameof(MessageId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Emoji), OrderByType.Asc, true)]
[SugarIndex("ix_sys_chat_message_reaction_tenant_id_conversation_id", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc)]
public sealed class SysChatMessageReaction : TenantScopedRecord
{
    /// <summary>
    /// 会话
    /// </summary>
    [SugarColumn(ColumnName = "conversation_id", IsNullable = false, ColumnDescription = "会话")]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    [SugarColumn(ColumnName = "message_id", IsNullable = false, ColumnDescription = "消息")]
    public Guid MessageId { get; set; }

    /// <summary>
    /// 回应的用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "回应的用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 回应时用户名字的快照
    /// </summary>
    [SugarColumn(ColumnName = "user_name", Length = 100, IsNullable = true, ColumnDescription = "回应时用户名字的快照")]
    public string? UserName { get; set; }

    /// <summary>
    /// 表情
    /// </summary>
    [SugarColumn(ColumnName = "emoji", Length = 16, IsNullable = false, ColumnDescription = "表情")]
    public string Emoji { get; set; } = string.Empty;
}
