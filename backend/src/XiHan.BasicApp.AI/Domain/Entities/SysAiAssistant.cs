// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 助手
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧，两侧的聊天都能用：提示词、提供商与知识范围的一份配置，另加可用的工具（方案 A3）
/// 编码唯一，建后不可改（会话按助手 ID 绑定）；缺省最多一个；软删除后可以重建
/// </remarks>
[SugarTable(TableName = "sys_ai_assistant", TableDescription = "助手")]
[ActiveUniqueIndex(nameof(Code), Name = "ux_sys_ai_assistant_code")]
public sealed class SysAiAssistant : AppAuditedEntity
{
    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 100, IsNullable = false, ColumnDescription = "编码")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    [SugarColumn(ColumnName = "avatar", Length = 500, IsNullable = true, ColumnDescription = "头像地址")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 简介
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true, ColumnDescription = "简介")]
    public string? Description { get; set; }

    /// <summary>
    /// 开场白（新建会话时作为第一条助手消息）
    /// </summary>
    [SugarColumn(ColumnName = "greeting", Length = 1000, IsNullable = true, ColumnDescription = "开场白（新建会话时作为第一条助手消息）")]
    public string? Greeting { get; set; }

    /// <summary>
    /// 提示词的编码（为空即内置的系统提示词）
    /// </summary>
    [SugarColumn(ColumnName = "prompt_code", Length = 100, IsNullable = true, ColumnDescription = "提示词的编码（为空即内置的系统提示词）")]
    public string? PromptCode { get; set; }

    /// <summary>
    /// 聊天提供商的编码（为空即缺省提供商）
    /// </summary>
    [SugarColumn(ColumnName = "provider_code", Length = 64, IsNullable = true, ColumnDescription = "聊天提供商的编码（为空即缺省提供商）")]
    public string? ProviderCode { get; set; }

    /// <summary>
    /// 是否检索知识库
    /// </summary>
    [SugarColumn(ColumnName = "enable_knowledge", IsNullable = false, ColumnDescription = "是否检索知识库")]
    public bool EnableKnowledge { get; set; } = true;

    /// <summary>
    /// 嵌入提供商的编码（为空即缺省提供商）
    /// </summary>
    [SugarColumn(ColumnName = "knowledge_provider_code", Length = 64, IsNullable = true, ColumnDescription = "嵌入提供商的编码（为空即缺省提供商）")]
    public string? KnowledgeProviderCode { get; set; }

    /// <summary>
    /// 检索的片段数
    /// </summary>
    [SugarColumn(ColumnName = "knowledge_top_k", IsNullable = false, ColumnDescription = "检索的片段数")]
    public int KnowledgeTopK { get; set; } = 5;

    /// <summary>
    /// 带入上下文的历史消息条数
    /// </summary>
    [SugarColumn(ColumnName = "history_rounds", IsNullable = false, ColumnDescription = "带入上下文的历史消息条数")]
    public int HistoryRounds { get; set; } = 10;

    /// <summary>
    /// 可用的工具名（JSON 数组，缺省没有工具）
    /// </summary>
    [SugarColumn(ColumnName = "tools", IsNullable = true, ColumnDescription = "可用的工具名（JSON 数组，缺省没有工具）")]
    [LongText]
    public string? Tools { get; set; }

    /// <summary>
    /// 是否缺省助手
    /// </summary>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否缺省助手")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public AiStatus Status { get; set; } = AiStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
