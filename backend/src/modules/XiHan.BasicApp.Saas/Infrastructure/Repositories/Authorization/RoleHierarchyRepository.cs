// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 角色层级仓储实现
/// </summary>
public sealed class RoleHierarchyRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysRoleHierarchy>(clientResolver), IRoleHierarchyRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetAncestorIdsAsync(IEnumerable<long> roleIds, bool includeSelf, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var roleIdArray = roleIds.Distinct().ToArray();
        if (roleIdArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(hierarchy => roleIdArray.Contains(hierarchy.DescendantId));
        if (!includeSelf)
        {
            query = query.Where(hierarchy => hierarchy.Depth > 0);
        }

        var ids = await query
            .OrderBy(hierarchy => hierarchy.Depth)
            .Select(hierarchy => hierarchy.AncestorId)
            .ToListAsync(cancellationToken);

        return ids.Distinct().ToArray();
    }
}
