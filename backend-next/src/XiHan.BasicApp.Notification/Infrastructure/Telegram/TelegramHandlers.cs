// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// 一条命令的登记。
/// </summary>
/// <remarks>
/// 主命令、别名、菜单说明、是否只给管理员、可选的正则（整段文本匹配，捕获组作参数）。
/// </remarks>
/// <param name="Name">主命令（带不带斜杠都行）。</param>
/// <param name="Description">菜单与 <c>/help</c> 里的说明。</param>
/// <param name="Aliases">别名。</param>
/// <param name="AdminOnly">是否只给管理员。</param>
/// <param name="Pattern">正则（主命令没匹配上时用整段文本匹配）。</param>
public sealed record TelegramCommand(string Name, string? Description = null, IReadOnlyList<string>? Aliases = null, bool AdminOnly = false, string? Pattern = null);

/// <summary>
/// 命令处理器（作用域服务）。
/// </summary>
/// <remarks>
/// 登记重复的命令或别名在启动时报错。
/// </remarks>
public interface ITelegramCommandHandler
{
    /// <summary>
    /// 登记。
    /// </summary>
    TelegramCommand Command { get; }

    /// <summary>
    /// 处理。
    /// </summary>
    /// <param name="context">上下文。</param>
    /// <param name="arguments">参数（命令后按空格切的各段，或正则的捕获组）。</param>
    /// <returns>处理。</returns>
    Task HandleAsync(TelegramContext context, IReadOnlyList<string> arguments);
}

/// <summary>
/// 回调处理器。
/// </summary>
/// <remarks>
/// <c>callback_data</c> 第一个冒号前的部分即 action（不分大小写）。
/// </remarks>
public interface ITelegramCallbackHandler
{
    /// <summary>
    /// action。
    /// </summary>
    string Action { get; }

    /// <summary>
    /// 是否只给管理员。
    /// </summary>
    bool AdminOnly { get; }

    /// <summary>
    /// 处理回调（拿到完整的 data）。
    /// </summary>
    /// <remarks>
    /// 结束后自动应答回调，可用 <see cref="TelegramContext.SetCallbackAnswer"/> 定制。
    /// </remarks>
    /// <param name="context">上下文。</param>
    /// <param name="data">完整的 data。</param>
    /// <returns>处理。</returns>
    Task HandleAsync(TelegramContext context, string data);
}

/// <summary>
/// 内联查询处理器：按 <see cref="Order"/> 取第一个 <see cref="CanHandle"/> 为真的。
/// </summary>
public interface ITelegramInlineQueryHandler
{
    /// <summary>
    /// 顺序（小的先）。
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 能否处理。
    /// </summary>
    /// <param name="query">查询（已去空格）。</param>
    /// <returns>能否。</returns>
    bool CanHandle(string query);

    /// <summary>
    /// 给出结果（Bot API 的 InlineQueryResult 数组）。
    /// </summary>
    /// <param name="context">上下文。</param>
    /// <param name="query">查询。</param>
    /// <returns>结果。</returns>
    Task<JsonArray> HandleAsync(TelegramContext context, string query);
}

/// <summary>
/// 按顺序试的处理器（深链、回复、普通消息、会话状态共用）：返回真即结束。
/// </summary>
public interface ITelegramOrderedHandler
{
    /// <summary>
    /// 顺序（小的先）。
    /// </summary>
    int Order { get; }
}

/// <summary>
/// <c>/start &lt;payload&gt;</c> 深链处理器。
/// </summary>
public interface ITelegramStartPayloadHandler : ITelegramOrderedHandler
{
    /// <summary>
    /// 处理深链。
    /// </summary>
    /// <param name="context">上下文。</param>
    /// <param name="payload">深链参数。</param>
    /// <returns>是否处理了；返回真即结束。</returns>
    Task<bool> HandleAsync(TelegramContext context, string payload);
}

/// <summary>
/// 回复别的消息时的处理器。
/// </summary>
public interface ITelegramReplyHandler : ITelegramOrderedHandler
{
    /// <summary>
    /// 处理这条回复。
    /// </summary>
    /// <param name="context">上下文。</param>
    /// <returns>是否处理了；返回真即结束。</returns>
    Task<bool> HandleAsync(TelegramContext context);
}

