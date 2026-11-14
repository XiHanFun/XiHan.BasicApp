// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 聊天会话（租户库实体）
/// </summary>
/// <remarks>
/// 落租户库（平台一侧租户为 <c>platform</c>）：两侧都能聊天，聊天记录随租户分库隔离
/// 单聊、群聊、部门群与助手会话共用一张表、按类型区分；单聊与助手会话的「只有一个」都落在 <see cref="PairKey"/> 的部分唯一索引上：单聊是两个用户 ID 小者在前，助手会话是 <c>ai:&lt;助手&gt;:&lt;用户&gt;</c>
/// 消息序号在会话行上分配：发消息时对 <see cref="LastSeq"/> 加一，行锁让同一会话的发送串行，序号不重不漏
/// 最后消息、预览与成员数冗余在会话上，会话列表不用去数消息与成员；代价是每条消息都改会话行，所以不进差异日志
/// 会话列表从本人的成员行出发、按 ID 取会话，在内存里按置顶与最后消息时间排，所以不给最后消息时间建索引：它随每条消息改，索引只会多一次写
/// 部门群跟着部门走：每个部门打开的是同一个部门群，名称随部门、不能改，成员随 Organization 的部门归属事件同步；部门是租户库实体，所以部门群只在租户一侧
/// 助手由 AI 包经 Contracts 的 <c>IChatAssistants</c> 提供，会话只记 <see cref="AssistantId"/> 与建会话时的名字和头像，本包不依赖 AI 包
/// 没有删除入口，只有群聊能由群主解散：解散即软删除会话与它的全部成员行，消息留着（保留清理只删消息），聊天审计连解散了的一起看
/// </remarks>
[SugarTable(TableName = "sys_chat_conversation", TableDescription = "聊天会话（租户库实体）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(PairKey), Name = "ux_sys_chat_conversation_tenant_id_pair_key")]
[SugarIndex("ix_sys_chat_conversation_tenant_id_department_id", nameof(TenantId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc)]
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
