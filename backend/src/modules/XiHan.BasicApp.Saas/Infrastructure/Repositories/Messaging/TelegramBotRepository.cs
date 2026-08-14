// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// Telegram 机器人仓储实现
/// </summary>
public sealed class TelegramBotRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysTelegramBot>(clientResolver), ITelegramBotRepository
{
    /// <summary>
    /// 按机器人名称查询（租户内唯一）
    /// </summary>
    public async Task<SysTelegramBot?> GetByNameAsync(string botName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bot => bot.BotName == botName)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取全部启用的机器人列表
    /// </summary>
    public async Task<List<SysTelegramBot>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bot => bot.IsEnabled)
            .OrderBy(bot => bot.Sort)
            .ToListAsync(cancellationToken);
    }
}
