// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 菜单仓储实现
/// </summary>
public sealed class MenuRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysMenu>(clientResolver), IMenuRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysMenu>> GetByParentIdAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(menu => menu.ParentId == parentId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysMenu>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .ToListAsync(cancellationToken);
    }
}
