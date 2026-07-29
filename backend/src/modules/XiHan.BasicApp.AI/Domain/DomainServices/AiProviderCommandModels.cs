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
/// AI Provider 会话探测结果
/// </summary>
public sealed record AiProviderChatProbe(bool Success, string? Message, long LatencyMs, string Model);

/// <summary>
/// AI Provider 嵌入探测结果（Dimensions 仅探测成功时有值）
/// </summary>
public sealed record AiProviderEmbeddingProbe(bool Success, string? Message, long LatencyMs, string Model, int? Dimensions);

/// <summary>
/// AI Provider 连接测试结果（Embedding 为 null 表示该 provider 未配置嵌入模型）
/// </summary>
public sealed record AiProviderTestResult(AiProviderChatProbe Chat, AiProviderEmbeddingProbe? Embedding)
{
    /// <summary>
    /// 总体是否可用（会话须通过；配置了嵌入模型时嵌入也须通过）
    /// </summary>
    public bool Success => Chat.Success && Embedding?.Success != false;
}
