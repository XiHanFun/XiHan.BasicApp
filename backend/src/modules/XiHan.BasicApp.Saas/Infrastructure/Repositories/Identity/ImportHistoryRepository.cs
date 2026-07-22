// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 导入历史仓储实现
/// </summary>
public sealed class ImportHistoryRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysImportHistory>(clientResolver), IImportHistoryRepository
{
    /// <inheritdoc />
    public async Task<List<SysImportHistory>> GetRecentByUserAsync(long userId, string pageCode, int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(history => history.UserId == userId && history.PageCode == pageCode)
            .OrderByDescending(history => history.CreatedTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
