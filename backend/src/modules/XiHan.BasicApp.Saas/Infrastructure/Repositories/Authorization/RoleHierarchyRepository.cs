#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyRepository
// Guid:92e5fe31-aa35-463a-9821-f01271b10ec9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
