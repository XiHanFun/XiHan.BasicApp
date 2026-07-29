// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.AI.Abstractions.Chat;
using XiHan.Framework.AI.Abstractions.Prompts;
using XiHan.Framework.AI.Abstractions.Rag;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.AI.Application.AppServices;

/// <summary>
/// 聊天助手应用服务（会话内与 AI 助手对话：检索增强 → 流式生成 → 落库）
/// </summary>
/// <remarks>
/// 回复是前端在发出用户消息后单独发起的一次调用，生成期间增量经 SignalR 推给本人，
/// 生成结束才落库为一条 <see cref="ChatMessageType.Assistant"/> 消息并按普通消息推送。
/// 助手输出与用户输入走同一套敏感词守卫。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "聊天助手")]
public sealed class ChatAssistantAppService : AiApplicationService, IChatAssistantAppService
{
    /// <summary>
    /// 未配置提示词时的内置系统提示词
    /// </summary>
    private const string DefaultSystemPrompt =
        "你是企业知识助手。回答要准确、简洁，使用 Markdown。" +
        "涉及知识库内容时仅依据给出的知识片段作答并标注片段编号；片段不足以回答时明确说明未找到依据，不要编造。";

    private readonly IAiAssistantRepository _assistantRepository;
    private readonly IChatDomainService _chatDomainService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatConversationMemberRepository _memberRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatRealtimePushService _pushService;
    private readonly IChatSensitiveWordGuard _sensitiveWordGuard;
    private readonly IKnowledgeRetriever _retriever;
    private readonly IRagPromptAugmenter _augmenter;
    private readonly IAiPromptStore _promptStore;
    private readonly IXiHanAiService _aiService;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<ChatAssistantAppService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatAssistantAppService(
        IAiAssistantRepository assistantRepository,
        IChatDomainService chatDomainService,
        IChatConversationRepository conversationRepository,
        IChatConversationMemberRepository memberRepository,
        IChatMessageRepository messageRepository,
        IChatRealtimePushService pushService,
        IChatSensitiveWordGuard sensitiveWordGuard,
        IKnowledgeRetriever retriever,
        IRagPromptAugmenter augmenter,
        IAiPromptStore promptStore,
        IXiHanAiService aiService,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ILogger<ChatAssistantAppService> logger)
    {
        _assistantRepository = assistantRepository;
        _chatDomainService = chatDomainService;
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _pushService = pushService;
        _sensitiveWordGuard = sensitiveWordGuard;
        _retriever = retriever;
        _augmenter = augmenter;
        _promptStore = promptStore;
        _aiService = aiService;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [HttpPost]
    public async Task<ChatAssistantConversationDto> OpenConversationAsync(ChatAssistantOpenDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var assistant = await GetEnabledAssistantOrThrowAsync(input.AssistantId, cancellationToken);

        var result = await _chatDomainService.GetOrCreateAssistantConversationAsync(
            new ChatAssistantConversationCommand(userId, assistant.BasicId, assistant.AssistantName, assistant.Avatar), cancellationToken);

        // 开场白只在新建会话时发一次，避免每次打开都灌一条
        if (result.Created && !string.IsNullOrWhiteSpace(assistant.Greeting))
        {
            var greeting = await _chatDomainService.AppendAssistantMessageAsync(
                new ChatAssistantMessageCommand(result.Conversation.BasicId, assistant.BasicId, assistant.AssistantName, assistant.Greeting), cancellationToken);
            await _pushService.PushMessageAsync(
                ChatApplicationMapper.ToMessageItemDto(greeting.Message), greeting.Conversation, greeting.RecipientUserIds);
        }

        return new ChatAssistantConversationDto
        {
            BasicId = result.Conversation.BasicId,
            AssistantId = assistant.BasicId,
            AssistantName = assistant.AssistantName,
            Avatar = assistant.Avatar,
            Created = result.Created
        };
    }

    /// <inheritdoc />
    [HttpPost]
    public async Task<ChatAssistantReplyResultDto> ReplyAsync(ChatAssistantReplyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ReplyId);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var conversation = await _conversationRepository.GetByIdAsync(input.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException("会话不存在。");
        if (conversation.ConversationType != ChatConversationType.Assistant || conversation.AssistantId is not { } assistantId)
        {
            throw new InvalidOperationException("该会话不是助手会话。");
        }

        _ = await _memberRepository.GetMemberAsync(conversation.BasicId, userId, cancellationToken)
            ?? throw new InvalidOperationException("仅会话成员可请求助手回复。");

        var assistant = await GetEnabledAssistantOrThrowAsync(assistantId, cancellationToken);
        IReadOnlyList<long> recipients = [userId];

        // 历史取自库而不是前端传参：前端可篡改上下文，且多端发起时应看到同一份历史
        var history = await _messageRepository.GetHistoryAsync(conversation.BasicId, null, Math.Max(assistant.HistoryRounds, 1), cancellationToken);
        var question = history.FirstOrDefault(message => message.MessageType == ChatMessageType.Text && !message.IsRecalled)?.Content;
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException("会话中没有可回答的提问。");
        }

        var builder = new StringBuilder();
        try
        {
            var messages = await BuildMessagesAsync(assistant, history, question, cancellationToken);
            var stream = _aiService.ChatStreamAsync(
                messages, new XiHanChatOptions { Provider = assistant.ProviderCode }, cancellationToken);

            await foreach (var update in stream)
            {
                if (string.IsNullOrEmpty(update.Text))
                {
                    continue;
                }

                _ = builder.Append(update.Text);
                await _pushService.PushAssistantDeltaAsync(conversation.BasicId, input.ReplyId, update.Text, recipients);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "助手回复生成失败，ConversationId={ConversationId}", conversation.BasicId);
            await _pushService.PushAssistantCompletedAsync(conversation.BasicId, input.ReplyId, messageId: null, ex.Message, recipients);
            return new ChatAssistantReplyResultDto { Error = ex.Message };
        }

        var answer = builder.ToString().Trim();
        if (answer.Length == 0)
        {
            const string emptyMessage = "助手没有产生任何内容。";
            await _pushService.PushAssistantCompletedAsync(conversation.BasicId, input.ReplyId, messageId: null, emptyMessage, recipients);
            return new ChatAssistantReplyResultDto { Error = emptyMessage };
        }

        // 助手输出与用户输入过同一套敏感词守卫；命中即不落库，按失败回传
        try
        {
            await _sensitiveWordGuard.EnsureAllowedAsync(answer, cancellationToken);
        }
        catch (Exception ex)
        {
            await _pushService.PushAssistantCompletedAsync(conversation.BasicId, input.ReplyId, messageId: null, ex.Message, recipients);
            return new ChatAssistantReplyResultDto { Error = ex.Message };
        }

        var result = await _chatDomainService.AppendAssistantMessageAsync(
            new ChatAssistantMessageCommand(conversation.BasicId, assistant.BasicId, assistant.AssistantName, answer), cancellationToken);
        await _pushService.PushMessageAsync(
            ChatApplicationMapper.ToMessageItemDto(result.Message), result.Conversation, result.RecipientUserIds);
        await _pushService.PushAssistantCompletedAsync(conversation.BasicId, input.ReplyId, result.Message.BasicId, error: null, recipients);

        return new ChatAssistantReplyResultDto { MessageId = result.Message.BasicId };
    }

