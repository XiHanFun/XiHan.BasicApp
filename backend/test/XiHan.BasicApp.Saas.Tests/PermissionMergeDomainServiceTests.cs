// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限合并领域服务测试：角色授权、用户直授与权限委派三源合并为授权快照的契约。
/// </summary>
public sealed class PermissionMergeDomainServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 三源授权应全部合并，来源/操作/有效期逐项映射正确。
    /// </summary>
    [Fact]
    public async Task Merge_ShouldCombineAllThreeSources()
    {
        var rolePermission = new SysRolePermission
        {
            RoleId = 10,
            PermissionId = 101,
            PermissionAction = PermissionAction.Grant,
            EffectiveTime = Now.AddHours(-1),
            ExpirationTime = Now.AddHours(1)
        };
        var userPermission = new SysUserPermission
        {
            UserId = 1,
            PermissionId = 102,
            PermissionAction = PermissionAction.Deny
        };
        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = 2,
            DelegateeUserId = 1,
            PermissionId = 103,
            EffectiveTime = Now.AddHours(-2),
            ExpirationTime = Now.AddHours(2)
        };
        var permission101 = CreatePermission(101, "saas:user:read", priority: 7);
        var permission102 = CreatePermission(102, "saas:user:create");
        var permission103 = CreatePermission(103, "saas:user:update");

        var fixture = CreateFixture(
            rolePermissions: [rolePermission],
            userPermissions: [userPermission],
            delegations: [delegation],
            permissions: [permission101, permission102, permission103]);

        var grants = await fixture.Service.MergePermissionGrantsAsync(
            userId: 1,
            roleIds: [10],
            now: Now);

        Assert.Equal(3, grants.Count);

        var roleGrant = Assert.Single(grants, grant => grant.Source == AuthorizationGrantSource.Role);
        Assert.Equal(101, roleGrant.PermissionId);
        Assert.Equal(PermissionAction.Grant, roleGrant.Action);
        Assert.Equal(7, roleGrant.Priority);
        Assert.True(roleGrant.Period.IsActive(Now));

        var userGrant = Assert.Single(grants, grant => grant.Source == AuthorizationGrantSource.User);
        Assert.Equal(102, userGrant.PermissionId);
        Assert.Equal(PermissionAction.Deny, userGrant.Action);

        var delegationGrant = Assert.Single(grants, grant => grant.Source == AuthorizationGrantSource.Delegation);
        Assert.Equal(103, delegationGrant.PermissionId);
        Assert.Equal(PermissionAction.Grant, delegationGrant.Action);
        Assert.Equal("saas:user:update", delegationGrant.PermissionCode);
    }

    /// <summary>
    /// 关联授权指向的权限实体缺失时跳过该授权而非抛异常。
    /// </summary>
    [Fact]
    public async Task Merge_ShouldSkipGrantsWhosePermissionIsMissing()
    {
        var rolePermission = new SysRolePermission { RoleId = 10, PermissionId = 999 };
        var userPermission = new SysUserPermission { UserId = 1, PermissionId = 102 };
        var permission102 = CreatePermission(102, "saas:user:create");

        var fixture = CreateFixture(
            rolePermissions: [rolePermission],
            userPermissions: [userPermission],
            delegations: [],
            permissions: [permission102]);

        var grants = await fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [10], now: Now);

        var grant = Assert.Single(grants);
        Assert.Equal(102, grant.PermissionId);
    }

    /// <summary>
    /// 未指定权限的委托（RoleId 形态的委托）不产生权限快照。
    /// </summary>
    [Fact]
    public async Task Merge_ShouldSkipDelegationsWithoutPermissionId()
    {
        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = 2,
            DelegateeUserId = 1,
            PermissionId = null,
            RoleId = 5,
            EffectiveTime = Now.AddHours(-1),
            ExpirationTime = Now.AddHours(1)
        };

        var fixture = CreateFixture(rolePermissions: [], userPermissions: [], delegations: [delegation], permissions: []);

        var grants = await fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [], now: Now);

        Assert.Empty(grants);
    }

    /// <summary>
    /// 停用权限合并后快照 IsEnabled 必须为 false（裁决阶段据此过滤）。
    /// </summary>
    [Fact]
    public async Task Merge_ShouldReflectPermissionStatus()
    {
        var userPermission = new SysUserPermission { UserId = 1, PermissionId = 102 };
        var disabledPermission = CreatePermission(102, "saas:user:create", enabled: false);

        var fixture = CreateFixture(rolePermissions: [], userPermissions: [userPermission], delegations: [], permissions: [disabledPermission]);

        var grants = await fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [], now: Now);

        var grant = Assert.Single(grants);
        Assert.False(grant.IsEnabled);
    }

    /// <summary>
    /// 无任何授权时返回空集合且不做批量权限查询。
    /// </summary>
    [Fact]
    public async Task Merge_WithoutAnyGrants_ShouldReturnEmpty()
    {
        var fixture = CreateFixture(rolePermissions: [], userPermissions: [], delegations: [], permissions: []);

        var grants = await fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [], now: Now);

        Assert.Empty(grants);
        fixture.PermissionRepository.Verify(
            repo => repo.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 角色集合为空时不得查询角色授权（避免无效查询），用户直授仍应合并。
    /// </summary>
    [Fact]
    public async Task Merge_WithEmptyRoleIds_ShouldNotQueryRolePermissions()
    {
        var userPermission = new SysUserPermission { UserId = 1, PermissionId = 102 };
        var permission102 = CreatePermission(102, "saas:user:create");

        var fixture = CreateFixture(rolePermissions: [], userPermissions: [userPermission], delegations: [], permissions: [permission102]);

        var grants = await fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [], now: Now);

        var grant = Assert.Single(grants);
        Assert.Equal(102, grant.PermissionId);
        fixture.RolePermissionRepository.Verify(
            repo => repo.GetValidByRoleIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 快照有效期应取自授权关联实体自身（角色授权取角色授权的期限）。
    /// </summary>
    [Fact]
    public async Task Merge_ShouldUseGrantPeriods()
    {
        var rolePermission = new SysRolePermission
        {
            RoleId = 10,
            PermissionId = 101,
            EffectiveTime = Now.AddHours(-5),
            ExpirationTime = Now.AddHours(5)
        };
        var permission101 = CreatePermission(101, "saas:user:read");

        var fixture = CreateFixture(rolePermissions: [rolePermission], userPermissions: [], delegations: [], permissions: [permission101]);

        var grants = await fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [10], now: Now);

        var grant = Assert.Single(grants);
        Assert.Equal(Now.AddHours(-5), grant.Period.EffectiveTime);
        Assert.Equal(Now.AddHours(5), grant.Period.ExpirationTime);
    }

    /// <summary>
    /// 已取消令牌必须立即抛出。
    /// </summary>
    [Fact]
    public async Task Merge_Cancelled_ShouldThrow()
    {
        var fixture = CreateFixture(rolePermissions: [], userPermissions: [], delegations: [], permissions: []);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.MergePermissionGrantsAsync(userId: 1, roleIds: [], now: Now, cts.Token));
    }

    /// <summary>
    /// 创建带仓储模拟的权限合并测试夹具。
    /// </summary>
    private static MergeFixture CreateFixture(
        IReadOnlyList<SysRolePermission> rolePermissions,
        IReadOnlyList<SysUserPermission> userPermissions,
        IReadOnlyList<SysPermissionDelegation> delegations,
        IReadOnlyList<SysPermission> permissions)
    {
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        rolePermissionRepository
            .Setup(repo => repo.GetValidByRoleIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissions);

        var userPermissionRepository = new Mock<IUserPermissionRepository>();
        userPermissionRepository
            .Setup(repo => repo.GetValidByUserIdAsync(
                It.IsAny<long>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        var delegationRepository = new Mock<IPermissionDelegationRepository>();
        delegationRepository
            .Setup(repo => repo.GetActiveByDelegateeIdAsync(
                It.IsAny<long>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegations);

        var permissionRepository = new Mock<IPermissionRepository>();
        permissionRepository
            .Setup(repo => repo.GetByIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var service = new PermissionMergeDomainService(
            rolePermissionRepository.Object,
            userPermissionRepository.Object,
            delegationRepository.Object,
            permissionRepository.Object);
        return new MergeFixture(service, rolePermissionRepository, permissionRepository);
    }

    /// <summary>
    /// 创建启用状态的权限实体。
    /// </summary>
    private static SysPermission CreatePermission(long id, string code, int priority = 0, bool enabled = true)
    {
        var permission = new SysPermission
        {
            PermissionCode = code,
            PermissionName = code,
            Priority = priority,
            Status = enabled ? EnableStatus.Enabled : EnableStatus.Disabled
        };
        SaasTestHelper.SetBasicId(permission, id);
        return permission;
    }

    /// <summary>
    /// 权限合并测试依赖集合。
    /// </summary>
    private sealed record MergeFixture(
        PermissionMergeDomainService Service,
        Mock<IRolePermissionRepository> RolePermissionRepository,
        Mock<IPermissionRepository> PermissionRepository);
}
