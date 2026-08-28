// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using System.Security.Claims;
using Moq;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 模仿登录的会话签发、令牌声明与模仿态禁用清单测试。
/// </summary>
/// <remarks>
/// 三条约定由本类锁住：
/// <list type="number">
/// <item>模仿会话是独立会话行，且寿命由传入时长决定，不跟随刷新令牌的 7 天；</item>
/// <item>模仿令牌带 <c>impersonator_*</c> 声明，退出模仿重签发的令牌不带；</item>
/// <item>模仿态下禁用清单里的权限码在鉴权入口一律判否。</item>
/// </list>
/// </remarks>
public sealed class SaasAppImpersonationSessionTests
{
    private const long OperatorUserId = 1001;
    private const long TargetUserId = 2002;

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSecurityRepository> _userSecurityRepository = new();
    private readonly Mock<ITenantUserRepository> _tenantUserRepository = new();
    private readonly Mock<IUserSessionRepository> _userSessionRepository = new();
    private readonly Mock<IOAuthTokenRepository> _oauthTokenRepository = new();

    private readonly DateTimeOffset _now = new(2026, 8, 29, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 模仿会话落成独立会话行：用户是被模仿者，模仿者信息与原会话标识随行。
    /// </summary>
    [Fact]
    public async Task IssueImpersonationAsync_ShouldCreateSeparateSessionCarryingImpersonator()
    {
        var captured = ArrangeSessionCapture();
        var originSession = BuildOriginSession();

        var session = await CreateDomainService().IssueImpersonationAsync(
            BuildTarget(),
            originSession,
            OperatorUserId,
            "admin",
            impersonatorTenantId: 7,
            "sess-impersonation",
            "jti-1",
            BuildTokenResult(),
            "排查导出失败",
            TimeSpan.FromMinutes(30),
            BuildClientInfo(),
            _now);

        Assert.Same(captured.Single(), session);
        Assert.Equal(TargetUserId, session.UserId);
        Assert.Equal("sess-impersonation", session.UserSessionId);
        Assert.NotEqual(originSession.UserSessionId, session.UserSessionId);
        Assert.Equal(OperatorUserId, session.ImpersonatorUserId);
        Assert.Equal("admin", session.ImpersonatorUserName);
        Assert.Equal(7, session.ImpersonatorTenantId);
        Assert.Equal(originSession.UserSessionId, session.ImpersonatorSessionId);
        Assert.Equal(_now, session.ImpersonationStartTime);
        Assert.Equal("排查导出失败", session.ImpersonationReason);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.False(session.IsLocked);
    }

    /// <summary>
    /// 模仿会话的过期时间取传入时长，而不是刷新令牌的过期时间。
    /// </summary>
    [Fact]
    public async Task IssueImpersonationAsync_ShouldExpireByGivenLifetime()
    {
        ArrangeSessionCapture();

        var session = await CreateDomainService().IssueImpersonationAsync(
            BuildTarget(),
            BuildOriginSession(),
            OperatorUserId,
            "admin",
            impersonatorTenantId: null,
            "sess-impersonation",
            "jti-1",
            BuildTokenResult(),
            reason: null,
            TimeSpan.FromMinutes(30),
            BuildClientInfo(),
            _now);

        Assert.Equal(_now.AddMinutes(30), session.ExpirationTime);
    }

    /// <summary>
    /// 模仿不做同设备顶下线，也不回写被模仿者的登录痕迹。
    /// </summary>
    [Fact]
    public async Task IssueImpersonationAsync_ShouldNotSupersedeDeviceSessionsOrTouchTargetUser()
    {
        ArrangeSessionCapture();

        _ = await CreateDomainService().IssueImpersonationAsync(
            BuildTarget(),
            BuildOriginSession(),
            OperatorUserId,
            "admin",
            impersonatorTenantId: null,
            "sess-impersonation",
            "jti-1",
            BuildTokenResult(),
            reason: null,
            TimeSpan.FromMinutes(30),
            BuildClientInfo(),
            _now);

        _userSessionRepository.Verify(
            repository => repository.GetActiveByUserAndDeviceIgnoreTenantAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userSecurityRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 非正的存活时长不接受——零或负值会签出一枚已过期的模仿会话。
    /// </summary>
    /// <param name="minutes">存活分钟数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IssueImpersonationAsync_NonPositiveLifetime_ShouldThrow(int minutes)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateDomainService().IssueImpersonationAsync(
            BuildTarget(),
            BuildOriginSession(),
            OperatorUserId,
            "admin",
            impersonatorTenantId: null,
            "sess-impersonation",
            "jti-1",
            BuildTokenResult(),
            reason: null,
            TimeSpan.FromMinutes(minutes),
            BuildClientInfo(),
            _now));
    }

