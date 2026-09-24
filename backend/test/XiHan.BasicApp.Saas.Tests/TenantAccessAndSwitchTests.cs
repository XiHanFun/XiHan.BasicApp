// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Bot.Email.Abstractions;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Password;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 可进入租户、登录落点与切换租户：有效成员关系 ∩ 可进入的租户一个口径，超管不例外；平台只对平台账号开放；
/// 切换租户换会话。
/// </summary>
public sealed class TenantAccessAndSwitchTests
{
    private const long UserId = 1001;
    private const long HomeTenantId = 1;
    private const long OtherTenantId = 2;
    private const long ThirdTenantId = 3;

    private static readonly DateTimeOffset Now = new(2026, 9, 24, 8, 0, 0, TimeSpan.Zero);

    // —— 可进入租户 ——

    /// <summary>
    /// 只留有效成员关系所在、且可进入的租户（停用 / 未完成配置 / 过期的剔除）
    /// </summary>
    [Fact]
    public async Task AccessibleTenants_ShouldDropUnavailableTenants()
    {
        var service = CreateAuthContext(
            [Membership(HomeTenantId), Membership(OtherTenantId), Membership(ThirdTenantId)],
            [
                Tenant(HomeTenantId, "home"),
                Tenant(OtherTenantId, "pending", configStatus: TenantConfigStatus.Pending),
                Tenant(ThirdTenantId, "expired", expiration: Now.AddSeconds(-1))
            ]);

        var accessible = await service.GetAccessibleTenantsAsync(UserId, Now);

        Assert.Equal([HomeTenantId], accessible.Select(item => item.Tenant.BasicId));
    }

    /// <summary>
    /// 按最近进入时间倒序，没进入过的排后，再按租户排序
    /// </summary>
    [Fact]
    public async Task AccessibleTenants_ShouldOrderByLastActiveThenSort()
    {
        var service = CreateAuthContext(
            [
                Membership(HomeTenantId),
                Membership(OtherTenantId, lastActive: Now.AddDays(-2)),
                Membership(ThirdTenantId, lastActive: Now.AddDays(-1))
            ],
            [Tenant(HomeTenantId, "home", sort: 1), Tenant(OtherTenantId, "other", sort: 2), Tenant(ThirdTenantId, "third", sort: 3)]);

        var accessible = await service.GetAccessibleTenantsAsync(UserId, Now);

        Assert.Equal([ThirdTenantId, OtherTenantId, HomeTenantId], accessible.Select(item => item.Tenant.BasicId));
    }

    // —— 登录落点 ——

    /// <summary>
    /// 最近进入过的租户优先
    /// </summary>
    [Fact]
    public void Landing_ShouldPreferLastActiveTenant()
    {
        AccessibleTenant[] accessible =
        [
            Accessible(HomeTenantId),
            Accessible(OtherTenantId, lastActive: Now.AddDays(-3)),
            Accessible(ThirdTenantId, lastActive: Now.AddDays(-1))
        ];

        Assert.Equal(ThirdTenantId, LoginLandingPolicy.Choose(accessible, HomeTenantId).Tenant.BasicId);
    }

    /// <summary>
    /// 都没进入过时落归属租户
    /// </summary>
    [Fact]
    public void Landing_ShouldFallToHomeTenantWhenNeverEntered()
    {
        AccessibleTenant[] accessible = [Accessible(OtherTenantId), Accessible(HomeTenantId)];

        Assert.Equal(HomeTenantId, LoginLandingPolicy.Choose(accessible, HomeTenantId).Tenant.BasicId);
    }

    /// <summary>
    /// 归属租户也进不去时落第一个可进入的
    /// </summary>
    [Fact]
    public void Landing_ShouldFallToFirstAccessibleWhenHomeUnavailable()
    {
        AccessibleTenant[] accessible = [Accessible(OtherTenantId), Accessible(ThirdTenantId)];

        Assert.Equal(OtherTenantId, LoginLandingPolicy.Choose(accessible, HomeTenantId).Tenant.BasicId);
    }

    // —— 会话轮换 ——

