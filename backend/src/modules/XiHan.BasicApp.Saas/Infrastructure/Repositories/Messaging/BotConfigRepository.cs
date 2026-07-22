// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 机器人配置仓储实现（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
public sealed class BotConfigRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysBotConfig>(clientResolver), IBotConfigRepository
{
    /// <inheritdoc />
    public async Task<SysBotConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.ConfigCode == configCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysBotConfig?> GetDefaultAsync(BotProviderType provider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.Provider == provider && config.IsDefault && config.IsEnabled)
            .FirstAsync(cancellationToken);
    }
}
