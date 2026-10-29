// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// 一条 Update 的处理（4.x TelegramUpdateDispatcher 同一个顺序）：群组白名单（去重之前）→ 去重 → 内联查询 → 会话状态 → 回调 → <c>/start</c> 深链 → 命令 → 回复 → 普通消息 → 兜底。
/// 处理在一次平台调用里进行（处理器能读写业务数据）；被取消即撤销去重标记，其他异常回一条出错提示（不撤销标记）。
/// 与 4.x 不同：回调 action 没登记也应答（4.x 不应答，客户端一直转圈）；<c>/cmd@别的机器人</c> 不处理（4.x 不校验）。
/// </summary>
/// <param name="scopes">作用域工厂。</param>
/// <param name="app">内核（开平台调用）。</param>
/// <param name="runtime">当前设置。</param>
/// <param name="notifier">出站。</param>
/// <param name="store">共享缓存的存储（去重）。</param>
/// <param name="options">选项。</param>
/// <param name="logger">日志。</param>
public sealed partial class TelegramDispatcher(IServiceScopeFactory scopes, App app, TelegramRuntime runtime, TelegramNotifier notifier, ICacheStore store, TelegramOptions options, ILogger<TelegramDispatcher> logger)
{
    private static readonly byte[] _marker = "1"u8.ToArray();

