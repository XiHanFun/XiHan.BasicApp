// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Application.Dtos;

/// <summary>
/// AI 提示词创建 DTO
/// </summary>
public sealed class AiPromptCreateDto
{
    public string PromptCode { get; set; } = string.Empty;
    public string PromptName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Version { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// AI 提示词更新 DTO（PromptCode 不可变，状态走独立 Status 接口）
/// </summary>
public sealed class AiPromptUpdateDto : BasicAppUDto
{
    public string PromptName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Version { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// AI 提示词状态更新 DTO
/// </summary>
public sealed class AiPromptStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// AI 提示词分页查询 DTO
/// </summary>
public sealed class AiPromptPageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public string? Category { get; set; }
    public bool? IsEnabled { get; set; }
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// AI 提示词列表项 DTO（不含正文）
/// </summary>
public class AiPromptListItemDto : BasicAppDto
{
    public string PromptCode { get; set; } = string.Empty;
    public string PromptName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Version { get; set; }
    public bool IsEnabled { get; set; }
    public int Sort { get; set; }
    public EnableStatus Status { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// AI 提示词详情 DTO（含正文）
/// </summary>
public sealed class AiPromptDetailDto : AiPromptListItemDto
{
    public string? Content { get; set; }
    public string? Remark { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
}
