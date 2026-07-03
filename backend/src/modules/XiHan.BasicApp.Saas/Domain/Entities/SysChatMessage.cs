#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysChatMessage
// Guid:1c5d4e7f-9e6b-4d8c-a0bf-4b5c6d7e8f9a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统聊天消息实体
/// 会话内的单条消息：文本 / 图片 / 文件 / 系统提示
/// </summary>
/// <remarks>
/// 关联：
/// - ConversationId → SysChatConversation；SenderUserId → SysUser；FileId → SysFile（图片/文件消息）
///
/// 写入：
/// - 只追加 + 撤回置标（IsRecalled=true 并清空 Content，保留行以维持时序）
/// - SenderUserName 为发送时快照（用户改名不回溯历史消息）
/// - ClientMessageId 由前端生成，用于乐观上屏去重与多端回显对齐
///
/// 查询：
/// - 会话历史分页：IX_CoId_CrTi（按会话倒序游标分页）
/// - 按发送人追溯：IX_SeUsId
///
/// 删除：
/// - 不支持业务删除；按保留期（配置 saas:chat:retention-days）由清理任务物理删除
/// - 不分月表：撤回/已读需按 id 更新，月分表不适用（仓库仅对 append-only 日志分月）
///
/// 场景：
/// - 站内 IM 消息流、未读计数来源、图片/文件经文件库预签名 URL 访问
/// </remarks>
[SugarTable(TableName = "Sys_Chat_Message", TableDescription = "系统聊天消息表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoId_CrTi", nameof(ConversationId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_SeUsId", nameof(SenderUserId), OrderByType.Asc)]
public partial class SysChatMessage : BasicAppCreationEntity
{
    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnName = "Conversation_Id", ColumnDescription = "会话ID", IsNullable = false)]
    public virtual long ConversationId { get; set; }

    /// <summary>
    /// 发送人用户ID（系统提示消息为 0）
    /// </summary>
    [SugarColumn(ColumnName = "Sender_User_Id", ColumnDescription = "发送人用户ID", IsNullable = false)]
    public virtual long SenderUserId { get; set; }

    /// <summary>
    /// 发送人用户名（发送时快照）
    /// </summary>
    [SugarColumn(ColumnName = "Sender_User_Name", ColumnDescription = "发送人用户名", Length = 50, IsNullable = true)]
    public virtual string? SenderUserName { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    [SugarColumn(ColumnName = "Message_Type", ColumnDescription = "消息类型")]
    public virtual ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

    /// <summary>
    /// 消息内容（文本正文；图片/文件为可选说明文字；撤回后清空）
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "消息内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 关联文件ID（图片/文件消息 → SysFile，经文件库预签名 URL 访问）
    /// </summary>
    [SugarColumn(ColumnName = "File_Id", ColumnDescription = "关联文件ID", IsNullable = true)]
    public virtual long? FileId { get; set; }

    /// <summary>
    /// 文件名（冗余快照，列表展示免查文件表）
    /// </summary>
    [SugarColumn(ColumnName = "File_Name", ColumnDescription = "文件名", Length = 200, IsNullable = true)]
    public virtual string? FileName { get; set; }

    /// <summary>
    /// 文件大小（字节，冗余快照）
    /// </summary>
    [SugarColumn(ColumnName = "File_Size", ColumnDescription = "文件大小", IsNullable = true)]
    public virtual long? FileSize { get; set; }

    /// <summary>
    /// 是否已撤回（撤回置标并清空内容，保留行维持时序）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Recalled", ColumnDescription = "是否已撤回")]
    public virtual bool IsRecalled { get; set; } = false;

    /// <summary>
    /// 撤回时间
    /// </summary>
    [SugarColumn(ColumnName = "Recall_Time", ColumnDescription = "撤回时间", IsNullable = true)]
    public virtual DateTimeOffset? RecallTime { get; set; }

    /// <summary>
    /// 客户端消息ID（前端生成，乐观上屏去重与多端回显对齐）
    /// </summary>
    [SugarColumn(ColumnName = "Client_Message_Id", ColumnDescription = "客户端消息ID", Length = 50, IsNullable = true)]
    public virtual string? ClientMessageId { get; set; }

    /// <summary>
    /// 被回复消息ID（回复消息；空为普通消息）
    /// </summary>
    [SugarColumn(ColumnName = "Reply_To_Message_Id", ColumnDescription = "被回复消息ID", IsNullable = true)]
    public virtual long? ReplyToMessageId { get; set; }

    /// <summary>
    /// 回复快照「{发送人}: {内容截断}」（发送时生成，不随原消息撤回/编辑回溯）
    /// </summary>
    [SugarColumn(ColumnName = "Reply_Preview", ColumnDescription = "回复快照", Length = 300, IsNullable = true)]
    public virtual string? ReplyPreview { get; set; }

    /// <summary>
    /// 编辑时间（非空即展示"已编辑"；仅文本消息限时可编辑）
    /// </summary>
    [SugarColumn(ColumnName = "Edited_Time", ColumnDescription = "编辑时间", IsNullable = true)]
    public virtual DateTimeOffset? EditedTime { get; set; }

    /// <summary>
    /// 被 @ 用户ID 逗号串（发送时校验均为会话成员）
    /// </summary>
    [SugarColumn(ColumnName = "Mentioned_User_Ids", ColumnDescription = "被@用户ID串", Length = 500, IsNullable = true)]
    public virtual string? MentionedUserIds { get; set; }

    /// <summary>
    /// 是否被 Pin（会话内置顶消息，每会话有上限）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Pinned", ColumnDescription = "是否被Pin")]
    public virtual bool IsPinned { get; set; } = false;

    /// <summary>
    /// Pin 操作人用户ID
    /// </summary>
    [SugarColumn(ColumnName = "Pinned_By_User_Id", ColumnDescription = "Pin操作人用户ID", IsNullable = true)]
    public virtual long? PinnedByUserId { get; set; }

    /// <summary>
    /// Pin 时间
    /// </summary>
    [SugarColumn(ColumnName = "Pinned_Time", ColumnDescription = "Pin时间", IsNullable = true)]
    public virtual DateTimeOffset? PinnedTime { get; set; }
}
