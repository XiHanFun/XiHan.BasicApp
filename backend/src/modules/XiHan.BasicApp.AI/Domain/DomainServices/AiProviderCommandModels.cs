// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI Provider 创建命令
/// </summary>
public sealed record AiProviderCreateCommand(
    string ConfigCode,
    string ConfigName,
    string Provider,
    string Model,
    string? EmbeddingModel,
    string? BaseUrl,
    string? ApiKey,
    int? MaxOutputTokens,
    float? Temperature,
    int? TimeoutSeconds,
    string? ExtraJson,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// AI Provider 更新命令
/// </summary>
/// <remarks>ApiKey 为 null/空表示保留原密钥；配置编码 ConfigCode 不可变，不在命令内。</remarks>
public sealed record AiProviderUpdateCommand(
    long BasicId,
    string ConfigName,
    string Provider,
    string Model,
    string? EmbeddingModel,
    string? BaseUrl,
    string? ApiKey,
    int? MaxOutputTokens,
    float? Temperature,
    int? TimeoutSeconds,
    string? ExtraJson,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// AI Provider 状态变更命令
/// </summary>
public sealed record AiProviderStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// AI Provider 命令结果
/// </summary>
public sealed record AiProviderCommandResult(SysAiProvider Provider);

/// <summary>
/// AI Provider 连接测试结果
/// </summary>
public sealed record AiProviderTestResult(bool Success, string? Message, long LatencyMs, string? Model);
