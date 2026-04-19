#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyRepository
// Guid:3bf3f534-e7d0-4e8d-a0f4-070ec7021fd6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 角色层级关系仓储实现
/// </summary>
public class RoleHierarchyRepository : IRoleHierarchyRepository
{
    private readonly ISqlSugarClientResolver _clientResolver;

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleHierarchyRepository(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<long>> GetInheritedRoleIdsAsync(
        IReadOnlyCollection<long> roleIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var distinctRoleIds = roleIds.Where(static id => id > 0).Distinct().ToArray();
        if (distinctRoleIds.Length == 0)
        {
            return [];
        }

        var query = DbClient.Queryable<SysRoleHierarchy>()
            .Where(hierarchy =>
                distinctRoleIds.Contains(hierarchy.DescendantId));
        query = ApplyTenantFilter(query, tenantId);

        var ancestorRoleIds = await query
            .Select(hierarchy => hierarchy.AncestorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roleIdSet = distinctRoleIds.ToHashSet();
        roleIdSet.UnionWith(ancestorRoleIds);
        return roleIdSet.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<long>> GetDirectParentRoleIdsAsync(
        long roleId,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            return [];
        }

        var query = DbClient.Queryable<SysRoleHierarchy>()
            .Where(hierarchy =>
                hierarchy.DescendantId == roleId
                && hierarchy.Depth == 1);
        query = ApplyTenantFilter(query, tenantId);

        return await query
            .Select(hierarchy => hierarchy.AncestorId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReplaceDirectParentsAsync(
        long roleId,
        IReadOnlyCollection<long> parentRoleIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roleIds = await GetTenantRoleIdsAsync(tenantId, cancellationToken);
        if (!roleIds.Contains(roleId))
        {
            throw new BusinessException(message: $"未找到角色: {roleId}");
        }

        var normalizedParentRoleIds = parentRoleIds
            .Where(static id => id > 0)
            .Distinct()
            .ToArray();
        if (normalizedParentRoleIds.Contains(roleId))
        {
            throw new BusinessException(message: "角色不能继承自身");
        }

        if (normalizedParentRoleIds.Except(roleIds).Any())
        {
            throw new BusinessException(message: "存在无效父角色 ID");
        }

        var directParentMap = await GetDirectParentMapAsync(tenantId, cancellationToken);
        directParentMap[roleId] = normalizedParentRoleIds;
        EnsureNoCycle(roleId, normalizedParentRoleIds, directParentMap);

        var closureRows = BuildClosureRows(roleIds, directParentMap, tenantId);

        var deleteable = DbClient.Deleteable<SysRoleHierarchy>();
        deleteable = ApplyTenantFilter(deleteable, tenantId);
        await deleteable.ExecuteCommandAsync(cancellationToken);

        if (closureRows.Count > 0)
        {
            await DbClient.Insertable(closureRows).ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RebuildHierarchyAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roleIds = await GetTenantRoleIdsAsync(tenantId, cancellationToken);
        var directParentMap = await GetDirectParentMapAsync(tenantId, cancellationToken);
        var closureRows = BuildClosureRows(roleIds, directParentMap, tenantId);

        var deleteable = DbClient.Deleteable<SysRoleHierarchy>();
        deleteable = ApplyTenantFilter(deleteable, tenantId);
        await deleteable.ExecuteCommandAsync(cancellationToken);

        if (closureRows.Count > 0)
        {
            await DbClient.Insertable(closureRows).ExecuteCommandAsync(cancellationToken);
        }
    }

    private async Task<HashSet<long>> GetTenantRoleIdsAsync(long? tenantId, CancellationToken cancellationToken)
    {
        var query = DbClient.Queryable<SysRole>()
            .Where(role => role.Status == YesOrNo.Yes);
        query = ApplyTenantFilter(query, tenantId);

        var roleIds = await query
            .Select(role => role.BasicId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. roleIds];
    }

    private async Task<Dictionary<long, long[]>> GetDirectParentMapAsync(long? tenantId, CancellationToken cancellationToken)
    {
        var query = DbClient.Queryable<SysRoleHierarchy>()
            .Where(hierarchy => hierarchy.Depth == 1);
        query = ApplyTenantFilter(query, tenantId);

        var rows = await query.ToListAsync(cancellationToken);
        return rows
            .GroupBy(static row => row.DescendantId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(row => row.AncestorId)
                    .Distinct()
                    .ToArray());
    }

    private static void EnsureNoCycle(
        long roleId,
        IReadOnlyCollection<long> directParentRoleIds,
        IReadOnlyDictionary<long, long[]> directParentMap)
    {
        foreach (var parentRoleId in directParentRoleIds)
        {
            if (HasPath(parentRoleId, roleId, directParentMap))
            {
                throw new BusinessException(message: "角色继承存在循环依赖");
            }
        }
    }

    private static bool HasPath(
        long currentRoleId,
        long targetRoleId,
        IReadOnlyDictionary<long, long[]> directParentMap)
    {
        var stack = new Stack<long>();
        var visited = new HashSet<long>();
        stack.Push(currentRoleId);

        while (stack.Count > 0)
        {
            var roleId = stack.Pop();
            if (!visited.Add(roleId))
            {
                continue;
            }

            if (roleId == targetRoleId)
            {
                return true;
            }

            if (directParentMap.TryGetValue(roleId, out var parentRoleIds))
            {
                foreach (var parentRoleId in parentRoleIds)
                {
                    stack.Push(parentRoleId);
                }
            }
        }

        return false;
    }

    private static List<SysRoleHierarchy> BuildClosureRows(
        IReadOnlyCollection<long> roleIds,
        IReadOnlyDictionary<long, long[]> directParentMap,
        long? tenantId)
    {
        var rows = new List<SysRoleHierarchy>(roleIds.Count * 2);
        foreach (var roleId in roleIds.OrderBy(static id => id))
        {
            rows.Add(new SysRoleHierarchy
            {
                TenantId = tenantId ?? 0,
                AncestorId = roleId,
                DescendantId = roleId,
                Depth = 0,
                Path = roleId.ToString()
            });

            var bestPaths = CollectAncestorPaths(roleId, directParentMap);
            foreach (var ancestorPath in bestPaths.Values)
            {
                rows.Add(new SysRoleHierarchy
                {
                    TenantId = tenantId ?? 0,
                    AncestorId = ancestorPath[0],
                    DescendantId = roleId,
                    Depth = ancestorPath.Count - 1,
                    Path = string.Join("/", ancestorPath)
                });
            }
        }

        return rows;
    }

    private static Dictionary<long, List<long>> CollectAncestorPaths(
        long descendantRoleId,
        IReadOnlyDictionary<long, long[]> directParentMap)
    {
        var bestPaths = new Dictionary<long, List<long>>();
        var queue = new Queue<List<long>>();
        if (directParentMap.TryGetValue(descendantRoleId, out var directParents))
        {
            foreach (var directParent in directParents.Distinct())
            {
                queue.Enqueue([directParent, descendantRoleId]);
            }
        }

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var ancestorRoleId = path[0];
            if (bestPaths.TryGetValue(ancestorRoleId, out var existingPath) && existingPath.Count <= path.Count)
            {
                continue;
            }

            bestPaths[ancestorRoleId] = path;

            if (!directParentMap.TryGetValue(ancestorRoleId, out var parentRoleIds))
            {
                continue;
            }

            foreach (var parentRoleId in parentRoleIds.Distinct())
            {
                if (path.Contains(parentRoleId))
                {
                    continue;
                }

                var nextPath = new List<long>(path.Count + 1) { parentRoleId };
                nextPath.AddRange(path);
                queue.Enqueue(nextPath);
            }
        }

        return bestPaths;
    }

    private static ISugarQueryable<T> ApplyTenantFilter<T>(ISugarQueryable<T> query, long? tenantId)
        where T : class, new()
    {
        return tenantId.HasValue
            ? query.Where("TenantId = @tenantId", new { tenantId })
            : query.Where("TenantId IS NULL");
    }

    private static IDeleteable<T> ApplyTenantFilter<T>(IDeleteable<T> deleteable, long? tenantId)
        where T : class, new()
    {
        return tenantId.HasValue
            ? deleteable.Where("TenantId = @tenantId", new { tenantId })
            : deleteable.Where("TenantId IS NULL");
    }
}
