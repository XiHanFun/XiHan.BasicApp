#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TelegramBotRepository
// Guid:c5eaec53-f07c-407c-a960-48b224a044d2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// <inheritdoc />
    public async Task<SysTelegramBot?> GetByNameAsync(string botName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bot => bot.BotName == botName)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SysTelegramBot>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bot => bot.IsEnabled)
            .OrderBy(bot => bot.Sort)
            .ToListAsync(cancellationToken);
    }
}
