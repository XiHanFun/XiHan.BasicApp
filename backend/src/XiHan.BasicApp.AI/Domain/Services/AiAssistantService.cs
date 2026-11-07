// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.Framework.AI.Abstractions.Tools;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Domain.Services;

/// <summary>
/// 助手的规则
/// </summary>
/// <remarks>
/// 编码唯一、建后不可改，缺省最多一个；保存时核对引用的提示词与提供商存在且启用、工具在目录里，配错当场报，不等到回复时才失败
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="prompts">提示词仓储</param>
/// <param name="catalog">提供商目录</param>
/// <param name="tools">工具目录</param>
public sealed class AiAssistantService(IRepository<SysAiAssistant, Guid> assistants, IRepository<SysAiPrompt, Guid> prompts, AiProviderCatalog catalog, IAIToolCatalog tools)
{
    /// <summary>
    /// 不存在
    /// </summary>
    /// <param name="id">助手 ID</param>
    public static Error NotFound(Guid id)
        => Error.NotFound(AIErrors.AssistantNotFound, AIMessages.AssistantNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 工具名写成存储形式
    /// </summary>
    /// <param name="names">工具名</param>
    /// <returns>JSON 数组；没有工具为 <see langword="null"/></returns>
    public static string? SerializeTools(IReadOnlyList<string> names)
        => names.Count == 0 ? null : JsonSerializer.Serialize(names, AIJsonContext.Default.IReadOnlyListString);

    /// <summary>
    /// 从存储形式读出工具名
    /// </summary>
    /// <param name="json">JSON 数组</param>
    public static IReadOnlyList<string> DeserializeTools(string? json)
        => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize(json, AIJsonContext.Default.IReadOnlyListString) ?? [];

    /// <summary>
    /// 按 ID 取助手
    /// </summary>
    /// <param name="id">助手 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SysAiAssistant>> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await assistants.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var assistant) ? assistant : NotFound(id);

    /// <summary>
    /// 按 ID 取启用中的助手
    /// </summary>
    /// <param name="id">助手 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>助手；不存在或已停用都按不存在处理</returns>
    public async Task<Result<SysAiAssistant>> FindActiveAsync(Guid id, CancellationToken cancellationToken)
        => (await assistants.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var assistant) && assistant.Status == AiStatus.Active ? assistant : NotFound(id);

    /// <summary>
    /// 编码是否已被别的助手占用
    /// </summary>
    /// <param name="code">编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<bool> CodeTakenAsync(string code, CancellationToken cancellationToken)
        => await assistants.CountAsync(EntityQuery.All<SysAiAssistant>().Where(new Specification<SysAiAssistant>(a => a.Code == code)), cancellationToken).ConfigureAwait(false) > 0;

    /// <summary>
    /// 核对引用
    /// </summary>
    /// <param name="promptCode">提示词的编码</param>
    /// <param name="providerCode">聊天提供商的编码</param>
    /// <param name="knowledgeProviderCode">嵌入提供商的编码</param>
    /// <param name="toolNames">工具名</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> CheckReferencesAsync(string? promptCode, string? providerCode, string? knowledgeProviderCode, IReadOnlyList<string> toolNames, CancellationToken cancellationToken)
    {
        foreach (var provider in new[] { providerCode, knowledgeProviderCode })
        {
            if (provider is not null && !catalog.Find(provider).HasValue)
            {
                return Error.Validation(AIErrors.AssistantProviderUnavailable, AIMessages.AssistantProviderUnavailable, MessageArguments.Of(("code", provider)));
            }
        }

        if (promptCode is not null)
        {
            var count = await prompts.CountAsync(EntityQuery.All<SysAiPrompt>().Where(new Specification<SysAiPrompt>(p => p.Code == promptCode && p.Status == AiStatus.Active)), cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                return Error.Validation(AIErrors.AssistantPromptUnavailable, AIMessages.AssistantPromptUnavailable, MessageArguments.Of(("code", promptCode)));
            }
        }

        foreach (var name in toolNames)
        {
            if (!tools.Find(name).HasValue)
            {
                return Error.Validation(AIErrors.AssistantToolUnavailable, AIMessages.AssistantToolUnavailable, MessageArguments.Of(("name", name)));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 取消别的助手的缺省标记
    /// </summary>
    /// <remarks>
    /// 缺省助手最多一个
    /// </remarks>
    /// <param name="except">保留缺省的助手</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result> ClearOtherDefaultsAsync(Guid except, CancellationToken cancellationToken)
    {
        var others = await assistants.ListAsync(EntityQuery.All<SysAiAssistant>().Where(new Specification<SysAiAssistant>(a => a.IsDefault && a.Id != except)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
            var updated = await assistants.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return Result.Success();
    }
}
