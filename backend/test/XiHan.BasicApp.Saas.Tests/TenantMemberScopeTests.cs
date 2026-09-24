// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 成员关系只在所属租户内维护：租户接口作用于当前租户并校验席位；平台只做支持人员入驻与离场、所有权转移。
/// </summary>
public sealed class TenantMemberScopeTests
{
    private const long TenantId = 7;

    private const long OtherTenantId = 8;

    #region 租户侧

    /// <summary>
    /// 平台上下文不能维护租户成员（旧接口按请求里的租户切过去写，租户管理员能往别的租户加人）
    /// </summary>
    [Fact]
    public async Task AddMember_InPlatformContext_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.AddTenantMemberAsync(Command(userId: 10)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 成员加入当前租户，立即生效的占用席位
    /// </summary>
    [Fact]
    public async Task AddMember_Accepted_JoinsCurrentTenantAndChecksSeat()
    {
        var fixture = new Fixture(currentTenantId: TenantId);

        var result = await fixture.Service.AddTenantMemberAsync(Command(userId: 10));

        Assert.Equal(TenantId, result.Member.TenantId);
        fixture.Quota.Verify(quota => quota.EnsureSeatQuotaAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 待接受的邀请还不占席位
    /// </summary>
    [Fact]
    public async Task InviteMember_Pending_DoesNotCheckSeat()
    {
        var fixture = new Fixture(currentTenantId: TenantId);

        _ = await fixture.Service.AddTenantMemberAsync(Command(userId: 10, requiresInvitation: true));

        fixture.Quota.Verify(quota => quota.EnsureSeatQuotaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 席位满时拒绝，不写入
    /// </summary>
    [Fact]
    public async Task AddMember_WhenSeatsExhausted_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        fixture.Quota
            .Setup(quota => quota.EnsureSeatQuotaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("席位已满"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.AddTenantMemberAsync(Command(userId: 10)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 租户不能添加支持人员
    /// </summary>
    [Fact]
    public async Task AddMember_AsPlatformAdmin_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddTenantMemberAsync(Command(userId: 10, memberType: TenantMemberType.PlatformAdmin)));
    }

    /// <summary>
    /// 成员关系严格隔离：别的租户的成员在本租户里取不到，视为不存在
    /// </summary>
    [Fact]
    public async Task UpdateStatus_OnOtherTenantMember_IsNotFound()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        fixture.AddMember(100, OtherTenantId, TenantMemberType.Member);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateTenantMemberStatusAsync(new TenantMemberStatusChangeCommand(100, ValidityStatus.Invalid, null)));

        Assert.Contains("不存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用的成员恢复有效时重新占用席位
    /// </summary>
    [Fact]
    public async Task UpdateStatus_Reactivate_ChecksSeat()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        var member = fixture.AddMember(100, TenantId, TenantMemberType.Member);
        member.Status = ValidityStatus.Invalid;

        _ = await fixture.Service.UpdateTenantMemberStatusAsync(new TenantMemberStatusChangeCommand(100, ValidityStatus.Valid, null));

        fixture.Quota.Verify(quota => quota.EnsureSeatQuotaAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 支持人员由平台维护：租户改不了，但能移除其访问
    /// </summary>
    [Fact]
    public async Task SupportMember_TenantCanRevokeButNotModify()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        var member = fixture.AddMember(100, TenantId, TenantMemberType.PlatformAdmin);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateTenantMemberStatusAsync(new TenantMemberStatusChangeCommand(100, ValidityStatus.Invalid, null)));

        await fixture.Service.DeleteTenantMemberAsync(100);

        Assert.Equal(TenantMemberInviteStatus.Revoked, member.InviteStatus);
        Assert.Equal(ValidityStatus.Invalid, member.Status);
    }

    /// <summary>
    /// 租户不能直接指派所有者：所有者只由开通与所有权转移产生
    /// </summary>
    [Fact]
    public async Task AddMember_AsOwner_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddTenantMemberAsync(Command(userId: 10, memberType: TenantMemberType.Owner)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 租户不能把成员改成所有者（否则一个租户会有多名所有者）
    /// </summary>
    [Fact]
    public async Task UpdateMember_PromoteToOwner_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        fixture.AddMember(101, TenantId, TenantMemberType.Member);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateTenantMemberAsync(new TenantMemberUpdateCommand(101, TenantMemberType.Owner, null, null, null, null, null)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 所有者关系始终有效：不能给它设失效时间，否则到期后租户就没有所有者了
    /// </summary>
    [Fact]
    public async Task UpdateMember_OwnerWithExpiration_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        fixture.AddMember(100, TenantId, TenantMemberType.Owner);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateTenantMemberAsync(new TenantMemberUpdateCommand(
                100, TenantMemberType.Owner, null, DateTimeOffset.UtcNow.AddDays(1), null, null, null)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 所有者关系不能退回待接受或拒绝（旧口径只拦了撤销和过期）
    /// </summary>
    [Theory]
    [InlineData(TenantMemberInviteStatus.Pending)]
    [InlineData(TenantMemberInviteStatus.Rejected)]
    [InlineData(TenantMemberInviteStatus.Revoked)]
    public async Task UpdateInviteStatus_Owner_StaysAccepted(TenantMemberInviteStatus inviteStatus)
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        fixture.AddMember(100, TenantId, TenantMemberType.Owner);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateTenantMemberInviteStatusAsync(new TenantMemberInviteStatusChangeCommand(100, inviteStatus, null)));
        fixture.VerifyNothingWritten();
    }

    #endregion

    #region 平台侧

    /// <summary>
    /// 支持人员入驻只在平台执行
    /// </summary>
    [Fact]
    public async Task AddSupportMember_InTenantContext_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.AddTenantSupportMemberAsync(SupportCommand(userId: 1)));
    }

    /// <summary>
    /// 只有平台账号能作为支持人员入驻
    /// </summary>
    [Fact]
    public async Task AddSupportMember_WithTenantAccount_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.AddTenantSupportMemberAsync(SupportCommand(userId: 10)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 平台账号以支持成员身份加入目标租户：成员行属于目标租户，不占席位
    /// </summary>
    [Fact]
    public async Task AddSupportMember_PlatformAccount_JoinsTargetTenant()
    {
        var fixture = new Fixture(currentTenantId: null);

        var result = await fixture.Service.AddTenantSupportMemberAsync(SupportCommand(userId: 1));

        Assert.Equal(TenantId, result.Member.TenantId);
        Assert.Equal(TenantMemberType.PlatformAdmin, result.Member.MemberType);
        Assert.Equal(TenantId, fixture.TenantIdWhenWritten);
        fixture.Quota.Verify(quota => quota.EnsureSeatQuotaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 平台只能移除支持人员，租户自己的成员由租户维护
    /// </summary>
    [Fact]
    public async Task RemoveSupportMember_OnRegularMember_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: null);
        fixture.AddMember(100, TenantId, TenantMemberType.Member);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.RemoveTenantSupportMemberAsync(TenantId, 100));
    }

    /// <summary>
    /// 所有权转移只在平台执行
    /// </summary>
    [Fact]
    public async Task TransferOwner_InTenantContext_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: TenantId);
        fixture.AddMember(100, TenantId, TenantMemberType.Owner);
        fixture.AddMember(101, TenantId, TenantMemberType.Member);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.TransferTenantOwnerAsync(new TenantOwnerTransferCommand(TenantId, 101)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 所有者身份与所有者角色一起移交：接任者成为所有者并拿到角色、期限清空；卸任者改为管理员，这条角色绑定失效。
    /// 全部在目标租户作用域里写
    /// </summary>
    [Fact]
    public async Task TransferOwner_MovesMembershipAndOwnerRole()
    {
        var fixture = new Fixture(currentTenantId: null);
        var previous = fixture.AddMember(100, TenantId, TenantMemberType.Owner);
        var next = fixture.AddMember(101, TenantId, TenantMemberType.Member);
        next.ExpirationTime = DateTimeOffset.UtcNow.AddDays(30);
        var previousBinding = fixture.AddOwnerRoleBinding(previous.UserId);

        var result = await fixture.Service.TransferTenantOwnerAsync(new TenantOwnerTransferCommand(TenantId, 101));

        Assert.Same(previous, result.PreviousOwner);
        Assert.Same(next, result.NewOwner);
        Assert.Equal(TenantMemberType.Admin, previous.MemberType);
        Assert.Equal(TenantMemberType.Owner, next.MemberType);
        Assert.Null(next.ExpirationTime);
        Assert.Equal(ValidityStatus.Invalid, previousBinding.Status);
        var incoming = Assert.Single(fixture.AddedUserRoles);
        Assert.Equal(next.UserId, incoming.UserId);
        Assert.Equal(Fixture.OwnerRoleId, incoming.RoleId);
        Assert.Equal(ValidityStatus.Valid, incoming.Status);
        Assert.Equal([TenantId], fixture.TenantIdsWhenRolesWritten.Distinct());
        Assert.Null(fixture.CurrentTenant.Id);
    }

    /// <summary>
    /// 支持人员、未接受邀请或已停用的成员不能接任所有者
    /// </summary>
    [Theory]
    [InlineData(TenantMemberType.PlatformAdmin, TenantMemberInviteStatus.Accepted, ValidityStatus.Valid)]
    [InlineData(TenantMemberType.Member, TenantMemberInviteStatus.Pending, ValidityStatus.Valid)]
    [InlineData(TenantMemberType.Member, TenantMemberInviteStatus.Accepted, ValidityStatus.Invalid)]
    public async Task TransferOwner_ToIneligibleMember_IsRejected(TenantMemberType memberType, TenantMemberInviteStatus inviteStatus, ValidityStatus status)
    {
        var fixture = new Fixture(currentTenantId: null);
        fixture.AddMember(100, TenantId, TenantMemberType.Owner);
        var candidate = fixture.AddMember(101, TenantId, memberType);
        candidate.InviteStatus = inviteStatus;
        candidate.Status = status;
        fixture.AddOwnerRoleBinding(1100);

        await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Service.TransferTenantOwnerAsync(new TenantOwnerTransferCommand(TenantId, 101)));
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 还没有所有者的租户走初始化管理员，不走转移
    /// </summary>
    [Fact]
    public async Task TransferOwner_WithoutOwner_IsRejected()
    {
        var fixture = new Fixture(currentTenantId: null);
        fixture.AddMember(101, TenantId, TenantMemberType.Member);

        await Assert.ThrowsAnyAsync<Exception>(
            () => fixture.Service.TransferTenantOwnerAsync(new TenantOwnerTransferCommand(TenantId, 101)));
        fixture.VerifyNothingWritten();
    }

    #endregion

    private static TenantMemberAddCommand Command(long userId, bool requiresInvitation = false, TenantMemberType memberType = TenantMemberType.Member) =>
        new(userId, memberType, null, null, null, null, null, requiresInvitation, 1);

    private static TenantSupportMemberAddCommand SupportCommand(long userId) =>
        new(TenantId, userId, null, null, "入驻排障", 1);

    /// <summary>
    /// 成员领域服务夹具
    /// </summary>
    private sealed class Fixture
    {
        public const long OwnerRoleId = 500;

        private readonly List<SysTenantUser> _members = [];

        private readonly List<SysUserRole> _userRoles = [];

        public Fixture(long? currentTenantId)
        {
            CurrentTenant = new TestCurrentTenant(currentTenantId);

            var tenant = new SysTenant { TenantCode = "t7", TenantName = "租户7" };
            SaasTestHelper.SetBasicId(tenant, TenantId);
            var tenantRepository = new Mock<ITenantRepository>();
            tenantRepository
                .Setup(repo => repo.GetByIdAsync(TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);

            // 用户 1 是平台账号，用户 10 是租户账号
            var platformAccount = new SysUser { UserName = "ops", TenantId = 0 };
            SaasTestHelper.SetBasicId(platformAccount, 1);
            var tenantAccount = new SysUser { UserName = "u10", TenantId = OtherTenantId };
            SaasTestHelper.SetBasicId(tenantAccount, 10);
            var userRepository = new Mock<IUserRepository>();
            userRepository.Setup(repo => repo.GetByIdIgnoreTenantAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(platformAccount);
            userRepository.Setup(repo => repo.GetByIdIgnoreTenantAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(tenantAccount);

            MemberRepository
                .Setup(repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long tenantId, long userId, CancellationToken _) =>
                    _members.FirstOrDefault(member => member.TenantId == tenantId && member.UserId == userId));
            MemberRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                // 与仓储的严格租户过滤同口径：只取得到当前上下文的行
                .ReturnsAsync((long id, CancellationToken _) => _members.FirstOrDefault(member =>
                    member.BasicId == id && member.TenantId == (CurrentTenant.Id ?? 0)));
            MemberRepository
                .Setup(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysTenantUser member, CancellationToken _) =>
                {
                    TenantIdWhenWritten = CurrentTenant.Id;
                    return member;
                });
            MemberRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysTenantUser member, CancellationToken _) => member);
            MemberRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysTenantUser, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysTenantUser, bool>> predicate, CancellationToken _) =>
                    _members.Where(member => member.TenantId == (CurrentTenant.Id ?? 0)).Where(predicate.Compile()).ToList());
            MemberRepository
                .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysTenantUser>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysTenantUser> members, CancellationToken _) => members.ToList());

            // 本租户的所有者角色（系统角色）；角色与绑定都按当前作用域取
            var ownerRole = SysRole.CreateTenantOwnerRole();
            ownerRole.TenantId = TenantId;
            SaasTestHelper.SetBasicId(ownerRole, OwnerRoleId);
            RoleRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysRole, bool>> predicate, CancellationToken _) =>
                    new[] { ownerRole }.Where(role => role.TenantId == (CurrentTenant.Id ?? 0)).Where(predicate.Compile()).ToList());
            UserRoleRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysUserRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysUserRole, bool>> predicate, CancellationToken _) =>
                    _userRoles.Where(userRole => userRole.TenantId == (CurrentTenant.Id ?? 0)).Where(predicate.Compile()).ToList());
            UserRoleRepository
                .Setup(repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysUserRole userRole, CancellationToken _) =>
                {
                    TenantIdsWhenRolesWritten.Add(CurrentTenant.Id);
                    AddedUserRoles.Add(userRole);
                    return userRole;
                });
            UserRoleRepository
                .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserRole>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserRole> userRoles, CancellationToken _) =>
                {
                    TenantIdsWhenRolesWritten.Add(CurrentTenant.Id);
                    return userRoles.ToList();
                });

            Service = new TenantDomainService(
                tenantRepository.Object,
                MemberRepository.Object,
                userRepository.Object,
                RoleRepository.Object,
                UserRoleRepository.Object,
                new Mock<ITenantProvisionDomainService>().Object,
                Quota.Object,
                CurrentTenant,
                new Mock<ITenantConnectionSecretProtector>().Object,
                new Mock<ITenantConnectionCacheInvalidator>().Object);
        }

        public TenantDomainService Service { get; }

        public TestCurrentTenant CurrentTenant { get; }

        public Mock<ITenantUserRepository> MemberRepository { get; } = new();

        public Mock<ITenantQuotaDomainService> Quota { get; } = new();

        public Mock<IRoleRepository> RoleRepository { get; } = new();

        public Mock<IUserRoleRepository> UserRoleRepository { get; } = new();

        public List<SysUserRole> AddedUserRoles { get; } = [];

        public List<long?> TenantIdsWhenRolesWritten { get; } = [];

        public long? TenantIdWhenWritten { get; private set; }

        public SysTenantUser AddMember(long basicId, long tenantId, TenantMemberType memberType)
        {
            var member = new SysTenantUser
            {
                TenantId = tenantId,
                UserId = basicId + 1000,
                MemberType = memberType,
                InviteStatus = TenantMemberInviteStatus.Accepted,
                Status = ValidityStatus.Valid
            };
            SaasTestHelper.SetBasicId(member, basicId);
            _members.Add(member);
            return member;
        }

        /// <summary>
        /// 给用户挂上本租户所有者角色的有效绑定
        /// </summary>
        public SysUserRole AddOwnerRoleBinding(long userId)
        {
            var binding = new SysUserRole
            {
                TenantId = TenantId,
                UserId = userId,
                RoleId = OwnerRoleId,
                Status = ValidityStatus.Valid
            };
            SaasTestHelper.SetBasicId(binding, 9000 + userId);
            _userRoles.Add(binding);
            return binding;
        }

        public void VerifyNothingWritten()
        {
            MemberRepository.Verify(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
            MemberRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
            MemberRepository.Verify(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysTenantUser>>(), It.IsAny<CancellationToken>()), Times.Never);
            UserRoleRepository.Verify(repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()), Times.Never);
            UserRoleRepository.Verify(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserRole>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