/// <summary>
/// 普通消息处理器。
/// </summary>
public interface ITelegramMessageHandler : ITelegramOrderedHandler
{
    /// <summary>
    /// 处理普通消息。
    /// </summary>
    /// <param name="context">上下文。</param>
    /// <returns>是否处理了；返回真即结束。</returns>
    Task<bool> HandleAsync(TelegramContext context);
}

/// <summary>
/// 会话状态处理器。
/// </summary>
/// <remarks>
/// 有进行中的会话（Step 不为空）时按顺序取第一个 <see cref="CanHandle"/> 为真的。
/// </remarks>
public interface ITelegramStateHandler : ITelegramOrderedHandler
{
    /// <summary>
    /// 能否处理这一步。
    /// </summary>
    /// <param name="currentStep">当前步骤。</param>
    /// <returns>能否。</returns>
    bool CanHandle(string currentStep);

    /// <summary>
    /// 处理。
    /// </summary>
    /// <param name="context">上下文。</param>
    /// <param name="state">会话状态。</param>
    /// <returns>处理。</returns>
    Task HandleAsync(TelegramContext context, TelegramConversationState state);
}

/// <summary>
/// 一段会话的状态（步骤与数据）。
/// </summary>
/// <param name="Step">当前步骤。</param>
/// <param name="Data">数据。</param>
public sealed record TelegramConversationState(string? Step, IReadOnlyDictionary<string, string> Data);