    /// <summary>
    /// 处理一条 Update。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="update">Update。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>处理。</returns>
    public async Task DispatchAsync(TelegramBot bot, TelegramUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(update);

        // 1. 群组与频道白名单（fail-closed，常驻命令例外；私聊不管）
        if (update.IsGroupOrChannel && !bot.IsGroupAllowed(update.ChatId) && !(update.IsCommand && TelegramCommands.IsAlwaysAvailable(update.Text)))
        {
            LogGroupRejected(logger, bot.Name, update.ChatId);
            return;
        }

        // 2. 去重
        var key = new CacheKey($"notification:telegram:dedup:{bot.Name.ToLowerInvariant()}:{update.UpdateId}");
        if (!await store.TryAddAsync(key, _marker, CacheEntryOptions.Expires(options.DedupLifetime), cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var failed = false;
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var context = App.CreateContext(scope.ServiceProvider, cancellationToken);
            var outcome = await app.InvokeAsync(context, async invocation =>
            {
                try
                {
                    await RunAsync(bot, update, invocation).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogHandlerFailed(logger, exception, bot.Name, update.UpdateId);
                    failed = true;
                }

                invocation.SetOutcome(Result.Success());
            }).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                LogInvocationFailed(logger, bot.Name, update.UpdateId, outcome.Error!.Code);
                failed = true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await store.RemoveAsync(key, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        if (failed && update.HasMessage && update.ChatId != 0 && !string.IsNullOrEmpty(options.Texts.InternalErrorReply))
        {
            var quoted = await notifier.SendTextAsync(bot, update.ChatId, options.Texts.InternalErrorReply, update.MessageId == 0 ? null : update.MessageId, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!quoted.Ok)
            {
                await notifier.SendTextAsync(bot, update.ChatId, options.Texts.InternalErrorReply, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RunAsync(TelegramBot bot, TelegramUpdate update, InvocationContext invocation)
    {
        var services = invocation.Services;
        var catalog = new TelegramCatalog(
            services.GetServices<ITelegramCommandHandler>(),
            services.GetServices<ITelegramCallbackHandler>(),
            services.GetServices<ITelegramInlineQueryHandler>(),
            services.GetServices<ITelegramStartPayloadHandler>(),
            services.GetServices<ITelegramReplyHandler>(),
            services.GetServices<ITelegramMessageHandler>(),
            services.GetServices<ITelegramStateHandler>());
        var context = new TelegramContext(bot, update, invocation, notifier, services.GetRequiredService<TelegramConversationStore>(), catalog);

        // 3. 内联查询（排在最前；异常吞掉）
        if (update.Kind == TelegramUpdateKind.InlineQuery)
        {
            await InlineQueryAsync(context).ConfigureAwait(false);
            return;
        }

        // 4. 兜底开关：机器人或平台任一开启
        var fallback = bot.EnableFallbackReply || runtime.Settings.EnableFallbackReply;

        // 5. 会话状态：有状态处理器、不是回调也不是命令时；没匹配上即继续往下、不清状态
        if (catalog.States.Count > 0 && update.HasMessage && !update.IsCommand)
        {
            var state = await context.GetStateAsync().ConfigureAwait(false);
            if (state?.Step is { Length: > 0 } step && catalog.States.FirstOrDefault(h => h.CanHandle(step)) is { } stateHandler)
            {
                await stateHandler.HandleAsync(context, state).ConfigureAwait(false);
                return;
            }
        }

        // 6. 回调
        if (update.Kind == TelegramUpdateKind.CallbackQuery)
        {
            await CallbackAsync(context).ConfigureAwait(false);
            return;
        }

        if (!update.HasMessage)
        {
            return;
        }

        var token = TelegramCommands.FirstToken(update.Text);
        var command = update.IsCommand ? TelegramCommands.Normalize(token) : null;
        if (command is not null && TelegramCommands.AddressedBot(token!) is { } addressed && !string.IsNullOrEmpty(bot.Username) && !string.Equals(addressed, bot.Username, StringComparison.OrdinalIgnoreCase))
        {
            // 发给群里别的机器人的命令
            return;
        }

        var arguments = TelegramCommands.Arguments(update.Text);

        // 7. /start 深链（在白名单与管理员检查之前，4.x 同）
        if (command == "/start" && arguments.Count > 0 && arguments[0].Length > 0)
        {
            foreach (var handler in catalog.StartPayloads)
            {
                try
                {
                    if (await handler.HandleAsync(context, arguments[0]).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogHandlerFailed(logger, exception, bot.Name, update.UpdateId);
                }
            }
        }

        // 8. 命令：先按主命令与别名找，找不到再用整段文本匹配正则
        (TelegramCommandRoute Route, IReadOnlyList<string> Arguments)? route = command is not null && catalog.Find(command) is { } exact
            ? (exact, arguments)
            : update.Text is { Length: > 0 } text ? catalog.Match(text) : null;
        if (route is { } found)
        {
            if (!found.Route.IsAllowed(bot.AllowedCommands))
            {
                await context.ReplyAsync(options.Texts.CommandDisabledReply).ConfigureAwait(false);
                return;
            }

            if (found.Route.Command.AdminOnly && !context.IsAdmin)
            {
                await context.ReplyAsync(options.Texts.CommandAdminOnlyReply).ConfigureAwait(false);
                return;
            }

            await found.Route.Handler.HandleAsync(context, found.Arguments).ConfigureAwait(false);
            return;
        }

        // 9. 回复；10. 普通消息
        if (update.IsReply && await FirstAsync(catalog.Replies, h => h.HandleAsync(context)).ConfigureAwait(false))
        {
            return;
        }

        if (await FirstAsync(catalog.Messages, h => h.HandleAsync(context)).ConfigureAwait(false))
        {
            return;
        }

        // 11. 兜底
        if (fallback && update.ChatId != 0)
        {
            await context.ReplyAsync(options.Texts.FallbackReply).ConfigureAwait(false);
        }
    }

    private static async Task<bool> FirstAsync<T>(IReadOnlyList<T> handlers, Func<T, Task<bool>> handle)
    {
        foreach (var handler in handlers)
        {
            if (await handle(handler).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task InlineQueryAsync(TelegramContext context)
    {
        var update = context.Update;
        if (update.InlineQueryId is null || context.Catalog.InlineQueries.FirstOrDefault(h => h.CanHandle(update.InlineQuery)) is not { } handler)
        {
            return;
        }

        try
        {
            var results = await handler.HandleAsync(context, update.InlineQuery).ConfigureAwait(false);
            await notifier.AnswerInlineQueryAsync(context.Bot, update.InlineQueryId, results, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogHandlerFailed(logger, exception, context.Bot.Name, update.UpdateId);
        }
    }

    private async Task CallbackAsync(TelegramContext context)
    {
        var update = context.Update;
        var data = update.CallbackData ?? string.Empty;
        var separator = data.IndexOf(':', StringComparison.Ordinal);
        var action = (separator >= 0 ? data[..separator] : data).Trim();
        var handler = action.Length == 0 ? null : context.Catalog.Callback(action);
        try
        {
            if (handler is null)
            {
                return;
            }

            if (handler.AdminOnly && !context.IsAdmin)
            {
                // 以消息回提示，回调应答不带文字（4.x 同）
                await context.ReplyAsync(options.Texts.CallbackAdminOnlyReply).ConfigureAwait(false);
                return;
            }

            await handler.HandleAsync(context, data).ConfigureAwait(false);
        }
        finally
        {
            if (!context.CallbackAnswered && update.CallbackQueryId is { } id)
            {
                await notifier.AnswerCallbackQueryAsync(context.Bot, id, context.CallbackAnswerText, context.CallbackAnswerShowAlert, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Telegram bot {Bot} ignored chat {ChatId}: not in the group allow-list.")]
    private static partial void LogGroupRejected(ILogger logger, string bot, long chatId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A Telegram handler of bot {Bot} failed on update {UpdateId}.")]
    private static partial void LogHandlerFailed(ILogger logger, Exception exception, string bot, long updateId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling update {UpdateId} of Telegram bot {Bot} failed: {Code}.")]
    private static partial void LogInvocationFailed(ILogger logger, string bot, long updateId, string code);
}
