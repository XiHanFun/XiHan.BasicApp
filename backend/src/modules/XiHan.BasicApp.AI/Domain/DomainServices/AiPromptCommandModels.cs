// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI 提示词创建命令
/// </summary>
public sealed record AiPromptCreateCommand(
    string PromptCode,
    string PromptName,
    string? Category,
    string? Version,
    string Content,
    bool IsEnabled,
    int Sort,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// AI 提示词更新命令（编码不可变，不在命令内）
/// </summary>
public sealed record AiPromptUpdateCommand(
    long BasicId,
    string PromptName,
    string? Category,
    string? Version,
    string Content,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// AI 提示词状态变更命令
/// </summary>
public sealed record AiPromptStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// AI 提示词命令结果
/// </summary>
public sealed record AiPromptCommandResult(SysAiPrompt Prompt);
