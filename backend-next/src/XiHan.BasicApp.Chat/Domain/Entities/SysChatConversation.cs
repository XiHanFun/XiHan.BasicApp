// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 聊天会话（租户库实体）。
/// </summary>
/// <remarks>
/// 单聊、群聊、部门群与助手会话，随租户库隔离（平台一侧租户为 <c>platform</c>）。单聊以 <see cref="PairKey"/>（两个用户 ID 小者在前）在本侧唯一，
/// 助手会话借同一唯一约束承载「一个用户对一个助手只有一个会话」（<c>ai:&lt;助手&gt;:&lt;用户&gt;</c>）。
/// 每发一条消息都更新最后消息与序号，所以不进差异日志。会话没有删除入口（照 4.x）。
/// </remarks>
[ActiveUniqueIndex(nameof(TenantId), nameof(PairKey))]
[SugarIndex("ix_{table}_tenant_id_department_id", nameof(TenantId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("ix_{table}_tenant_id_last_message_at", nameof(TenantId), OrderByType.Asc, nameof(LastMessageAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysChatConversation : TenantScopedAuditedEntity
{
    /// <summary>
    /// 会话类型。
    /// </summary>
    public ChatConversationType Type { get; set; }

    /// <summary>
    /// 会话名称（群名、部门名或助手名）。
    /// </summary>
    /// <remarks>
    /// 单聊为空，显示对端的名字。
    /// </remarks>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// 头像地址。
    /// </summary>
    [StringLength(500)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 单聊与助手会话的唯一键。
    /// </summary>
    [StringLength(100)]
    public string? PairKey { get; set; }

    /// <summary>
    /// 部门群对应的部门。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 群主（只有群聊有）。
    /// </summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// 助手会话对应的助手。
    /// </summary>
    public Guid? AssistantId { get; set; }

    /// <summary>
    /// 成员数。
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// 已发出的最大消息序号。
    /// </summary>
    /// <remarks>
    /// 会话内的消息按序号排序；发消息时对会话行加一取得（行锁串行同一会话的发送）。
    /// </remarks>
    public long LastSeq { get; set; }

    /// <summary>
    /// 最后一条消息。
    /// </summary>
    public Guid? LastMessageId { get; set; }

    /// <summary>
    /// 最后一条消息的时间（UTC）。
    /// </summary>
    public DateTimeOffset? LastMessageAt { get; set; }

    /// <summary>
    /// 最后一条消息的预览。
    /// </summary>
    [StringLength(200)]
    public string? LastMessagePreview { get; set; }

    /// <summary>
    /// 群公告。
    /// </summary>
    [StringLength(2000)]
    public string? Announcement { get; set; }

    /// <summary>
    /// 群描述。
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// 会话类型。
/// </summary>
public enum ChatConversationType
{
    /// <summary>
    /// 单聊。
    /// </summary>
#pragma warning disable CA1720 // 照 4.x 的线上取值 Single（前端按名称取值）
    Single = 1,
#pragma warning restore CA1720

    /// <summary>
    /// 群聊。
    /// </summary>
    Group = 2,

    /// <summary>
    /// 部门群。
    /// </summary>
    Department = 3,

    /// <summary>
    /// 助手会话。
    /// </summary>
    Assistant = 4,
}
