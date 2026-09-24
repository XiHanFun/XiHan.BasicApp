// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 角色父角色批量变更：闭包表先摘边后加边，新增补齐传递闭包，移除只删仅经由被摘边可达的闭包行。
/// </summary>
public sealed class RoleParentBatchUpdateTests
{
    /// <summary>
    /// 两个孤立角色建立继承：补齐双方自反行与直接边。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AddFirstParent_ShouldCreateSelfRowsAndDirectEdge()
    {
        var fixture = new Fixture(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, add: [1]);

        Assert.Equal([(1L, 1L, 0), (1L, 2L, 1), (2L, 2L, 0)], fixture.AddedTriples());
        Assert.Equal([1L], result.AddedParentRoleIds);
    }

    /// <summary>
    /// 挂到已有链条下：祖先链上每一级都生成到新角色的闭包行，深度按路径累加。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AddParentInChain_ShouldPropagateClosure()
    {
        var fixture = new Fixture(1, 2, 3);
        fixture.AddChain(1, 2);

        _ = await fixture.UpdateAsync(roleId: 3, add: [2]);

        Assert.Equal([(1L, 3L, 2), (2L, 3L, 1), (3L, 3L, 0)], fixture.AddedTriples());
    }

    /// <summary>
    /// 已是直接父角色：视为已达成，不写库。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AlreadyDirectParent_ShouldBeNoChange()
    {
        var fixture = new Fixture(1, 2);
        fixture.AddChain(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, add: [1]);

        Assert.Empty(result.AddedParentRoleIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 把自己的后代设为父角色会成环，整批拒绝。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AddDescendantAsParent_ShouldRejectCycle()
    {
        var fixture = new Fixture(1, 2);
        fixture.AddChain(1, 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 1, add: [2]));

        Assert.Contains("环路", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 角色不能继承自己。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AddSelf_ShouldReject()
    {
        var fixture = new Fixture(1);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 1, add: [1]));

        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 摘掉链尾的父边：删掉经由它可达的闭包行，上游的边不动。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RemoveParent_ShouldDeleteOnlyClosureThroughIt()
    {
        var fixture = new Fixture(1, 2, 3);
        fixture.AddChain(1, 2, 3);

        var result = await fixture.UpdateAsync(roleId: 3, remove: [2]);

        Assert.Equal([(1L, 3L, 2), (2L, 3L, 1)], fixture.DeletedTriples());
        Assert.Equal([2L], result.RemovedParentRoleIds);
        Assert.Contains(fixture.Remaining(), row => row.AncestorId == 1 && row.DescendantId == 2);
    }

    /// <summary>
    /// 被摘的直接边另有替代路径时拒绝：只摘它不改变可达性，闭包行无从删起。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RemoveParentWithAlternativePath_ShouldReject()
    {
        var fixture = new Fixture(1, 2, 3);
        fixture.AddChain(1, 2, 3);
        fixture.PromoteToDirect(1, 3);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 3, remove: [1]));

        Assert.Contains("替代路径", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 互为替代路径的两条父边一并摘掉：按整批的剩余边判定，不被彼此挡住。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RemoveMutuallyAlternativeParents_ShouldSucceed()
    {
        var fixture = new Fixture(1, 2, 3);
        fixture.AddChain(1, 2, 3);
        fixture.PromoteToDirect(1, 3);

        var result = await fixture.UpdateAsync(roleId: 3, remove: [1, 2]);

        Assert.Equal([(1L, 3L, 1), (2L, 3L, 1)], fixture.DeletedTriples());
        Assert.Equal([1L, 2L], result.RemovedParentRoleIds.Order());
    }

    /// <summary>
    /// 同一次提交换父角色：先摘旧边再加新边，旧祖先到本角色的闭包行被删，新祖先的补上。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_MoveToAnotherParent_ShouldRemoveThenAdd()
    {
        var fixture = new Fixture(1, 2, 3);
        fixture.AddChain(1, 3);
        fixture.AddChain(2);

        var result = await fixture.UpdateAsync(roleId: 3, add: [2], remove: [1]);

        Assert.Equal([(1L, 3L, 1)], fixture.DeletedTriples());
        Assert.Equal([(2L, 3L, 1)], fixture.AddedTriples());
        Assert.Equal([2L], result.AddedParentRoleIds);
        Assert.Equal([1L], result.RemovedParentRoleIds);
    }

    /// <summary>
    /// 同一父角色既加又移时以新增为准。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AddAndRemoveSameParent_ShouldKeepParent()
    {
        var fixture = new Fixture(1, 2);
        fixture.AddChain(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, add: [1], remove: [1]);

        Assert.Empty(result.RemovedParentRoleIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 批量变更的测试夹具：闭包行保存在内存里，删除与写入逐次记录。
    /// </summary>
    private sealed class Fixture
    {
        private readonly List<SysRoleHierarchy> _rows = [];

        private readonly List<SysRole> _roles = [];

        private long _nextRowId = 1000;

        public Fixture(params long[] roleIds)
        {
            foreach (var roleId in roleIds)
            {
                var role = new SysRole
                {
                    TenantId = 7,
                    RoleCode = $"ROLE-{roleId}",
                    RoleName = $"角色{roleId}",
                    RoleType = RoleType.Custom,
                    Status = EnableStatus.Enabled
                };
                SaasTestHelper.SetBasicId(role, roleId);
                _roles.Add(role);
            }

            var roleRepository = new Mock<IRoleRepository>();
            roleRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _roles.FirstOrDefault(role => role.BasicId == id));

            HierarchyRepository
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => (IReadOnlyList<SysRoleHierarchy>)_rows.ToList());
            HierarchyRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Expression<Func<SysRoleHierarchy, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysRoleHierarchy, bool>> predicate, CancellationToken _) =>
                {
                    var matched = _rows.Where(predicate.Compile()).ToList();
                    Deleted.AddRange(matched);
                    _rows.RemoveAll(matched.Contains);
                    return true;
                });
            HierarchyRepository
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRoleHierarchy>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysRoleHierarchy> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Added.AddRange(list);
                    return list.ToArray();
                });

