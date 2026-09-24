// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
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
using XiHan.Framework.Security.Password;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 强制改密测试：参数「密码设置」的 forceChange 开启、且账号的密码由他人设置时，密码登录签出的会话先锁定到改密为止。
/// </summary>
/// <remarks>
/// 两个条件缺一不锁：参数默认关闭，关闭时谁都不锁；开启后只锁密码由他人设置的账号（本人改过密码的不锁）。
/// 锁定时给本人发改密提醒，提醒失败不影响锁定。
/// </remarks>
public sealed class SaasAppForcedPasswordChangeTests
{
    private const long UserId = 1001;
    private const string LoginName = "member";
    private const string Password = "P@ssw0rd!";

    private readonly Mock<IAuthenticationDomainService> _authenticationDomainService = new();
    private readonly Mock<ILoginSessionDomainService> _loginSessionDomainService = new();
    private readonly Mock<IAuthTokenIssueService> _authTokenIssueService = new();
    private readonly Mock<ISaasConfigurationService> _configuration = new();
    private readonly Mock<IUserNotificationDispatchService> _notifications = new();

    private string? _issuedLockReason = "未签发";

    /// <summary>
    /// 构造函数：密码认证直接成功，记下签发会话时的初始锁定原因。
    /// </summary>
    public SaasAppForcedPasswordChangeTests()
    {
        _authTokenIssueService
            .Setup(service => service.IssueAccessToken(It.IsAny<AuthAccessTokenIssueCommand>()))
            .Returns(new AuthAccessTokenIssueResult(BuildTokenResult(), new LoginTokenDto { AccessToken = "access", RefreshToken = "refresh", ExpiresIn = 3600 }));
        _loginSessionDomainService
            .Setup(service => service.IssuePasswordLoginAsync(
                It.IsAny<SysUser>(), It.IsAny<SysUserSecurity?>(), It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<JwtTokenResult>(), It.IsAny<string?>(), It.IsAny<ClientInfo>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((SysUser _, SysUserSecurity? _, long? _, string _, string _, JwtTokenResult _, string? _, ClientInfo _, DateTimeOffset _, string? lockReason, CancellationToken _) =>
                _issuedLockReason = lockReason)
            .ReturnsAsync(new LoginSessionIssueResult(BuildSession(), []));
    }

    /// <summary>
    /// 参数开启且密码由他人设置：会话以强制改密锁定签出，并提醒本人改密。
    /// </summary>
    [Fact]
    public async Task Login_ForceChangeOnAndPasswordSetByOthers_ShouldLockSession()
    {
        var service = CreateService(passwordChangeRequired: true, forceChange: true);

        var result = await service.LoginAsync(Request());

        Assert.NotNull(result.Token);
        Assert.Equal(SessionLockReasons.PasswordChangeRequired, _issuedLockReason);
        _notifications.Verify(
            notify => notify.DispatchToUserAsync(
                UserId, It.IsAny<string>(), It.IsAny<string?>(), NotificationType.Security, "auth.password-change-required",
                UserId, It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 提醒发送失败不影响锁定：锁才是硬约束。
    /// </summary>
    [Fact]
    public async Task Login_ForceChangeNotificationFails_ShouldStillLockSession()
    {
        _notifications
            .Setup(notify => notify.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(), It.IsAny<string?>(),
                It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("通知通道不可用"));
        var service = CreateService(passwordChangeRequired: true, forceChange: true);

        _ = await service.LoginAsync(Request());

        Assert.Equal(SessionLockReasons.PasswordChangeRequired, _issuedLockReason);
    }

    /// <summary>
    /// 参数关闭（默认）：密码由他人设置也不锁、不提醒。
    /// </summary>
    [Fact]
    public async Task Login_ForceChangeOff_ShouldNotLock()
    {
        var service = CreateService(passwordChangeRequired: true, forceChange: false);

        _ = await service.LoginAsync(Request());

        Assert.Null(_issuedLockReason);
        _notifications.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 本人改过密码：参数开启也不锁，且不必读取参数。
    /// </summary>
    [Fact]
    public async Task Login_PasswordSetBySelf_ShouldNotLockNorReadSetting()
    {
        var service = CreateService(passwordChangeRequired: false, forceChange: true);

        _ = await service.LoginAsync(Request());

        Assert.Null(_issuedLockReason);
        _configuration.Verify(
            config => config.GetJsonAsync(SaasConfigKeys.Auth.Password, It.IsAny<SaasPasswordSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notifications.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 参数默认值：强制改密默认关闭。
    /// </summary>
    [Fact]
    public void PasswordSettings_ForceChangeShouldDefaultOff()
    {
        Assert.False(new SaasPasswordSettings().ForceChange);
    }

    private static LoginRequestDto Request()
    {
        return new LoginRequestDto { Username = LoginName, Password = Password };
    }

    private AuthAppService CreateService(bool passwordChangeRequired, bool forceChange)
    {
        var user = new SysUser { UserName = LoginName, TenantId = 0, Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(user, UserId);
        var security = new SysUserSecurity { UserId = UserId, PasswordChangeRequired = passwordChangeRequired };
        _authenticationDomainService
            .Setup(service => service.AuthenticatePasswordLoginAsync(LoginName, Password, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginAuthenticationResult.Success(user, security));

        _configuration
            .Setup(config => config.GetJsonAsync(SaasConfigKeys.Auth.Password, It.IsAny<SaasPasswordSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaasPasswordSettings { ForceChange = forceChange });

        var clientInfoProvider = new Mock<IClientInfoProvider>();
        clientInfoProvider.Setup(provider => provider.GetCurrent()).Returns(new ClientInfo { IpAddress = "127.0.0.1" });

        var authorizationSnapshotQueryService = new Mock<IAuthorizationSnapshotQueryService>();
        authorizationSnapshotQueryService
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot([], [], [], []));

        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new AuthAppService(
            _authenticationDomainService.Object,
            _loginSessionDomainService.Object,
            Mock.Of<IAuthContextQueryService>(),
            authorizationSnapshotQueryService.Object,
            Mock.Of<IMenuRouteQueryService>(),
            Mock.Of<IPermissionChecker>(),
            _configuration.Object,
            _authTokenIssueService.Object,
            Mock.Of<IAuthEmailLoginCodeService>(),
            Mock.Of<IImpersonationPolicyService>(),
            Mock.Of<IProfileVerificationService>(),
            Mock.Of<IMessageDeliveryService>(),
            Mock.Of<IOtpService>(),
            Mock.Of<IEmailConfigStore>(),
            Mock.Of<ILocalEventBus>(),
            new TestCurrentTenant(),
            Mock.Of<ICurrentUser>(),
            clientInfoProvider.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<ITraceIdProvider>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<ITenantUserRepository>(),
            Mock.Of<IUserSessionRepository>(),
            Mock.Of<IUserDomainService>(),
            Mock.Of<IExternalLoginStore>(),
            cache,
            _notifications.Object,
            Mock.Of<IPasswordHasher>(),
            Mock.Of<ISaasCacheInvalidator>(),
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILoginThrottleService>(),
            Mock.Of<ICaptchaService>(),
            new TwoFactorTicketService(cache),
            new ConfigurationBuilder().Build(),
            NullLogger<AuthAppService>.Instance);
    }

    private static SysUserSession BuildSession()
    {
        var session = new SysUserSession
        {
            UserId = UserId,
            UserSessionId = "sess-1",
            DeviceType = DeviceType.Web,
            Status = SessionStatus.Active
        };
        SaasTestHelper.SetBasicId(session, 900);
        return session;
    }

    private static JwtTokenResult BuildTokenResult()
    {
        var issuedAt = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);
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
}
