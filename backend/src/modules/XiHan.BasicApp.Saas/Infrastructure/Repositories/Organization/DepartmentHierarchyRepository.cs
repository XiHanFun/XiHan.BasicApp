// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 部门层级仓储实现
/// </summary>
public sealed class DepartmentHierarchyRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysDepartmentHierarchy>(clientResolver), IDepartmentHierarchyRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetDescendantIdsAsync(long departmentId, bool includeSelf, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(hierarchy => hierarchy.AncestorId == departmentId);
        if (!includeSelf)
        {
            query = query.Where(hierarchy => hierarchy.Depth > 0);
        }

        var ids = await query
            .OrderBy(hierarchy => hierarchy.Depth)
            .Select(hierarchy => hierarchy.DescendantId)
            .ToListAsync(cancellationToken);

        return ids.Distinct().ToArray();
    }
}
