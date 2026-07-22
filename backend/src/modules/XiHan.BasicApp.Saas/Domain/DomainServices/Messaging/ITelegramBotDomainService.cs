// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// Telegram 机器人领域服务
/// </summary>
public interface ITelegramBotDomainService
{
    /// <summary>
    /// 创建 Telegram 机器人
    /// </summary>
    Task<TelegramBotCommandResult> CreateTelegramBotAsync(TelegramBotCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 Telegram 机器人
    /// </summary>
    Task<TelegramBotCommandResult> UpdateTelegramBotAsync(TelegramBotUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 Telegram 机器人启用状态
    /// </summary>
    Task<TelegramBotCommandResult> UpdateTelegramBotStatusAsync(TelegramBotStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除 Telegram 机器人
    /// </summary>
    Task<TelegramBotCommandResult> DeleteTelegramBotAsync(long id, CancellationToken cancellationToken = default);
}
