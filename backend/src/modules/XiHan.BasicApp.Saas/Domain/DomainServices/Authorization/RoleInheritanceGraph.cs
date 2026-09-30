// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色继承图：以直接继承边（上级 → 下级）为真源的纯计算
/// </summary>
/// <remarks>
/// 间接继承、继承深度（最短路径长度）与路径都由这里从直接边推出；
/// 多条最短路径时按上级主键从小到大取第一条，结果确定。
/// </remarks>
public sealed class RoleInheritanceGraph
{
    private readonly Dictionary<long, SortedSet<long>> _parents = [];

    private readonly Dictionary<long, SortedSet<long>> _children = [];

    /// <summary>
    /// 由直接继承边构建
    /// </summary>
    /// <param name="directEdges">直接继承边（上级, 下级）</param>
    public RoleInheritanceGraph(IEnumerable<(long AncestorId, long DescendantId)> directEdges)
    {
        ArgumentNullException.ThrowIfNull(directEdges);

        foreach (var (ancestorId, descendantId) in directEdges)
        {
            AddEdge(ancestorId, descendantId);
        }
    }

    /// <summary>
    /// 由继承边实体构建
    /// </summary>
    public static RoleInheritanceGraph FromEdges(IEnumerable<SysRoleHierarchy> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);
        return new RoleInheritanceGraph(edges.Select(edge => (edge.AncestorId, edge.DescendantId)));
    }

    /// <summary>
    /// 全部直接继承边
    /// </summary>
    public IEnumerable<(long AncestorId, long DescendantId)> Edges =>
        _parents.SelectMany(pair => pair.Value.Select(parentId => (parentId, pair.Key)));

    /// <summary>
    /// 直接上级
    /// </summary>
    public IReadOnlyCollection<long> ParentsOf(long roleId) =>
        _parents.TryGetValue(roleId, out var parents) ? parents : [];

    /// <summary>
    /// 是否存在这条直接继承边
    /// </summary>
    public bool HasEdge(long ancestorId, long descendantId) =>
        _parents.TryGetValue(descendantId, out var parents) && parents.Contains(ancestorId);

    /// <summary>
    /// 加一条直接继承边
    /// </summary>
    public void AddEdge(long ancestorId, long descendantId)
    {
        if (ancestorId <= 0 || descendantId <= 0 || ancestorId == descendantId)
        {
            throw new ArgumentException("继承边两端必须是不同的有效角色。");
        }

        GetOrAdd(_parents, descendantId).Add(ancestorId);
        GetOrAdd(_children, ancestorId).Add(descendantId);
    }

    /// <summary>
    /// 摘一条直接继承边
    /// </summary>
    public void RemoveEdge(long ancestorId, long descendantId)
    {
        if (_parents.TryGetValue(descendantId, out var parents))
        {
            _ = parents.Remove(ancestorId);
        }

        if (_children.TryGetValue(ancestorId, out var children))
        {
            _ = children.Remove(descendantId);
        }
    }

    /// <summary>
    /// 全部上级（不含自身）及到各上级的最短路径
    /// </summary>
    /// <param name="roleId">起点角色</param>
    /// <param name="canPass">上级能否参与：不能参与的上级不计入，也不再经由它向上继承；为空表示都能参与</param>
    /// <returns>上级主键 → 最短路径（从上级到起点）</returns>
    public IReadOnlyDictionary<long, RoleInheritancePath> AncestorsOf(long roleId, Func<long, bool>? canPass = null) =>
        Walk(roleId, _parents, canPass, towardAncestors: true);

    /// <summary>
    /// 全部下级（不含自身）及到各下级的最短路径
    /// </summary>
    /// <param name="roleId">起点角色</param>
    /// <returns>下级主键 → 最短路径（从起点到下级）</returns>
    public IReadOnlyDictionary<long, RoleInheritancePath> DescendantsOf(long roleId) =>
        Walk(roleId, _children, canPass: null, towardAncestors: false);

    /// <summary>
    /// 按层走图：先到达即最短；同层按主键从小到大，路径取确定的一条
    /// </summary>
    private static Dictionary<long, RoleInheritancePath> Walk(
        long roleId,
        Dictionary<long, SortedSet<long>> adjacency,
        Func<long, bool>? canPass,
        bool towardAncestors)
    {
        var via = new Dictionary<long, long>();
        var depths = new Dictionary<long, int>();
        var frontier = new List<long> { roleId };
        var depth = 0;
        while (frontier.Count > 0)
        {
            depth++;
            var next = new List<long>();
            foreach (var current in frontier)
            {
                if (!adjacency.TryGetValue(current, out var neighbors))
                {
                    continue;
                }

                foreach (var neighborId in neighbors)
                {
                    if (neighborId == roleId || depths.ContainsKey(neighborId) || canPass?.Invoke(neighborId) == false)
                    {
                        continue;
                    }

                    depths[neighborId] = depth;
                    via[neighborId] = current;
                    next.Add(neighborId);
                }
            }

            frontier = next;
        }

        var result = new Dictionary<long, RoleInheritancePath>(depths.Count);
        foreach (var (reachedId, reachedDepth) in depths)
        {
            // 从到达点回溯到起点，路径统一按「上级 → 下级」排列
            var path = new List<long>(reachedDepth + 1) { reachedId };
            var step = reachedId;
            while (step != roleId)
            {
                step = via[step];
                path.Add(step);
            }

            if (!towardAncestors)
            {
                path.Reverse();
            }

            result[reachedId] = new RoleInheritancePath(reachedDepth, path);
        }

        return result;
    }

    private static SortedSet<long> GetOrAdd(Dictionary<long, SortedSet<long>> map, long key)
    {
        if (!map.TryGetValue(key, out var set))
        {
            set = [];
            map[key] = set;
        }

        return set;
    }
}

/// <summary>
/// 两个角色之间的最短继承路径
/// </summary>
/// <param name="Depth">继承深度：1 为直接继承</param>
/// <param name="RoleIds">路径上的角色，按「上级 → 下级」排列</param>
public sealed record RoleInheritancePath(int Depth, IReadOnlyList<long> RoleIds);