    /// <summary>
    /// 结束模仿把模仿会话整条置为已撤销，并吊销其关联令牌台账。
    /// </summary>
    [Fact]
    public async Task RevokeImpersonationAsync_ShouldRevokeSessionAndTokens()
    {
        var impersonationSession = BuildImpersonationSession();
        var token = new SysOAuthToken { SessionId = impersonationSession.BasicId, IsRevoked = false };
        _oauthTokenRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysOAuthToken, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);
        _oauthTokenRepository
            .Setup(repository => repository.UpdateRangeAsync(It.IsAny<IEnumerable<SysOAuthToken>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);
        _userSessionRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysUserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession session, CancellationToken _) => session);

        var revoked = await CreateDomainService().RevokeImpersonationAsync(impersonationSession, "结束模仿登录", _now);

        Assert.Equal(SessionStatus.Revoked, revoked.Status);
        Assert.Equal(_now, revoked.RevokedTime);
        Assert.Equal("结束模仿登录", revoked.RevokedReason);
        Assert.Equal(_now, revoked.LogoutTime);
        Assert.True(token.IsRevoked);
    }

    /// <summary>
    /// 模仿令牌带上四条模仿者声明，被模仿者仍是令牌主体。
    /// </summary>
    [Fact]
    public void IssueAccessToken_WithImpersonator_ShouldWriteImpersonatorClaims()
    {
        var claims = CaptureIssuedClaims(new AuthAccessTokenIssueCommand(
            BuildTarget(),
            TenantId: 10,
            "sess-impersonation",
            "jti-1",
            ["member"],
            [],
            DeviceId: null,
            OperatorUserId,
            "admin",
            7,
            "运维租户"));

        Assert.Equal(TargetUserId.ToString(), Value(claims, XiHanClaimTypes.UserId));
        Assert.Equal(OperatorUserId.ToString(), Value(claims, XiHanClaimTypes.ImpersonatorUserId));
        Assert.Equal("admin", Value(claims, XiHanClaimTypes.ImpersonatorUserName));
        Assert.Equal("7", Value(claims, XiHanClaimTypes.ImpersonatorTenantId));
        Assert.Equal("运维租户", Value(claims, XiHanClaimTypes.ImpersonatorTenantName));
    }

    /// <summary>
    /// 普通登录令牌不带任何模仿者声明。
    /// </summary>
    [Fact]
    public void IssueAccessToken_WithoutImpersonator_ShouldNotWriteImpersonatorClaims()
    {
        var claims = CaptureIssuedClaims(new AuthAccessTokenIssueCommand(
            BuildTarget(),
            TenantId: 10,
            "sess-normal",
            "jti-1",
            ["member"],
            [],
            DeviceId: null));

        Assert.DoesNotContain(claims, claim => claim.Type.StartsWith("impersonator_", StringComparison.Ordinal));
    }

    /// <summary>
    /// 令牌身份解析带出模仿者标识，刷新令牌端点据此拒绝模仿会话续命。
    /// </summary>
    [Fact]
    public void ResolveTokenIdentity_ShouldCarryImpersonatorUserId()
    {
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(service => service.GetClaimsFromToken(It.IsAny<string>())).Returns(
        [
            new Claim(XiHanClaimTypes.UserId, TargetUserId.ToString()),
            new Claim(XiHanClaimTypes.UserName, "target"),
            new Claim(XiHanClaimTypes.SessionId, "sess-impersonation"),
            new Claim(XiHanClaimTypes.ImpersonatorUserId, OperatorUserId.ToString())
        ]);

        var identity = new AuthTokenIssueService(jwt.Object).ResolveTokenIdentity("token");

        Assert.NotNull(identity);
        Assert.Equal(OperatorUserId, identity.ImpersonatorUserId);
    }

    /// <summary>
    /// 模仿态下禁用清单里的权限码一律判否，即使快照里持有它。
    /// </summary>
    /// <param name="permissionCode">权限码。</param>
    [Theory]
    [InlineData("saas:user-security:reset-password")]
    [InlineData("saas:role-permission:grant")]
    [InlineData("saas:user-role:grant")]
    [InlineData("saas:impersonation:start")]
    [InlineData("saas:tenant:delete")]
    public async Task IsGrantedAsync_WhileImpersonating_ShouldDenyBlacklistedCodes(string permissionCode)
    {
        var checker = CreatePermissionChecker(isImpersonating: true, permissions: [permissionCode]);

        Assert.False(await checker.IsGrantedAsync(TargetUserId.ToString(), permissionCode));
    }

    /// <summary>
    /// 模仿态下通配 * 也顶不掉禁用清单。
    /// </summary>
    [Fact]
    public async Task IsGrantedAsync_WhileImpersonatingWithWildcard_ShouldStillDenyBlacklistedCodes()
    {
        var checker = CreatePermissionChecker(isImpersonating: true, permissions: ["*"]);

        Assert.False(await checker.IsGrantedAsync(TargetUserId.ToString(), SaasPermissionCodes.UserSecurity.ResetPassword));
        Assert.True(await checker.IsGrantedAsync(TargetUserId.ToString(), SaasPermissionCodes.User.Read));
    }

    /// <summary>
    /// 非模仿态不受禁用清单影响。
    /// </summary>
    [Fact]
    public async Task IsGrantedAsync_WithoutImpersonation_ShouldAllowBlacklistedCodes()
    {
        var checker = CreatePermissionChecker(isImpersonating: false, permissions: [SaasPermissionCodes.UserSecurity.ResetPassword]);

        Assert.True(await checker.IsGrantedAsync(TargetUserId.ToString(), SaasPermissionCodes.UserSecurity.ResetPassword));
    }

    /// <summary>
    /// 模仿态下权限清单下发也剔除禁用码，前端按钮不会显示成可点。
    /// </summary>
    [Fact]
    public async Task GetGrantedPermissionsAsync_WhileImpersonating_ShouldFilterBlacklistedCodes()
    {
        var checker = CreatePermissionChecker(
            isImpersonating: true,
            permissions: [SaasPermissionCodes.User.Read, SaasPermissionCodes.UserSecurity.ResetPassword]);

        var permissions = await checker.GetGrantedPermissionsAsync(TargetUserId.ToString());

        Assert.Contains(SaasPermissionCodes.User.Read, permissions);
        Assert.DoesNotContain(SaasPermissionCodes.UserSecurity.ResetPassword, permissions);
    }

    /// <summary>
    /// 任一权限判定在模仿态下只看未被禁用的那部分。
    /// </summary>
    [Fact]
    public async Task IsAnyGrantedAsync_WhileImpersonating_ShouldIgnoreBlacklistedCodes()
    {
        var checker = CreatePermissionChecker(isImpersonating: true, permissions: [SaasPermissionCodes.User.Read]);

        Assert.True(await checker.IsAnyGrantedAsync(
            TargetUserId.ToString(),
            [SaasPermissionCodes.UserSecurity.ResetPassword, SaasPermissionCodes.User.Read]));
        Assert.False(await checker.IsAnyGrantedAsync(
            TargetUserId.ToString(),
            [SaasPermissionCodes.UserSecurity.ResetPassword]));
    }

    /// <summary>
    /// 全部权限判定在模仿态下遇到禁用码直接判否。
    /// </summary>
    [Fact]
    public async Task IsAllGrantedAsync_WhileImpersonating_ShouldDenyWhenAnyCodeIsBlacklisted()
    {
        var checker = CreatePermissionChecker(isImpersonating: true, permissions: ["*"]);

        Assert.False(await checker.IsAllGrantedAsync(
            TargetUserId.ToString(),
            [SaasPermissionCodes.User.Read, SaasPermissionCodes.UserSecurity.ResetPassword]));
    }

    /// <summary>
    /// 自助端点守卫在模仿态下拒绝，非模仿态放行。
    /// </summary>
    [Fact]
    public void EnsureNotImpersonating_ShouldThrowOnlyWhileImpersonating()
    {
        var exception = Assert.Throws<UserFriendlyException>(
            () => BuildCurrentUser(isImpersonating: true).Object.EnsureNotImpersonating("修改密码"));
        Assert.Contains("修改密码", exception.Message, StringComparison.Ordinal);

        BuildCurrentUser(isImpersonating: false).Object.EnsureNotImpersonating("修改密码");
    }

    /// <summary>
    /// 会话时长归一到上下限之间。
    /// </summary>
    /// <param name="minutes">配置值。</param>
    /// <param name="expectedMinutes">归一结果。</param>
    [Theory]
    [InlineData(30, 30)]
    [InlineData(0, ImpersonationDefaults.MinSessionMinutes)]
    [InlineData(-5, ImpersonationDefaults.MinSessionMinutes)]
    [InlineData(100000, ImpersonationDefaults.MaxSessionMinutes)]
    public void NormalizeSessionLifetime_ShouldClampToBounds(int minutes, int expectedMinutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), ImpersonationDefaults.NormalizeSessionLifetime(minutes));
    }

    /// <summary>
    /// 禁用清单里的权限码必须是真实播种的码，否则清单形同虚设。
    /// </summary>
    [Fact]
    public void DeniedPermissionCodes_ShouldAllBeSeeded()
    {
        var seeded = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = ImpersonationDefaults.DeniedPermissionCodes
            .Where(code => !seeded.Contains(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.True(unknown.Count == 0, $"以下模仿态禁用码不在播种清单里：{string.Join(", ", unknown)}");
    }

    /// <summary>
    /// 模仿类权限码本身必须在禁用清单里，杜绝模仿链。
    /// </summary>
    [Fact]
    public void DeniedPermissionCodes_ShouldContainImpersonationCodes()
    {
        Assert.Contains(SaasPermissionCodes.Impersonation.Start, ImpersonationDefaults.DeniedPermissionCodes);
        Assert.Contains(SaasPermissionCodes.Impersonation.CrossTenant, ImpersonationDefaults.DeniedPermissionCodes);
    }

    private LoginSessionDomainService CreateDomainService()
    {
        return new LoginSessionDomainService(
            _userRepository.Object,
            _userSecurityRepository.Object,
            _tenantUserRepository.Object,
            _userSessionRepository.Object,
            _oauthTokenRepository.Object);
    }

    private List<SysUserSession> ArrangeSessionCapture()
    {
        var captured = new List<SysUserSession>();
        _userSessionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<SysUserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession session, CancellationToken _) =>
            {
                captured.Add(session);
                return session;
            });
        _oauthTokenRepository
            .Setup(repository => repository.AddAsync(It.IsAny<SysOAuthToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysOAuthToken token, CancellationToken _) => token);
        return captured;
    }

    private SaasPermissionChecker CreatePermissionChecker(bool isImpersonating, List<string> permissions)
    {
        var snapshots = new Mock<IAuthorizationSnapshotQueryService>();
        snapshots
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot([], permissions, []));

        var sessions = new Mock<IUserSessionRepository>();
        sessions
            .Setup(repository => repository.GetByUserSessionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession?)null);

        return new SaasPermissionChecker(snapshots.Object, sessions.Object, BuildCurrentUser(isImpersonating).Object);
    }

    private static Mock<ICurrentUser> BuildCurrentUser(bool isImpersonating)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.UserId).Returns(TargetUserId);
        currentUser
            .Setup(user => user.FindClaim(XiHanClaimTypes.ImpersonatorUserId))
            .Returns(isImpersonating ? new Claim(XiHanClaimTypes.ImpersonatorUserId, OperatorUserId.ToString()) : null);
        currentUser.Setup(user => user.FindClaim(XiHanClaimTypes.SessionId)).Returns((Claim?)null);
        return currentUser;
    }

    private static List<Claim> CaptureIssuedClaims(AuthAccessTokenIssueCommand command)
    {
        List<Claim>? captured = null;
        var jwt = new Mock<IJwtTokenService>();
        jwt
            .Setup(service => service.GenerateAccessToken(It.IsAny<List<Claim>>()))
            .Returns((List<Claim> claims) =>
            {
                captured = claims;
                return BuildTokenResult();
            });

        _ = new AuthTokenIssueService(jwt.Object).IssueAccessToken(command);

        Assert.NotNull(captured);
        return captured;
    }

    private static string? Value(List<Claim> claims, string claimType)
    {
        return claims.Find(claim => claim.Type == claimType)?.Value;
    }

    private static SysUser BuildTarget()
    {
        var target = new SysUser { UserName = "target" };
        SaasTestHelper.SetBasicId(target, TargetUserId);
        return target;
    }

    private static SysUserSession BuildOriginSession()
    {
        var session = new SysUserSession
        {
            UserId = OperatorUserId,
            UserSessionId = "sess-origin",
            DeviceId = "device-1",
            DeviceType = DeviceType.Web,
            DeviceName = "Chrome",
            Status = SessionStatus.Active
        };
        SaasTestHelper.SetBasicId(session, 900);
        return session;
    }

    private static SysUserSession BuildImpersonationSession()
    {
        var session = new SysUserSession
        {
            UserId = TargetUserId,
            UserSessionId = "sess-impersonation",
            ImpersonatorUserId = OperatorUserId,
            ImpersonatorSessionId = "sess-origin",
            Status = SessionStatus.Active
        };
        SaasTestHelper.SetBasicId(session, 901);
        return session;
    }

    private static JwtTokenResult BuildTokenResult()
    {
        var issuedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);
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

    private static ClientInfo BuildClientInfo()
    {
        return new ClientInfo
        {
            IpAddress = "127.0.0.1",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            DeviceName = "Chrome",
            Location = "本地"
        };
    }
}
