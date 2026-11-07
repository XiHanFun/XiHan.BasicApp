// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.Contracts.Assistants;
using XiHan.Framework.AI.Abstractions.Chat;
using XiHan.Framework.AI.Abstractions.Knowledge;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Infrastructure;

/// <summary>
/// 聊天的助手应答
/// </summary>
/// <remarks>
/// 系统提示词 + 历史 + 检索增强后的本轮提问，流式生成
/// 没配提示词时用消息目录里按调用方区域取的缺省提示词（方案 D8）；引用的提示词停用了回复失败、不回落；检索失败回复失败、不退化成纯模型回答（方案 D7）；
/// 最大输出与温度取提供商的配置（方案 D2）；工具只取助手勾选的（方案 A3），框架把它们绑到当前调用，以调用方身份执行，没有权限时模型收到拒绝、回复本身不失败
/// </remarks>
/// <param name="assistants">助手仓储</param>
/// <param name="prompts">提示词仓储</param>
/// <param name="service">助手规则</param>
/// <param name="knowledge">知识检索</param>
/// <param name="augmenter">检索增强</param>
/// <param name="chat">聊天</param>
/// <param name="catalog">提供商目录</param>
public sealed class AiChatAssistants(
    IRepository<SysAiAssistant, Guid> assistants,
    IRepository<SysAiPrompt, Guid> prompts,
    AiAssistantService service,
    KnowledgeDocumentService knowledge,
    IRagPromptAugmenter augmenter,
    IAIChat chat,
    AiProviderCatalog catalog) : IChatAssistants
{
    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<ChatAssistantInfo>>> ListAvailableAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = await assistants.ListAsync(EntityQuery.All<SysAiAssistant>().Where(new Specification<SysAiAssistant>(a => a.Status == AiStatus.Active)), context.CancellationToken).ConfigureAwait(false);
        return rows.OrderBy(static a => a.Order).ThenBy(static a => a.Name, StringComparer.Ordinal).Select(InfoOf).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<Result<ChatAssistantInfo>> FindAsync(InvocationContext context, Guid assistantId)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindActiveAsync(assistantId, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var assistant) ? InfoOf(assistant) : found.Error!;
    }

    /// <inheritdoc />
    public async ValueTask<Result<IAsyncEnumerable<string>>> StreamReplyAsync(InvocationContext context, Guid assistantId, IReadOnlyList<ChatAssistantTurn> turns)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(turns);
        var found = await service.FindActiveAsync(assistantId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var assistant))
        {
            return found.Error!;
        }

        var system = await SystemPromptAsync(assistant, context).ConfigureAwait(false);
        if (!system.TryGetValue(out var prompt))
        {
            return system.Error!;
        }

        var messages = new List<ChatMessage> { new(ChatRole.System, prompt) };
        messages.AddRange(turns.Select(static turn => new ChatMessage(turn.Role == ChatAssistantRole.Assistant ? ChatRole.Assistant : ChatRole.User, turn.Text)));
        var augmented = await AugmentAsync(assistant, messages, context).ConfigureAwait(false);
        if (augmented.IsFailure)
        {
            return augmented.Error!;
        }

        var stream = await chat.StreamAsync(
            new AIChatRequest
            {
                Messages = messages,
                Provider = assistant.ProviderCode,
                Tools = AiAssistantService.DeserializeTools(assistant.Tools),
                Options = catalog.ChatOptionsOf(assistant.ProviderCode),
            },
            context.CancellationToken).ConfigureAwait(false);
        return stream.TryGetValue(out var updates) ? Result.Success(TextsOf(updates, context.CancellationToken)) : stream.Error!;
    }

    private static ChatAssistantInfo InfoOf(SysAiAssistant assistant)
        => new(assistant.Id, assistant.Code, assistant.Name, assistant.Avatar, assistant.Description, assistant.Greeting, assistant.IsDefault, assistant.HistoryRounds);

    private static async IAsyncEnumerable<string> TextsOf(IAsyncEnumerable<ChatResponseUpdate> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }

    /// <summary>
    /// 取系统提示词
    /// </summary>
    /// <remarks>
    /// 提示词正文原样使用；没配用缺省的，引用的提示词没了或停用了即失败
    /// </remarks>
    private async Task<Result<string>> SystemPromptAsync(SysAiAssistant assistant, InvocationContext context)
    {
        if (assistant.PromptCode is not { } code)
        {
            return context.Localize(AIMessages.AssistantDefaultSystemPrompt);
        }

        var rows = await prompts.ListAsync(EntityQuery.All<SysAiPrompt>().Where(new Specification<SysAiPrompt>(p => p.Code == code && p.Status == AiStatus.Active)), context.CancellationToken).ConfigureAwait(false);
        return rows.Count > 0
            ? rows[0].Content
            : Error.Unavailable(AIErrors.AssistantPromptDisabled, AIMessages.AssistantPromptDisabled, MessageArguments.Of(("code", code)));
    }

    /// <summary>
    /// 在调用方一侧检索，增强最后一条用户消息
    /// </summary>
    /// <remarks>
    /// 没有命中保留原问题
    /// </remarks>
    private async Task<Result> AugmentAsync(SysAiAssistant assistant, List<ChatMessage> messages, InvocationContext context)
    {
        var last = messages.FindLastIndex(static message => message.Role == ChatRole.User);
        if (!assistant.EnableKnowledge || last < 0)
        {
            return Result.Success();
        }

        var question = messages[last].Text;
        var retrieved = await knowledge.RetrieveAsync(question, assistant.KnowledgeTopK, assistant.KnowledgeProviderCode, context).ConfigureAwait(false);
        if (!retrieved.TryGetValue(out var chunks))
        {
            return retrieved.Error!;
        }

        if (chunks.Count > 0)
        {
            messages[last] = new ChatMessage(ChatRole.User, augmenter.Augment(question, chunks));
        }

        return Result.Success();
    }
}
