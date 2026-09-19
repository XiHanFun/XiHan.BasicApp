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
    /// <summary>
    /// 根据用户ID获取统计信息
    /// </summary>
    public async Task<SysUserStatistics?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(stats => stats.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户获取用户全部统计快照
    /// </summary>
    public async Task<IReadOnlyList<SysUserStatistics>> GetListByUserIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 聚合任务按日志的租户戳分别落快照，同一个人的活跃散落在各租户行下
        return await CreateNoTenantQueryable()
            .Where(stats => stats.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
