// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字典项仓储实现
/// </summary>
public sealed class DictItemRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysDictItem>(clientResolver), IDictItemRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(item => item.DictId == dictId)
            .ToListAsync(cancellationToken);
    }
}
