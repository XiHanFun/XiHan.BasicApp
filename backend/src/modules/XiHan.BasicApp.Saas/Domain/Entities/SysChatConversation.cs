// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统聊天会话实体
/// 在线聊天的会话聚合：单聊 / 群聊 / 部门群 三种形态的统一载体
/// </summary>
/// <remarks>
/// 关联：
/// - 反向：SysChatConversationMember（1:N 成员）、SysChatMessage（1:N 消息）
/// - DepartmentId → SysDepartment（仅部门群）；OwnerUserId → SysUser（仅群聊群主）
///
/// 写入：
/// - 单聊：PairKey = "{小UserId}_{大UserId}"，租户内唯一（UX_TeId_PaKe），保证同一对用户只有一个单聊会话
/// - 群聊：OwnerUserId 必填、ConversationName 必填；部门群：DepartmentId 租户内唯一（按类型约定，服务层保证）
/// - LastMessage* 三字段由发消息路径冗余维护（会话列表免 JOIN 消息表）
///
/// 查询：
/// - 我的会话列表：经成员表反查会话 id，按 IX_TeId_LaMeTi 倒序
/// - 单聊定位：UX_TeId_PaKe；部门群定位：IX_TeId_DeId
///
/// 删除：
/// - 仅软删；软删后消息保留（按保留期由清理任务物理删除）
///
/// 场景：
/// - 站内 IM：同事单聊、项目群聊、部门自动群
/// </remarks>
[SugarTable(TableName = "Sys_Chat_Conversation", TableDescription = "系统聊天会话表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_PaKe", nameof(TenantId), OrderByType.Asc, nameof(PairKey), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_LaMeTi", nameof(TenantId), OrderByType.Asc, nameof(LastMessageTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_DeId", nameof(TenantId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc)]
public partial class SysChatConversation : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 会话类型
    /// </summary>
    [SugarColumn(ColumnName = "Conversation_Type", ColumnDescription = "会话类型")]
    public virtual ChatConversationType ConversationType { get; set; } = ChatConversationType.Single;

    /// <summary>
    /// 会话名称（单聊为空，前端以对端用户名展示；群聊/部门群必填）
    /// </summary>
    [SugarColumn(ColumnName = "Conversation_Name", ColumnDescription = "会话名称", Length = 100, IsNullable = true)]
    public virtual string? ConversationName { get; set; }

    /// <summary>
    /// 会话头像（群聊可自定义；单聊为空取对端头像）
    /// </summary>
    [SugarColumn(ColumnName = "Avatar", ColumnDescription = "会话头像", Length = 500, IsNullable = true)]
    public virtual string? Avatar { get; set; }

    /// <summary>
    /// 单聊配对键："{小UserId}_{大UserId}"（仅单聊，租户内唯一；群聊/部门群为空）
    /// </summary>
    [SugarColumn(ColumnName = "Pair_Key", ColumnDescription = "单聊配对键", Length = 50, IsNullable = true)]
    public virtual string? PairKey { get; set; }

    /// <summary>
    /// 部门ID（仅部门群；服务层保证同一部门只建一个部门群）
    /// </summary>
    [SugarColumn(ColumnName = "Department_Id", ColumnDescription = "部门ID", IsNullable = true)]
    public virtual long? DepartmentId { get; set; }

    /// <summary>
    /// 群主用户ID（仅群聊；单聊/部门群为空）
    /// </summary>
    [SugarColumn(ColumnName = "Owner_User_Id", ColumnDescription = "群主用户ID", IsNullable = true)]
    public virtual long? OwnerUserId { get; set; }

    /// <summary>
    /// 成员数量（冗余计数，成员增删时同步维护）
    /// </summary>
    [SugarColumn(ColumnName = "Member_Count", ColumnDescription = "成员数量")]
    public virtual int MemberCount { get; set; } = 0;

    /// <summary>
    /// 最后一条消息ID（冗余，会话列表免 JOIN）
    /// </summary>
    [SugarColumn(ColumnName = "Last_Message_Id", ColumnDescription = "最后一条消息ID", IsNullable = true)]
    public virtual long? LastMessageId { get; set; }

    /// <summary>
    /// 最后一条消息时间（会话列表排序依据）
    /// </summary>
    [SugarColumn(ColumnName = "Last_Message_Time", ColumnDescription = "最后一条消息时间", IsNullable = true)]
    public virtual DateTimeOffset? LastMessageTime { get; set; }

    /// <summary>
    /// 最后一条消息预览（截断文本或 [图片]/[文件] 占位；撤回时同步改为撤回占位）
    /// </summary>
    [SugarColumn(ColumnName = "Last_Message_Preview", ColumnDescription = "最后一条消息预览", Length = 200, IsNullable = true)]
    public virtual string? LastMessagePreview { get; set; }

    /// <summary>
    /// 群公告（群聊/部门群；变更时追加系统提示消息）
    /// </summary>
    [SugarColumn(ColumnName = "Announcement", ColumnDescription = "群公告", Length = 2000, IsNullable = true)]
    public virtual string? Announcement { get; set; }

    /// <summary>
    /// 群描述
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "群描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }
}
