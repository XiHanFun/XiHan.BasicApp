// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 租户版本权限仓储实现
/// </summary>
public sealed class TenantEditionPermissionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysTenantEditionPermission>(clientResolver), ITenantEditionPermissionRepository
{
    /// <summary>
    /// 根据版本ID获取权限映射列表
    /// </summary>
    public async Task<IReadOnlyList<SysTenantEditionPermission>> GetByEditionIdAsync(long editionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(mapping => mapping.EditionId == editionId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 全量替换版本权限映射（先删后插）
    /// </summary>
    public async Task ReplaceByEditionIdAsync(long editionId, IEnumerable<SysTenantEditionPermission> items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DeleteAsync(mapping => mapping.EditionId == editionId, cancellationToken);

        var list = items.ToList();
        if (list.Count > 0)
        {
            await AddRangeAsync(list, cancellationToken);
        }
    }
}
