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
    /// <summary>
    /// 当前上下文可见的全部直接继承边
    /// </summary>
    public async Task<IReadOnlyList<SysRoleHierarchy>> GetEdgesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户获取有角色直接继承了给定角色的租户
    /// </summary>
    public async Task<IReadOnlyList<long>> GetTenantIdsInheritingAsync(IReadOnlyCollection<long> parentRoleIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parentRoleIds);

        var parentIds = parentRoleIds.Where(id => id > 0).Distinct().ToArray();
        if (parentIds.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(hierarchy => hierarchy.TenantId != 0 && parentIds.Contains(hierarchy.AncestorId))
            .Select(hierarchy => hierarchy.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
