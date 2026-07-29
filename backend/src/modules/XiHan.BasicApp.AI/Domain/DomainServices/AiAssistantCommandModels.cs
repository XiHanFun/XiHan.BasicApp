// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI 助手创建命令
/// </summary>
public sealed record AiAssistantCreateCommand(
    string AssistantCode,
    string AssistantName,
    string? Avatar,
    string? Description,
    string? Greeting,
    string? PromptCode,
    string? ProviderCode,
    bool EnableKnowledge,
    string? KnowledgeProviderCode,
    int KnowledgeTopK,
    int HistoryRounds,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// AI 助手更新命令（助手编码不可变）
/// </summary>
public sealed record AiAssistantUpdateCommand(
    long BasicId,
    string AssistantName,
    string? Avatar,
    string? Description,
    string? Greeting,
    string? PromptCode,
    string? ProviderCode,
    bool EnableKnowledge,
    string? KnowledgeProviderCode,
    int KnowledgeTopK,
    int HistoryRounds,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// AI 助手状态变更命令
/// </summary>
public sealed record AiAssistantStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// AI 助手命令结果
/// </summary>
public sealed record AiAssistantCommandResult(SysAiAssistant Assistant);
