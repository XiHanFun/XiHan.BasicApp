// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// Telegram 的出站（4.x TelegramNotifier 同一套）：发消息、发图片 / 文件、改消息与改按钮会重试并审计——429、5xx 与网络错误按退避重试
/// （429 等 <c>retry_after</c>，超过 <see cref="TelegramRetryOptions.MaxRetryAfterSeconds"/> 即按失败处理），成功或最终失败各记一条 <see cref="SysTelegramMessage"/>，
/// 最终失败再私发每个管理员一条告警（告警本身不再告警）；回调应答、内联查询应答与管理调用（getMe、setWebhook 等）直接调用，不重试不审计。
/// </summary>
/// <param name="api">Bot API。</param>
/// <param name="runtime">当前设置（网络）。</param>
/// <param name="scopes">作用域工厂（写审计）。</param>
/// <param name="options">选项。</param>
/// <param name="clock">时钟。</param>
/// <param name="logger">日志。</param>
public sealed partial class TelegramNotifier(TelegramApi api, TelegramRuntime runtime, IServiceScopeFactory scopes, TelegramOptions options, TimeProvider clock, ILogger<TelegramNotifier> logger)
{
    /// <summary>
    /// 发一条文本消息。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="text">正文。</param>
    /// <param name="replyToMessageId">要引用的消息。</param>
    /// <param name="parseMode">解析模式（<c>none</c> / <c>markdown</c> / <c>markdownv2</c> / <c>html</c>，认不出按纯文本）。</param>
    /// <param name="replyMarkup">按钮（Bot API 的 <c>reply_markup</c>）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> SendTextAsync(TelegramBot bot, long chatId, string text, long? replyToMessageId = null, string? parseMode = null, JsonObject? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var payload = new JsonObject { ["chat_id"] = chatId, ["text"] = text };
        return SendAsync(bot, chatId, "sendMessage", "text", text, parseMode, payload, replyToMessageId, replyMarkup, alert: true, cancellationToken);
    }

    /// <summary>
    /// 发一张图片（URL 或 Telegram 的 file_id）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="photo">图片地址或 file_id。</param>
    /// <param name="caption">说明。</param>
    /// <param name="replyToMessageId">要引用的消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> SendPhotoAsync(TelegramBot bot, long chatId, string photo, string? caption = null, long? replyToMessageId = null, CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject { ["chat_id"] = chatId, ["photo"] = photo };
        if (caption is not null)
        {
            payload["caption"] = caption;
        }

        return SendAsync(bot, chatId, "sendPhoto", "photo", caption, null, payload, replyToMessageId, null, alert: true, cancellationToken);
    }

    /// <summary>
    /// 发一个文件（URL 或 Telegram 的 file_id）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="document">文件地址或 file_id。</param>
    /// <param name="caption">说明（审计里没有说明时记文件）。</param>
    /// <param name="replyToMessageId">要引用的消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> SendDocumentAsync(TelegramBot bot, long chatId, string document, string? caption = null, long? replyToMessageId = null, CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject { ["chat_id"] = chatId, ["document"] = document };
        if (caption is not null)
        {
            payload["caption"] = caption;
        }

        return SendAsync(bot, chatId, "sendDocument", "document", caption ?? document, null, payload, replyToMessageId, null, alert: true, cancellationToken);
    }

    /// <summary>
    /// 改一条消息的文字。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="messageId">消息。</param>
    /// <param name="text">新正文。</param>
    /// <param name="parseMode">解析模式。</param>
    /// <param name="replyMarkup">按钮。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> EditMessageTextAsync(TelegramBot bot, long chatId, long messageId, string text, string? parseMode = null, JsonObject? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject { ["chat_id"] = chatId, ["message_id"] = messageId, ["text"] = text };
        return SendAsync(bot, chatId, "editMessageText", "text", text, parseMode, payload, null, replyMarkup, alert: true, cancellationToken);
    }

    /// <summary>
    /// 改一条消息的按钮。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="messageId">消息。</param>
    /// <param name="replyMarkup">新按钮（为空即去掉）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> EditMessageReplyMarkupAsync(TelegramBot bot, long chatId, long messageId, JsonObject? replyMarkup, CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject { ["chat_id"] = chatId, ["message_id"] = messageId };
        return SendAsync(bot, chatId, "editMessageReplyMarkup", "reply_markup", null, null, payload, null, replyMarkup, alert: true, cancellationToken);
    }

    /// <summary>
    /// 应答回调（不重试、不审计）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="callbackQueryId">回调 ID。</param>
    /// <param name="text">提示文字（为空即只停止转圈）。</param>
    /// <param name="showAlert">是否弹窗。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> AnswerCallbackQueryAsync(TelegramBot bot, string callbackQueryId, string? text = null, bool showAlert = false, CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject { ["callback_query_id"] = callbackQueryId };
        if (!string.IsNullOrEmpty(text))
        {
            payload["text"] = text;
            payload["show_alert"] = showAlert;
        }

        return CallAsync(bot, "answerCallbackQuery", payload, cancellationToken);
    }

    /// <summary>
    /// 应答内联查询（缓存 300 秒，4.x 同；不重试、不审计）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="inlineQueryId">内联查询 ID。</param>
    /// <param name="results">结果（Bot API 的 InlineQueryResult 数组）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> AnswerInlineQueryAsync(TelegramBot bot, string inlineQueryId, JsonArray results, CancellationToken cancellationToken = default)
        => CallAsync(bot, "answerInlineQuery", new JsonObject { ["inline_query_id"] = inlineQueryId, ["results"] = results, ["cache_time"] = 300 }, cancellationToken);

    /// <summary>
    /// 直接调一个方法（管理调用；不重试、不审计）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="method">方法。</param>
    /// <param name="payload">载荷。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<TelegramApiResult> CallAsync(TelegramBot bot, string method, JsonObject payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        return api.CallAsync(runtime.Settings.Network, bot.Token, method, payload, cancellationToken);
    }

    /// <summary>
    /// 解析模式的规整：认得的返回 Bot API 的写法，其余为 <see langword="null"/>（纯文本）。
    /// </summary>
    /// <param name="parseMode">原值。</param>
    /// <returns>Bot API 的解析模式。</returns>
    public static string? ParseModeOf(string? parseMode) => parseMode?.Trim().ToLowerInvariant() switch
    {
        "markdown" => "Markdown",
        "markdownv2" => "MarkdownV2",
        "html" => "HTML",
        _ => null,
    };

    private async Task<TelegramApiResult> SendAsync(TelegramBot bot, long chatId, string method, string messageType, string? content, string? parseMode, JsonObject payload, long? replyToMessageId, JsonObject? replyMarkup, bool alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bot);
        var mode = ParseModeOf(parseMode);
        if (mode is not null)
        {
            payload["parse_mode"] = mode;
        }

        if (replyToMessageId is { } replyTo)
        {
            payload["reply_parameters"] = new JsonObject { ["message_id"] = replyTo, ["allow_sending_without_reply"] = true };
        }

        if (replyMarkup is not null)
        {
            payload["reply_markup"] = replyMarkup;
        }

        var watch = Stopwatch.StartNew();
        var retry = options.Retry;
        TelegramApiResult result;
        var attempt = 0;
        while (true)
        {
            attempt++;
            result = await api.CallAsync(runtime.Settings.Network, bot.Token, method, payload, cancellationToken).ConfigureAwait(false);
            if (result.Ok || !result.Transient || attempt > retry.MaxRetries)
            {
                break;
            }

            var delay = result.ErrorCode == 429 && result.RetryAfter is { } after
                ? TimeSpan.FromSeconds(after)
                : TimeSpan.FromMilliseconds(Math.Min(retry.MaxDelayMs, retry.BaseDelayMs * Math.Pow(2, attempt - 1)));
            if (delay > TimeSpan.FromSeconds(retry.MaxRetryAfterSeconds))
            {
                break;
            }

            await Task.Delay(delay, clock, cancellationToken).ConfigureAwait(false);
        }

        watch.Stop();
        await AuditAsync(bot, chatId, method, messageType, content, mode ?? "None", result, watch.ElapsedMilliseconds, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            LogSendFailed(logger, bot.Name, method, chatId, result.ErrorCode, result.Description);
            if (alert && retry.NotifyAdminOnFinalFailure)
            {
                await AlertAdminsAsync(bot, chatId, method, result, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private async Task AlertAdminsAsync(TelegramBot bot, long chatId, string method, TelegramApiResult failure, CancellationToken cancellationToken)
    {
        var text = string.Create(CultureInfo.InvariantCulture, $"{options.Texts.SendFailureAdminNotifyTitle}\nBot: {bot.Name}\nChatId: {chatId}\nApi: {method}\nError: {failure.Description}");
        foreach (var admin in bot.AdminUsers.Where(a => a != chatId))
        {
            var payload = new JsonObject { ["chat_id"] = admin, ["text"] = text };
            await SendAsync(bot, admin, "sendMessage", "text", text, null, payload, null, null, alert: false, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task AuditAsync(TelegramBot bot, long chatId, string method, string messageType, string? content, string parseMode, TelegramApiResult result, long elapsed, CancellationToken cancellationToken)
    {
        try
        {
            var now = clock.GetUtcNow();
            await StoreScope.WriteAsync(scopes, async (services, ct) =>
            {
                var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
                await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
                var row = new SysTelegramMessage
                {
                    Id = services.GetRequiredService<IIdGenerator<Guid>>().NewId(),
                    BotName = bot.Name,
                    BotId = bot.Id,
                    ChatId = chatId,
                    ApiMethod = method,
                    MessageType = messageType,
                    Content = content,
                    ParseMode = parseMode,
                    TelegramMessageId = result.MessageId,
                    Success = result.Ok,
                    ErrorCode = result.Ok ? null : result.ErrorCode,
                    ErrorMessage = result.Ok ? null : Clip(result.Description, 1000),
                    ElapsedMs = elapsed,
                    SentAt = now,
                    CreatedAt = now,
                };
                await unitOfWork.ConnectionFor(typeof(SysTelegramMessage)).Insertable(row).ExecuteCommandAsync(ct).ConfigureAwait(false);
                return Result.Success();
            }, CancellationToken.None).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // 审计写不进去不影响发送结果（4.x 同吞掉）
        catch (Exception exception)
#pragma warning restore CA1031
        {
            LogAuditFailed(logger, exception, bot.Name, method);
        }
    }

    private static string? Clip(string? value, int max) => value is null || value.Length <= max ? value : value[..max];

    [LoggerMessage(Level = LogLevel.Warning, Message = "Telegram bot {Bot} {Method} to {ChatId} failed: {Code} {Description}")]
    private static partial void LogSendFailed(ILogger logger, string bot, string method, long chatId, int? code, string? description);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Writing the Telegram outbound audit of bot {Bot} {Method} failed.")]
    private static partial void LogAuditFailed(ILogger logger, Exception exception, string bot, string method);
}
