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
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authentication.OneTimeCode;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Bot.Email.Abstractions;
using XiHan.Framework.Bot.Sms.Abstractions;
using XiHan.Framework.Bot.Sms.Options;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Password;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 手机号验证码登录：登录设置开放才可用、只认已验证且唯一的手机号、短信网关没配好直接报错、验证码一次性、失败留登录事件。
/// </summary>
public sealed class SaasAppPhoneLoginTests
{
    private const string Phone = "13800138000";
    private const long UserId = 1001;

    private readonly Mock<ISaasConfigurationService> _configuration = new();
    private readonly Mock<IAuthenticationDomainService> _authenticationDomainService = new();
    private readonly Mock<IAuthPhoneLoginCodeService> _phoneLoginCodeService = new();
    private readonly Mock<ILocalEventBus> _localEventBus = new();
    private readonly Mock<IAccessPolicyLoginGuard> _loginGuard = new();
    private readonly SysUser _user = BuildUser(UserId);

    /// <summary>
    /// 构造函数：缺省开放密码与手机号登录，手机号定位到一个可用账号。
    /// </summary>
    public SaasAppPhoneLoginTests()
    {
        OpenLoginMethods(SaasLoginMethods.Password, SaasLoginMethods.Phone);
        _authenticationDomainService
            .Setup(service => service.AuthenticatePhoneLoginAsync(Phone, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => LoginAuthenticationResult.Success(_user, new SysUserSecurity { UserId = UserId, PhoneVerified = true }));
        _phoneLoginCodeService.SetupGet(service => service.ExpiresInSeconds).Returns(600);
        _phoneLoginCodeService
            .Setup(service => service.IssueAndSendAsync(Phone, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");
    }

    /// <summary>
    /// 登录设置没开放手机号：发码与登录都拒绝，不发码、不消费验证码。
    /// </summary>
    [Fact]
    public async Task PhoneLogin_MethodNotOpened_ShouldRejectBothEndpoints()
    {
        OpenLoginMethods(SaasLoginMethods.Password, SaasLoginMethods.Email);
        var service = CreateService();

        var sendError = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PhoneLoginCodeAsync(new PhoneLoginCodeRequestDto { Phone = Phone }));
        var loginError = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PhoneLoginAsync(new PhoneLoginRequestDto { Phone = Phone, Code = "123456" }));

        Assert.Equal("未开放手机号登录。", sendError.Message);
        Assert.Equal("未开放手机号登录。", loginError.Message);
        _phoneLoginCodeService.Verify(item => item.IssueAndSendAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _phoneLoginCodeService.Verify(item => item.TryConsumeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 发码：定位到账号后给这个手机号发短信，返回有效期；非开发环境不回显验证码。
    /// </summary>
    [Fact]
    public async Task PhoneLoginCode_ShouldSendToTheLocatedAccount()
    {
        var service = CreateService();

        var result = await service.PhoneLoginCodeAsync(new PhoneLoginCodeRequestDto { Phone = $" {Phone} " });

        Assert.Equal(600, result.ExpiresInSeconds);
        Assert.Null(result.DebugCode);
        _phoneLoginCodeService.Verify(item => item.IssueAndSendAsync(Phone, UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 发码：手机号定位不到可用账号时按认证结果报错，不发短信。
    /// </summary>
    [Fact]
    public async Task PhoneLoginCode_UnknownPhone_ShouldNotSend()
    {
        _authenticationDomainService
            .Setup(service => service.AuthenticatePhoneLoginAsync(Phone, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginAuthenticationResult.Failed(LoginResult.InvalidCredentials, "手机号未注册或验证码错误。"));
        var service = CreateService();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PhoneLoginCodeAsync(new PhoneLoginCodeRequestDto { Phone = Phone }));

        Assert.Equal("手机号未注册或验证码错误。", error.Message);
        _phoneLoginCodeService.Verify(item => item.IssueAndSendAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 发码：同一手机号 + IP 一分钟内只能发一次。
    /// </summary>
    [Fact]
    public async Task PhoneLoginCode_SecondRequestWithinAMinute_ShouldBeRateLimited()
    {
        var service = CreateService();
        _ = await service.PhoneLoginCodeAsync(new PhoneLoginCodeRequestDto { Phone = Phone });

        await Assert.ThrowsAnyAsync<Exception>(() => service.PhoneLoginCodeAsync(new PhoneLoginCodeRequestDto { Phone = Phone }));

        _phoneLoginCodeService.Verify(item => item.IssueAndSendAsync(Phone, UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 登录：验证码不对直接拒绝，不去认证账号。
    /// </summary>
    [Fact]
    public async Task PhoneLogin_WrongCode_ShouldRejectBeforeAuthentication()
    {
        _phoneLoginCodeService
            .Setup(service => service.TryConsumeAsync(Phone, "000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PhoneLoginAsync(new PhoneLoginRequestDto { Phone = Phone, Code = "000000" }));

        Assert.Equal("验证码无效或已过期。", error.Message);
        _authenticationDomainService.Verify(
            item => item.AuthenticatePhoneLoginAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 登录：验证码对、账号不可用时发登录失败事件（写登录日志），登录名记手机号，并按客户端 IP 计一次登录失败。
    /// </summary>
    [Fact]
    public async Task PhoneLogin_AccountUnavailable_ShouldPublishLoginFailedEvent()
    {
        _phoneLoginCodeService
            .Setup(service => service.TryConsumeAsync(Phone, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authenticationDomainService
            .Setup(service => service.AuthenticatePhoneLoginAsync(Phone, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginAuthenticationResult.Failed(LoginResult.AccountDisabled, "用户已被禁用。"));
        var service = CreateService();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PhoneLoginAsync(new PhoneLoginRequestDto { Phone = Phone, Code = "123456" }));

        Assert.Equal("用户已被禁用。", error.Message);
        _localEventBus.Verify(
            bus => bus.PublishAsync(
                It.Is<AuthLoginFailedDomainEvent>(item => item.UserName == Phone && item.LoginResult == LoginResult.AccountDisabled),
                It.IsAny<bool>()),
            Times.Once);
        _loginGuard.Verify(guard => guard.CountFailedLoginAsync("127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    private void OpenLoginMethods(params string[] methods)
    {
        _configuration
            .Setup(service => service.GetLoginConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginConfigDto { LoginMethods = [.. methods] });
    }

    private AuthAppService CreateService()
    {
        var clientInfoProvider = new Mock<IClientInfoProvider>();
        clientInfoProvider.Setup(provider => provider.GetCurrent()).Returns(new ClientInfo { IpAddress = "127.0.0.1" });
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        return new AuthAppService(
            _authenticationDomainService.Object,
            Mock.Of<ILoginSessionDomainService>(),
            Mock.Of<IAuthContextQueryService>(),
            Mock.Of<IAuthorizationSnapshotQueryService>(),
            Mock.Of<IMenuRouteQueryService>(),
            Mock.Of<IPermissionChecker>(),
            _configuration.Object,
            Mock.Of<IAuthTokenIssueService>(),
            Mock.Of<IAuthEmailLoginCodeService>(),
            _phoneLoginCodeService.Object,
            Mock.Of<IImpersonationPolicyService>(),
            Mock.Of<IProfileVerificationService>(),
            Mock.Of<IMessageDeliveryService>(),
            Mock.Of<IOtpService>(),
            Mock.Of<IEmailConfigStore>(),
            _localEventBus.Object,
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
            Mock.Of<IUserNotificationDispatchService>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<ISaasCacheInvalidator>(),
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILoginThrottleService>(),
            Mock.Of<ICaptchaService>(),
            new TwoFactorTicketService(cache),
            _loginGuard.Object,
            new ConfigurationBuilder().Build(),
            NullLogger<AuthAppService>.Instance);
    }

    private static SysUser BuildUser(long id)
    {
        var user = new SysUser { UserName = $"user{id}", Phone = Phone, TenantId = 0, Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(user, id);
        return user;
    }
}

/// <summary>
/// 手机号登录的账号定位：只认已验证的手机号，同号多账号不替用户选。
/// </summary>
public sealed class AuthenticationDomainServicePhoneLoginTests
{
    private const string Phone = "13800138000";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserSecurityRepository> _securities = new();

    /// <summary>
    /// 没有账号用这个手机号：按凭据错误失败。
    /// </summary>
    [Fact]
    public async Task UnknownPhone_ShouldFailWithInvalidCredentials()
    {
        var result = await AuthenticateAsync([]);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginResult.InvalidCredentials, result.FailureResult);
    }

    /// <summary>
    /// 账号上的手机号没验证过：不能拿它登录。
    /// </summary>
    [Fact]
    public async Task UnverifiedPhone_ShouldFailWithInvalidCredentials()
    {
        var result = await AuthenticateAsync([(1, false)]);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginResult.InvalidCredentials, result.FailureResult);
    }

    /// <summary>
    /// 同一手机号在两个账号上都验证过：不替用户选，提示改用账号密码。
    /// </summary>
    [Fact]
    public async Task PhoneVerifiedOnTwoAccounts_ShouldFail()
    {
        var result = await AuthenticateAsync([(1, true), (2, true)]);

        Assert.False(result.Succeeded);
        Assert.Contains("多个账号", result.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// 一个已验证、一个未验证：登录到已验证的那个账号。
    /// </summary>
    [Fact]
    public async Task OneVerifiedAccount_ShouldSucceed()
    {
        var result = await AuthenticateAsync([(1, false), (2, true)]);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.User!.BasicId);
    }

    private async Task<LoginAuthenticationResult> AuthenticateAsync(IReadOnlyList<(long Id, bool Verified)> accounts)
    {
        var users = accounts.Select(account =>
        {
            var user = new SysUser { UserName = $"user{account.Id}", Phone = Phone, Status = EnableStatus.Enabled };
            SaasTestHelper.SetBasicId(user, account.Id);
            _securities
                .Setup(repository => repository.GetByUserIdAsync(account.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SysUserSecurity { UserId = account.Id, PhoneVerified = account.Verified });
            return user;
        }).ToList();
        _users
            .Setup(repository => repository.GetListByPhoneGloballyAsync(Phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var service = new AuthenticationDomainService(
            Mock.Of<IAuthenticationService>(),
            _users.Object,
            _securities.Object,
            Mock.Of<ITenantUserRepository>());
        return await service.AuthenticatePhoneLoginAsync(Phone, null, DateTimeOffset.UtcNow);
    }
}

/// <summary>
/// 手机号登录验证码服务：短信网关没配或停用时报错且不发码，配好时用登录验证码模板发到该手机号。
/// </summary>
public sealed class AuthPhoneLoginCodeServiceTests
{
    private const string Phone = "13800138000";

    private readonly Mock<IOneTimeCodeService> _codes = new();
    private readonly Mock<ISmsConfigStore> _smsConfig = new();
    private readonly Mock<IMessageDeliveryService> _delivery = new();

    /// <summary>
    /// 构造函数：发码固定返回 654321。
    /// </summary>
    public AuthPhoneLoginCodeServiceTests()
    {
        _codes
            .Setup(service => service.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<OneTimeCodeOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneTimeCodeIssueResult("654321", 600));
    }

    /// <summary>
    /// 网关没配、配了但停用：都报错，不生成验证码、不写短信。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IssueAndSend_GatewayMissingOrDisabled_ShouldThrowWithoutSending(bool configuredButDisabled)
    {
        _smsConfig
            .Setup(store => store.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredButDisabled ? new SmsChannelConfig { IsEnabled = false } : null);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().IssueAndSendAsync(Phone, 1001));

        Assert.Contains("短信网关未配置或未启用", error.Message, StringComparison.Ordinal);
        _codes.Verify(service => service.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<OneTimeCodeOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        _delivery.Verify(service => service.CreateSmsAsync(It.IsAny<SmsCreateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 网关启用但模板映射里没有登录验证码模板：云厂商只按服务商模板发，发件箱必然失败，所以发码前就报错，不生成验证码、不写短信。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("{\"auth-sms-verification-code\":{\"templateCode\":\"SMS_1\"}}")]
    [InlineData("{\"auth-sms-login-code\":{\"templateCode\":\" \"}}")]
    public async Task IssueAndSend_LoginTemplateNotMapped_ShouldThrowWithoutSending(string? templateMap)
    {
        _smsConfig
            .Setup(store => store.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsChannelConfig { IsEnabled = true, TemplateMap = templateMap });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().IssueAndSendAsync(Phone, 1001));

        Assert.Contains(SaasMessageTemplateCodes.Auth.SmsLoginCode, error.Message, StringComparison.Ordinal);
        _codes.Verify(service => service.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<OneTimeCodeOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
        _delivery.Verify(service => service.CreateSmsAsync(It.IsAny<SmsCreateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 网关可用：生成验证码并按登录验证码模板写一条短信给该手机号；模板映射的键不区分大小写，与网关客户端一致。
    /// </summary>
    [Fact]
    public async Task IssueAndSend_GatewayReady_ShouldSendLoginCodeSms()
    {
        _smsConfig
            .Setup(store => store.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsChannelConfig { IsEnabled = true, TemplateMap = "{\"AUTH-SMS-LOGIN-CODE\":{\"templateCode\":\"SMS_123456\"}}" });

        var code = await CreateService().IssueAndSendAsync($" {Phone} ", 1001);

        Assert.Equal("654321", code);
        _delivery.Verify(
            service => service.CreateSmsAsync(
                It.Is<SmsCreateCommand>(command =>
                    command.ToPhone == Phone
                    && command.ReceiverId == 1001
                    && command.TemplateCode == SaasMessageTemplateCodes.Auth.SmsLoginCode
                    && command.SmsType == SmsType.VerificationCode
                    && command.TemplateParams!.Contains("654321", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 校验：空手机号或空验证码直接不通过，不去消费在途码。
    /// </summary>
    [Fact]
    public async Task TryConsume_BlankInput_ShouldFailWithoutConsuming()
    {
        Assert.False(await CreateService().TryConsumeAsync(Phone, " "));
        Assert.False(await CreateService().TryConsumeAsync(" ", "123456"));

        _codes.Verify(service => service.TryConsumeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private AuthPhoneLoginCodeService CreateService()
    {
        return new AuthPhoneLoginCodeService(_codes.Object, _smsConfig.Object, Mock.Of<IEmailConfigStore>(), _delivery.Object);
    }
}
