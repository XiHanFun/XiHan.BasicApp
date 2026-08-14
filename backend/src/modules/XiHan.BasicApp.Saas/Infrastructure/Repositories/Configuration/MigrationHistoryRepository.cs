// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 迁移历史仓储实现
/// </summary>
public sealed class MigrationHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysMigrationHistory>(clientResolver), IMigrationHistoryRepository
{
    /// <summary>
    /// 根据版本号获取迁移记录
    /// </summary>
    public async Task<SysMigrationHistory?> GetByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(history => history.Version == version)
            .FirstAsync(cancellationToken);
    }
}
