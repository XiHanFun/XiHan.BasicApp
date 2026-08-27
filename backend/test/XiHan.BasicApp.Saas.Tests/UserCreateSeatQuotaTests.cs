// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 创建用户的席位配额守卫测试。
/// </summary>
/// <remarks>
/// 这组测试守的是一条不变式：<b>任何新增租户成员的路径都必须先过席位配额</b>。
/// 目前 SysTenantUser 只有 CreateUserAsync 一条写入路径，但实体注释里描述的邀请流程
/// （Pending → Accepted）迟早会落地。届时新增的入口若绕开配额校验，配额就被架空了——
/// 这里把现有路径钉死，至少保证重构不会悄悄摘掉它。
/// </remarks>
public sealed class UserCreateSeatQuotaTests
{
    /// <summary>
    /// 席位超限时拒绝创建，且在写库之前就拒绝。
    /// </summary>
    [Fact]
    public async Task CreateUser_WhenSeatQuotaExceeded_ShouldRejectBeforeWritingUser()
    {
        var fixture = CreateFixture();
        _ = fixture.QuotaDomainService
            .Setup(service => service.EnsureSeatQuotaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("租户席位已达上限"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateUserAsync(CreateCommand()));

        Assert.Contains("席位已达上限", exception.Message, StringComparison.Ordinal);
        fixture.UserRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 普通成员必须过一次席位配额，且增量为 1。
    /// </summary>
    [Fact]
    public async Task CreateUser_ForNormalMember_ShouldCheckSeatQuotaExactlyOnce()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.CreateUserAsync(CreateCommand(TenantMemberType.Member));

        fixture.QuotaDomainService.Verify(
            service => service.EnsureSeatQuotaAsync(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 本流程创建不出平台管理员，请求会在配额校验之前就被拒。
    /// </summary>
    /// <remarks>
    /// 这条锁的是配额侧「为什么不用特判 PlatformAdmin」：EnsureMemberTypeCanBeCreated 把
    /// Owner 与 PlatformAdmin 挡在 ValidateCreateCommand 阶段，配额校验根本轮不到它们，
    /// 在那里写 MemberType != PlatformAdmin 是死分支。
    /// 「平台管理员不占席位」的口径由统计侧的 CountActiveMembersByTenantIdsAsync 承担——
    /// 平台方代管一个租户不该白吃客户一个付费席位。
    /// 若哪天放开了本流程创建 PlatformAdmin，这条会失败，提醒同时回看配额分支。
    /// </remarks>
    [Theory]
    [InlineData(TenantMemberType.PlatformAdmin)]
    [InlineData(TenantMemberType.Owner)]
    public async Task CreateUser_ForReservedMemberType_ShouldRejectBeforeQuotaCheck(TenantMemberType memberType)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateUserAsync(CreateCommand(memberType)));

        Assert.Contains("专项流程", exception.Message, StringComparison.Ordinal);
        fixture.QuotaDomainService.Verify(
            service => service.EnsureSeatQuotaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 构造创建用户命令。
    /// </summary>
    private static UserCreateCommand CreateCommand(TenantMemberType memberType = TenantMemberType.Member)
    {
        return new UserCreateCommand(
            UserName: "quota_probe",
            InitialPassword: "Quota@Probe123",
            RealName: null,
            NickName: "配额探针",
            Avatar: null,
            Email: "quota_probe@example.com",
            Phone: null,
            Gender: UserGender.Unknown,
            Birthday: null,
            Status: EnableStatus.Enabled,
            Country: null,
            MemberType: memberType,
            EffectiveTime: null,
            ExpirationTime: null,
            DisplayName: "配额探针",
            InviteRemark: null,
            Remark: null,
            OperatorUserId: null);
    }

    /// <summary>
    /// 构造被测服务及其依赖替身（仅打通到写库为止的最短路径）。
    /// </summary>
    private static CreateFixtureResult CreateFixture()
    {
        var userRepository = new Mock<IUserRepository>();
        _ = userRepository
            .Setup(repo => repo.ExistsUserNameAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = userRepository
            .Setup(repo => repo.ExistsEmailGloballyAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = userRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestUser(101));

        var authenticationService = new Mock<IAuthenticationService>();
        _ = authenticationService
            .Setup(service => service.ValidatePasswordStrengthAsync(It.IsAny<string>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(new PasswordValidationResult { IsValid = true });

        var quotaDomainService = new Mock<ITenantQuotaDomainService>();

        var currentTenant = new Mock<ICurrentTenant>();
        _ = currentTenant.SetupGet(context => context.Id).Returns(7L);

        var service = new UserDomainService(
            userRepository.Object,
            new Mock<IUserSecurityRepository>().Object,
            new Mock<ITenantUserRepository>().Object,
            new Mock<IPasswordHasher>().Object,
            authenticationService.Object,
            new Mock<IUserRoleRepository>().Object,
            new Mock<IRoleRepository>().Object,
            new Mock<IUserPermissionRepository>().Object,
            new Mock<IPermissionRepository>().Object,
            new Mock<IUserDataScopeRepository>().Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IUserDepartmentRepository>().Object,
            new Mock<IUserSessionRepository>().Object,
            currentTenant.Object,
            new Mock<IPasswordHistoryDomainService>().Object,
            new Mock<IConstraintRuleEnforcementDomainService>().Object,
            quotaDomainService.Object,
            NullLogger<UserDomainService>.Instance);

        return new CreateFixtureResult(service, userRepository, quotaDomainService);
    }

    /// <summary>
    /// 用户测试替身：BasicId 为 protected set，经派生类构造赋值，避免反射。
    /// </summary>
    private sealed class TestUser : SysUser
    {
        public TestUser(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 创建用户测试依赖集合。
    /// </summary>
    private sealed record CreateFixtureResult(
        UserDomainService Service,
        Mock<IUserRepository> UserRepository,
        Mock<ITenantQuotaDomainService> QuotaDomainService);
}
