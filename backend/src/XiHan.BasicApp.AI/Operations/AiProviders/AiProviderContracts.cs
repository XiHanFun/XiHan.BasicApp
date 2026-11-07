// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.AiProviders;

/// <summary>
/// 提供商
/// </summary>
/// <remarks>
/// API 密钥不回显，只说有没有
/// </remarks>
/// <param name="Id">主键</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Vendor">厂商</param>
/// <param name="ChatModel">聊天模型</param>
/// <param name="EmbeddingModel">嵌入模型</param>
/// <param name="Endpoint">端点</param>
/// <param name="HasApiKey">是否设了 API 密钥</param>
/// <param name="MaxOutputTokens">最大输出令牌数</param>
/// <param name="Temperature">采样温度</param>
/// <param name="TimeoutSeconds">单次调用超时（秒）</param>
/// <param name="IsDefault">是否缺省提供商</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record AiProviderResponse(
    Guid Id,
    string Code,
    string Name,
    string Vendor,
    string ChatModel,
    string? EmbeddingModel,
    string? Endpoint,
    bool HasApiKey,
    int? MaxOutputTokens,
    float? Temperature,
    int? TimeoutSeconds,
    bool IsDefault,
    AiStatus Status,
    int Order,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="provider">提供商</param>
    public static AiProviderResponse From(SysAiProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new AiProviderResponse(
            provider.Id,
            provider.Code,
            provider.Name,
            provider.Vendor,
            provider.ChatModel,
            provider.EmbeddingModel,
            provider.Endpoint,
            !string.IsNullOrEmpty(provider.ApiKey),
            provider.MaxOutputTokens,
            provider.Temperature,
            provider.TimeoutSeconds,
            provider.IsDefault,
            provider.Status,
            provider.Order,
            provider.Remark,
            provider.ConcurrencyStamp,
            provider.CreatedAt,
            provider.ModifiedAt);
    }
}

/// <summary>
/// 测试连接的结果
/// </summary>
/// <param name="Success">聊天与嵌入（配了嵌入模型时）是否都通</param>
/// <param name="Chat">聊天模型的探测</param>
/// <param name="Embedding">嵌入模型的探测；没配嵌入模型为空</param>
public sealed record AiProviderTestResponse(bool Success, AiProviderProbe Chat, AiProviderProbe? Embedding);

/// <summary>
/// 一次探测的结果
/// </summary>
/// <param name="Success">是否通</param>
/// <param name="Message">成功时是聊天的结束原因，失败时是提供商给的原因</param>
/// <param name="ElapsedMilliseconds">耗时（毫秒）</param>
/// <param name="Model">探测的模型</param>
/// <param name="Dimensions">嵌入向量的维度；聊天探测为空</param>
public sealed record AiProviderProbe(bool Success, string? Message, long ElapsedMilliseconds, string Model, int? Dimensions);

/// <summary>
/// <c>ai-providers.get</c>、<c>set-default</c> 与 <c>test</c> 的请求
/// </summary>
/// <param name="Id">提供商 ID</param>
public sealed record AiProviderIdRequest(Guid Id);

