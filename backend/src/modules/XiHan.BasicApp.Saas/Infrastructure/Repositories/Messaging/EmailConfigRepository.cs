#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailConfigRepository
// Guid:4c8a2e57-6b19-4d70-8f3c-1a9d5e7b2c48
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 邮件网关配置仓储实现
/// </summary>
public sealed class EmailConfigRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysEmailConfig>(clientResolver), IEmailConfigRepository
{
    /// <inheritdoc />
    public async Task<SysEmailConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.ConfigCode == configCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysEmailConfig?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.IsDefault && config.IsEnabled)
            .FirstAsync(cancellationToken);
    }
}