/// <summary>
/// 会话状态的存放（共享缓存）。
/// </summary>
/// <remarks>
/// 键为「机器人:会话:用户」，缺省 10 分钟，4.x 同。
/// </remarks>
/// <param name="cache">共享缓存。</param>
public sealed class TelegramConversationStore(ISharedCache cache)
{
    /// <summary>
    /// 缺省保留时长。
    /// </summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 取。
    /// </summary>
    /// <param name="bot">机器人名称。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="userId">用户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>状态；没有为 <see langword="null"/>。</returns>
    public async Task<TelegramConversationState?> GetAsync(string bot, long chatId, long userId, CancellationToken cancellationToken)
    {
        var found = await cache.GetAsync<string>(KeyOf(bot, chatId, userId), cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, NotificationJsonContext.Default.TelegramConversationState);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 存（≤ 0 的时长按缺省）。
    /// </summary>
    /// <param name="bot">机器人名称。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="userId">用户。</param>
    /// <param name="state">状态。</param>
    /// <param name="lifetime">保留时长。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存。</returns>
    public async Task SetAsync(string bot, long chatId, long userId, TelegramConversationState state, TimeSpan? lifetime, CancellationToken cancellationToken)
    {
        var ttl = lifetime is { } l && l > TimeSpan.Zero ? l : DefaultLifetime;
        await cache.SetAsync(KeyOf(bot, chatId, userId), JsonSerializer.Serialize(state, NotificationJsonContext.Default.TelegramConversationState), CacheEntryOptions.Expires(ttl), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 清掉。
    /// </summary>
    /// <param name="bot">机器人名称。</param>
    /// <param name="chatId">会话。</param>
    /// <param name="userId">用户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>清。</returns>
    public async Task ClearAsync(string bot, long chatId, long userId, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(bot, chatId, userId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string bot, long chatId, long userId) => new($"notification:telegram:conversation:{bot.ToLowerInvariant()}:{chatId}:{userId}");
}

/// <summary>
/// 处理一条 Update 时交给处理器的上下文。
/// </summary>
/// <remarks>
/// 机器人、Update、是否管理员、平台调用、回复与会话状态的便捷方法、回调应答的定制。
/// </remarks>
public sealed class TelegramContext
{
    private readonly TelegramNotifier _notifier;
    private readonly TelegramConversationStore _states;

    /// <summary>
    /// 建上下文。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="update">Update。</param>
    /// <param name="invocation">这次处理所在的平台调用。</param>
    /// <param name="notifier">出站。</param>
    /// <param name="states">会话状态。</param>
    /// <param name="catalog">处理器目录。</param>
    public TelegramContext(TelegramBot bot, TelegramUpdate update, InvocationContext invocation, TelegramNotifier notifier, TelegramConversationStore states, TelegramCatalog catalog)
    {
        Bot = bot;
        Update = update;
        Invocation = invocation;
        Catalog = catalog;
        _notifier = notifier;
        _states = states;
    }

    /// <summary>
    /// 机器人。
    /// </summary>
    public TelegramBot Bot { get; }

    /// <summary>
    /// Update。
    /// </summary>
    public TelegramUpdate Update { get; }

    /// <summary>
    /// 这次处理所在的平台调用（处理器要读写业务数据时用）。
    /// </summary>
    public InvocationContext Invocation { get; }

    /// <summary>
    /// 处理器目录（<c>/help</c> 用）。
    /// </summary>
    public TelegramCatalog Catalog { get; }

    /// <summary>
    /// 发送人是否管理员。
    /// </summary>
    public bool IsAdmin => Bot.IsAdmin(Update.UserId);

    /// <summary>
    /// 出站。
    /// </summary>
    public TelegramNotifier Notifier => _notifier;

    /// <summary>
    /// 取消令牌。
    /// </summary>
    public CancellationToken CancellationToken => Invocation.CancellationToken;

    /// <summary>
    /// 回调应答的文字（处理器定制）。
    /// </summary>
    public string? CallbackAnswerText { get; private set; }

    /// <summary>
    /// 回调应答是否弹窗。
    /// </summary>
    public bool CallbackAnswerShowAlert { get; private set; }

    /// <summary>
    /// 处理器自己应答过回调（分发器不再应答）。
    /// </summary>
    public bool CallbackAnswered { get; private set; }

    /// <summary>
    /// 回复当前消息（引用它）。
    /// </summary>
    /// <remarks>
    /// 会话为 0 即不发。
    /// </remarks>
    /// <param name="text">正文。</param>
    /// <param name="parseMode">解析模式。</param>
    /// <param name="replyMarkup">按钮。</param>
    /// <returns>结果；没发为 <see langword="null"/>。</returns>
    public async Task<TelegramApiResult?> ReplyAsync(string text, string? parseMode = null, JsonObject? replyMarkup = null)
        => Update.ChatId == 0 || string.IsNullOrEmpty(text)
            ? null
            : await _notifier.SendTextAsync(Bot, Update.ChatId, text, Update.MessageId == 0 ? null : Update.MessageId, parseMode, replyMarkup, CancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 在当前会话里发一条（不引用）。
    /// </summary>
    /// <param name="text">正文。</param>
    /// <param name="parseMode">解析模式。</param>
    /// <param name="replyMarkup">按钮。</param>
    /// <returns>结果；没发为 <see langword="null"/>。</returns>
    public async Task<TelegramApiResult?> SendAsync(string text, string? parseMode = null, JsonObject? replyMarkup = null)
        => Update.ChatId == 0 || string.IsNullOrEmpty(text)
            ? null
            : await _notifier.SendTextAsync(Bot, Update.ChatId, text, null, parseMode, replyMarkup, CancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 取当前用户在这个会话里的状态。
    /// </summary>
    /// <returns>状态。</returns>
    public Task<TelegramConversationState?> GetStateAsync() => _states.GetAsync(Bot.Name, Update.ChatId, Update.UserId, CancellationToken);

    /// <summary>
    /// 设当前用户在这个会话里的状态。
    /// </summary>
    /// <param name="step">步骤。</param>
    /// <param name="data">数据。</param>
    /// <param name="lifetime">保留时长（缺省 10 分钟）。</param>
    /// <returns>设。</returns>
    public Task SetStateAsync(string step, IReadOnlyDictionary<string, string>? data = null, TimeSpan? lifetime = null)
        => _states.SetAsync(Bot.Name, Update.ChatId, Update.UserId, new TelegramConversationState(step, data ?? new Dictionary<string, string>()), lifetime, CancellationToken);

    /// <summary>
    /// 清掉当前用户在这个会话里的状态。
    /// </summary>
    /// <returns>清。</returns>
    public Task ClearStateAsync() => _states.ClearAsync(Bot.Name, Update.ChatId, Update.UserId, CancellationToken);

    /// <summary>
    /// 定制回调应答。
    /// </summary>
    /// <param name="text">文字。</param>
    /// <param name="showAlert">是否弹窗。</param>
    public void SetCallbackAnswer(string? text, bool showAlert = false)
    {
        CallbackAnswerText = text;
        CallbackAnswerShowAlert = showAlert;
    }

    /// <summary>
    /// 标记处理器自己应答过回调。
    /// </summary>
    public void MarkCallbackAnswered() => CallbackAnswered = true;
}
