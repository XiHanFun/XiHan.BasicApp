// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 账号域边界：身份类操作只对当前上下文注册的账号；平台建的是平台账号（无成员关系、无席位）；
/// 删除 / 停用是账号级操作，要跨租户核对所有者，删除时逐租户作废成员关系。
/// </summary>
public sealed class UserAccountBoundaryTests
{
    private const long TenantId = 7;
    private const long OtherTenantId = 9;
    private const long UserId = 101;

    /// <summary>
    /// 平台里建账号：不查席位、不写成员关系，账号与安全记录照常写入
    /// </summary>
    [Fact]
    public async Task CreateUser_InPlatform_WritesNoMembershipAndSkipsSeatQuota()
    {
        var fixture = new Fixture(tenantId: null);

        _ = await fixture.Service.CreateUserAsync(CreateCommand());

        fixture.Quota.Verify(service => service.EnsureSeatQuotaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.TenantUsers.Verify(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Securities.Verify(repo => repo.AddAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 租户里建账号：占一个席位并成为本租户已接受的成员
    /// </summary>
    [Fact]
    public async Task CreateUser_InTenant_ChecksSeatAndWritesAcceptedMembership()
    {
        var fixture = new Fixture(TenantId);
        SysTenantUser? membership = null;
        _ = fixture.TenantUsers
            .Setup(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
            .Callback<SysTenantUser, CancellationToken>((entity, _) => membership = entity)
            .ReturnsAsync((SysTenantUser entity, CancellationToken _) => entity);

        _ = await fixture.Service.CreateUserAsync(CreateCommand());

        fixture.Quota.Verify(service => service.EnsureSeatQuotaAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(membership);
        Assert.Equal(UserId, membership!.UserId);
        Assert.Equal(TenantMemberInviteStatus.Accepted, membership.InviteStatus);
    }

    /// <summary>
    /// 外部成员的账号取不到（账号严格隔离）：身份类操作明确拒绝并说明原因，不落任何写入
    /// </summary>
    [Fact]
    public async Task IdentityOperations_OnExternalMember_AreRejectedWithReason()
    {
        var fixture = new Fixture(TenantId);
        _ = fixture.TenantUsers
            .Setup(repo => repo.GetMembershipAsync(TenantId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Membership(TenantId, TenantMemberType.Member));

        var operations = new Func<Task>[]
        {
            () => fixture.Service.UpdateUserStatusAsync(new UserStatusChangeCommand(UserId, EnableStatus.Disabled, null)),
            () => fixture.Service.DeleteUserAsync(UserId),
            () => fixture.Service.UpdateUserLockAsync(new UserLockChangeCommand(UserId, true, null, null)),
            () => fixture.Service.ResetUserPasswordAsync(new UserPasswordResetCommand(UserId, "Reset@Probe123", null, null))
        };

        foreach (var operation in operations)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(operation);
            Assert.Contains("外部成员", exception.Message, StringComparison.Ordinal);
        }

        fixture.Users.Verify(repo => repo.UpdateAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Securities.Verify(repo => repo.UpdateAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 既不是本地账号也不是本租户成员：按不存在处理
    /// </summary>
    [Fact]
    public async Task IdentityOperations_OnUnknownUser_ReportNotFound()
    {
        var fixture = new Fixture(TenantId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteUserAsync(UserId));

        Assert.Equal("用户不存在。", exception.Message);
    }

    /// <summary>
    /// 删除账号：它在每个租户的有效成员关系都作废，且写入时切入该成员关系所在租户；已作废的不重复写
    /// </summary>
    [Fact]
    public async Task DeleteUser_RevokesMembershipsInEachTenant()
    {
        var fixture = new Fixture(TenantId);
        fixture.GivenHomeAccount();
        var home = Membership(TenantId, TenantMemberType.Member);
        var external = Membership(OtherTenantId, TenantMemberType.Member);
        var revoked = Membership(11, TenantMemberType.Member);
        revoked.InviteStatus = TenantMemberInviteStatus.Revoked;
        revoked.Status = ValidityStatus.Invalid;
        fixture.GivenMemberships(home, external, revoked);
        var writes = new List<(long MembershipTenant, long? ContextTenant)>();
        _ = fixture.TenantUsers
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
            .Callback<SysTenantUser, CancellationToken>((entity, _) => writes.Add((entity.TenantId, fixture.CurrentTenant.Id)))
            .ReturnsAsync((SysTenantUser entity, CancellationToken _) => entity);

        await fixture.Service.DeleteUserAsync(UserId);

        Assert.Equal([(TenantId, (long?)TenantId), (OtherTenantId, (long?)OtherTenantId)], writes);
        Assert.All([home, external], membership =>
        {
            Assert.Equal(TenantMemberInviteStatus.Revoked, membership.InviteStatus);
            Assert.Equal(ValidityStatus.Invalid, membership.Status);
        });
        Assert.Equal(TenantId, fixture.CurrentTenant.Id);
        fixture.Users.Verify(repo => repo.SoftDeleteAsync(It.Is<SysUser>(user => user.BasicId == UserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 账号是任何一个租户的有效所有者：不能删除，也不能停用
    /// </summary>
    [Fact]
    public async Task DeleteOrDisable_OwnerOfAnotherTenant_IsRejected()
    {
        var fixture = new Fixture(TenantId);
        fixture.GivenHomeAccount();
        fixture.GivenMemberships(Membership(TenantId, TenantMemberType.Member), Membership(OtherTenantId, TenantMemberType.Owner));

        var delete = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteUserAsync(UserId));
        var disable = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateUserStatusAsync(new UserStatusChangeCommand(UserId, EnableStatus.Disabled, null)));

        Assert.Contains("所有者", delete.Message, StringComparison.Ordinal);
        Assert.Contains("所有者", disable.Message, StringComparison.Ordinal);
        fixture.TenantUsers.Verify(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Users.Verify(repo => repo.UpdateAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 已撤销的所有者关系不再挡删除
    /// </summary>
    [Fact]
    public async Task DeleteUser_RevokedOwnership_DoesNotBlock()
    {
        var fixture = new Fixture(TenantId);
        fixture.GivenHomeAccount();
        var formerOwner = Membership(OtherTenantId, TenantMemberType.Owner);
        formerOwner.InviteStatus = TenantMemberInviteStatus.Revoked;
        formerOwner.Status = ValidityStatus.Invalid;
        fixture.GivenMemberships(formerOwner);

        await fixture.Service.DeleteUserAsync(UserId);

        fixture.Users.Verify(repo => repo.SoftDeleteAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SysTenantUser Membership(long tenantId, TenantMemberType memberType)
    {
        return new SysTenantUser
        {
            TenantId = tenantId,
            UserId = UserId,
            MemberType = memberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid
        };
    }

    private static UserCreateCommand CreateCommand()
    {
        return new UserCreateCommand(
            UserName: "boundary_probe",
            InitialPassword: "Boundary@Probe123",
            RealName: null,
            NickName: null,
            Avatar: null,
            Email: "boundary_probe@example.com",
            Phone: null,
            Gender: UserGender.Unknown,
            Birthday: null,
            Status: EnableStatus.Enabled,
            Country: null,
            MemberType: TenantMemberType.Member,
            EffectiveTime: null,
            ExpirationTime: null,
            DisplayName: null,
            InviteRemark: null,
            Remark: null,
            OperatorUserId: null);
    }

    /// <summary>
    /// 被测服务与依赖替身
    /// </summary>
    private sealed class Fixture
    {
        public Fixture(long? tenantId)
        {
            CurrentTenant = new TestCurrentTenant(tenantId);
            _ = Users
                .Setup(repo => repo.ExistsUserNameAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _ = Users
                .Setup(repo => repo.ExistsEmailGloballyAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _ = Users
                .Setup(repo => repo.AddAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysUser user, CancellationToken _) =>
                {
                    SaasTestHelper.SetBasicId(user, UserId);
                    return user;
                });
            _ = TenantUsers
                .Setup(repo => repo.GetAllByUserIdIgnoreTenantAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var authentication = new Mock<IAuthenticationService>();
            _ = authentication
                .Setup(service => service.ValidatePasswordStrengthAsync(It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(new PasswordValidationResult { IsValid = true });

            Service = new UserDomainService(
                Users.Object,
                Securities.Object,
                TenantUsers.Object,
                new Mock<IPasswordHasher>().Object,
                authentication.Object,
                new Mock<IUserRoleRepository>().Object,
                new Mock<IRoleRepository>().Object,
                new Mock<IUserPermissionRepository>().Object,
                new Mock<IPermissionRepository>().Object,
                new Mock<IUserDataScopeRepository>().Object,
                new Mock<IDepartmentRepository>().Object,
                new Mock<IUserDepartmentRepository>().Object,
                new Mock<IUserSessionRepository>().Object,
                CurrentTenant,
                new Mock<IPasswordHistoryDomainService>().Object,
                new Mock<IConstraintRuleEnforcementDomainService>().Object,
                Quota.Object,
                NullLogger<UserDomainService>.Instance);
        }

        public UserDomainService Service { get; }

        public TestCurrentTenant CurrentTenant { get; }

        public Mock<IUserRepository> Users { get; } = new();

        public Mock<IUserSecurityRepository> Securities { get; } = new();

        public Mock<ITenantUserRepository> TenantUsers { get; } = new();

        public Mock<ITenantQuotaDomainService> Quota { get; } = new();

        /// <summary>
        /// 账号注册在当前上下文（严格隔离下取得到）
        /// </summary>
        public void GivenHomeAccount()
        {
            var user = new SysUser { TenantId = CurrentTenant.Id ?? 0, UserName = "boundary_probe" };
            SaasTestHelper.SetBasicId(user, UserId);
            _ = Users.Setup(repo => repo.GetByIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _ = Securities
                .Setup(repo => repo.GetFirstAsync(It.IsAny<Expression<Func<SysUserSecurity, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysUserSecurity?)null);
        }

        /// <summary>
        /// 账号在各租户的成员关系
        /// </summary>
        public void GivenMemberships(params SysTenantUser[] memberships)
        {
            _ = TenantUsers
                .Setup(repo => repo.GetAllByUserIdIgnoreTenantAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberships);
        }
    }
}
