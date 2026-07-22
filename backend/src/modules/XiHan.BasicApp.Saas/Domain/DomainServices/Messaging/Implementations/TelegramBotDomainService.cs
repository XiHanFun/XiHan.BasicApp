// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// Telegram 机器人领域服务实现
/// </summary>
public sealed class TelegramBotDomainService
    : ITelegramBotDomainService
{
    private readonly ITelegramBotRepository _telegramBotRepository;

    private readonly ITelegramBotTokenProtector _tokenProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TelegramBotDomainService(
        ITelegramBotRepository telegramBotRepository,
        ITelegramBotTokenProtector tokenProtector)
    {
        _telegramBotRepository = telegramBotRepository;
        _tokenProtector = tokenProtector;
    }

    /// <inheritdoc />
    public async Task<TelegramBotCommandResult> CreateTelegramBotAsync(TelegramBotCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var botName = command.BotName.Trim();
        if (await _telegramBotRepository.AnyAsync(bot => bot.BotName == botName, cancellationToken))
        {
            throw new InvalidOperationException("Telegram 机器人名称已存在。");
        }

        var bot = new SysTelegramBot
        {
            BotName = botName,
            Token = _tokenProtector.Protect(command.Token.Trim())!,
            AdminUsers = NormalizeLongList(command.AdminUsers, "管理员用户Id列表"),
            AllowedGroupChatIds = NormalizeLongList(command.AllowedGroupChatIds, "群组ChatId白名单"),
            AllowedCommands = NormalizeStringList(command.AllowedCommands),
            EnableFallbackReply = command.EnableFallbackReply,
            IsEnabled = command.IsEnabled,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        return new TelegramBotCommandResult(await _telegramBotRepository.AddAsync(bot, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TelegramBotCommandResult> UpdateTelegramBotAsync(TelegramBotUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var bot = await GetTelegramBotOrThrowAsync(command.BasicId, cancellationToken);

        var botName = command.BotName.Trim();
        if (!string.Equals(bot.BotName, botName, StringComparison.Ordinal)
            && await _telegramBotRepository.AnyAsync(item => item.BotName == botName && item.BasicId != command.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("Telegram 机器人名称已存在。");
        }

        bot.BotName = botName;
        bot.AdminUsers = NormalizeLongList(command.AdminUsers, "管理员用户Id列表");
        bot.AllowedGroupChatIds = NormalizeLongList(command.AllowedGroupChatIds, "群组ChatId白名单");
        bot.AllowedCommands = NormalizeStringList(command.AllowedCommands);
        bot.EnableFallbackReply = command.EnableFallbackReply;
        bot.Sort = command.Sort;
        bot.Remark = NormalizeNullable(command.Remark);

        // Token 为空表示保留原 Token（前端脱敏不回显）；提供则加密落库
        var token = NormalizeNullable(command.Token);
        if (token is not null)
        {
            bot.Token = _tokenProtector.Protect(token)!;
        }

        return new TelegramBotCommandResult(await _telegramBotRepository.UpdateAsync(bot, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TelegramBotCommandResult> UpdateTelegramBotStatusAsync(TelegramBotStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "Telegram 机器人主键必须大于 0。");
        var bot = await GetTelegramBotOrThrowAsync(command.BasicId, cancellationToken);

        bot.IsEnabled = command.IsEnabled;
        return new TelegramBotCommandResult(await _telegramBotRepository.UpdateAsync(bot, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<TelegramBotCommandResult> DeleteTelegramBotAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bot = await GetTelegramBotOrThrowAsync(id, cancellationToken);
        if (!await _telegramBotRepository.DeleteAsync(bot, cancellationToken))
        {
            throw new InvalidOperationException("Telegram 机器人删除失败。");
        }

        return new TelegramBotCommandResult(bot);
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 规范化逗号分隔的 long 串（去空项/去重/写入前校验，非法项直接抛错，避免运行期被静默跳过）
    /// </summary>
    private static string? NormalizeLongList(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var items = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (items.Length == 0)
        {
            return null;
        }

        foreach (var item in items)
        {
            if (!long.TryParse(item, out _))
            {
                throw new InvalidOperationException($"{fieldName}包含非法项「{item}」，必须是逗号分隔的数字 Id。");
            }
        }

        return string.Join(',', items);
    }

    /// <summary>
    /// 规范化逗号分隔的字符串列表（去空项/去重）
    /// </summary>
    private static string? NormalizeStringList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var items = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return items.Length == 0 ? null : string.Join(',', items);
    }

    private static void ValidateCommonInput(int sort)
    {
        if (sort < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sort), "排序不能小于 0。");
        }
    }

    private static void ValidateCreateCommand(TelegramBotCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.BotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Token);
        ValidateCommonInput(command.Sort);
    }

    private static void ValidateUpdateCommand(TelegramBotUpdateCommand command)
    {
        EnsureId(command.BasicId, "Telegram 机器人主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.BotName);
        ValidateCommonInput(command.Sort);
    }

    private async Task<SysTelegramBot> GetTelegramBotOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "Telegram 机器人主键必须大于 0。");
        return await _telegramBotRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Telegram 机器人不存在。");
    }
}
