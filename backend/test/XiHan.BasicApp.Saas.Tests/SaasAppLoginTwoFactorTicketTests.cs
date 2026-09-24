// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
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
using XiHan.Framework.Security.Password;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 图形验证码与两步验证同时开启时的密码登录三段式测试。
/// </summary>
/// <remarks>
/// 两步验证是无状态三段式：凭据 → 选方式 / 下发码 → 提交码，每段都重新提交同一个登录请求。
/// 图形验证码消费即销毁，因此第二段起不能再靠它，只能凭首段签发的两步验证票据免图形码。
/// 本类锁住的约定：
/// <list type="number">
/// <item>首段通过图形码与密码后随挑战签发票据；</item>
/// <item>后续阶段带票免图形码，票据在有效期内可多次出示，登录完成后作废；</item>
/// <item>不带票的后续阶段仍要图形码，带上方式或验证码不能绕过；</item>
/// <item>票据不存在 / 过期 / 不属于本次认证用户一律按「两步验证已过期」拒绝，且伪票不能借免图形码去试密码；</item>
/// <item>票据同时绑定首段登录名：持自己合法票据也不能免图形码去试探他人账号，登录名不匹配在密码认证之前即作废拒绝。</item>
/// </list>
/// </remarks>
public sealed class SaasAppLoginTwoFactorTicketTests
{
    private const long UserId = 1001;
    private const long OtherUserId = 2002;
    private const string LoginName = "admin";
    private const string OtherLoginName = "someone-else";
    private const string Password = "P@ssw0rd!";
    private const string CaptchaId = "captcha-1";
    private const string CaptchaCode = "4821";
    private const string TotpCode = "123456";
    private const string TicketExpiredMessage = "两步验证已过期，请重新登录。";
    private const string CaptchaInvalidMessage = "验证码错误或已过期，请重试。";

    private readonly FakeDistributedCache _cache = new();
    private readonly HashSet<string> _liveCaptchas = [CaptchaId];
    private readonly Mock<ICaptchaService> _captchaService = new();
    private readonly Mock<IAuthenticationDomainService> _authenticationDomainService = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly Mock<ILoginSessionDomainService> _loginSessionDomainService = new();
    private readonly Mock<IAuthTokenIssueService> _authTokenIssueService = new();

    private readonly SysUser _user = BuildUser(UserId, LoginName);

    /// <summary>
    /// 构造函数：图形码一次性消费、密码认证进入两步验证、认证器码固定。
    /// </summary>
    public SaasAppLoginTwoFactorTicketTests()
    {
        _captchaService.Setup(service => service.IsEnabled).Returns(true);
        _captchaService
            .Setup(service => service.TryConsumeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, string? code, CancellationToken _) =>
                id is not null && code == CaptchaCode && _liveCaptchas.Remove(id));

