// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 成员关系只在所属租户内维护：租户接口作用于当前租户并校验席位；平台只做支持人员入驻与离场。
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
    /// 读共享口径下能读到别处的行，按当前租户精确比对：别的租户的成员视为不存在
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
        private readonly List<SysTenantUser> _members = [];

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
                .ReturnsAsync((long id, CancellationToken _) => _members.FirstOrDefault(member => member.BasicId == id));
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

            Service = new TenantDomainService(
                tenantRepository.Object,
                MemberRepository.Object,
                userRepository.Object,
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

        public void VerifyNothingWritten()
        {
            MemberRepository.Verify(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
            MemberRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
