// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// 命令名的规整与常驻命令（4.x TelegramCommandGuards 同）：去掉 <c>@机器人名</c>、补斜杠、转小写。
/// </summary>
public static class TelegramCommands
{
    /// <summary>
    /// 不受群组白名单限制的常驻命令（4.x 同）。
    /// </summary>
    public static IReadOnlySet<string> AlwaysAvailable { get; } = new HashSet<string>(["/start", "/myid", "/id", "/help", "/h"], StringComparer.Ordinal);

    /// <summary>
    /// 规整一个命令 token；不是命令即 <see langword="null"/>。
    /// </summary>
    /// <param name="token">token（可带 <c>@bot</c>，可不带斜杠）。</param>
    /// <returns>规整后的命令。</returns>
    public static string? Normalize(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var value = token.Trim();
        var at = value.IndexOf('@', StringComparison.Ordinal);
        if (at >= 0)
        {
            value = value[..at];
        }

        if (value.Length == 0)
        {
            return null;
        }

        return (value[0] == '/' ? value : "/" + value).ToLowerInvariant();
    }

    /// <summary>
    /// token 里 <c>@</c> 后面的机器人名（没有为 <see langword="null"/>）。
    /// </summary>
    /// <param name="token">token。</param>
    /// <returns>机器人名。</returns>
    public static string? AddressedBot(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var at = token.IndexOf('@', StringComparison.Ordinal);
        return at >= 0 && at < token.Length - 1 ? token[(at + 1)..].Trim() : null;
    }

    /// <summary>
    /// 一条命令是不是常驻命令。
    /// </summary>
    /// <param name="text">消息文本。</param>
    /// <returns>是否。</returns>
    public static bool IsAlwaysAvailable(string? text) => Normalize(FirstToken(text)) is { } command && AlwaysAvailable.Contains(command);

    /// <summary>
    /// 第一个 token（按空格切）。
    /// </summary>
    /// <param name="text">文本。</param>
    /// <returns>token。</returns>
    public static string? FirstToken(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];

    /// <summary>
    /// 命令后面按空格切的参数。
    /// </summary>
    /// <param name="text">文本。</param>
    /// <returns>参数。</returns>
    public static IReadOnlyList<string> Arguments(string? text)
        => string.IsNullOrWhiteSpace(text) ? [] : text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
}

/// <summary>
/// 一条已登记的命令路由。
/// </summary>
/// <param name="Command">登记（主命令已规整）。</param>
/// <param name="Handler">处理器。</param>
/// <param name="Aliases">规整后的别名。</param>
/// <param name="Pattern">正则。</param>
public sealed record TelegramCommandRoute(TelegramCommand Command, ITelegramCommandHandler Handler, IReadOnlyList<string> Aliases, Regex? Pattern)
{
    /// <summary>
    /// 主命令或任一别名在白名单里（白名单为空即放行，4.x 同）。
    /// </summary>
    /// <param name="allowed">白名单（规整后）。</param>
    /// <returns>是否放行。</returns>
    public bool IsAllowed(IReadOnlyList<string> allowed)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        return allowed.Count == 0 || allowed.Contains(Command.Name, StringComparer.Ordinal) || Aliases.Any(a => allowed.Contains(a, StringComparer.Ordinal));
    }
}

