// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Application.Dtos;

/// <summary>
/// AI 助手创建 DTO
/// </summary>
public sealed class AiAssistantCreateDto
{
    public string AssistantCode { get; set; } = string.Empty;
    public string AssistantName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Description { get; set; }
    public string? Greeting { get; set; }
    public string? PromptCode { get; set; }
    public string? ProviderCode { get; set; }
    public bool EnableKnowledge { get; set; } = true;
    public string? KnowledgeProviderCode { get; set; }
    public int KnowledgeTopK { get; set; } = 5;
    public int HistoryRounds { get; set; } = 10;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// AI 助手更新 DTO
/// </summary>
/// <remarks>AssistantCode 不可变，不在此 DTO。</remarks>
public sealed class AiAssistantUpdateDto : BasicAppUDto
{
    public string AssistantName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Description { get; set; }
    public string? Greeting { get; set; }
    public string? PromptCode { get; set; }
    public string? ProviderCode { get; set; }
    public bool EnableKnowledge { get; set; } = true;
    public string? KnowledgeProviderCode { get; set; }
    public int KnowledgeTopK { get; set; } = 5;
    public int HistoryRounds { get; set; } = 10;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// AI 助手状态更新 DTO
/// </summary>
public sealed class AiAssistantStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// AI 助手单体动作 DTO（设为默认，POST 携带主键）
/// </summary>
public sealed class AiAssistantActionDto : BasicAppDto
{
}

/// <summary>
/// AI 助手分页查询 DTO
/// </summary>
public sealed class AiAssistantPageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsEnabled { get; set; }
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// AI 助手列表项 DTO
/// </summary>
public class AiAssistantListItemDto : BasicAppDto
{
    public string AssistantCode { get; set; } = string.Empty;
    public string AssistantName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Description { get; set; }
    public string? PromptCode { get; set; }
    public string? ProviderCode { get; set; }
    public bool EnableKnowledge { get; set; }
    public string? KnowledgeProviderCode { get; set; }
    public int KnowledgeTopK { get; set; }
    public int HistoryRounds { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public int Sort { get; set; }
    public EnableStatus Status { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// AI 助手详情 DTO
/// </summary>
public sealed class AiAssistantDetailDto : AiAssistantListItemDto
{
    public string? Greeting { get; set; }
    public string? Remark { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// 可用助手 DTO（聊天页选择助手用，只暴露展示所需字段）
/// </summary>
public sealed class AiAssistantOptionDto : BasicAppDto
{
    public string AssistantCode { get; set; } = string.Empty;
    public string AssistantName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}
