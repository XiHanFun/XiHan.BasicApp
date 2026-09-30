// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 角色继承领域服务：只存直接继承边；新增校验环路、重复、系统角色、启停与作用范围；
/// 解除不受替代路径限制；变更后按新关系复核职责分离；有效上级跳过并切断停用角色。
/// </summary>
public sealed class RoleHierarchyDomainServiceTests
{
    private const long TenantId = 7;

    /// <summary>
    /// 新增上级只写这一条直接边。
    /// </summary>
    [Fact]
    public async Task AddParent_ShouldWriteDirectEdgeOnly()
    {
        var fixture = new Fixture().Roles(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, add: [1]);

        Assert.Equal([(1L, 2L)], fixture.Added);
        Assert.Equal([1L], result.AddedParentRoleIds);
    }

    /// <summary>
    /// 已是直接上级：视为已达成，不写库也不复核。
    /// </summary>
    [Fact]
    public async Task AddExistingParent_ShouldBeNoChange()
    {
        var fixture = new Fixture().Roles(1, 2).Edge(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, add: [1]);

        Assert.Empty(result.AddedParentRoleIds);
        Assert.Empty(fixture.Added);
        Assert.Empty(fixture.EvaluatedSets);
    }

    /// <summary>
    /// 把自己的下级设为上级会成环。
    /// </summary>
    [Fact]
    public async Task AddDescendantAsParent_ShouldRejectCycle()
    {
        var fixture = new Fixture().Roles(1, 2, 3).Edge(1, 2).Edge(2, 3);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 1, add: [3]));

        Assert.Contains("环路", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Added);
    }

    /// <summary>
    /// 角色不能继承自己。
    /// </summary>
    [Fact]
    public async Task AddSelf_ShouldReject()
    {
        var fixture = new Fixture().Roles(1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 1, add: [1]));

        Assert.Contains("继承自己", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已经间接继承的角色不必再直接继承。
    /// </summary>
    [Fact]
    public async Task AddIndirectAncestor_ShouldReject()
    {
        var fixture = new Fixture().Roles(1, 2, 3).Edge(1, 2).Edge(2, 3);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 3, add: [1]));

        Assert.Contains("间接继承", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 系统角色不参与继承：不能当上级，也不能给它设上级。
    /// </summary>
    [Fact]
    public async Task SystemRole_ShouldNotParticipate()
    {
        var fixture = new Fixture().Roles(1, 2).Role(9, roleType: RoleType.System);

        var asParent = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 2, add: [9]));
        var asChild = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 9, add: [1]));

        Assert.Contains("系统角色", asParent.Message, StringComparison.Ordinal);
        Assert.Contains("系统角色", asChild.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用角色不能设为上级。
    /// </summary>
    [Fact]
    public async Task DisabledParent_ShouldReject()
    {
        var fixture = new Fixture().Roles(2).Role(1, status: EnableStatus.Disabled);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 2, add: [1]));

        Assert.Contains("停用", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 全局角色的继承只在平台维护；平台里全局角色只能继承全局角色。
    /// </summary>
    [Fact]
    public async Task GlobalRole_ScopeRules()
    {
        var inTenant = new Fixture().Roles(1).Role(5, tenantId: 0);
        var tenantError = await Assert.ThrowsAsync<InvalidOperationException>(() => inTenant.UpdateAsync(roleId: 5, add: [1]));
        Assert.Contains("平台运维", tenantError.Message, StringComparison.Ordinal);

        var inPlatform = new Fixture(currentTenantId: null).Roles(1).Role(5, tenantId: 0);
        var platformError = await Assert.ThrowsAsync<InvalidOperationException>(() => inPlatform.UpdateAsync(roleId: 5, add: [1]));
        Assert.Contains("只能继承全局角色", platformError.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 解除上级只删这条边。
    /// </summary>
    [Fact]
    public async Task RemoveParent_ShouldDeleteEdge()
    {
        var fixture = new Fixture().Roles(1, 2).Edge(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, remove: [1]);

        Assert.Equal([(1L, 2L)], fixture.Deleted);
        Assert.Equal([1L], result.RemovedParentRoleIds);
    }

    /// <summary>
    /// 冗余的直接上级（另有路径可达）也能解除，解除后仍经另一条路径间接继承。
    /// </summary>
    [Fact]
    public async Task RemoveRedundantDirectParent_ShouldSucceed()
    {
        var fixture = new Fixture().Roles(1, 2, 3).Edge(1, 3).Edge(1, 2).Edge(2, 3);

        _ = await fixture.UpdateAsync(roleId: 3, remove: [1]);

        Assert.Equal([(1L, 3L)], fixture.Deleted);
        var ancestors = await fixture.Service.GetEffectiveAncestorsAsync([3]);
        Assert.Equal([1L, 2], ancestors[3].Keys.Order());
    }

    /// <summary>
    /// 先解除后新增：把上级从 2 换成 2 的上级 1，不会被「已间接继承」挡住。
    /// </summary>
    [Fact]
    public async Task MoveParent_ShouldRemoveThenAdd()
    {
        var fixture = new Fixture().Roles(1, 2, 3).Edge(1, 2).Edge(2, 3);

        var result = await fixture.UpdateAsync(roleId: 3, add: [1], remove: [2]);

        Assert.Equal([(2L, 3L)], fixture.Deleted);
        Assert.Equal([(1L, 3L)], fixture.Added);
        Assert.Equal([1L], result.AddedParentRoleIds);
        Assert.Equal([2L], result.RemovedParentRoleIds);
    }

    /// <summary>
    /// 同一上级本次既加又解时以新增为准。
    /// </summary>
    [Fact]
    public async Task AddAndRemoveSameParent_ShouldKeepParent()
    {
        var fixture = new Fixture().Roles(1, 2).Edge(1, 2);

        var result = await fixture.UpdateAsync(roleId: 2, add: [1], remove: [1]);

        Assert.Empty(result.AddedParentRoleIds);
        Assert.Empty(result.RemovedParentRoleIds);
        Assert.Empty(fixture.Deleted);
    }

    /// <summary>
    /// 变更后复核职责分离：本角色及其下级各自的继承链，以及持有它们的成员在本上下文的全部角色；阻断类违规抛出。
    /// </summary>
    [Fact]
    public async Task SeparationOfDuty_ShouldCheckAffectedRolesAndHolders()
    {
        var fixture = new Fixture().Roles(1, 2, 3, 9).Edge(2, 3)
            .Holder(userId: 50, roleId: 3).Holder(userId: 50, roleId: 9)
            .Holder(userId: 60, roleId: 3, bindingTenantId: 0)
            .BlockWhen(set => set.Contains(9));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(roleId: 2, add: [1]));

        Assert.Contains("用户主键 50", exception.Message, StringComparison.Ordinal);
        Assert.Equal(["2", "3", "3,9"], fixture.EvaluatedSets.Select(set => string.Join(",", set.Order())).Order());
    }

    /// <summary>
    /// 平台调整全局角色的继承：继承了它或持有它的租户逐个切入复核。
    /// </summary>
    [Fact]
    public async Task GlobalRoleChange_ShouldCheckAffectedTenants()
    {
        var fixture = new Fixture(currentTenantId: null).Role(1, tenantId: 0).Role(2, tenantId: 0)
            .InheritingTenants(7)
            .HoldingTenants(8, 7);

        _ = await fixture.UpdateAsync(roleId: 2, add: [1]);

        Assert.Equal([0L, 7, 8], fixture.EvaluatedTenants);
    }

    /// <summary>
    /// 有效上级跳过停用角色并切断经由它的继承。
    /// </summary>
    [Fact]
    public async Task EffectiveAncestors_ShouldSkipAndCutDisabledRoles()
    {
        var fixture = new Fixture().Roles(1, 3, 4).Role(2, status: EnableStatus.Disabled)
            .Edge(1, 2).Edge(2, 3).Edge(4, 3);

        var ancestors = await fixture.Service.GetEffectiveAncestorsAsync([3]);

        Assert.Equal([4L], ancestors[3].Keys);
    }

    private sealed class Fixture
    {
        private readonly List<SysRole> _roles = [];

        private readonly List<SysRoleHierarchy> _edges = [];

        private readonly List<SysUserRole> _bindings = [];

        private readonly TestCurrentTenant _currentTenant;

        private IReadOnlyList<long> _inheritingTenants = [];

        private IReadOnlyList<long> _holdingTenants = [];

        private Func<IReadOnlyCollection<long>, bool> _blocking = _ => false;

        private long _nextRowId = 1000;

        public Fixture(long? currentTenantId = TenantId)
        {
            _currentTenant = new TestCurrentTenant(currentTenantId);

            var roleRepository = new Mock<IRoleRepository>();
            roleRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _roles.FirstOrDefault(role => role.BasicId == id));
            roleRepository
                .Setup(repo => repo.GetEnabledByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) =>
                    [.. _roles.Where(role => ids.Contains(role.BasicId) && role.Status == EnableStatus.Enabled)]);

            var hierarchyRepository = new Mock<IRoleHierarchyRepository>();
            hierarchyRepository
                .Setup(repo => repo.GetEdgesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => [.. _edges]);
            hierarchyRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<Expression<Func<SysRoleHierarchy, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysRoleHierarchy, bool>> predicate, CancellationToken _) =>
                {
                    var matched = _edges.Where(predicate.Compile()).ToList();
                    Deleted.AddRange(matched.Select(edge => (edge.AncestorId, edge.DescendantId)));
                    _edges.RemoveAll(matched.Contains);
                    return true;
                });
            hierarchyRepository
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRoleHierarchy>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysRoleHierarchy> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    foreach (var edge in list)
                    {
                        SaasTestHelper.SetBasicId(edge, _nextRowId++);
                        _edges.Add(edge);
                        Added.Add((edge.AncestorId, edge.DescendantId));
                    }

                    return list.ToArray();
                });
            hierarchyRepository
                .Setup(repo => repo.GetTenantIdsInheritingAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _inheritingTenants);

            var userRoleRepository = new Mock<IUserRoleRepository>();
            userRoleRepository
                .Setup(repo => repo.GetValidByRoleIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<long> ids, DateTimeOffset _, CancellationToken _) => [.. _bindings.Where(binding => ids.Contains(binding.RoleId))]);
            userRoleRepository
                .Setup(repo => repo.GetValidByUserIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<long> ids, DateTimeOffset _, CancellationToken _) => [.. _bindings.Where(binding => ids.Contains(binding.UserId))]);
            userRoleRepository
                .Setup(repo => repo.GetValidTenantIdsByRoleIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _holdingTenants);

            var enforcement = new Mock<IConstraintRuleEnforcementDomainService>();
            enforcement
                .Setup(service => service.EvaluateRoleSetsAsync(It.IsAny<IReadOnlyList<IReadOnlyCollection<long>>>(), ConstraintType.SSD, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<IReadOnlyCollection<long>> sets, ConstraintType _, CancellationToken _) =>
                {
                    EvaluatedTenants.Add(_currentTenant.Id ?? 0);
                    EvaluatedSets.AddRange(sets);
                    return [.. sets.Select(set => _blocking(set)
                        ? new ConstraintEnforcementResult([new ConstraintViolation(1, "SSD-01", "出纳与会计", ConstraintType.SSD, 0, [.. set], ViolationAction.Deny)])
                        : ConstraintEnforcementResult.Pass)];
                });

            Service = new RoleHierarchyDomainService(
                hierarchyRepository.Object,
                roleRepository.Object,
                userRoleRepository.Object,
                enforcement.Object,
                _currentTenant,
                NullLogger<RoleHierarchyDomainService>.Instance);
        }

        public RoleHierarchyDomainService Service { get; }

        public List<(long AncestorId, long DescendantId)> Added { get; } = [];

        public List<(long AncestorId, long DescendantId)> Deleted { get; } = [];

        public List<IReadOnlyCollection<long>> EvaluatedSets { get; } = [];

        public List<long> EvaluatedTenants { get; } = [];

        public Fixture Roles(params long[] roleIds)
        {
            foreach (var roleId in roleIds)
            {
                _ = Role(roleId);
            }

            return this;
        }

        public Fixture Role(long roleId, long tenantId = TenantId, RoleType roleType = RoleType.Custom, EnableStatus status = EnableStatus.Enabled)
        {
            var role = new SysRole
            {
                TenantId = tenantId,
                RoleCode = $"ROLE-{roleId}",
                RoleName = $"角色{roleId}",
                RoleType = roleType,
                Status = status
            };
            SaasTestHelper.SetBasicId(role, roleId);
            _roles.Add(role);
            return this;
        }

        public Fixture Edge(long parentId, long childId)
        {
            var edge = new SysRoleHierarchy { TenantId = TenantId, AncestorId = parentId, DescendantId = childId };
            SaasTestHelper.SetBasicId(edge, _nextRowId++);
            _edges.Add(edge);
            return this;
        }

        public Fixture Holder(long userId, long roleId, long bindingTenantId = TenantId)
        {
            _bindings.Add(new SysUserRole { UserId = userId, RoleId = roleId, TenantId = bindingTenantId, Status = ValidityStatus.Valid });
            return this;
        }

        public Fixture BlockWhen(Func<IReadOnlyCollection<long>, bool> blocking)
        {
            _blocking = blocking;
            return this;
        }

        public Fixture InheritingTenants(params long[] tenantIds)
        {
            _inheritingTenants = tenantIds;
            return this;
        }

        public Fixture HoldingTenants(params long[] tenantIds)
        {
            _holdingTenants = tenantIds;
            return this;
        }

        public Task<RoleHierarchyBatchUpdateResult> UpdateAsync(long roleId, long[]? add = null, long[]? remove = null) =>
            Service.UpdateParentsAsync(new RoleHierarchyBatchUpdateCommand(roleId, add ?? [], remove ?? []));
    }
}
