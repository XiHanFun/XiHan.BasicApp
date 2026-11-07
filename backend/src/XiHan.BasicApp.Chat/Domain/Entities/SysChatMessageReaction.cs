// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 消息的表情回应（租户库实体）
/// </summary>
/// <remarks>
/// 同一消息、同一用户、同一表情只有一条，再点即取消（物理删除）；存活期随所属消息，保留清理先删回应再删消息
/// </remarks>
[SugarIndex("ux_{table}_message_user_emoji", nameof(TenantId), OrderByType.Asc, nameof(MessageId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Emoji), OrderByType.Asc, true)]
[SugarIndex("ix_{table}_tenant_id_conversation_id", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc)]
public sealed class SysChatMessageReaction : TenantScopedRecord
{
    /// <summary>
    /// 会话
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// 回应的用户
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 回应时用户名字的快照
    /// </summary>
    [StringLength(100)]
    public string? UserName { get; set; }

    /// <summary>
    /// 表情
    /// </summary>
    [StringLength(16)]
    public string Emoji { get; set; } = string.Empty;
}