            Service = new RoleDomainService(
                roleRepository.Object,
                new Mock<IUserRoleRepository>().Object,
                new Mock<IRolePermissionRepository>().Object,
                HierarchyRepository.Object,
                new Mock<IRoleDataScopeRepository>().Object,
                new Mock<IPermissionRepository>().Object,
                new Mock<IDepartmentRepository>().Object,
                new TestCurrentTenant(7));
        }

        public RoleDomainService Service { get; }

        public Mock<IRoleHierarchyRepository> HierarchyRepository { get; } = new();

        public List<SysRoleHierarchy> Deleted { get; } = [];

        public List<SysRoleHierarchy> Added { get; } = [];

        public Task<RoleHierarchyBatchUpdateResult> UpdateAsync(long roleId, long[]? add = null, long[]? remove = null) =>
            Service.BatchUpdateRoleParentsAsync(new RoleHierarchyBatchUpdateCommand(roleId, add ?? [], remove ?? []));

        /// <summary>
        /// 按 祖先→…→后代 的顺序铺一条链：每个角色的自反行，以及链上任意两级之间的闭包行。
        /// </summary>
        public void AddChain(params long[] roleIds)
        {
            for (var i = 0; i < roleIds.Length; i++)
            {
                for (var j = i; j < roleIds.Length; j++)
                {
                    AddRow(roleIds[i], roleIds[j], j - i);
                }
            }
        }

        /// <summary>
        /// 把一条已有的传递闭包行改成直接边（模拟先有 A→C、后又挂上 A→B→C 留下的冗余直接边）。
        /// </summary>
        public void PromoteToDirect(long ancestorId, long descendantId)
        {
            var row = _rows.Single(hierarchy => hierarchy.AncestorId == ancestorId && hierarchy.DescendantId == descendantId);
            row.Depth = 1;
            row.Path = $"{ancestorId}/{descendantId}";
        }

        public IReadOnlyList<SysRoleHierarchy> Remaining() => _rows;

        public IReadOnlyList<(long, long, int)> AddedTriples() => ToTriples(Added);

        public IReadOnlyList<(long, long, int)> DeletedTriples() => ToTriples(Deleted);

        public void VerifyNothingWritten()
        {
            HierarchyRepository.Verify(
                repo => repo.DeleteAsync(It.IsAny<Expression<Func<SysRoleHierarchy, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            HierarchyRepository.Verify(
                repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRoleHierarchy>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static List<(long, long, int)> ToTriples(IEnumerable<SysRoleHierarchy> rows) =>
            [.. rows
                .Select(row => (row.AncestorId, row.DescendantId, row.Depth))
                .Order()];

        private void AddRow(long ancestorId, long descendantId, int depth)
        {
            var row = new SysRoleHierarchy
            {
                AncestorId = ancestorId,
                DescendantId = descendantId,
                Depth = depth,
                Path = ancestorId == descendantId ? $"{ancestorId}" : $"{ancestorId}/{descendantId}"
            };
            SaasTestHelper.SetBasicId(row, _nextRowId++);
            _rows.Add(row);
        }
    }
}
