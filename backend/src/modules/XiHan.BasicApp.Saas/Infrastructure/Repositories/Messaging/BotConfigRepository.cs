#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BotConfigRepository
// Guid:3f377caa-6a5a-4f83-b340-51d7e7a1a52c
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
