#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysChatConversationMember
// Guid:0b4c3d6e-8d5a-4c7b-9eaf-3a4b5c6d7e8f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统聊天会话成员实体
/// 用户与会话的多对多关系 + 每成员会话态（未读数/已读位/免打扰）
/// </summary>
/// <remarks>
/// 关联：
/// - ConversationId → SysChatConversation；UserId → SysUser
///
/// 写入：
/// - TenantId + ConversationId + UserId 唯一（UX_TeId_CoId_UsId）
/// - 发消息路径对除发送者外的成员 UnreadCount 原子自增；标记已读置零并记录已读位
/// - 退群/被移出 = 软删；重新入群恢复或新建（服务层处理）
///
/// 查询：
/// - 我的会话：IX_TeId_UsId 反查会话 id 集
/// - 会话成员列表/推送收件人：IX_CoId
///
/// 删除：
/// - 仅软删（保留成员变动痕迹）
///
/// 场景：
/// - 会话列表未读角标、总未读数、@我 扩展、免打扰过滤
/// </remarks>
[SugarTable(TableName = "Sys_Chat_Conversation_Member", TableDescription = "系统聊天会话成员表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoId_UsId", nameof(TenantId), OrderByType.Asc, nameof(ConversationId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoId", nameof(ConversationId), OrderByType.Asc)]
public partial class SysChatConversationMember : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnName = "Conversation_Id", ColumnDescription = "会话ID", IsNullable = false)]
    public virtual long ConversationId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 成员角色
    /// </summary>
    [SugarColumn(ColumnName = "Member_Role", ColumnDescription = "成员角色")]
    public virtual ChatMemberRole MemberRole { get; set; } = ChatMemberRole.Member;

    /// <summary>
    /// 未读消息数（发消息路径原子自增，标记已读置零）
    /// </summary>
    [SugarColumn(ColumnName = "Unread_Count", ColumnDescription = "未读消息数")]
    public virtual int UnreadCount { get; set; } = 0;

    /// <summary>
    /// 最后已读消息ID（已读位，扩展已读回执用）
    /// </summary>
    [SugarColumn(ColumnName = "Last_Read_Message_Id", ColumnDescription = "最后已读消息ID", IsNullable = true)]
    public virtual long? LastReadMessageId { get; set; }

    /// <summary>
    /// 最后已读时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Read_Time", ColumnDescription = "最后已读时间", IsNullable = true)]
    public virtual DateTimeOffset? LastReadTime { get; set; }

    /// <summary>
    /// 是否免打扰（免打扰会话不推顶部提醒，仅计未读）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Muted", ColumnDescription = "是否免打扰")]
    public virtual bool IsMuted { get; set; } = false;

    /// <summary>
    /// 是否置顶会话（个人维度，会话列表置顶优先排序）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Pinned", ColumnDescription = "是否置顶会话")]
    public virtual bool IsPinned { get; set; } = false;

    /// <summary>
    /// 入群时间
    /// </summary>
    [SugarColumn(ColumnName = "Join_Time", ColumnDescription = "入群时间")]
    public virtual DateTimeOffset JoinTime { get; set; }
}
