// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 继承链上的一个角色：上级链里是上级，下级链里是下级。
/// </summary>
/// <param name="RoleId">角色。</param>
/// <param name="Depth">最短的继承深度（直接上级或直接下级为 1）。</param>
/// <param name="Path">最短路径上的角色，按「上级 → 下级」排列，含两端。</param>
/// <param name="IsEffective">授予此刻是否沿继承传到（路径上作为上级的角色都启用）。</param>
public sealed record RoleRelative(Guid RoleId, int Depth, IReadOnlyList<Guid> Path, bool IsEffective);

/// <summary>
/// 一侧的角色继承图。
/// </summary>
/// <remarks>
/// 直接边与边上角色的启停。祖先按广度优先展开，停用的上级不传下授予、也截断经过它的继承（4.x 同）。
/// </remarks>
public sealed class RoleGraph
{
    private readonly Dictionary<Guid, List<Guid>> _parents = [];
    private readonly Dictionary<Guid, List<Guid>> _children = [];
    private readonly HashSet<Guid> _inactive = [];

    /// <summary>
    /// 由直接边与停用的角色组装。
    /// </summary>
    /// <param name="edges">直接边（下级，上级）。</param>
    /// <param name="inactive">边上停用的角色。</param>
    public RoleGraph(IEnumerable<(Guid RoleId, Guid ParentId)> edges, IEnumerable<Guid> inactive)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(inactive);
        foreach (var (roleId, parentId) in edges)
        {
            Add(_parents, roleId, parentId);
            Add(_children, parentId, roleId);
        }

        _inactive.UnionWith(inactive);
    }

    /// <summary>
    /// 直接上级。
    /// </summary>
    public IReadOnlyList<Guid> ParentsOf(Guid roleId) => _parents.TryGetValue(roleId, out var parents) ? parents : [];

    /// <summary>
    /// 是否有下级。
    /// </summary>
    public bool HasChildren(Guid roleId) => _children.ContainsKey(roleId);

    /// <summary>
    /// 取此刻传得到授予的上级与深度（不含自身）。
    /// </summary>
    /// <remarks>
    /// 只经过启用的上级，停用的上级与它之上的不算。
    /// </remarks>
    public IReadOnlyList<(Guid RoleId, int Depth)> EffectiveAncestorsOf(Guid roleId)
    {
        var found = new List<(Guid, int)>();
        var seen = new HashSet<Guid> { roleId };
        var queue = new Queue<(Guid Id, int Depth)>();
        queue.Enqueue((roleId, 0));
        while (queue.TryDequeue(out var current))
        {
            foreach (var parent in ParentsOf(current.Id))
            {
                if (_inactive.Contains(parent) || !seen.Add(parent))
                {
                    continue;
                }

                found.Add((parent, current.Depth + 1));
                queue.Enqueue((parent, current.Depth + 1));
            }
        }

        return found;
    }

    /// <summary>
    /// 全部上级（不含自身，含停用与被截断的），按深度排列。
    /// </summary>
    public IReadOnlyList<RoleRelative> AncestorsOf(Guid roleId)
    {
        var effective = EffectiveAncestorsOf(roleId).Select(static a => a.RoleId).ToHashSet();
        return Walk(roleId, _parents).Select(w => new RoleRelative(w.RoleId, w.Depth, [.. Enumerable.Reverse(w.Path)], effective.Contains(w.RoleId))).ToList();
    }

    /// <summary>
    /// 取全部下级（不含自身），按深度排列。
    /// </summary>
    /// <remarks>
    /// 本角色停用时都不传到。
    /// </remarks>
    public IReadOnlyList<RoleRelative> DescendantsOf(Guid roleId)
    {
        var effective = new HashSet<Guid>();
        if (!_inactive.Contains(roleId))
        {
            var queue = new Queue<Guid>();
            queue.Enqueue(roleId);
            while (queue.TryDequeue(out var current))
            {
                foreach (var child in _children.TryGetValue(current, out var children) ? children : [])
                {
                    // 下级本身停用也拿得到（它的持有人不算数是另一回事），但停用的下级不再往下传
                    if (effective.Add(child) && !_inactive.Contains(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        return Walk(roleId, _children).Select(w => new RoleRelative(w.RoleId, w.Depth, w.Path, effective.Contains(w.RoleId))).ToList();
    }

    /// <summary>
    /// 按一个方向广度优先走（不看启停），带最短路径（从起点到该角色）。
    /// </summary>
    private static List<(Guid RoleId, int Depth, List<Guid> Path)> Walk(Guid start, Dictionary<Guid, List<Guid>> next)
    {
        var found = new List<(Guid, int, List<Guid>)>();
        var seen = new HashSet<Guid> { start };
        var queue = new Queue<(Guid Id, List<Guid> Path)>();
        queue.Enqueue((start, [start]));
        while (queue.TryDequeue(out var current))
        {
            foreach (var other in next.TryGetValue(current.Id, out var ids) ? ids : [])
            {
                if (!seen.Add(other))
                {
                    continue;
                }

                List<Guid> path = [.. current.Path, other];
                found.Add((other, path.Count - 1, path));
                queue.Enqueue((other, path));
            }
        }

        return found;
    }

    private static void Add(Dictionary<Guid, List<Guid>> map, Guid key, Guid value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            map[key] = list = [];
        }

        list.Add(value);
    }
}

/// <summary>
/// 角色继承图的缓存（单例）。
/// </summary>
/// <remarks>
/// 每侧一份，放在共享缓存里 60 秒，继承变更与角色启停、删除时失效。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查库）。</param>
public sealed class RoleHierarchy(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 一侧的继承图。
    /// </summary>
    public async Task<RoleGraph> GraphOfAsync(string owner, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner), ct => LoadAsync(owner, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        var edges = new List<(Guid, Guid)>();
        var inactive = new List<Guid>();
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts[0] == "-")
            {
                inactive.Add(Guid.ParseExact(parts[1], "N"));
            }
            else
            {
                edges.Add((Guid.ParseExact(parts[0], "N"), Guid.ParseExact(parts[1], "N")));
            }
        }

        return new RoleGraph(edges, inactive);
    }

    /// <summary>
    /// 一侧的继承图失效。
    /// </summary>
    public async Task InvalidateAsync(string owner, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner) => new($"access:hierarchy:{owner}");

    private async ValueTask<string> LoadAsync(string owner, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var edges = await services.GetRequiredService<IRepository<SysRoleInheritance, Guid>>()
                .ListAsync(EntityQuery.All<SysRoleInheritance>().Where(new Specification<SysRoleInheritance>(e => e.OwnerTenantId == owner)), ct)
                .ConfigureAwait(false);
            if (edges.Count == 0)
            {
                return string.Empty;
            }

            var ids = edges.SelectMany(static e => new[] { e.RoleId, e.ParentId }).Distinct().ToList();
            var inactive = (await services.GetRequiredService<IRepository<SysRole, Guid>>().FindManyAsync(ids, ct).ConfigureAwait(false))
                .Where(static r => r.Status != RoleStatus.Active)
                .Select(static r => r.Id);
            return string.Join('\n', edges.Select(static e => $"{e.RoleId:N}\t{e.ParentId:N}").Concat(inactive.Select(static id => $"-\t{id:N}")));
        }, cancellationToken).ConfigureAwait(false);
}
