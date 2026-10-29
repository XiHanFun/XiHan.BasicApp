// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Frozen;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// 一个运行中的 Telegram 机器人。
/// </summary>
/// <remarks>
/// 从 <see cref="Domain.Entities.SysTelegramBot"/> 规整而来：令牌已解密、名称与令牌去空格、管理员只取 &gt; 0、群组只取 ≠ 0、命令规整去重，4.x 同。
/// </remarks>
/// <param name="Id">机器人 ID。</param>
/// <param name="Name">名称（Webhook 路由段，不分大小写）。</param>
/// <param name="Token">令牌。</param>
/// <param name="AdminUsers">管理员用户 ID。</param>
/// <param name="AllowedGroupChatIds">群组与频道白名单（空即拒收所有群组与频道）。</param>
/// <param name="AllowedCommands">命令白名单（规整成带斜杠的小写；空即不限）。</param>
/// <param name="EnableFallbackReply">机器人自己的兜底回复开关。</param>
public sealed record TelegramBot(Guid Id, string Name, string Token, IReadOnlyList<long> AdminUsers, IReadOnlyList<long> AllowedGroupChatIds, IReadOnlyList<string> AllowedCommands, bool EnableFallbackReply)
{
    /// <summary>
    /// <c>getMe</c> 拿到的机器人用户 ID（启动前为 0）。
    /// </summary>
    public long BotUserId { get; init; }

    /// <summary>
    /// <c>getMe</c> 拿到的用户名（不带 @）。
    /// </summary>
    /// <remarks>
    /// 启动前为空。
    /// </remarks>
    public string? Username { get; init; }

    /// <summary>
    /// 是否管理员。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <returns>是否。</returns>
    public bool IsAdmin(long userId) => userId > 0 && AdminUsers.Contains(userId);

    /// <summary>
    /// 群组或频道是否允许。
    /// </summary>
    /// <remarks>
    /// fail-closed：白名单为空、chat ID 为 0 一律拒收，4.x 同。
    /// </remarks>
    /// <param name="chatId">会话 ID。</param>
    /// <returns>是否。</returns>
    public bool IsGroupAllowed(long chatId) => chatId != 0 && AllowedGroupChatIds.Count > 0 && AllowedGroupChatIds.Contains(chatId);

    /// <summary>
    /// 配置是否与另一份一致。
    /// </summary>
    /// <remarks>
    /// 比较名称、令牌、ID、管理员、群组、命令、兜底；4.x 同一组字段，变了即重启这个机器人。
    /// </remarks>
    /// <param name="other">另一份。</param>
    /// <returns>是否一致。</returns>
    public bool SameConfigAs(TelegramBot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Id == other.Id
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(Token, other.Token, StringComparison.Ordinal)
            && EnableFallbackReply == other.EnableFallbackReply
            && AdminUsers.Order().SequenceEqual(other.AdminUsers.Order())
            && AllowedGroupChatIds.Order().SequenceEqual(other.AllowedGroupChatIds.Order())
            && AllowedCommands.Order(StringComparer.Ordinal).SequenceEqual(other.AllowedCommands.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }
}

/// <summary>
/// 当前生效的 Telegram 设置与运行中的机器人（单例）。
/// </summary>
/// <remarks>
/// 由 <see cref="TelegramBotManager"/> 更新，Webhook 端点与出站读。
/// </remarks>
public sealed class TelegramRuntime
{
    private volatile Snapshot _current = new(TelegramSettings.Default, FrozenDictionary<string, TelegramBot>.Empty, false);

    /// <summary>
    /// 当前设置。
    /// </summary>
    public TelegramSettings Settings => _current.Settings;

    /// <summary>
    /// 是否在运行（总开关开、已加载过一次）。
    /// </summary>
    public bool IsRunning => _current.Running;

    /// <summary>
    /// 运行中的机器人。
    /// </summary>
    public IReadOnlyCollection<TelegramBot> Bots => _current.Bots.Values;

    /// <summary>
    /// 按名称找运行中的机器人。
    /// </summary>
    /// <remarks>
    /// 不分大小写、先去空格，4.x 同。
    /// </remarks>
    /// <param name="name">名称。</param>
    /// <returns>机器人；没有为 <see langword="null"/>。</returns>
    public TelegramBot? Find(string? name)
        => string.IsNullOrWhiteSpace(name) ? null : _current.Bots.GetValueOrDefault(name.Trim());

    /// <summary>
    /// 换上新的设置与机器人。
    /// </summary>
    /// <param name="settings">设置。</param>
    /// <param name="bots">机器人。</param>
    /// <param name="running">是否在运行。</param>
    public void Publish(TelegramSettings settings, IEnumerable<TelegramBot> bots, bool running)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(bots);
        _current = new Snapshot(settings, bots.ToFrozenDictionary(static b => b.Name, StringComparer.OrdinalIgnoreCase), running);
    }

    /// <summary>
    /// 换掉一个机器人（<c>getMe</c> 之后回填用户名）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    public void Replace(TelegramBot bot)
    {
        ArgumentNullException.ThrowIfNull(bot);
        var current = _current;
        if (!current.Bots.ContainsKey(bot.Name))
        {
            return;
        }

        var bots = current.Bots.Values.Select(b => string.Equals(b.Name, bot.Name, StringComparison.OrdinalIgnoreCase) ? bot : b);
        _current = current with { Bots = bots.ToFrozenDictionary(static b => b.Name, StringComparer.OrdinalIgnoreCase) };
    }

    private sealed record Snapshot(TelegramSettings Settings, FrozenDictionary<string, TelegramBot> Bots, bool Running);
}
