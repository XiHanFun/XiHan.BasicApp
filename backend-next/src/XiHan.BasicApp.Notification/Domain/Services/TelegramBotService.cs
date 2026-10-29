// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.RegularExpressions;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Domain.Services;

/// <summary>
/// Telegram 机器人内容（令牌为空即保留原值；新建时必填）。
/// </summary>
/// <param name="Name">名称。</param>
/// <param name="Token">令牌。</param>
/// <param name="AdminUsers">管理员用户 ID（逗号分隔）。</param>
/// <param name="AllowedGroupChatIds">群组白名单（逗号分隔）。</param>
/// <param name="AllowedCommands">命令白名单（逗号分隔）。</param>
/// <param name="EnableFallbackReply">是否兜底回复。</param>
/// <param name="BroadcastChatIds">广播群（逗号分隔）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record TelegramBotDraft(string Name, string? Token, string? AdminUsers, string? AllowedGroupChatIds, string? AllowedCommands, bool EnableFallbackReply, string? BroadcastChatIds, bool IsEnabled, int Order, string? Remark);

/// <summary>
/// Telegram 机器人的规则（平台托管）：名称唯一；令牌形如 <c>数字:字符串</c>、加密存储；用户与会话 ID 是逗号分隔的整数，命令是逗号分隔的命令名（去掉斜杠、转小写、去重）。
/// </summary>
/// <param name="bots">机器人仓储。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="ids">标识生成器。</param>
public sealed partial class TelegramBotService(IRepository<SysTelegramBot, Guid> bots, ISecretProtector secrets, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 令牌的加密用途。
    /// </summary>
    public const string TokenPurpose = "XiHan.BasicApp.Notification.TelegramToken";

    /// <summary>
    /// 取一个机器人。
    /// </summary>
    /// <param name="id">机器人 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>机器人；不存在即 <c>NOTIFICATION_NOTFOUND_009</c>。</returns>
    public async Task<Result<SysTelegramBot>> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await bots.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var bot)
            ? bot
            : Error.NotFound(NotificationErrors.TelegramBotNotFound, NotificationMessages.TelegramBotNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 广播目标：每个启用的机器人的每个广播群。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>机器人与 chat ID。</returns>
    public async Task<IReadOnlyList<(SysTelegramBot Bot, string ChatId)>> BroadcastTargetsAsync(CancellationToken cancellationToken)
    {
        var rows = await bots.ListAsync(EntityQuery.All<SysTelegramBot>().Where(new Specification<SysTelegramBot>(b => b.IsEnabled && b.BroadcastChatIds != null)), cancellationToken).ConfigureAwait(false);
        return rows.OrderBy(static b => b.Order).ThenBy(static b => b.Name, StringComparer.Ordinal)
            .SelectMany(static b => Split(b.BroadcastChatIds).Select(chat => (b, chat)))
            .ToList();
    }

    /// <summary>
    /// 新建。
    /// </summary>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新机器人。</returns>
    public async Task<Result<SysTelegramBot>> CreateAsync(TelegramBotDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (string.IsNullOrWhiteSpace(draft.Token))
        {
            return TokenInvalid();
        }

        var bot = new SysTelegramBot { Id = ids.NewId() };
        var applied = await ApplyAsync(bot, draft, cancellationToken).ConfigureAwait(false);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        var added = await bots.AddAsync(bot, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : bot;
    }

    /// <summary>
    /// 修改（令牌不填即保留）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateAsync(SysTelegramBot bot, TelegramBotDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(draft);
        var applied = await ApplyAsync(bot, draft, cancellationToken).ConfigureAwait(false);
        return applied.IsFailure ? applied : await bots.UpdateAsync(bot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批量删除。
    /// </summary>
    /// <param name="botIds">机器人 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(IReadOnlyList<Guid> botIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(botIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in botIds.Distinct())
        {
            var found = await FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var bot))
            {
                failed.Add(new BatchFailure(id, NotificationErrors.TelegramBotNotFound));
            }
            else if ((await bots.RemoveAsync(bot, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 发到某个会话用的目标（令牌解密；解不开即 <see langword="null"/>）。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="chatId">chat ID。</param>
    /// <returns>目标。</returns>
    public BotEndpoint? EndpointOf(SysTelegramBot bot, string chatId)
    {
        ArgumentNullException.ThrowIfNull(bot);
        return secrets.TryUnprotect(TokenPurpose, bot.Token, out var token)
            ? new BotEndpoint(BotChannel.Telegram, string.Empty, token, null, chatId, bot.Id, bot.Name)
            : null;
    }

    /// <summary>
    /// 逗号分隔的列表。
    /// </summary>
    /// <param name="value">原文。</param>
    /// <returns>各项。</returns>
    public static IReadOnlyList<string> Split(string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [GeneratedRegex(@"^\d{3,}:[A-Za-z0-9_-]{20,}$", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^[a-z0-9_]{1,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex CommandPattern();

    private static Error TokenInvalid() => Error.Validation(NotificationErrors.TelegramTokenInvalid, NotificationMessages.TelegramTokenInvalid);

    /// <summary>
    /// 整数 ID 列表：规整成「a,b,c」，有一项不是整数即错误。
    /// </summary>
    private static (string? Value, Error? Error) NormalizeIds(string? value, string field)
    {
        var items = Split(value);
        foreach (var item in items)
        {
            if (!long.TryParse(item, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
            {
                return (null, Error.Validation(NotificationErrors.TelegramIdsInvalid, NotificationMessages.TelegramIdsInvalid, MessageArguments.Of(("field", field), ("item", item))));
            }
        }

        return (items.Count == 0 ? null : string.Join(',', items.Distinct(StringComparer.Ordinal)), null);
    }

    /// <summary>
    /// 命令列表：去掉斜杠、转小写、去重，有一项不是命令名即错误。
    /// </summary>
    private static (string? Value, Error? Error) NormalizeCommands(string? value)
    {
        var items = Split(value).Select(static c => c.TrimStart('/').ToLowerInvariant()).ToList();
        foreach (var item in items)
        {
            if (!CommandPattern().IsMatch(item))
            {
                return (null, Error.Validation(NotificationErrors.TelegramCommandsInvalid, NotificationMessages.TelegramCommandsInvalid, MessageArguments.Of(("item", item))));
            }
        }

        return (items.Count == 0 ? null : string.Join(',', items.Distinct(StringComparer.Ordinal)), null);
    }

    private async Task<Result> ApplyAsync(SysTelegramBot bot, TelegramBotDraft draft, CancellationToken cancellationToken)
    {
        var name = draft.Name.Trim();
        var id = bot.Id;
        var taken = await bots.ListAsync(EntityQuery.All<SysTelegramBot>().Where(new Specification<SysTelegramBot>(b => b.Name == name && b.Id != id)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        if (taken.Count > 0)
        {
            return Error.Conflict(NotificationErrors.TelegramBotNameTaken, NotificationMessages.TelegramBotNameTaken, MessageArguments.Of(("name", name)));
        }

        if (!string.IsNullOrWhiteSpace(draft.Token) && !TokenPattern().IsMatch(draft.Token.Trim()))
        {
            return TokenInvalid();
        }

        var admins = NormalizeIds(draft.AdminUsers, "adminUsers");
        var groups = NormalizeIds(draft.AllowedGroupChatIds, "allowedGroupChatIds");
        var broadcast = NormalizeIds(draft.BroadcastChatIds, "broadcastChatIds");
        var commands = NormalizeCommands(draft.AllowedCommands);
        if ((admins.Error ?? groups.Error ?? broadcast.Error ?? commands.Error) is { } invalid)
        {
            return invalid;
        }

        bot.Name = name;
        if (!string.IsNullOrWhiteSpace(draft.Token))
        {
            bot.Token = secrets.Protect(TokenPurpose, draft.Token.Trim());
        }

        bot.AdminUsers = admins.Value;
        bot.AllowedGroupChatIds = groups.Value;
        bot.AllowedCommands = commands.Value;
        bot.BroadcastChatIds = broadcast.Value;
        bot.EnableFallbackReply = draft.EnableFallbackReply;
        bot.IsEnabled = draft.IsEnabled;
        bot.Order = draft.Order;
        bot.Remark = string.IsNullOrWhiteSpace(draft.Remark) ? null : draft.Remark.Trim();
        return Result.Success();
    }
}
