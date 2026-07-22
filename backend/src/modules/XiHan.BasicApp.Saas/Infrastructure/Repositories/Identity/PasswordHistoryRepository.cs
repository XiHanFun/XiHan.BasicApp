// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 密码历史仓储实现
/// </summary>
public sealed class PasswordHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPasswordHistory>(clientResolver), IPasswordHistoryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysPasswordHistory>> GetRecentByUserIdAsync(long userId, int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(history => history.UserId == userId)
            .OrderByDescending(history => history.ChangedTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
