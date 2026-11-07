// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.AiPrompts;

/// <summary>
/// 提示词的列表项
/// </summary>
/// <remarks>
/// 不带正文
/// </remarks>
/// <param name="Id">主键</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Category">分类</param>
/// <param name="Version">版本标签</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record AiPromptResponse(
    Guid Id,
    string Code,
    string Name,
    string? Category,
    string? Version,
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
    /// <param name="prompt">提示词</param>
    public static AiPromptResponse From(SysAiPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return new AiPromptResponse(prompt.Id, prompt.Code, prompt.Name, prompt.Category, prompt.Version, prompt.Status, prompt.Order, prompt.Remark, prompt.ConcurrencyStamp, prompt.CreatedAt, prompt.ModifiedAt);
    }
}

/// <summary>
/// 提示词的详情
/// </summary>
/// <param name="Id">主键</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Category">分类</param>
/// <param name="Version">版本标签</param>
/// <param name="Content">正文</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record AiPromptDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string? Category,
    string? Version,
    string Content,
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
    /// <param name="prompt">提示词</param>
    public static AiPromptDetailResponse From(SysAiPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return new AiPromptDetailResponse(prompt.Id, prompt.Code, prompt.Name, prompt.Category, prompt.Version, prompt.Content, prompt.Status, prompt.Order, prompt.Remark, prompt.ConcurrencyStamp, prompt.CreatedAt, prompt.ModifiedAt);
    }
}

/// <summary>
/// <c>ai-prompts.get</c> 的请求
/// </summary>
/// <param name="Id">提示词 ID</param>
public sealed record AiPromptIdRequest(Guid Id);

/// <summary>
/// <c>ai-prompts.create</c> 的请求
/// </summary>
/// <remarks>
/// 编码唯一，建后不可改
/// </remarks>
/// <param name="Code">编码（唯一）</param>
/// <param name="Name">名称</param>
/// <param name="Content">正文</param>
/// <param name="Category">分类</param>
/// <param name="Version">版本标签</param>
/// <param name="Status">初始状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record CreateAiPromptRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required] string Content,
    [StringLength(100)] string? Category = null,
    [StringLength(100)] string? Version = null,
    AiStatus Status = AiStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (value, member) in new[] { (Code, nameof(Code)), (Name, nameof(Name)), (Content, nameof(Content)) })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return new ValidationResult(AIMessages.FieldBlank.L(), [member]);
            }
        }
    }
}

/// <summary>
/// <c>ai-prompts.update</c> 的请求
/// </summary>
/// <remarks>
/// 编码不可改；启停走 <c>set-status</c>。并发戳与当前的不同即冲突（框架仓储判定）
/// </remarks>
/// <param name="Id">提示词 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Name">名称</param>
/// <param name="Content">正文</param>
/// <param name="Category">分类</param>
/// <param name="Version">版本标签</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record UpdateAiPromptRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Required] string Content,
    [StringLength(100)] string? Category = null,
    [StringLength(100)] string? Version = null,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (value, member) in new[] { (Name, nameof(Name)), (Content, nameof(Content)) })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return new ValidationResult(AIMessages.FieldBlank.L(), [member]);
            }
        }
    }
}

/// <summary>
/// <c>ai-prompts.set-status</c> 的请求
/// </summary>
/// <param name="Id">提示词 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetAiPromptStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    AiStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>ai-prompts.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">提示词 ID，1 到 100 个</param>
public sealed record AiPromptIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
