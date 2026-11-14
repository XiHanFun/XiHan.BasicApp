// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 聊天会话（租户库实体）
/// </summary>
/// <remarks>
/// 单聊、群聊、部门群与助手会话，随租户库隔离（平台一侧租户为 <c>platform</c>）。单聊以 <see cref="PairKey"/>（两个用户 ID 小者在前）在本侧唯一，
/// 助手会话借同一唯一约束承载「一个用户对一个助手只有一个会话」（<c>ai:&lt;助手&gt;:&lt;用户&gt;</c>）
/// 每发一条消息都更新最后消息与序号，所以不进差异日志。会话没有删除入口
/// </remarks>
[SugarTable(TableName = "sys_chat_conversation", TableDescription = "聊天会话（租户库实体）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(PairKey), Name = "ux_sys_chat_conversation_tenant_id_pair_key")]
[SugarIndex("ix_sys_chat_conversation_tenant_id_department_id", nameof(TenantId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("ix_sys_chat_conversation_tenant_id_last_message_at", nameof(TenantId), OrderByType.Asc, nameof(LastMessageAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysChatConversation : TenantScopedAuditedEntity
{
    /// <summary>
    /// 会话类型
    /// </summary>
    [SugarColumn(ColumnName = "type", IsNullable = false, ColumnDescription = "会话类型")]
    public ChatConversationType Type { get; set; }

    /// <summary>
    /// 会话名称（群名、部门名或助手名）
    /// </summary>
    /// <remarks>
    /// 单聊为空，显示对端的名字
    /// </remarks>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = true, ColumnDescription = "会话名称（群名、部门名或助手名）")]
    public string? Name { get; set; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [SugarColumn(ColumnName = "avatar", Length = 500, IsNullable = true, ColumnDescription = "头像地址")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 单聊与助手会话的唯一键
    /// </summary>
    [SugarColumn(ColumnName = "pair_key", Length = 100, IsNullable = true, ColumnDescription = "单聊与助手会话的唯一键")]
    public string? PairKey { get; set; }

    /// <summary>
    /// 部门群对应的部门
    /// </summary>
    [SugarColumn(ColumnName = "department_id", IsNullable = true, ColumnDescription = "部门群对应的部门")]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 群主（只有群聊有）
    /// </summary>
    [SugarColumn(ColumnName = "owner_user_id", IsNullable = true, ColumnDescription = "群主（只有群聊有）")]
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// 助手会话对应的助手
    /// </summary>
    [SugarColumn(ColumnName = "assistant_id", IsNullable = true, ColumnDescription = "助手会话对应的助手")]
    public Guid? AssistantId { get; set; }

    /// <summary>
    /// 成员数
    /// </summary>
    [SugarColumn(ColumnName = "member_count", IsNullable = false, ColumnDescription = "成员数")]
    public int MemberCount { get; set; }

    /// <summary>
    /// 已发出的最大消息序号
    /// </summary>
    /// <remarks>
    /// 会话内的消息按序号排序；发消息时对会话行加一取得（行锁串行同一会话的发送）
    /// </remarks>
    [SugarColumn(ColumnName = "last_seq", IsNullable = false, ColumnDescription = "已发出的最大消息序号")]
    public long LastSeq { get; set; }

    /// <summary>
    /// 最后一条消息
    /// </summary>
    [SugarColumn(ColumnName = "last_message_id", IsNullable = true, ColumnDescription = "最后一条消息")]
    public Guid? LastMessageId { get; set; }

    /// <summary>
    /// 最后一条消息的时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "last_message_at", IsNullable = true, ColumnDescription = "最后一条消息的时间（UTC）")]
    public DateTimeOffset? LastMessageAt { get; set; }

    /// <summary>
    /// 最后一条消息的预览
    /// </summary>
    [SugarColumn(ColumnName = "last_message_preview", Length = 200, IsNullable = true, ColumnDescription = "最后一条消息的预览")]
    public string? LastMessagePreview { get; set; }

    /// <summary>
    /// 群公告
    /// </summary>
    [SugarColumn(ColumnName = "announcement", Length = 2000, IsNullable = true, ColumnDescription = "群公告")]
    public string? Announcement { get; set; }

    /// <summary>
    /// 群描述
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true, ColumnDescription = "群描述")]
    public string? Description { get; set; }
}

/// <summary>
/// 会话类型
/// </summary>
public enum ChatConversationType
{
    /// <summary>
    /// 单聊
    /// </summary>
#pragma warning disable CA1720 // 线上取值是 Single（前端按名称取值）
    Single = 1,
#pragma warning restore CA1720

    /// <summary>
    /// 群聊
    /// </summary>
    Group = 2,

    /// <summary>
    /// 部门群
    /// </summary>
    Department = 3,

    /// <summary>
    /// 助手会话
    /// </summary>
    Assistant = 4,
}
