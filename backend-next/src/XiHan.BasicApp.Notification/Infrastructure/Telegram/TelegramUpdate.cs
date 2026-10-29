// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// Update 的种类（Webhook 与长轮询都只订这几种，4.x 轮询同一份列表）。
/// </summary>
public enum TelegramUpdateKind
{
    /// <summary>
    /// 其他（不处理）。
    /// </summary>
    Other = 0,

    /// <summary>
    /// 新消息。
    /// </summary>
    Message = 1,

    /// <summary>
    /// 编辑过的消息。
    /// </summary>
    EditedMessage = 2,

    /// <summary>
    /// 频道贴文。
    /// </summary>
    ChannelPost = 3,

    /// <summary>
    /// 编辑过的频道贴文。
    /// </summary>
    EditedChannelPost = 4,

    /// <summary>
    /// 回调（内联键盘按钮）。
    /// </summary>
    CallbackQuery = 5,

    /// <summary>
    /// 内联查询。
    /// </summary>
    InlineQuery = 6,
}

/// <summary>
/// 一条 Update 里路由要用的字段（从原始 JSON 读，不引 SDK）。
/// </summary>
public sealed record TelegramUpdate
{
    /// <summary>
    /// 订阅的种类（<c>allowed_updates</c>）。
    /// </summary>
    public static IReadOnlyList<string> AllowedUpdates { get; } = ["message", "edited_message", "channel_post", "edited_channel_post", "callback_query", "inline_query"];

    /// <summary>
    /// Update ID。
    /// </summary>
    public long UpdateId { get; init; }

    /// <summary>
    /// 种类。
    /// </summary>
    public TelegramUpdateKind Kind { get; init; }

    /// <summary>
    /// 会话 ID（没有为 0）。
    /// </summary>
    public long ChatId { get; init; }

    /// <summary>
    /// 会话类型（<c>private</c>、<c>group</c>、<c>supergroup</c>、<c>channel</c>）。
    /// </summary>
    public string? ChatType { get; init; }

    /// <summary>
    /// 发送人 ID（频道贴文为 0）。
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 发送人的语言（缺省 <c>zh-CN</c>，4.x 同）。
    /// </summary>
    public string LanguageCode { get; init; } = "zh-CN";

    /// <summary>
    /// 消息 ID（回调为按钮所在消息的 ID）。
    /// </summary>
    public long MessageId { get; init; }

    /// <summary>
    /// 文本（或图片等的说明文字）。
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// 是否回复了别的消息。
    /// </summary>
    public bool IsReply { get; init; }

    /// <summary>
    /// 被回复的消息 ID。
    /// </summary>
    public long? ReplyToMessageId { get; init; }

    /// <summary>
    /// 回调 ID。
    /// </summary>
    public string? CallbackQueryId { get; init; }

    /// <summary>
    /// 回调数据。
    /// </summary>
    public string? CallbackData { get; init; }

    /// <summary>
    /// 内联查询 ID。
    /// </summary>
    public string? InlineQueryId { get; init; }

    /// <summary>
    /// 内联查询文本（已去空格）。
    /// </summary>
    public string InlineQuery { get; init; } = string.Empty;

    /// <summary>
    /// 有消息（新消息、编辑过的消息与频道贴文）。
    /// </summary>
    public bool HasMessage => Kind is TelegramUpdateKind.Message or TelegramUpdateKind.EditedMessage or TelegramUpdateKind.ChannelPost or TelegramUpdateKind.EditedChannelPost;

    /// <summary>
    /// 是群组或频道（白名单管这些）。
    /// </summary>
    public bool IsGroupOrChannel => ChatType is "group" or "supergroup" or "channel";

    /// <summary>
    /// 文本以斜杠开头（命令）。
    /// </summary>
    public bool IsCommand => HasMessage && Text is { Length: > 1 } text && text[0] == '/';

    /// <summary>
    /// 解析；不是对象或没有 <c>update_id</c> 即 <see langword="null"/>。
    /// </summary>
    /// <param name="root">原始 JSON。</param>
    /// <returns>Update。</returns>
    public static TelegramUpdate? Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("update_id", out var idElement) || !idElement.TryGetInt64(out var updateId))
        {
            return null;
        }

        foreach (var (name, kind) in new[] { ("message", TelegramUpdateKind.Message), ("edited_message", TelegramUpdateKind.EditedMessage), ("channel_post", TelegramUpdateKind.ChannelPost), ("edited_channel_post", TelegramUpdateKind.EditedChannelPost) })
        {
            if (root.TryGetProperty(name, out var message) && message.ValueKind == JsonValueKind.Object)
            {
                return FromMessage(updateId, kind, message);
            }
        }

        if (root.TryGetProperty("callback_query", out var callback) && callback.ValueKind == JsonValueKind.Object)
        {
            var from = Object(callback, "from");
            var message = Object(callback, "message");
            var chat = message is { } m ? Object(m, "chat") : null;
            return new TelegramUpdate
            {
                UpdateId = updateId,
                Kind = TelegramUpdateKind.CallbackQuery,
                CallbackQueryId = String(callback, "id"),
                CallbackData = String(callback, "data"),
                UserId = from is { } f ? Int64(f, "id") : 0,
                LanguageCode = from is { } lf ? String(lf, "language_code") ?? "zh-CN" : "zh-CN",
                ChatId = chat is { } c ? Int64(c, "id") : 0,
                ChatType = chat is { } ct ? String(ct, "type") : null,
                MessageId = message is { } mm ? Int64(mm, "message_id") : 0,
            };
        }

        if (root.TryGetProperty("inline_query", out var inline) && inline.ValueKind == JsonValueKind.Object)
        {
            var from = Object(inline, "from");
            return new TelegramUpdate
            {
                UpdateId = updateId,
                Kind = TelegramUpdateKind.InlineQuery,
                InlineQueryId = String(inline, "id"),
                InlineQuery = String(inline, "query")?.Trim() ?? string.Empty,
                UserId = from is { } f ? Int64(f, "id") : 0,
                LanguageCode = from is { } lf ? String(lf, "language_code") ?? "zh-CN" : "zh-CN",
            };
        }

        return new TelegramUpdate { UpdateId = updateId, Kind = TelegramUpdateKind.Other };
    }

    private static TelegramUpdate FromMessage(long updateId, TelegramUpdateKind kind, JsonElement message)
    {
        var chat = Object(message, "chat");
        var from = Object(message, "from");
        var reply = Object(message, "reply_to_message");
        return new TelegramUpdate
        {
            UpdateId = updateId,
            Kind = kind,
            ChatId = chat is { } c ? Int64(c, "id") : 0,
            ChatType = chat is { } ct ? String(ct, "type") : null,
            UserId = from is { } f ? Int64(f, "id") : 0,
            LanguageCode = from is { } lf ? String(lf, "language_code") ?? "zh-CN" : "zh-CN",
            MessageId = Int64(message, "message_id"),
            Text = String(message, "text") ?? String(message, "caption"),
            IsReply = reply is not null,
            ReplyToMessageId = reply is { } r ? Int64(r, "message_id") : null,
        };
    }

    private static JsonElement? Object(JsonElement parent, string name) => parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static string? String(JsonElement parent, string name) => parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static long Int64(JsonElement parent, string name) => parent.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : 0;
}
