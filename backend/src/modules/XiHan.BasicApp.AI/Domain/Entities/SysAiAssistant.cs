// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 系统 AI 助手实体（在线聊天中可对话的助手：提示词 + provider + 知识范围的一份配置）
/// </summary>
/// <remarks>
/// 关联：
/// - PromptCode → SysAiPrompt.PromptCode（系统提示词，空则用内置默认）
/// - ProviderCode → SysAiProvider.ConfigCode（会话模型，空则用默认 provider）
/// - KnowledgeProviderCode → SysAiProvider.ConfigCode（嵌入检索用，空则用默认 provider）
/// - 反向：SysChatConversation.AssistantId（每用户每助手一个会话）
///
/// 写入：
/// - AssistantCode 租户内唯一，创建后不可改（会话按助手主键绑定，编码仅作人读标识）
/// - IsDefault 单选互斥，由服务层保证
///
/// 场景：
/// - 在线聊天页与助手一对一提问，回复经知识库检索增强
/// </remarks>
[SugarTable(TableName = "Sys_Ai_Assistant", TableDescription = "系统 AI 助手表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_AsCd", nameof(TenantId), OrderByType.Asc, nameof(AssistantCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysAiAssistant : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 助手编码（租户内唯一）
    /// </summary>
    [SugarColumn(ColumnName = "Assistant_Code", ColumnDescription = "助手编码", Length = 100, IsNullable = false)]
    public virtual string AssistantCode { get; set; } = string.Empty;

    /// <summary>
    /// 助手名称（会话名与消息发送人名快照取自此处）
    /// </summary>
    [SugarColumn(ColumnName = "Assistant_Name", ColumnDescription = "助手名称", Length = 100, IsNullable = false)]
    public virtual string AssistantName { get; set; } = string.Empty;

    /// <summary>
    /// 助手头像（会话头像）
    /// </summary>
    [SugarColumn(ColumnName = "Avatar", ColumnDescription = "助手头像", Length = 500, IsNullable = true)]
    public virtual string? Avatar { get; set; }

    /// <summary>
    /// 助手简介（助手列表展示）
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "助手简介", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 开场白（新建会话时作为第一条助手消息；空则不发）
    /// </summary>
    [SugarColumn(ColumnName = "Greeting", ColumnDescription = "开场白", Length = 1000, IsNullable = true)]
    public virtual string? Greeting { get; set; }

    /// <summary>
    /// 系统提示词编码（→ SysAiPrompt.PromptCode；空则用内置默认）
    /// </summary>
    [SugarColumn(ColumnName = "Prompt_Code", ColumnDescription = "系统提示词编码", Length = 100, IsNullable = true)]
    public virtual string? PromptCode { get; set; }

    /// <summary>
    /// 会话模型 provider 编码（→ SysAiProvider.ConfigCode；空则用默认 provider）
    /// </summary>
    [SugarColumn(ColumnName = "Provider_Code", ColumnDescription = "会话provider编码", Length = 100, IsNullable = true)]
    public virtual string? ProviderCode { get; set; }

    /// <summary>
    /// 是否挂知识库检索
    /// </summary>
    [SugarColumn(ColumnName = "Enable_Knowledge", ColumnDescription = "是否挂知识库检索")]
    public virtual bool EnableKnowledge { get; set; } = true;

    /// <summary>
    /// 嵌入检索 provider 编码（→ SysAiProvider.ConfigCode；空则用默认 provider）
    /// </summary>
    [SugarColumn(ColumnName = "Knowledge_Provider_Code", ColumnDescription = "嵌入检索provider编码", Length = 100, IsNullable = true)]
    public virtual string? KnowledgeProviderCode { get; set; }

    /// <summary>
    /// 检索返回片段数
    /// </summary>
    [SugarColumn(ColumnName = "Knowledge_Top_K", ColumnDescription = "检索返回片段数")]
    public virtual int KnowledgeTopK { get; set; } = 5;

    /// <summary>
    /// 带入上下文的历史消息条数
    /// </summary>
    [SugarColumn(ColumnName = "History_Rounds", ColumnDescription = "带入历史消息条数")]
    public virtual int HistoryRounds { get; set; } = 10;

    /// <summary>
    /// 是否默认助手（租户内至多一个，聊天页默认打开）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Default", ColumnDescription = "是否默认助手")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用（禁用后不出现在助手列表，也不能发起会话）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
