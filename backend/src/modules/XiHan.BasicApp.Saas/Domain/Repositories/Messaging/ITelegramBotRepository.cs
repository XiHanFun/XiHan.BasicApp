// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// Telegram 机器人仓储接口
/// </summary>
public interface ITelegramBotRepository : ISaasRepository<SysTelegramBot>
{
    /// <summary>
    /// 按机器人名称查询（租户内唯一）
    /// </summary>
    Task<SysTelegramBot?> GetByNameAsync(string botName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部启用的机器人列表
    /// </summary>
    Task<List<SysTelegramBot>> GetEnabledListAsync(CancellationToken cancellationToken = default);
}