        _authenticationDomainService
            .Setup(service => service.AuthenticatePasswordLoginAsync(It.Is<string>(value => IsLoginName(value)), Password, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => LoginAuthenticationResult.TwoFactorRequired(_user, BuildTotpSecurity(UserId)));
        _authenticationDomainService
            .Setup(service => service.AuthenticatePasswordLoginAsync(It.Is<string>(value => IsLoginName(value)), It.Is<string>(value => value != Password), null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginAuthenticationResult.Failed(LoginResult.InvalidCredentials, "用户名或密码错误。"));

        _otpService
            .Setup(service => service.VerifyTotpCode(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string _, string code) => code == TotpCode);

        _authTokenIssueService
            .Setup(service => service.IssueAccessToken(It.IsAny<AuthAccessTokenIssueCommand>()))
            .Returns(new AuthAccessTokenIssueResult(BuildTokenResult(), new LoginTokenDto { AccessToken = "access", RefreshToken = "refresh", ExpiresIn = 3600 }));
        _loginSessionDomainService
            .Setup(service => service.IssuePasswordLoginAsync(
                It.IsAny<SysUser>(), It.IsAny<SysUserSecurity?>(), It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<JwtTokenResult>(), It.IsAny<string?>(), It.IsAny<ClientInfo>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginSessionIssueResult(BuildSession(), []));
    }

    /// <summary>
    /// 首段通过图形码与密码：挑战响应随行两步验证票据，票据绑定本次认证出的用户与首段提交的登录名。
    /// </summary>
    [Fact]
    public async Task Login_FirstStage_ShouldIssueTicketWithChallenge()
    {
        var service = CreateService();

        var challenge = await service.LoginAsync(CredentialsRequest());

        Assert.True(challenge.RequiresTwoFactor);
        Assert.Null(challenge.Token);
        Assert.False(string.IsNullOrWhiteSpace(challenge.TwoFactorTicket));
        var payload = await new TwoFactorTicketService(_cache).ResolveAsync(challenge.TwoFactorTicket);
        Assert.NotNull(payload);
        Assert.Equal(UserId, payload.UserId);
        Assert.Equal(LoginName, payload.Login);
    }

    /// <summary>
    /// 用户报告的故障：三段都带同一枚图形码，第二段起图形码已销毁必红。带票后第二、三段免图形码，三段走通并签发令牌。
    /// </summary>
    [Fact]
    public async Task Login_ThreeStagesWithTicket_ShouldSkipCaptchaAndIssueToken()
    {
        var service = CreateService();
        var challenge = await service.LoginAsync(CredentialsRequest());
        var ticket = challenge.TwoFactorTicket!;

        // 第二段：选方式（不带图形码，只带票）
        var selected = await service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorTicket = ticket
        });
        Assert.True(selected.RequiresTwoFactor);
        Assert.Equal("totp", selected.TwoFactorMethod);
        Assert.Equal(ticket, selected.TwoFactorTicket);

        // 第三段：提交验证码（不带图形码，只带票）
        var completed = await service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorCode = TotpCode,
            TwoFactorTicket = ticket
        });

        Assert.False(completed.RequiresTwoFactor);
        Assert.NotNull(completed.Token);
        Assert.Equal("access", completed.Token.AccessToken);
        _captchaService.Verify(
            captcha => captcha.TryConsumeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 登录完成后票据作废：再拿同一枚票据发起后续阶段按「两步验证已过期」拒绝。
    /// </summary>
    [Fact]
    public async Task Login_AfterSuccess_ShouldRevokeTicket()
    {
        var service = CreateService();
        var ticket = (await service.LoginAsync(CredentialsRequest())).TwoFactorTicket!;
        _ = await service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorCode = TotpCode,
            TwoFactorTicket = ticket
        });

        Assert.False(_cache.Values.ContainsKey(TwoFactorTicketService.CacheKey(ticket)));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorTicket = ticket
        }));
        Assert.Equal(TicketExpiredMessage, error.Message);
    }

    /// <summary>
    /// 不带票的第二段仍要图形码：只带方式或验证码不能绕过图形验证码（图形码已在首段销毁，故必红）。
    /// </summary>
    [Fact]
    public async Task Login_SecondStageWithoutTicket_ShouldStillRequireCaptcha()
    {
        var service = CreateService();
        _ = await service.LoginAsync(CredentialsRequest());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            CaptchaId = CaptchaId,
            CaptchaCode = CaptchaCode,
            TwoFactorMethod = "totp",
            TwoFactorCode = TotpCode
        }));

        Assert.Equal(CaptchaInvalidMessage, error.Message);
        _authenticationDomainService.Verify(
            auth => auth.AuthenticatePasswordLoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 伪造票据：在触碰密码认证之前即拒绝，不能借「带票免图形码」去试密码。
    /// </summary>
    [Fact]
    public async Task Login_WithUnknownTicket_ShouldRejectBeforeAuthentication()
    {
        var service = CreateService();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = "wrong-password",
            TwoFactorMethod = "totp",
            TwoFactorTicket = "not-a-ticket"
        }));

        Assert.Equal(TicketExpiredMessage, error.Message);
        _authenticationDomainService.Verify(
            auth => auth.AuthenticatePasswordLoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _captchaService.Verify(
            captcha => captcha.TryConsumeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 票据过期：超过有效期后后续阶段按「两步验证已过期」拒绝。
    /// </summary>
    [Fact]
    public async Task Login_WithExpiredTicket_ShouldReject()
    {
        var service = CreateService();
        var ticket = (await service.LoginAsync(CredentialsRequest())).TwoFactorTicket!;

        _cache.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorTicket = ticket
        }));
        Assert.Equal(TicketExpiredMessage, error.Message);
    }

    /// <summary>
    /// 票据未过期时可在有效期内多次出示：选方式、重发、提交码各段都沿用同一枚票。
    /// </summary>
    [Fact]
    public async Task Login_TicketWithinLifetime_ShouldBeReusableAcrossStages()
    {
        var service = CreateService();
        var ticket = (await service.LoginAsync(CredentialsRequest())).TwoFactorTicket!;

        _cache.Advance(TimeSpan.FromMinutes(9));

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var challenge = await service.LoginAsync(new LoginRequestDto
            {
                Username = LoginName,
                Password = Password,
                TwoFactorMethod = "totp",
                TwoFactorTicket = ticket
            });
            Assert.Equal(ticket, challenge.TwoFactorTicket);
        }
    }

    /// <summary>
    /// 票据登录名相符但绑定用户不一致（例如同名账号已被重建）：密码认证出的用户与票据绑定用户不一致，
    /// 按「两步验证已过期」拒绝、作废票据且不签发令牌。
    /// </summary>
    [Fact]
    public async Task Login_TicketBoundToAnotherUser_ShouldRejectAndRevoke()
    {
        var service = CreateService();
        var foreignTicket = await new TwoFactorTicketService(_cache).IssueAsync(OtherUserId, LoginName);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorCode = TotpCode,
            TwoFactorTicket = foreignTicket
        }));

        Assert.Equal(TicketExpiredMessage, error.Message);
        Assert.False(_cache.Values.ContainsKey(TwoFactorTicketService.CacheKey(foreignTicket)));
        _authTokenIssueService.Verify(issue => issue.IssueAccessToken(It.IsAny<AuthAccessTokenIssueCommand>()), Times.Never);
    }

    /// <summary>
    /// 审核指出的弱点：持自己账号合法票据的人拿它去试探他人账号的错密码。登录名与票据不符须在密码认证之前
    /// 即按「两步验证已过期」拒绝并作废票据——不能返回「账号或密码错误」泄露猜测结果，也不消费图形码。
    /// </summary>
    [Fact]
    public async Task Login_ForeignTicketWithWrongPassword_ShouldRejectBeforeAuthenticationAndRevoke()
    {
        var service = CreateService();
        var foreignTicket = await new TwoFactorTicketService(_cache).IssueAsync(OtherUserId, OtherLoginName);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = "wrong-password",
            TwoFactorMethod = "totp",
            TwoFactorTicket = foreignTicket
        }));

        Assert.Equal(TicketExpiredMessage, error.Message);
        Assert.False(_cache.Values.ContainsKey(TwoFactorTicketService.CacheKey(foreignTicket)));
        _authenticationDomainService.Verify(
            auth => auth.AuthenticatePasswordLoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _captchaService.Verify(
            captcha => captcha.TryConsumeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 他人票据 + 他人账号的正确密码：同样在密码认证之前按「两步验证已过期」拒绝并作废票据，
    /// 与错密码文案一致、不签发令牌，猜对与猜错不可区分。
    /// </summary>
    [Fact]
    public async Task Login_ForeignTicketWithCorrectPassword_ShouldRejectAndRevoke()
    {
        var service = CreateService();
        var foreignTicket = await new TwoFactorTicketService(_cache).IssueAsync(OtherUserId, OtherLoginName);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorCode = TotpCode,
            TwoFactorTicket = foreignTicket
        }));

        Assert.Equal(TicketExpiredMessage, error.Message);
        Assert.False(_cache.Values.ContainsKey(TwoFactorTicketService.CacheKey(foreignTicket)));
        _authenticationDomainService.Verify(
            auth => auth.AuthenticatePasswordLoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _authTokenIssueService.Verify(issue => issue.IssueAccessToken(It.IsAny<AuthAccessTokenIssueCommand>()), Times.Never);
    }

    /// <summary>
    /// 自己的票据 + 自己的账号：登录名按既有规范去空白、不区分大小写比对，后续阶段照旧免图形码并签发令牌。
    /// </summary>
    [Fact]
    public async Task Login_OwnTicketWithDifferentLoginCasing_ShouldStillSkipCaptcha()
    {
        var service = CreateService();
        var ticket = (await service.LoginAsync(CredentialsRequest())).TwoFactorTicket!;

        var completed = await service.LoginAsync(new LoginRequestDto
        {
            Username = " ADMIN ",
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorCode = TotpCode,
            TwoFactorTicket = ticket
        });

        Assert.False(completed.RequiresTwoFactor);
        Assert.NotNull(completed.Token);
        _captchaService.Verify(
            captcha => captcha.TryConsumeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 带票但两步验证码错误：票据不作废，用户可换码重试。
    /// </summary>
    [Fact]
    public async Task Login_WrongTwoFactorCodeWithTicket_ShouldKeepTicket()
    {
        var service = CreateService();
        var ticket = (await service.LoginAsync(CredentialsRequest())).TwoFactorTicket!;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            TwoFactorMethod = "totp",
            TwoFactorCode = "000000",
            TwoFactorTicket = ticket
        }));

        Assert.True(_cache.Values.ContainsKey(TwoFactorTicketService.CacheKey(ticket)));
    }

    private static bool IsLoginName(string value)
    {
        return string.Equals(value, LoginName, StringComparison.OrdinalIgnoreCase);
    }

    private static LoginRequestDto CredentialsRequest()
    {
        return new LoginRequestDto
        {
            Username = LoginName,
            Password = Password,
            CaptchaId = CaptchaId,
            CaptchaCode = CaptchaCode
        };
    }

    private AuthAppService CreateService()
    {
        var clientInfoProvider = new Mock<IClientInfoProvider>();
        clientInfoProvider.Setup(provider => provider.GetCurrent()).Returns(new ClientInfo { IpAddress = "127.0.0.1" });

        var authorizationSnapshotQueryService = new Mock<IAuthorizationSnapshotQueryService>();
        authorizationSnapshotQueryService
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot([], [], [], []));

        return new AuthAppService(
            _authenticationDomainService.Object,
            _loginSessionDomainService.Object,
            Mock.Of<IAuthContextQueryService>(),
            authorizationSnapshotQueryService.Object,
            Mock.Of<IMenuRouteQueryService>(),
            Mock.Of<IPermissionChecker>(),
            Mock.Of<ISaasConfigurationService>(),
            _authTokenIssueService.Object,
            Mock.Of<IAuthEmailLoginCodeService>(),
            Mock.Of<IImpersonationPolicyService>(),
            Mock.Of<IProfileVerificationService>(),
            Mock.Of<IMessageDeliveryService>(),
            _otpService.Object,
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
            _cache,
            Mock.Of<IUserNotificationDispatchService>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<ISaasCacheInvalidator>(),
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILoginThrottleService>(),
            _captchaService.Object,
            new TwoFactorTicketService(_cache),
            new ConfigurationBuilder().Build(),
            NullLogger<AuthAppService>.Instance);
    }

    private static SysUser BuildUser(long id, string userName)
    {
        var user = new SysUser { UserName = userName, TenantId = 0, Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(user, id);
        return user;
    }

    private static SysUserSecurity BuildTotpSecurity(long userId)
    {
        return new SysUserSecurity
        {
            UserId = userId,
            TwoFactorEnabled = true,
            TwoFactorMethod = TwoFactorMethod.Totp,
            TwoFactorSecret = "JBSWY3DPEHPK3PXP"
        };
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

    /// <summary>
    /// 分布式缓存替身：尊重相对绝对过期，时钟可推进以模拟票据过期。
    /// </summary>
    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _entries = new(StringComparer.Ordinal);

        private DateTimeOffset _now = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

        public IReadOnlyDictionary<string, string> Values => _entries
            .Where(entry => entry.Value.ExpiresAt is null || entry.Value.ExpiresAt > _now)
            .ToDictionary(entry => entry.Key, entry => entry.Value.Value, StringComparer.Ordinal);

        public void Advance(TimeSpan duration)
        {
            _now = _now.Add(duration);
        }

        public byte[]? Get(string key)
        {
            return Values.TryGetValue(key, out var value) ? Encoding.UTF8.GetBytes(value) : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _entries.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var expiresAt = options.AbsoluteExpirationRelativeToNow is { } relative
                ? _now.Add(relative)
                : options.AbsoluteExpiration;
            _entries[key] = (Encoding.UTF8.GetString(value), expiresAt);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
