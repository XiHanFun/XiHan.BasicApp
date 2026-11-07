// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.AiAssistants;

/// <summary>
/// 助手
/// </summary>
/// <param name="Id">主键</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Avatar">头像地址</param>
/// <param name="Description">简介</param>
/// <param name="Greeting">开场白</param>
/// <param name="PromptCode">提示词的编码，为空即内置的系统提示词</param>
/// <param name="ProviderCode">聊天提供商的编码，为空即缺省提供商</param>
/// <param name="EnableKnowledge">是否检索知识库</param>
/// <param name="KnowledgeProviderCode">嵌入提供商的编码，为空即缺省提供商</param>
/// <param name="KnowledgeTopK">检索的片段数</param>
/// <param name="HistoryRounds">带入上下文的历史消息条数</param>
/// <param name="Tools">可用的工具名</param>
/// <param name="IsDefault">是否缺省助手</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record AiAssistantResponse(
    Guid Id,
    string Code,
    string Name,
    string? Avatar,
    string? Description,
    string? Greeting,
    string? PromptCode,
    string? ProviderCode,
    bool EnableKnowledge,
    string? KnowledgeProviderCode,
    int KnowledgeTopK,
    int HistoryRounds,
    IReadOnlyList<string> Tools,
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
    /// <param name="assistant">助手</param>
    public static AiAssistantResponse From(SysAiAssistant assistant)
    {
        ArgumentNullException.ThrowIfNull(assistant);
        return new AiAssistantResponse(assistant.Id, assistant.Code, assistant.Name, assistant.Avatar, assistant.Description, assistant.Greeting, assistant.PromptCode, assistant.ProviderCode, assistant.EnableKnowledge, assistant.KnowledgeProviderCode, assistant.KnowledgeTopK, assistant.HistoryRounds, AiAssistantService.DeserializeTools(assistant.Tools), assistant.IsDefault, assistant.Status, assistant.Order, assistant.Remark, assistant.ConcurrencyStamp, assistant.CreatedAt, assistant.ModifiedAt);
    }
}

/// <summary>
/// 按 ID 的请求（详情、设为缺省）
/// </summary>
/// <param name="Id">助手 ID</param>
public sealed record AiAssistantIdRequest(Guid Id);

/// <summary>
/// <c>ai-assistants.create</c> 的请求
/// </summary>
/// <remarks>
/// 编码唯一、不含空白，建后不可改（照 4.x）；引用的提示词与提供商须存在且启用，工具须在目录里
/// </remarks>
/// <param name="Code">编码（唯一）</param>
/// <param name="Name">名称</param>
/// <param name="Avatar">头像地址</param>
/// <param name="Description">简介</param>
/// <param name="Greeting">开场白</param>
/// <param name="PromptCode">提示词的编码</param>
/// <param name="ProviderCode">聊天提供商的编码</param>
/// <param name="EnableKnowledge">是否检索知识库</param>
/// <param name="KnowledgeProviderCode">嵌入提供商的编码</param>
/// <param name="KnowledgeTopK">检索的片段数</param>
/// <param name="HistoryRounds">带入上下文的历史消息条数</param>
/// <param name="Tools">可用的工具名</param>
/// <param name="IsDefault">是否缺省助手</param>
/// <param name="Status">初始状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record CreateAiAssistantRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(500)] string? Avatar = null,
    [StringLength(500)] string? Description = null,
    [StringLength(1000)] string? Greeting = null,
    [StringLength(100)] string? PromptCode = null,
    [StringLength(64)] string? ProviderCode = null,
    bool EnableKnowledge = true,
    [StringLength(64)] string? KnowledgeProviderCode = null,
    [Range(1, 20)] int KnowledgeTopK = 5,
    [Range(0, 50)] int HistoryRounds = 10,
    [MaxLength(100)] IReadOnlyList<string>? Tools = null,
    bool IsDefault = false,
    AiStatus Status = AiStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Code) || Code.Trim().Any(char.IsWhiteSpace))
        {
            yield return new ValidationResult(AIMessages.AssistantCodeInvalid.L(), [nameof(Code)]);
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(AIMessages.FieldBlank.L(), [nameof(Name)]);
        }
    }
}

/// <summary>
/// <c>ai-assistants.update</c> 的请求
/// </summary>
/// <remarks>
/// 编码不可改；启停走 <c>set-status</c>。并发戳与当前的不同即冲突（框架仓储判定）
/// </remarks>
/// <param name="Id">助手 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Name">名称</param>
/// <param name="Avatar">头像地址</param>
/// <param name="Description">简介</param>
/// <param name="Greeting">开场白</param>
/// <param name="PromptCode">提示词的编码</param>
/// <param name="ProviderCode">聊天提供商的编码</param>
/// <param name="EnableKnowledge">是否检索知识库</param>
/// <param name="KnowledgeProviderCode">嵌入提供商的编码</param>
/// <param name="KnowledgeTopK">检索的片段数</param>
/// <param name="HistoryRounds">带入上下文的历史消息条数</param>
/// <param name="Tools">可用的工具名</param>
/// <param name="IsDefault">是否缺省助手</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record UpdateAiAssistantRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(500)] string? Avatar = null,
    [StringLength(500)] string? Description = null,
    [StringLength(1000)] string? Greeting = null,
    [StringLength(100)] string? PromptCode = null,
    [StringLength(64)] string? ProviderCode = null,
    bool EnableKnowledge = true,
    [StringLength(64)] string? KnowledgeProviderCode = null,
    [Range(1, 20)] int KnowledgeTopK = 5,
    [Range(0, 50)] int HistoryRounds = 10,
    [MaxLength(100)] IReadOnlyList<string>? Tools = null,
    bool IsDefault = false,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(AIMessages.FieldBlank.L(), [nameof(Name)]);
        }
    }
}

/// <summary>
/// <c>ai-assistants.set-status</c> 的请求
/// </summary>
/// <param name="Id">助手 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetAiAssistantStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    AiStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>ai-assistants.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">助手 ID，1 到 100 个</param>
public sealed record AiAssistantIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>ai-assistants.tools</c> 的请求（没有参数）
/// </summary>
public sealed record AiAssistantToolsRequest;

/// <summary>
/// 可勾选的工具
/// </summary>
/// <param name="Name">工具名</param>
/// <param name="Description">说明（调用方的区域）</param>
public sealed record AiAssistantToolItem(string Name, string? Description);

/// <summary>
/// <c>ai-assistants.tools</c> 的响应
/// </summary>
/// <param name="Items">可勾选的工具</param>
/// <param name="KnowledgeAvailable">有没有向量库：没有时开着「检索知识库」的助手回复会失败，表单据此提示</param>
public sealed record AiAssistantToolsResponse(IReadOnlyList<AiAssistantToolItem> Items, bool KnowledgeAvailable);
