// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Operations.ChatConversations;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Assistants;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Domain.Services;

/// <summary>
/// 聊天里的助手
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>ChatAssistantAppService</c>：回复是前端发出用户消息后单独发起的调用，生成期间增量推给本人，结束才落库为一条助手消息并按普通消息推送；助手输出与用户输入过同一套敏感词
/// 与 4.x 不同：AI 关掉时没有 <see cref="IChatAssistants"/> 的实现，助手操作明确返回 <c>CHAT_UNAVAILABLE_001</c>；失败以 Problem Details 结束，同时立即推 <c>ChatAssistantCompleted</c> 带错误（方案 D13），不返回伪成功
/// 写库都在生成结束之后：生成期间这次调用没有开事务，增量才能立即推（方案 D10）
/// </remarks>
/// <param name="assistants">助手应答（AI 实现）</param>
/// <param name="store">按侧读写聊天表</param>
/// <param name="conversations">会话服务</param>
/// <param name="messages">消息服务</param>
/// <param name="policy">聊天策略（敏感词）</param>
/// <param name="push">推送</param>
/// <param name="logger">日志</param>
public sealed partial class ChatAssistantService(
    IEnumerable<IChatAssistants> assistants,
    ChatStore store,
    ChatConversationService conversations,
    ChatMessageService messages,
    ChatPolicy policy,
    ChatPush push,
    ILogger<ChatAssistantService> logger)
{
    private readonly IChatAssistants? _assistants = assistants.FirstOrDefault();

    /// <summary>
    /// 可用的助手
    /// </summary>
    public async Task<Result<ChatAssistantsResponse>> ListAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_assistants is null)
        {
            return Unavailable();
        }

        var listed = await _assistants.ListAvailableAsync(context).ConfigureAwait(false);
        return listed.TryGetValue(out var items)
            ? new ChatAssistantsResponse(items.Select(static item => new ChatAssistantItem(item.Id, item.Code, item.Name, item.Avatar, item.Description, item.IsDefault)).ToList())
            : listed.Error!;
    }

    /// <summary>
    /// 打开与助手的会话，没有即建
    /// </summary>
    /// <remarks>
    /// 新建时发开场白（照 4.x）
    /// </remarks>
    public async Task<Result<ChatAssistantOpenResponse>> OpenAsync(InvocationContext context, ChatCaller caller, Guid assistantId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(caller);
        if (_assistants is null)
        {
            return Unavailable();
        }

        var found = await _assistants.FindAsync(context, assistantId).ConfigureAwait(false);
        if (!found.TryGetValue(out var assistant))
        {
            return found.Error!;
        }

        var opened = await conversations.OpenAssistantAsync(context, caller, assistant).ConfigureAwait(false);
        if (!opened.TryGetValue(out var conversation))
        {
            return opened.Error!;
        }

        if (conversation.Created && !string.IsNullOrWhiteSpace(assistant.Greeting))
        {
            var greeting = await messages.AppendAssistantAsync(context, caller.Side, conversation.Conversation.Id, assistant.Id, assistant.Name, assistant.Greeting).ConfigureAwait(false);
            if (!greeting.TryGetValue(out var appended))
            {
                return greeting.Error!;
            }

            await push.MessageAsync(caller.Side, ChatStore.ItemOf(appended.Message), appended.Conversation, [caller.UserId], context.CancellationToken).ConfigureAwait(false);
        }

        return new ChatAssistantOpenResponse(conversation.Conversation.Id, assistant.Id, assistant.Name, assistant.Avatar, conversation.Created);
    }

    /// <summary>
    /// 回复会话里最近一条用户提问
    /// </summary>
    /// <remarks>
    /// 上下文取自库而不是前端（前端可以篡改，多端发起时也该看到同一份）
    /// </remarks>
    public async Task<Result<ReplyAssistantResponse>> ReplyAsync(InvocationContext context, ChatCaller caller, Guid conversationId, string replyId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(caller);
        if (_assistants is null)
        {
            return Unavailable();
        }

        var found = await store.ConversationAsync(caller.Side, conversationId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var conversation))
        {
            return found.Error!;
        }

        if (conversation.Type != ChatConversationType.Assistant || conversation.AssistantId is not { } assistantId)
        {
            return Error.Validation(ChatErrors.NotAssistantConversation, ChatMessages.NotAssistantConversation);
        }

        var member = await store.RequireMemberAsync(caller.Side, conversationId, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            return member.Error!;
        }

        var assistantFound = await _assistants.FindAsync(context, assistantId).ConfigureAwait(false);
        if (!assistantFound.TryGetValue(out var assistant))
        {
            return await FailAsync(context, caller, conversationId, replyId, assistantFound.Error!).ConfigureAwait(false);
        }

        var turns = await TurnsAsync(context, caller.Side, conversationId, assistant.HistoryRounds).ConfigureAwait(false);
        if (!turns.Any(static turn => turn.Role == ChatAssistantRole.User))
        {
            return await FailAsync(context, caller, conversationId, replyId, Error.Validation(ChatErrors.NoQuestion, ChatMessages.NoQuestion)).ConfigureAwait(false);
        }

        var generated = await GenerateAsync(context, caller, conversationId, replyId, assistantId, turns).ConfigureAwait(false);
        if (!generated.TryGetValue(out var answer))
        {
            return await FailAsync(context, caller, conversationId, replyId, generated.Error!).ConfigureAwait(false);
        }

        var allowed = await policy.EnsureAllowedAsync(context, answer).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return await FailAsync(context, caller, conversationId, replyId, allowed.Error!).ConfigureAwait(false);
        }

        var appended = await messages.AppendAssistantAsync(context, caller.Side, conversationId, assistantId, assistant.Name, answer).ConfigureAwait(false);
        if (!appended.TryGetValue(out var result))
        {
            return appended.Error!;
        }

        await push.MessageAsync(caller.Side, ChatStore.ItemOf(result.Message), result.Conversation, [caller.UserId], context.CancellationToken).ConfigureAwait(false);
        await push.AssistantCompletedAsync(caller.Side, conversationId, replyId, result.Message.Id, caller.UserId, context.CancellationToken).ConfigureAwait(false);
        return new ReplyAssistantResponse(result.Message.Id);
    }

    private static Error Unavailable() => Error.Unavailable(ChatErrors.AssistantUnavailable, ChatMessages.AssistantUnavailable);

    /// <summary>
    /// 库里最近的 <c>max(历史条数, 1)</c> 条未撤回的提问与回复，按时间从早到晚
    /// </summary>
    private async Task<List<ChatAssistantTurn>> TurnsAsync(InvocationContext context, string side, Guid conversationId, int historyRounds)
    {
        var rows = await store.Messages(side)
            .Where(m => m.ConversationId == conversationId && !m.IsRecalled && (m.Type == ChatMessageType.Text || m.Type == ChatMessageType.Assistant))
            .OrderBy(m => m.Seq, OrderByType.Desc)
            .Take(Math.Max(historyRounds, 1))
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return rows
            .Where(static m => !string.IsNullOrWhiteSpace(m.Content))
            .Reverse()
            .Select(static m => new ChatAssistantTurn(m.Type == ChatMessageType.Assistant ? ChatAssistantRole.Assistant : ChatAssistantRole.User, m.Content!))
            .ToList();
    }

    /// <summary>
    /// 流式生成并逐段推给本人，返回全文
    /// </summary>
    /// <remarks>
    /// 模型中途出错由枚举抛出（流已经交出去了，不能再返回结果），这里接住按失败处理；调用方取消照常抛出
    /// </remarks>
    private async Task<Result<string>> GenerateAsync(InvocationContext context, ChatCaller caller, Guid conversationId, string replyId, Guid assistantId, IReadOnlyList<ChatAssistantTurn> turns)
    {
        var stream = await _assistants!.StreamReplyAsync(context, assistantId, turns).ConfigureAwait(false);
        if (!stream.TryGetValue(out var updates))
        {
            return stream.Error!;
        }

        var builder = new StringBuilder();
        try
        {
            await foreach (var delta in updates.WithCancellation(context.CancellationToken).ConfigureAwait(false))
            {
                builder.Append(delta);
                await push.AssistantDeltaAsync(caller.Side, conversationId, replyId, delta, caller.UserId, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogReplyFailed(logger, exception, conversationId);
            return Error.Unavailable(ChatErrors.AssistantReplyFailed, ChatMessages.AssistantReplyFailed, MessageArguments.Of(("reason", exception.Message)));
        }

        var answer = builder.ToString().Trim();
        return answer.Length > 0 ? answer : Error.Unavailable(ChatErrors.AssistantReplyEmpty, ChatMessages.AssistantReplyEmpty);
    }

    /// <summary>
    /// 失败：立即推给本人，再把错误交回调用方
    /// </summary>
    private async Task<Result<ReplyAssistantResponse>> FailAsync(InvocationContext context, ChatCaller caller, Guid conversationId, string replyId, Error error)
    {
        await push.AssistantFailedAsync(caller.Side, conversationId, replyId, error.Code, context.Localize(error), caller.UserId, context.CancellationToken).ConfigureAwait(false);
        return error;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Generating an assistant reply failed in conversation {ConversationId}.")]
    private static partial void LogReplyFailed(ILogger logger, Exception exception, Guid conversationId);
}
