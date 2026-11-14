// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 消息的表情回应（租户库实体）
/// </summary>
/// <remarks>
/// 每人每个表情一行，不存成消息上的计数或 JSON：回应是开关，点即插入、再点即物理删除，各人的回应互不覆盖，也不用改消息行
/// 同一消息、同一用户、同一表情只有一行，用普通唯一索引（这张表没有软删除）；读与删都按消息，这个索引就够，不另按会话建
/// 存回应时的名字快照：用户在平台库、不能联表
/// 存活期随所属消息：保留清理按所属消息的时间先删回应、再删消息；按回应自己的时间删会漏掉老消息上的新回应
/// </remarks>
[SugarTable(TableName = "sys_chat_message_reaction", TableDescription = "消息的表情回应（租户库实体）")]
[SugarIndex("ux_sys_chat_message_reaction_message_user_emoji", nameof(TenantId), OrderByType.Asc, nameof(MessageId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Emoji), OrderByType.Asc, true)]
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