/// <summary>
/// <c>ai-providers.create</c> 的请求
/// </summary>
/// <remarks>
/// API 密钥加密存储；设为缺省时别的提供商取消缺省。编码按框架 Provider 名的文法（方案 N4 D1），建后不可改
/// </remarks>
/// <param name="Code">编码（唯一）</param>
/// <param name="Name">名称</param>
/// <param name="ChatModel">聊天模型</param>
/// <param name="Vendor">厂商（只用于分组展示，如 OpenAI、DeepSeek）</param>
/// <param name="EmbeddingModel">嵌入模型</param>
/// <param name="Endpoint">端点：绝对的 http 或 https 地址，厂商是 OpenAI 时可留空，走它的官方端点；别的厂商必填</param>
/// <param name="ApiKey">API 密钥</param>
/// <param name="MaxOutputTokens">最大输出令牌数</param>
/// <param name="Temperature">采样温度</param>
/// <param name="TimeoutSeconds">单次调用超时（秒）</param>
/// <param name="IsDefault">是否缺省提供商</param>
/// <param name="Status">初始状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record CreateAiProviderRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression("^[a-z0-9][a-z0-9._-]{0,63}$")] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(100, MinimumLength = 1)] string ChatModel,
    [Required, StringLength(50, MinimumLength = 1)] string Vendor,
    [StringLength(100)] string? EmbeddingModel = null,
    [StringLength(500)] string? Endpoint = null,
    [StringLength(1000)] string? ApiKey = null,
    [Range(1, int.MaxValue)] int? MaxOutputTokens = null,
    [Range(0d, 2d)] float? Temperature = null,
    [Range(1, 3600)] int? TimeoutSeconds = null,
    bool IsDefault = false,
    AiStatus Status = AiStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (value, member) in new[] { (Name, nameof(Name)), (Vendor, nameof(Vendor)), (ChatModel, nameof(ChatModel)) })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return new ValidationResult(AIMessages.FieldBlank.L(), [member]);
            }
        }

        if (!AiProviderService.IsEndpoint(Endpoint))
        {
            yield return new ValidationResult(AIMessages.ProviderEndpointInvalid.L(), [nameof(Endpoint)]);
        }

        if (AiProviderService.MissesEndpoint(Vendor, Endpoint))
        {
            yield return new ValidationResult(AIMessages.ProviderEndpointRequired.L(), [nameof(Endpoint)]);
        }
    }
}

/// <summary>
/// <c>ai-providers.update</c> 的请求
/// </summary>
/// <remarks>
/// 编码不可改；API 密钥留空即保留原密钥；启停走 <c>set-status</c>。并发戳与当前的不同即冲突（框架仓储判定）
/// </remarks>
/// <param name="Id">提供商 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Name">名称</param>
/// <param name="ChatModel">聊天模型</param>
/// <param name="Vendor">厂商（只用于分组展示，如 OpenAI、DeepSeek）</param>
/// <param name="EmbeddingModel">嵌入模型</param>
/// <param name="Endpoint">端点：绝对的 http 或 https 地址，厂商是 OpenAI 时可留空，走它的官方端点；别的厂商必填</param>
/// <param name="ApiKey">API 密钥，留空即不改</param>
/// <param name="MaxOutputTokens">最大输出令牌数</param>
/// <param name="Temperature">采样温度</param>
/// <param name="TimeoutSeconds">单次调用超时（秒）</param>
/// <param name="IsDefault">是否缺省提供商</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record UpdateAiProviderRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required, StringLength(100, MinimumLength = 1)] string ChatModel,
    [Required, StringLength(50, MinimumLength = 1)] string Vendor,
    [StringLength(100)] string? EmbeddingModel = null,
    [StringLength(500)] string? Endpoint = null,
    [StringLength(1000)] string? ApiKey = null,
    [Range(1, int.MaxValue)] int? MaxOutputTokens = null,
    [Range(0d, 2d)] float? Temperature = null,
    [Range(1, 3600)] int? TimeoutSeconds = null,
    bool IsDefault = false,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (value, member) in new[] { (Name, nameof(Name)), (Vendor, nameof(Vendor)), (ChatModel, nameof(ChatModel)) })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return new ValidationResult(AIMessages.FieldBlank.L(), [member]);
            }
        }

        if (!AiProviderService.IsEndpoint(Endpoint))
        {
            yield return new ValidationResult(AIMessages.ProviderEndpointInvalid.L(), [nameof(Endpoint)]);
        }

        if (AiProviderService.MissesEndpoint(Vendor, Endpoint))
        {
            yield return new ValidationResult(AIMessages.ProviderEndpointRequired.L(), [nameof(Endpoint)]);
        }
    }
}

/// <summary>
/// <c>ai-providers.set-status</c> 的请求
/// </summary>
/// <param name="Id">提供商 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetAiProviderStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    AiStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>ai-providers.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">提供商 ID，1 到 100 个</param>
public sealed record AiProviderIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
