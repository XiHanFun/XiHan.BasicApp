#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITelegramBotAppService
// Guid:4e7254fd-2b98-435e-9e83-bf48ff6c3703
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// Telegram 机器人命令应用服务接口
/// </summary>
public interface ITelegramBotAppService : IApplicationService
{
    /// <summary>
    /// 创建 Telegram 机器人
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人详情</returns>
    Task<TelegramBotDetailDto> CreateTelegramBotAsync(TelegramBotCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 Telegram 机器人
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人详情</returns>
    Task<TelegramBotDetailDto> UpdateTelegramBotAsync(TelegramBotUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 Telegram 机器人启停状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人详情</returns>
    Task<TelegramBotDetailDto> UpdateTelegramBotStatusAsync(TelegramBotStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除 Telegram 机器人
    /// </summary>
    /// <param name="id">Telegram 机器人主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteTelegramBotAsync(long id, CancellationToken cancellationToken = default);
}
