// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// Telegram 机器人应用层映射器
/// </summary>
public static class TelegramBotApplicationMapper
{
    /// <summary>
    /// 映射 Telegram 机器人创建命令
    /// </summary>
    public static TelegramBotCreateCommand ToCreateCommand(TelegramBotCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TelegramBotCreateCommand(
            input.BotName,
            input.Token,
            input.AdminUsers,
            input.AllowedGroupChatIds,
            input.AllowedCommands,
            input.EnableFallbackReply,
            input.IsEnabled,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射 Telegram 机器人更新命令
    /// </summary>
    public static TelegramBotUpdateCommand ToUpdateCommand(TelegramBotUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TelegramBotUpdateCommand(
            input.BasicId,
            input.BotName,
            input.Token,
            input.AdminUsers,
            input.AllowedGroupChatIds,
            input.AllowedCommands,
            input.EnableFallbackReply,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射 Telegram 机器人状态命令
    /// </summary>
    public static TelegramBotStatusChangeCommand ToStatusCommand(TelegramBotStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TelegramBotStatusChangeCommand(input.BasicId, input.IsEnabled);
    }

    /// <summary>
    /// 映射 Telegram 机器人列表项（Token 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="bot">Telegram 机器人</param>
    /// <returns>Telegram 机器人列表项 DTO</returns>
    public static TelegramBotListItemDto ToListItemDto(SysTelegramBot bot)
    {
        ArgumentNullException.ThrowIfNull(bot);

        return new TelegramBotListItemDto
        {
            BasicId = bot.BasicId,
            BotName = bot.BotName,
            HasToken = !string.IsNullOrEmpty(bot.Token),
            EnableFallbackReply = bot.EnableFallbackReply,
            IsEnabled = bot.IsEnabled,
            Sort = bot.Sort,
            CreatedTime = bot.CreatedTime,
            ModifiedTime = bot.ModifiedTime
        };
    }

    /// <summary>
    /// 映射 Telegram 机器人详情（Token 脱敏，仅标识是否已配置）
    /// </summary>
    /// <param name="bot">Telegram 机器人</param>
    /// <returns>Telegram 机器人详情 DTO</returns>
    public static TelegramBotDetailDto ToDetailDto(SysTelegramBot bot)
    {
        ArgumentNullException.ThrowIfNull(bot);

        return new TelegramBotDetailDto
        {
            BasicId = bot.BasicId,
            BotName = bot.BotName,
            HasToken = !string.IsNullOrEmpty(bot.Token),
            AdminUsers = bot.AdminUsers,
            AllowedGroupChatIds = bot.AllowedGroupChatIds,
            AllowedCommands = bot.AllowedCommands,
            EnableFallbackReply = bot.EnableFallbackReply,
            IsEnabled = bot.IsEnabled,
            Sort = bot.Sort,
            Remark = bot.Remark,
            CreatedTime = bot.CreatedTime,
            CreatedId = bot.CreatedId,
            CreatedBy = bot.CreatedBy,
            ModifiedTime = bot.ModifiedTime,
            ModifiedId = bot.ModifiedId,
            ModifiedBy = bot.ModifiedBy
        };
    }
}
