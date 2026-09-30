// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 角色继承图：从直接边推出上级 / 下级、最短深度与路径，停用角色切断经由它的继承。
/// </summary>
public sealed class RoleInheritanceGraphTests
{
    /// <summary>
    /// 链上每一级都是上级，深度按最短路径算，路径按「上级 → 下级」排列。
    /// </summary>
    [Fact]
    public void AncestorsOf_Chain_ShouldReturnDepthAndPath()
    {
        var graph = new RoleInheritanceGraph([(1, 2), (2, 3)]);

        var ancestors = graph.AncestorsOf(3);

        Assert.Equal(1, ancestors[2].Depth);
        Assert.Equal([2L, 3], ancestors[2].RoleIds);
        Assert.Equal(2, ancestors[1].Depth);
        Assert.Equal([1L, 2, 3], ancestors[1].RoleIds);
        Assert.Empty(graph.AncestorsOf(1));
    }

    /// <summary>
    /// 多条路径时取最短；同样短时按上级主键从小到大取，结果确定。
    /// </summary>
    [Fact]
    public void AncestorsOf_MultiplePaths_ShouldTakeShortestDeterministically()
    {
        // 1→3→4、1→2→4 同长，另有冗余直接边 1→4
        var graph = new RoleInheritanceGraph([(1, 3), (3, 4), (1, 2), (2, 4)]);

        Assert.Equal([1L, 2, 4], graph.AncestorsOf(4)[1].RoleIds);

        graph.AddEdge(1, 4);
        Assert.Equal(1, graph.AncestorsOf(4)[1].Depth);
    }

    /// <summary>
    /// 不能参与的上级不计入，也不再经由它向上继承；有别的路径时照样可达。
    /// </summary>
    [Fact]
    public void AncestorsOf_WithBlockedRole_ShouldCutThroughIt()
    {
        var graph = new RoleInheritanceGraph([(1, 2), (2, 3)]);

        Assert.Empty(graph.AncestorsOf(3, id => id != 2));

        graph.AddEdge(4, 3);
        graph.AddEdge(1, 4);
        var ancestors = graph.AncestorsOf(3, id => id != 2);
        Assert.Equal([1L, 4], ancestors.Keys.Order());
        Assert.Equal([1L, 4, 3], ancestors[1].RoleIds);
    }

    /// <summary>
    /// 下级同样给出最短深度与路径。
    /// </summary>
    [Fact]
    public void DescendantsOf_ShouldReturnDepthAndPath()
    {
        var graph = new RoleInheritanceGraph([(1, 2), (2, 3), (1, 3)]);

        var descendants = graph.DescendantsOf(1);

        Assert.Equal(1, descendants[2].Depth);
        Assert.Equal(1, descendants[3].Depth);
        Assert.Equal([1L, 3], descendants[3].RoleIds);
        Assert.Equal([3L], graph.DescendantsOf(2).Keys);
    }

    /// <summary>
    /// 摘边后不再可达；自己继承自己的边直接拒绝。
    /// </summary>
    [Fact]
    public void RemoveEdge_AndSelfEdge()
    {
        var graph = new RoleInheritanceGraph([(1, 2)]);
        graph.RemoveEdge(1, 2);

        Assert.False(graph.HasEdge(1, 2));
        Assert.Empty(graph.AncestorsOf(2));
        Assert.Throws<ArgumentException>(() => graph.AddEdge(5, 5));
    }
}