    /// <summary>
    /// 组装送给模型的消息：系统提示词 + 历史 + （检索增强后的）本轮提问
    /// </summary>
    private async Task<List<ChatMessage>> BuildMessagesAsync(
        SysAiAssistant assistant,
        IReadOnlyList<SysChatMessage> history,
        string question,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage> { new(ChatRole.System, await ResolveSystemPromptAsync(assistant, cancellationToken)) };

        // 历史按库里的倒序取回，这里翻正
        foreach (var message in history.Reverse())
        {
            if (message.IsRecalled || string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            switch (message.MessageType)
            {
                case ChatMessageType.Text:
                    messages.Add(new ChatMessage(ChatRole.User, message.Content));
                    break;
                case ChatMessageType.Assistant:
                    messages.Add(new ChatMessage(ChatRole.Assistant, message.Content));
                    break;
                default:
                    break;
            }
        }

        // 最后一条即本轮提问，检索增强后替换掉
        var lastIndex = messages.FindLastIndex(message => message.Role == ChatRole.User);
        var prompt = assistant.EnableKnowledge
            ? await AugmentAsync(assistant, question, cancellationToken)
            : question;
        if (lastIndex >= 0)
        {
            messages[lastIndex] = new ChatMessage(ChatRole.User, prompt);
        }
        else
        {
            messages.Add(new ChatMessage(ChatRole.User, prompt));
        }

        return messages;
    }

    /// <summary>
    /// 检索知识片段并注入提问（检索失败不阻断回答，退化为纯模型回答）
    /// </summary>
    private async Task<string> AugmentAsync(SysAiAssistant assistant, string question, CancellationToken cancellationToken)
    {
        try
        {
            var filter = new RetrievalFilter { TenantId = _currentTenant.Id ?? 0 };
            var retrieved = await _retriever.RetrieveAsync(question, assistant.KnowledgeTopK, filter, assistant.KnowledgeProviderCode, cancellationToken);
            return retrieved.Count == 0 ? question : _augmenter.Augment(question, retrieved);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "助手知识检索失败，本轮退化为纯模型回答，AssistantId={AssistantId}", assistant.BasicId);
            return question;
        }
    }

    /// <summary>
    /// 解析系统提示词（助手指定了提示词编码则取库中内容，取不到用内置默认）
    /// </summary>
    private async Task<string> ResolveSystemPromptAsync(SysAiAssistant assistant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(assistant.PromptCode))
        {
            return DefaultSystemPrompt;
        }

        var template = await _promptStore.GetAsync(assistant.PromptCode, version: null, cancellationToken);
        return string.IsNullOrWhiteSpace(template?.Content) ? DefaultSystemPrompt : template.Content;
    }

    private async Task<SysAiAssistant> GetEnabledAssistantOrThrowAsync(long assistantId, CancellationToken cancellationToken)
    {
        if (assistantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assistantId), "助手主键必须大于 0。");
        }

        return await _assistantRepository.GetEnabledByIdAsync(assistantId, cancellationToken)
            ?? throw new InvalidOperationException("助手不存在或已禁用。");
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }
}
