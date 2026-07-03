#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysChatMessageReaction
// Guid:3f6b5a8d-1c2e-4f9a-b7d0-5c6d7e8f9a0b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统聊天消息表情回应实体
/// </summary>
/// <remarks>
/// 关联：ConversationId → SysChatConversation；MessageId → SysChatMessage；UserId → SysUser
///
/// 写入：
/// - toggle 语义：同 (消息, 用户, 表情) 已存在则物理删除（取消回应），否则新增
/// - UserName 为回应时快照（回应汇总展示免查用户表）
///
/// 查询：
/// - 历史消息批量带出：IX_MeId（按消息ID聚合）
///
/// 删除：
/// - toggle-off 物理删除；消息按保留期清理时由清理任务级联删除
/// </remarks>
[SugarTable(TableName = "Sys_Chat_Message_Reaction", TableDescription = "系统聊天消息表情回应表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_MeId_UsId_Em", nameof(TenantId), OrderByType.Asc, nameof(MessageId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Emoji), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_MeId", nameof(MessageId), OrderByType.Asc)]
public partial class SysChatMessageReaction : BasicAppCreationEntity
{
    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnName = "Conversation_Id", ColumnDescription = "会话ID", IsNullable = false)]
    public virtual long ConversationId { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    [SugarColumn(ColumnName = "Message_Id", ColumnDescription = "消息ID", IsNullable = false)]
    public virtual long MessageId { get; set; }

    /// <summary>
    /// 回应用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "回应用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 回应用户名（回应时快照）
    /// </summary>
    [SugarColumn(ColumnName = "User_Name", ColumnDescription = "回应用户名", Length = 50, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// 表情（Unicode emoji 字符）
    /// </summary>
    [SugarColumn(ColumnName = "Emoji", ColumnDescription = "表情", Length = 16, IsNullable = false)]
    public virtual string Emoji { get; set; } = string.Empty;
}
