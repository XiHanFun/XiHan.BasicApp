// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户统计仓储实现
/// </summary>
public sealed class UserStatisticsRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserStatistics>(clientResolver), IUserStatisticsRepository
{
    /// <inheritdoc />
    public async Task<SysUserStatistics?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(stats => stats.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
