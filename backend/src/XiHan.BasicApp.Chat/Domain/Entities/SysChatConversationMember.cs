// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Chat.Domain.Entities;

/// <summary>
/// 会话成员（租户库实体）
/// </summary>
/// <remarks>
/// 成员行同时承载「某人在某会话」的个人状态（角色、禁言、置顶、免打扰、未读数、已读位）：这些和成员身份同生同灭，不另建表
/// 未读数冗余存：发消息时一条更新语句给其他成员加一、标已读清零，会话列表不用按已读位去数消息；已读位存序号、只前移，群的已读回执按它比较
/// 退群、被移出或离开部门即软删除，部分唯一索引让再入群另写一行
/// 入群的人须注册在本侧或是本侧的租户成员：成员行记的是建行时的一侧，不拦就会建出对方那一侧看不到的会话
/// 部门群的成员随 Organization 的部门归属事件进出，按处理时的归属核对，消息乱序或重复投递都收敛到同一结果
/// 用户在平台库、聊天在租户库，分库时不能联表，名字按主键批量补；读写频繁，所以不进差异日志
/// </remarks>
[SugarTable(TableName = "sys_chat_conversation_member", TableDescription = "会话成员（租户库实体）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(ConversationId), nameof(UserId), Name = "ux_sys_chat_conversation_member_conversation_user")]
[SugarIndex("ix_sys_chat_conversation_member_tenant_id_user_id", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
[SugarIndex("ix_sys_chat_conversation_member_tenant_id_conversation_id", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysChatConversationMember : TenantScopedAuditedEntity
{
    /// <summary>
    /// 会话
    /// </summary>
    [SugarColumn(ColumnName = "conversation_id", IsNullable = false, ColumnDescription = "会话")]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 成员角色
    /// </summary>
    [SugarColumn(ColumnName = "role", IsNullable = false, ColumnDescription = "成员角色")]
    public ChatMemberRole Role { get; set; } = ChatMemberRole.Member;

    /// <summary>
    /// 未读数
    /// </summary>
    [SugarColumn(ColumnName = "unread_count", IsNullable = false, ColumnDescription = "未读数")]
    public int UnreadCount { get; set; }

    /// <summary>
    /// 已读到的消息
    /// </summary>
    [SugarColumn(ColumnName = "last_read_message_id", IsNullable = true, ColumnDescription = "已读到的消息")]
    public Guid? LastReadMessageId { get; set; }

    /// <summary>
    /// 已读到的消息序号（没读过为 0）
    /// </summary>
    [SugarColumn(ColumnName = "last_read_seq", IsNullable = false, ColumnDescription = "已读到的消息序号（没读过为 0）")]
    public long LastReadSeq { get; set; }

    /// <summary>
    /// 最后一次标记已读的时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "last_read_at", IsNullable = true, ColumnDescription = "最后一次标记已读的时间（UTC）")]
    public DateTimeOffset? LastReadAt { get; set; }

    /// <summary>
    /// 免打扰（个人设置）
    /// </summary>
    [SugarColumn(ColumnName = "is_muted", IsNullable = false, ColumnDescription = "免打扰（个人设置）")]
    public bool IsMuted { get; set; }

    /// <summary>
    /// 会话置顶（个人设置）
    /// </summary>
    [SugarColumn(ColumnName = "is_pinned", IsNullable = false, ColumnDescription = "会话置顶（个人设置）")]
    public bool IsPinned { get; set; }

    /// <summary>
    /// 被禁言
    /// </summary>
    [SugarColumn(ColumnName = "is_silenced", IsNullable = false, ColumnDescription = "被禁言")]
    public bool IsSilenced { get; set; }

    /// <summary>
    /// 入会时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "joined_at", IsNullable = false, ColumnDescription = "入会时间（UTC）")]
    public DateTimeOffset JoinedAt { get; set; }
}

/// <summary>
/// 成员角色
/// </summary>
public enum ChatMemberRole
{
    /// <summary>
    /// 群主
    /// </summary>
    Owner = 1,

    /// <summary>
    /// 管理员
    /// </summary>
    Admin = 2,

    /// <summary>
    /// 普通成员
    /// </summary>
    Member = 3,
}