    /// <summary>
    /// 切换租户：旧会话吊销、令牌台账吊销；新会话沿用设备与登录时间、落新令牌台账、回写成员最近进入时间
    /// </summary>
    [Fact]
    public async Task SessionSwitch_ShouldRevokeOldAndContinueOnSameDevice()
    {
        var sessions = new Mock<IUserSessionRepository>();
        sessions.Setup(repository => repository.UpdateAsync(It.IsAny<SysUserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession session, CancellationToken _) => session);
        sessions.Setup(repository => repository.AddAsync(It.IsAny<SysUserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession session, CancellationToken _) =>
            {
                // 写入拦截器按上下文盖租户戳、分配主键，这里模拟之
                session.TenantId = OtherTenantId;
                SaasTestHelper.SetBasicId(session, 902);
                return session;
            });
        var tokens = new Mock<IOAuthTokenRepository>();
        tokens.Setup(repository => repository.AddAsync(It.IsAny<SysOAuthToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysOAuthToken token, CancellationToken _) => token);
        var membership = Membership(OtherTenantId);
        var memberships = new Mock<ITenantUserRepository>();
        memberships.Setup(repository => repository.GetMembershipAsync(OtherTenantId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        memberships.Setup(repository => repository.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantUser member, CancellationToken _) => member);

        var service = new LoginSessionDomainService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IUserSecurityRepository>(),
            memberships.Object,
            sessions.Object,
            tokens.Object);
        var current = CurrentSession();

        var continued = await service.SwitchTenantAsync(current, "sess-new", "jti-new", TokenResult(), Now);

        Assert.Equal(SessionStatus.Revoked, current.Status);
        Assert.Equal(Now, current.RevokedTime);
        Assert.Equal("sess-new", continued.UserSessionId);
        Assert.Equal(SessionStatus.Active, continued.Status);
        Assert.Equal(current.DeviceId, continued.DeviceId);
        Assert.Equal(current.LoginTime, continued.LoginTime);
        Assert.Equal("jti-new", continued.CurrentAccessTokenJti);
        tokens.Verify(repository => repository.RevokeBySessionIdsAsync(
            It.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 901L })), Now, It.IsAny<CancellationToken>()), Times.Once);
        tokens.Verify(repository => repository.AddAsync(
            It.Is<SysOAuthToken>(token => token.SessionId == 902 && token.AccessTokenJti == "jti-new"), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(Now, membership.LastActiveTime);
    }

    /// <summary>
    /// 锁定的会话不能换上下文
    /// </summary>
    [Fact]
    public async Task SessionSwitch_LockedSession_ShouldThrow()
    {
        var service = new LoginSessionDomainService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IUserSecurityRepository>(),
            Mock.Of<ITenantUserRepository>(),
            Mock.Of<IUserSessionRepository>(),
            Mock.Of<IOAuthTokenRepository>());
        var current = CurrentSession();
        current.IsLocked = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SwitchTenantAsync(current, "sess-new", "jti-new", TokenResult(), Now));
        Assert.Equal(SessionStatus.Active, current.Status);
    }

    // —— 切换租户准入 ——

    /// <summary>
    /// 平台只对平台账号开放：租户账号进不了平台
    /// </summary>
    [Fact]
    public async Task Switch_TenantAccountToPlatform_ShouldThrow()
    {
        var harness = new SwitchHarness(User(HomeTenantId));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.SwitchTenantAsync(new SwitchTenantRequestDto { TenantId = null }));

        Assert.Contains("只有平台账号", error.Message, StringComparison.Ordinal);
        harness.VerifyNoSwitch();
    }

    /// <summary>
    /// 超管也要有成员关系才能进租户
    /// </summary>
    [Fact]
    public async Task Switch_SuperAdminWithoutMembership_ShouldThrow()
    {
        var harness = new SwitchHarness(User(0), isSuperAdmin: true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.SwitchTenantAsync(new SwitchTenantRequestDto { TenantId = OtherTenantId }));

        Assert.Contains("不是目标租户的有效成员", error.Message, StringComparison.Ordinal);
        harness.VerifyNoSwitch();
    }

    /// <summary>
    /// 进入可进入的租户：在目标上下文里构建快照、签发不带平台身份的令牌、续接会话、失效旧会话闸门
    /// </summary>
    [Fact]
    public async Task Switch_ToAccessibleTenant_ShouldContinueSessionInTargetContext()
    {
        var harness = new SwitchHarness(User(HomeTenantId));
        harness.Accessible.Add(Accessible(OtherTenantId));

        _ = await harness.Service.SwitchTenantAsync(new SwitchTenantRequestDto { TenantId = OtherTenantId });

        Assert.Equal(OtherTenantId, harness.SnapshotTenantId);
        Assert.NotNull(harness.IssuedCommand);
        Assert.Equal(OtherTenantId, harness.IssuedCommand.TenantId);
        Assert.NotEqual("sess-old", harness.IssuedCommand.SessionBusinessId);
        harness.SessionDomain.Verify(domain => domain.SwitchTenantAsync(
            It.Is<SysUserSession>(session => session.UserSessionId == "sess-old"),
            harness.IssuedCommand.SessionBusinessId,
            It.IsAny<string>(),
            It.IsAny<JwtTokenResult>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
        harness.CacheInvalidator.Verify(cache => cache.InvalidateSessionStateAsync("sess-old", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 平台账号进入平台：令牌不带租户
    /// </summary>
    [Fact]
    public async Task Switch_PlatformAccountToPlatform_ShouldIssueTenantlessToken()
    {
        var harness = new SwitchHarness(User(0));

        _ = await harness.Service.SwitchTenantAsync(new SwitchTenantRequestDto { TenantId = null });

        Assert.Null(harness.SnapshotTenantId);
        Assert.Null(harness.IssuedCommand?.TenantId);
    }

    private static AuthContextQueryService CreateAuthContext(IReadOnlyList<SysTenantUser> memberships, IReadOnlyList<SysTenant> tenants)
    {
        var membershipRepository = new Mock<ITenantUserRepository>();
        membershipRepository.Setup(repository => repository.GetActiveByUserIdAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberships);
        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) => [.. tenants.Where(tenant => ids.Contains(tenant.BasicId))]);

        return new AuthContextQueryService(Mock.Of<IUserRepository>(), tenantRepository.Object, membershipRepository.Object);
    }

    private static SysTenantUser Membership(long tenantId, DateTimeOffset? lastActive = null)
    {
        return new SysTenantUser
        {
            TenantId = tenantId,
            UserId = UserId,
            MemberType = TenantMemberType.Member,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid,
            LastActiveTime = lastActive
        };
    }

    private static SysTenant Tenant(
        long id,
        string code,
        int sort = 0,
        TenantConfigStatus configStatus = TenantConfigStatus.Configured,
        DateTimeOffset? expiration = null)
    {
        var tenant = new SysTenant
        {
            TenantCode = code,
            TenantName = code,
            TenantStatus = TenantStatus.Normal,
            ConfigStatus = configStatus,
            ExpirationTime = expiration,
            Sort = sort
        };
        SaasTestHelper.SetBasicId(tenant, id);
        return tenant;
    }

    private static AccessibleTenant Accessible(long tenantId, DateTimeOffset? lastActive = null)
    {
        return new AccessibleTenant(Membership(tenantId, lastActive), Tenant(tenantId, $"t{tenantId}"));
    }

    private static SysUser User(long homeTenantId)
    {
        var user = new SysUser { UserName = "someone", TenantId = homeTenantId, Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(user, UserId);
        return user;
    }

    private static SysUserSession CurrentSession()
    {
        var session = new SysUserSession
        {
            UserId = UserId,
            UserSessionId = "sess-old",
            TenantId = HomeTenantId,
            DeviceType = DeviceType.Web,
            DeviceId = "device-1",
            DeviceName = "Web",
            LoginTime = Now.AddHours(-5),
            LastActivityTime = Now.AddMinutes(-1),
            Status = SessionStatus.Active
        };
        SaasTestHelper.SetBasicId(session, 901);
        return session;
    }

    private static JwtTokenResult TokenResult()
    {
        var issuedAt = Now.UtcDateTime;
        return new JwtTokenResult
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            IssuedAt = issuedAt,
            ExpiresAt = issuedAt.AddHours(1),
            RefreshTokenExpiresAt = issuedAt.AddDays(7)
        };
    }

    /// <summary>
    /// 切换租户的装配：真实的 AuthAppService，外部依赖全部替身，记录快照构建时的上下文与签发的令牌命令
    /// </summary>
    private sealed class SwitchHarness
    {
        private readonly TestCurrentTenant _currentTenant = new(HomeTenantId);

        public SwitchHarness(SysUser user, bool isSuperAdmin = false)
        {
            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(current => current.UserId).Returns(UserId);
            currentUser.Setup(current => current.TenantId).Returns(HomeTenantId);
            currentUser.Setup(current => current.IsInRole("super_admin")).Returns(isSuperAdmin);
            currentUser.Setup(current => current.FindClaim(XiHanClaimTypes.SessionId))
                .Returns(new Claim(XiHanClaimTypes.SessionId, "sess-old"));

            var users = new Mock<IUserRepository>();
            users.Setup(repository => repository.GetByIdIgnoreTenantAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var memberships = new Mock<ITenantUserRepository>();
            memberships.Setup(repository => repository.GetActiveByUserIdAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => [.. Accessible.Select(item => item.Membership)]);

            var authContext = new Mock<IAuthContextQueryService>();
            authContext.Setup(service => service.GetAccessibleTenantsAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Accessible);

            var sessions = new Mock<IUserSessionRepository>();
            sessions.Setup(repository => repository.GetByUserSessionIdAsync("sess-old", It.IsAny<CancellationToken>()))
                .ReturnsAsync(CurrentSession());

            var snapshots = new Mock<IAuthorizationSnapshotQueryService>();
            snapshots.Setup(service => service.BuildAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Callback(() => SnapshotTenantId = _currentTenant.Id)
                .ReturnsAsync(new AuthorizationSnapshot([], [], [], []));

            var tokenIssue = new Mock<IAuthTokenIssueService>();
            tokenIssue.Setup(service => service.IssueAccessToken(It.IsAny<AuthAccessTokenIssueCommand>()))
                .Callback((AuthAccessTokenIssueCommand command) => IssuedCommand = command)
                .Returns(new AuthAccessTokenIssueResult(TokenResult(), new LoginTokenDto { AccessToken = "access", RefreshToken = "refresh", ExpiresIn = 3600 }));

            SessionDomain.Setup(domain => domain.SwitchTenantAsync(
                    It.IsAny<SysUserSession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<JwtTokenResult>(),
                    It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysUserSession session, string id, string _, JwtTokenResult _, DateTimeOffset _, CancellationToken _) =>
                    new SysUserSession { UserId = session.UserId, UserSessionId = id, Status = SessionStatus.Active });

            var clientInfo = new Mock<IClientInfoProvider>();
            clientInfo.Setup(provider => provider.GetCurrent()).Returns(new ClientInfo { IpAddress = "127.0.0.1" });

            Service = new AuthAppService(
                Mock.Of<IAuthenticationDomainService>(),
                SessionDomain.Object,
                authContext.Object,
                snapshots.Object,
                Mock.Of<IMenuRouteQueryService>(),
                Mock.Of<IPermissionChecker>(),
                Mock.Of<ISaasConfigurationService>(),
                tokenIssue.Object,
                Mock.Of<IAuthEmailLoginCodeService>(),
                Mock.Of<IImpersonationPolicyService>(),
                Mock.Of<IProfileVerificationService>(),
                Mock.Of<IMessageDeliveryService>(),
                Mock.Of<IOtpService>(),
                Mock.Of<IEmailConfigStore>(),
                Mock.Of<ILocalEventBus>(),
                _currentTenant,
                currentUser.Object,
                clientInfo.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<ITraceIdProvider>(),
                users.Object,
                memberships.Object,
                sessions.Object,
                Mock.Of<IUserDomainService>(),
                Mock.Of<IExternalLoginStore>(),
                Mock.Of<IDistributedCache>(),
                Mock.Of<IUserNotificationDispatchService>(),
                Mock.Of<IPasswordHasher>(),
                CacheInvalidator.Object,
                Mock.Of<IWebHostEnvironment>(),
                Mock.Of<ILoginThrottleService>(),
                Mock.Of<ICaptchaService>(),
                new TwoFactorTicketService(Mock.Of<IDistributedCache>()),
                new ConfigurationBuilder().Build(),
                NullLogger<AuthAppService>.Instance);
        }

        public AuthAppService Service { get; }

        public List<AccessibleTenant> Accessible { get; } = [];

        public Mock<ILoginSessionDomainService> SessionDomain { get; } = new();

        public Mock<ISaasCacheInvalidator> CacheInvalidator { get; } = new();

        public long? SnapshotTenantId { get; private set; } = -1;

        public AuthAccessTokenIssueCommand? IssuedCommand { get; private set; }

        public void VerifyNoSwitch()
        {
            SessionDomain.Verify(domain => domain.SwitchTenantAsync(
                It.IsAny<SysUserSession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<JwtTokenResult>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