/// <summary>
/// 一次处理用的处理器目录（从作用域里的处理器建，登记有误即抛出：命令或别名重复、action 重复、正则写错，4.x 同在构建时报错）。
/// </summary>
public sealed class TelegramCatalog
{
    private readonly Dictionary<string, TelegramCommandRoute> _commands = new(StringComparer.Ordinal);
    private readonly List<TelegramCommandRoute> _routes = [];
    private readonly Dictionary<string, ITelegramCallbackHandler> _callbacks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 建目录。
    /// </summary>
    /// <param name="commands">命令处理器。</param>
    /// <param name="callbacks">回调处理器。</param>
    /// <param name="inlineQueries">内联查询处理器。</param>
    /// <param name="startPayloads">深链处理器。</param>
    /// <param name="replies">回复处理器。</param>
    /// <param name="messages">普通消息处理器。</param>
    /// <param name="states">会话状态处理器。</param>
    public TelegramCatalog(
        IEnumerable<ITelegramCommandHandler> commands,
        IEnumerable<ITelegramCallbackHandler> callbacks,
        IEnumerable<ITelegramInlineQueryHandler> inlineQueries,
        IEnumerable<ITelegramStartPayloadHandler> startPayloads,
        IEnumerable<ITelegramReplyHandler> replies,
        IEnumerable<ITelegramMessageHandler> messages,
        IEnumerable<ITelegramStateHandler> states)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(callbacks);
        var seen = new HashSet<Type>();
        foreach (var handler in commands)
        {
            // 同一个处理器登记两次只算一次（4.x 同）
            if (!seen.Add(handler.GetType()))
            {
                continue;
            }

            var command = handler.Command;
            var name = TelegramCommands.Normalize(command.Name) ?? throw new InvalidOperationException($"Telegram command handler {handler.GetType().Name} has no command name.");
            var aliases = (command.Aliases ?? []).Select(TelegramCommands.Normalize).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
            var pattern = string.IsNullOrWhiteSpace(command.Pattern) ? null : new Regex(command.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            var route = new TelegramCommandRoute(command with { Name = name }, handler, aliases, pattern);
            foreach (var key in aliases.Prepend(name))
            {
                if (!_commands.TryAdd(key, route))
                {
                    throw new InvalidOperationException($"Telegram command {key} is registered twice.");
                }
            }

            _routes.Add(route);
        }

        foreach (var handler in callbacks)
        {
            var action = handler.Action?.Trim();
            if (string.IsNullOrEmpty(action) || !_callbacks.TryAdd(action, handler) && _callbacks[action].GetType() != handler.GetType())
            {
                throw new InvalidOperationException($"Telegram callback action '{action}' is empty or registered twice.");
            }
        }

        InlineQueries = [.. inlineQueries.OrderBy(static h => h.Order)];
        StartPayloads = [.. startPayloads.OrderBy(static h => h.Order)];
        Replies = [.. replies.OrderBy(static h => h.Order)];
        Messages = [.. messages.OrderBy(static h => h.Order)];
        States = [.. states.OrderBy(static h => h.Order)];
    }

    /// <summary>
    /// 命令路由（按登记顺序）。
    /// </summary>
    public IReadOnlyList<TelegramCommandRoute> Routes => _routes;

    /// <summary>
    /// 内联查询处理器（按顺序）。
    /// </summary>
    public IReadOnlyList<ITelegramInlineQueryHandler> InlineQueries { get; }

    /// <summary>
    /// 深链处理器（按顺序）。
    /// </summary>
    public IReadOnlyList<ITelegramStartPayloadHandler> StartPayloads { get; }

    /// <summary>
    /// 回复处理器（按顺序）。
    /// </summary>
    public IReadOnlyList<ITelegramReplyHandler> Replies { get; }

    /// <summary>
    /// 普通消息处理器（按顺序）。
    /// </summary>
    public IReadOnlyList<ITelegramMessageHandler> Messages { get; }

    /// <summary>
    /// 会话状态处理器（按顺序）。
    /// </summary>
    public IReadOnlyList<ITelegramStateHandler> States { get; }

    /// <summary>
    /// 按主命令或别名找路由。
    /// </summary>
    /// <param name="command">规整后的命令。</param>
    /// <returns>路由。</returns>
    public TelegramCommandRoute? Find(string command) => _commands.GetValueOrDefault(command);

    /// <summary>
    /// 用整段文本依次匹配正则路由：参数取非空的捕获组，没有捕获组即匹配到的文本。
    /// </summary>
    /// <param name="text">文本。</param>
    /// <returns>路由与参数。</returns>
    public (TelegramCommandRoute Route, IReadOnlyList<string> Arguments)? Match(string text)
    {
        foreach (var route in _routes.Where(static r => r.Pattern is not null))
        {
            Match match;
            try
            {
                match = route.Pattern!.Match(text);
            }
            catch (RegexMatchTimeoutException)
            {
                continue;
            }

            if (!match.Success)
            {
                continue;
            }

            var groups = match.Groups.Values.Skip(1).Where(static g => g.Success && g.Value.Length > 0).Select(static g => g.Value).ToList();
            return (route, groups.Count > 0 ? groups : [match.Value]);
        }

        return null;
    }

    /// <summary>
    /// 按 action 找回调处理器。
    /// </summary>
    /// <param name="action">action。</param>
    /// <returns>处理器。</returns>
    public ITelegramCallbackHandler? Callback(string action) => _callbacks.GetValueOrDefault(action);

