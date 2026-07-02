#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITelegramBotDomainService
// Guid:30e1eb15-e6b1-4c6e-a24f-d1cabfaf7136
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
