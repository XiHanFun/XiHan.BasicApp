// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限委托领域服务测试：委托创建/撤销/状态变更的时间与主体约束。
/// </summary>
public sealed class PermissionDelegationDomainServiceTests
{
    /// <summary>
    /// 委托人与被委托人不能相同。
    /// </summary>
    [Fact]
    public async Task Create_WithSameDelegatorAndDelegatee_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(delegatorUserId: 1, delegateeUserId: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("委托人和被委托人不能相同", exception.Message, StringComparison.Ordinal);
        fixture.TenantUserRepository.Verify(
            repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 委托人主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidDelegatorId_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(delegatorUserId: 0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));
    }

    /// <summary>
    /// 失效时间不能为空（未填写任何失效时间时传入 default(DateTimeOffset)）。
    /// </summary>
    [Fact]
    public async Task Create_WithDefaultExpirationTime_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = new PermissionDelegationCreateCommand(
            1, 2, 101, null, null, default(DateTimeOffset), "出差临时授权", null);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("失效时间不能为空", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 失效时间必须晚于当前时间。
    /// </summary>
    [Fact]
    public async Task Create_WithExpirationInPast_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(expirationTime: DateTimeOffset.UtcNow.AddMinutes(-1));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("失效时间必须晚于当前时间", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 失效时间必须晚于生效时间。
    /// </summary>
    [Fact]
    public async Task Create_WithEffectiveAfterExpiration_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(
            effectiveTime: DateTimeOffset.UtcNow.AddDays(2),
            expirationTime: DateTimeOffset.UtcNow.AddDays(1));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("失效时间必须晚于生效时间", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 委托人必须是当前租户成员。
    /// </summary>
    [Fact]
    public async Task Create_WhenDelegatorNotMember_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.TenantUserRepository
            .Setup(repo => repo.GetMembershipAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantUser?)null);
        fixture.TenantUserRepository
            .Setup(repo => repo.GetMembershipAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember(2));
        var command = CreateCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("委托人不是当前租户成员", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用权限不能参与权限委托。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledPermission_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMembers();
        fixture.PermissionRepository
            .Setup(repo => repo.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission(101, EnableStatus.Disabled));
        var command = CreateCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("停用权限不能参与权限委托", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用角色不能参与权限委托。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledRole_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMembers();
        fixture.RoleRepository
            .Setup(repo => repo.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole(55, EnableStatus.Disabled));
        var command = CreateCommand(permissionId: null, roleId: 55);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("停用角色不能参与权限委托", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 相同委托人/被委托人/权限的委托已存在时必须拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateDelegation_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMembers();
        fixture.PermissionRepository
            .Setup(repo => repo.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission(101));
        fixture.DelegationRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysPermissionDelegation, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var command = CreateCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePermissionDelegationAsync(command));

        Assert.Contains("权限委托已存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 无生效时间（立即生效）的委托创建后状态为生效中。
    /// </summary>
    [Fact]
    public async Task Create_WithImmediateEffect_ShouldBeActive()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMembers();
        fixture.PermissionRepository
            .Setup(repo => repo.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission(101));
        var command = CreateCommand();

        var result = await fixture.Service.CreatePermissionDelegationAsync(command);

        Assert.Equal(400, result.DelegationId);
        Assert.True(result.IsActive);
        fixture.DelegationRepository.Verify(
            repo => repo.AddAsync(
                It.Is<SysPermissionDelegation>(delegation =>
                    delegation.DelegationStatus == DelegationStatus.Active
                    && delegation.DelegatorUserId == 1
                    && delegation.DelegateeUserId == 2
                    && delegation.PermissionId == 101),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 未来生效时间的委托创建后状态为待生效（仍视为授予方向）。
    /// </summary>
    [Fact]
    public async Task Create_WithFutureEffectiveTime_ShouldBePending()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMembers();
        fixture.PermissionRepository
            .Setup(repo => repo.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission(101));
        var command = CreateCommand(effectiveTime: DateTimeOffset.UtcNow.AddHours(2));

        var result = await fixture.Service.CreatePermissionDelegationAsync(command);

        Assert.True(result.IsActive);
        fixture.DelegationRepository.Verify(
            repo => repo.AddAsync(
                It.Is<SysPermissionDelegation>(delegation =>
                    delegation.DelegationStatus == DelegationStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 撤销委托后状态为已撤销，审计结果标记为收回方向。
    /// </summary>
    [Fact]
    public async Task Revoke_ShouldMarkRevokedAndInactive()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(400);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);
        fixture.DelegationRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysPermissionDelegation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPermissionDelegation entity, CancellationToken _) => entity);

        var result = await fixture.Service.RevokePermissionDelegationAsync(400);

        Assert.False(result.IsActive);
        Assert.Equal(2, result.DelegateeUserId);
        Assert.Equal(101, result.PermissionId);
        Assert.Equal(DelegationStatus.Revoked, delegation.DelegationStatus);
    }

    /// <summary>
    /// 已撤销的委托不能再次更新。
    /// </summary>
    [Fact]
    public async Task Update_OnRevokedDelegation_ShouldThrow()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(400, status: DelegationStatus.Revoked);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);
        var command = CreateUpdateCommand(400);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePermissionDelegationAsync(command));

        Assert.Contains("已撤销权限委托不能更新", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已撤销的委托不能重新生效。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_RevokedToActive_ShouldThrow()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(400, status: DelegationStatus.Revoked);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePermissionDelegationStatusAsync(
                new PermissionDelegationStatusCommand(400, DelegationStatus.Active, null)));

        Assert.Contains("已撤销权限委托不能重新生效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 待生效状态必须存在晚于当前时间的生效时间。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ToPendingWithoutFutureEffectiveTime_ShouldThrow()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(400, effectiveTime: null);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePermissionDelegationStatusAsync(
                new PermissionDelegationStatusCommand(400, DelegationStatus.Pending, null)));

        Assert.Contains("必须存在晚于当前时间的生效时间", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 生效中状态必须处于当前有效期内。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ToActiveWithExpiredPeriod_ShouldThrow()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(
            400,
            effectiveTime: null,
            expirationTime: DateTimeOffset.UtcNow.AddMinutes(-1),
            status: DelegationStatus.Expired);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePermissionDelegationStatusAsync(
                new PermissionDelegationStatusCommand(400, DelegationStatus.Active, null)));

        Assert.Contains("必须处于当前有效期内", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未到失效时间的委托不能标记为已过期。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ToExpiredBeforeExpirationTime_ShouldThrow()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(400, effectiveTime: null);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePermissionDelegationStatusAsync(
                new PermissionDelegationStatusCommand(400, DelegationStatus.Expired, null)));

        Assert.Contains("不能标记为已过期", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 撤销状态可以幂等重入。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_RevokedToRevoked_ShouldSucceed()
    {
        var fixture = CreateFixture();
        var delegation = CreateDelegation(400, status: DelegationStatus.Revoked);
        fixture.DelegationRepository
            .Setup(repo => repo.GetByIdAsync(400, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);
        fixture.DelegationRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysPermissionDelegation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPermissionDelegation entity, CancellationToken _) => entity);

        var result = await fixture.Service.UpdatePermissionDelegationStatusAsync(
            new PermissionDelegationStatusCommand(400, DelegationStatus.Revoked, null));

        Assert.False(result.IsActive);
    }

    /// <summary>
    /// 构造委托创建命令。
    /// </summary>
    private static PermissionDelegationCreateCommand CreateCommand(
        long delegatorUserId = 1,
        long delegateeUserId = 2,
        long? permissionId = 101,
        long? roleId = null,
        DateTimeOffset? effectiveTime = null,
        DateTimeOffset? expirationTime = null)
    {
        return new PermissionDelegationCreateCommand(
            delegatorUserId,
            delegateeUserId,
            permissionId,
            roleId,
            effectiveTime,
            expirationTime ?? DateTimeOffset.UtcNow.AddDays(1),
            "出差临时授权",
            null);
    }

    /// <summary>
    /// 构造委托更新命令。
    /// </summary>
    private static PermissionDelegationUpdateCommand CreateUpdateCommand(long basicId)
    {
        return new PermissionDelegationUpdateCommand(
            basicId,
            1,
            2,
            101,
            null,
            null,
            DateTimeOffset.UtcNow.AddDays(1),
            "续期",
            null);
    }

    /// <summary>
    /// 构造委托实体。
    /// </summary>
    private static SysPermissionDelegation CreateDelegation(
        long id,
        DateTimeOffset? effectiveTime = null,
        DateTimeOffset? expirationTime = null,
        DelegationStatus status = DelegationStatus.Active)
    {
        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = 1,
            DelegateeUserId = 2,
            PermissionId = 101,
            DelegationStatus = status,
            EffectiveTime = effectiveTime,
            ExpirationTime = expirationTime ?? DateTimeOffset.UtcNow.AddDays(1)
        };
        SaasTestHelper.SetBasicId(delegation, id);
        return delegation;
    }

    /// <summary>
    /// 构造有效租户成员。
    /// </summary>
    private static SysTenantUser CreateMember(long userId)
    {
        return new SysTenantUser
        {
            UserId = userId,
            MemberType = TenantMemberType.Member,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid
        };
    }

    /// <summary>
    /// 构造启用/停用权限。
    /// </summary>
    private static SysPermission CreatePermission(long id, EnableStatus status = EnableStatus.Enabled)
    {
        var permission = new SysPermission
        {
            TenantId = 7,
            PermissionCode = $"saas:res:{id}",
            PermissionName = $"权限{id}",
            Status = status
        };
        SaasTestHelper.SetBasicId(permission, id);
        return permission;
    }

    /// <summary>
    /// 构造启用/停用角色。
    /// </summary>
    private static SysRole CreateRole(long id, EnableStatus status = EnableStatus.Enabled)
    {
        var role = new SysRole
        {
            TenantId = 7,
            RoleCode = $"ROLE-{id}",
            RoleName = $"角色{id}",
            Status = status
        };
        SaasTestHelper.SetBasicId(role, id);
        return role;
    }

    /// <summary>
    /// 创建带仓储模拟的权限委托测试夹具。
    /// </summary>
    private static DelegationFixture CreateFixture()
    {
        var delegationRepository = new Mock<IPermissionDelegationRepository>();
        delegationRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysPermissionDelegation, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        delegationRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysPermissionDelegation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPermissionDelegation entity, CancellationToken _) =>
            {
                SaasTestHelper.SetBasicId(entity, 400);
                return entity;
            });

        var tenantUserRepository = new Mock<ITenantUserRepository>();
        var permissionRepository = new Mock<IPermissionRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns((long?)7);

        var service = new PermissionDelegationDomainService(
            delegationRepository.Object,
            tenantUserRepository.Object,
            permissionRepository.Object,
            roleRepository.Object,
            currentTenant.Object);
        return new DelegationFixture(service, delegationRepository, tenantUserRepository, permissionRepository, roleRepository);
    }

    /// <summary>
    /// 权限委托测试依赖集合。
    /// </summary>
    private sealed record DelegationFixture(
        PermissionDelegationDomainService Service,
        Mock<IPermissionDelegationRepository> DelegationRepository,
        Mock<ITenantUserRepository> TenantUserRepository,
        Mock<IPermissionRepository> PermissionRepository,
        Mock<IRoleRepository> RoleRepository)
    {
        /// <summary>
        /// 预设委托人与被委托人均为有效租户成员。
        /// </summary>
        public void SetupValidMembers()
        {
            TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateMember(1));
            TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateMember(2));
        }
    }
}