    /// <summary>
    /// <c>/help</c> 的列表：按命令白名单过滤、非管理员不列只给管理员的、按登记顺序、不列别名（4.x 同）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="isAdmin">是否管理员。</param>
    /// <returns>各行。</returns>
    public IReadOnlyList<string> HelpLines(TelegramBot bot, bool isAdmin)
    {
        ArgumentNullException.ThrowIfNull(bot);
        return _routes
            .Where(r => r.IsAllowed(bot.AllowedCommands) && (isAdmin || !r.Command.AdminOnly))
            .Select(static r => string.IsNullOrWhiteSpace(r.Command.Description) ? r.Command.Name : $"{r.Command.Name} - {r.Command.Description}")
            .ToList();
    }

    /// <summary>
    /// 命令菜单（<c>setMyCommands</c>）：不含只给管理员的、按白名单过滤；命令名去掉斜杠，说明为空用命令名、不足 3 个字补成「说明 / 命令」（4.x 同）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="preferAlias">群组菜单用第一个别名（4.x 同）。</param>
    /// <returns>菜单。</returns>
    public JsonArray Menu(TelegramBot bot, bool preferAlias)
    {
        ArgumentNullException.ThrowIfNull(bot);
        var menu = new JsonArray();
        foreach (var route in _routes.Where(r => !r.Command.AdminOnly && r.IsAllowed(bot.AllowedCommands)))
        {
            var command = preferAlias && route.Aliases.Count > 0 ? route.Aliases[0] : route.Command.Name;
            var name = command.TrimStart('/');
            var description = string.IsNullOrWhiteSpace(route.Command.Description) ? name : route.Command.Description.Trim();
            if (description.Length < 3)
            {
                description = string.Create(CultureInfo.InvariantCulture, $"{description} / {command}");
            }

            menu.Add(new JsonObject { ["command"] = name, ["description"] = description });
        }

        return menu;
    }
}

/// <summary>
/// 内置 <c>/start</c>：欢迎语（没有用户名用机器人名称，忽略深链参数，4.x 同）。
/// </summary>
/// <param name="options">选项。</param>
public sealed class StartCommandHandler(TelegramOptions options) : ITelegramCommandHandler
{
    /// <inheritdoc />
    public TelegramCommand Command { get; } = new("/start", options.Texts.StartDescription);

    /// <inheritdoc />
    public async Task HandleAsync(TelegramContext context, IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        var name = string.IsNullOrWhiteSpace(context.Bot.Username) ? context.Bot.Name : context.Bot.Username;
        await context.ReplyAsync(options.Texts.StartReply.Replace("{botUsername}", name, StringComparison.Ordinal)).ConfigureAwait(false);
    }
}

/// <summary>
/// 内置 <c>/help</c>（别名 <c>/h</c>）：「可用命令：」加每行一条；列表为空即不发（4.x 同）。
/// </summary>
/// <param name="options">选项。</param>
public sealed class HelpCommandHandler(TelegramOptions options) : ITelegramCommandHandler
{
    /// <inheritdoc />
    public TelegramCommand Command { get; } = new("/help", options.Texts.HelpDescription, ["/h"]);

    /// <inheritdoc />
    public async Task HandleAsync(TelegramContext context, IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        var lines = context.Catalog.HelpLines(context.Bot, context.IsAdmin);
        if (lines.Count == 0)
        {
            return;
        }

        await context.ReplyAsync(string.Join(Environment.NewLine, lines.Prepend(options.Texts.HelpTitle))).ConfigureAwait(false);
    }
}

/// <summary>
/// 内置 <c>/myid</c>（别名 <c>/id</c>）：会话与用户 ID（不带千分位，4.x 同）。
/// </summary>
/// <param name="options">选项。</param>
public sealed class MyIdCommandHandler(TelegramOptions options) : ITelegramCommandHandler
{
    /// <inheritdoc />
    public TelegramCommand Command { get; } = new("/myid", options.Texts.MyIdDescription, ["/id"]);

    /// <inheritdoc />
    public async Task HandleAsync(TelegramContext context, IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        var text = options.Texts.MyIdReply
            .Replace("{chatId}", context.Update.ChatId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{userId}", context.Update.UserId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        await context.ReplyAsync(text).ConfigureAwait(false);
    }
}
