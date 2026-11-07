// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 助手
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧，两侧的聊天都能用（照 4.x）：提示词、提供商与知识范围的一份配置，另加可用的工具（方案 A3）
/// 编码唯一，建后不可改（会话按助手 ID 绑定）；缺省最多一个；软删除后可以重建
/// </remarks>
[ActiveUniqueIndex(nameof(Code))]
public sealed class SysAiAssistant : AppAuditedEntity
{
    /// <summary>
    /// 编码
    /// </summary>
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    [StringLength(500)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 简介
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 开场白（新建会话时作为第一条助手消息）
    /// </summary>
    [StringLength(1000)]
    public string? Greeting { get; set; }

    /// <summary>
    /// 提示词的编码（为空即内置的系统提示词）
    /// </summary>
    [StringLength(100)]
    public string? PromptCode { get; set; }

    /// <summary>
    /// 聊天提供商的编码（为空即缺省提供商）
    /// </summary>
    [StringLength(64)]
    public string? ProviderCode { get; set; }

    /// <summary>
    /// 是否检索知识库
    /// </summary>
    public bool EnableKnowledge { get; set; } = true;

    /// <summary>
    /// 嵌入提供商的编码（为空即缺省提供商）
    /// </summary>
    [StringLength(64)]
    public string? KnowledgeProviderCode { get; set; }

    /// <summary>
    /// 检索的片段数
    /// </summary>
    public int KnowledgeTopK { get; set; } = 5;

    /// <summary>
    /// 带入上下文的历史消息条数
    /// </summary>
    public int HistoryRounds { get; set; } = 10;

    /// <summary>
    /// 可用的工具名（JSON 数组，缺省没有工具）
    /// </summary>
    [LongText]
    public string? Tools { get; set; }

    /// <summary>
    /// 是否缺省助手
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public AiStatus Status { get; set; } = AiStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
