#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthAppService
// Guid:4e9d3226-6a03-4f55-8a58-7238ddde850f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 认证应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "认证", RouteTemplate = "api/Auth")]
public sealed class AuthAppService
    : SaasApplicationService, IAuthAppService
{
    /// <summary>
    /// 超级管理员角色编码（与种子/授权快照约定一致，运行时特判 *）
    /// </summary>
    private const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 默认租户标识：自助注册 / 找回密码在缺省范围时落到该租户（与基础身份种子约定一致）
    /// </summary>
    private const long DefaultRegistrationTenantId = 1;

    private readonly IAuthContextQueryService _authContextQueryService;

    private readonly IAuthenticationDomainService _authenticationDomainService;

    private readonly IAuthorizationSnapshotQueryService _authorizationSnapshotQueryService;

    private readonly IAuthTokenIssueService _authTokenIssueService;

    private readonly IAuthEmailLoginCodeService _emailLoginCodeService;

    private readonly IMessageDeliveryService _messageDeliveryService;

    private readonly IOtpService _otpService;

    private readonly IOptionsMonitor<EmailSenderOptions> _emailSenderOptions;

    private readonly IClientInfoProvider _clientInfoProvider;

    private readonly ICurrentTenant _currentTenant;

    private readonly ICurrentUser _currentUser;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly ILocalEventBus _localEventBus;

    private readonly ILoginSessionDomainService _loginSessionDomainService;

    private readonly IMenuRouteQueryService _menuRouteQueryService;

    private readonly ISaasConfigurationService _saasConfigurationService;

    private readonly IUserRepository _userRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserDomainService _userDomainService;

    private readonly IExternalLoginStore _externalLoginStore;

    private readonly IDistributedCache _distributedCache;

    private readonly IUserNotificationDispatchService _userNotificationDispatchService;

    private readonly ILogger<AuthAppService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthAppService(
        IAuthenticationDomainService authenticationDomainService,
        ILoginSessionDomainService loginSessionDomainService,
        IAuthContextQueryService authContextQueryService,
        IAuthorizationSnapshotQueryService authorizationSnapshotQueryService,
        IMenuRouteQueryService menuRouteQueryService,
        ISaasConfigurationService saasConfigurationService,
        IAuthTokenIssueService authTokenIssueService,
        IAuthEmailLoginCodeService emailLoginCodeService,
        IMessageDeliveryService messageDeliveryService,
        IOtpService otpService,
        IOptionsMonitor<EmailSenderOptions> emailSenderOptions,
        ILocalEventBus localEventBus,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository,
        ITenantUserRepository tenantUserRepository,
        IUserDomainService userDomainService,
        IExternalLoginStore externalLoginStore,
        IDistributedCache distributedCache,
        IUserNotificationDispatchService userNotificationDispatchService,
        ILogger<AuthAppService> logger)
    {
        _authenticationDomainService = authenticationDomainService;
        _loginSessionDomainService = loginSessionDomainService;
        _authContextQueryService = authContextQueryService;
        _authorizationSnapshotQueryService = authorizationSnapshotQueryService;
        _menuRouteQueryService = menuRouteQueryService;
        _saasConfigurationService = saasConfigurationService;
        _authTokenIssueService = authTokenIssueService;
        _emailLoginCodeService = emailLoginCodeService;
        _messageDeliveryService = messageDeliveryService;
        _otpService = otpService;
        _emailSenderOptions = emailSenderOptions;
        _localEventBus = localEventBus;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
        _tenantUserRepository = tenantUserRepository;
        _userDomainService = userDomainService;
        _externalLoginStore = externalLoginStore;
        _distributedCache = distributedCache;
        _userNotificationDispatchService = userNotificationDispatchService;
        _logger = logger;
    }

    /// <inheritdoc />
    // 非公开 API（[DynamicApi(IsEnabled=false)]），仅由 OAuth 回调端点经"未代理目标实例"直接调用。
    // [UnitOfWork(IsDisabled=true)] 表明本流程不开启外层事务：匿名端点无 UoW 中间件预留的工作单元，
    // 若开事务会让拦截器在新作用域里急切 BEGIN/COMPLETE 而死锁；各步仓储/领域服务各自即时提交。
    [DynamicApi(IsEnabled = false)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<ExternalLoginResultDto> ExternalLoginAsync(ExternalLoginCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = command.Provider?.Trim();
        var providerKey = command.ProviderKey?.Trim();
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
        {
            return ExternalLoginResultDto.Fail("invalid", "第三方账号信息不完整。");
        }

        var info = new ExternalLoginInfo
        {
            Provider = provider,
            ProviderKey = providerKey,
            DisplayName = command.DisplayName,
            Email = command.Email,
            AvatarUrl = command.AvatarUrl
        };

        return command.IsBind
            ? await BindExternalLoginAsync(info, command.BindUserId, cancellationToken)
            : await LoginByExternalAsync(info, cancellationToken);
    }

    /// <inheritdoc />
    // 非事务：仅操作分布式缓存，无需 DB 事务；避免 UoW 拦截器在新作用域里急切开启事务
    [UnitOfWork(IsDisabled = true)]
    public async Task<string> CreateOAuthBindTicketAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        await _distributedCache.SetStringAsync(
            OAuthBindTicket.CacheKey(ticket),
            userId.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);
        return ticket;
    }

    /// <inheritdoc />
    [AllowAnonymous]
    public async Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _saasConfigurationService.GetLoginConfigAsync(cancellationToken);
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [UnitOfWork(true)]
    public async Task<RegisterResultDto> RegisterAsync(RegisterRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userName = NormalizeRequired(input.Username, "用户名不能为空。", 50, "用户名不能超过 50 个字符。");
        var password = NormalizeRequired(input.Password, "密码不能为空。", 200, "密码不能超过 200 个字符。");
        var nickName = string.IsNullOrWhiteSpace(input.NickName) ? userName : input.NickName.Trim();
        // 邮箱是全平台唯一的登录身份标识，注册必填（唯一性由用户领域服务统一校验）
        var email = NormalizeRequired(input.Email, "邮箱不能为空。", 256, "邮箱不能超过 256 个字符。");
        if (!email.Contains('@'))
        {
            throw new ArgumentException("邮箱格式无效。");
        }

        // 自助注册统一落到默认租户，注册即成为普通成员（不分配角色，权限由管理员后续授权）
        using var tenantScope = _currentTenant.Change(DefaultRegistrationTenantId, DefaultRegistrationTenantId.ToString());

        var command = new UserCreateCommand(
            UserName: userName,
            InitialPassword: password,
            RealName: null,
            NickName: nickName,
            Avatar: null,
            Email: email,
            Phone: null,
            Gender: UserGender.Unknown,
            Birthday: null,
            Status: EnableStatus.Enabled,
            TimeZone: null,
            Language: "zh-CN",
            Country: null,
            MemberType: TenantMemberType.Member,
            EffectiveTime: null,
            ExpirationTime: null,
            DisplayName: nickName,
            InviteRemark: null,
            Remark: "自助注册账号",
            OperatorUserId: null);

        UserCommandResult result;
        try
        {
            result = await _userDomainService.CreateUserAsync(command, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // 领域校验失败（用户名已存在 / 密码不符合策略 等）转为用户友好提示（400），避免被框架兜底为 500
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }

        // 欢迎邮件尽力而为：投递失败只记录日志，绝不阻断注册主流程
        if (_emailSenderOptions.CurrentValue.IsConfigured)
        {
            try
            {
                await SendWelcomeEmailAsync(email, result.User, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "注册欢迎邮件发送失败，UserId={UserId}", result.User.BasicId);
            }
        }

        return new RegisterResultDto
        {
            UserId = result.User.BasicId,
            UserName = result.User.UserName
        };
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [UnitOfWork(true)]
    public async Task<PasswordResetResultDto> PasswordResetRequestAsync(PasswordResetRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var email = NormalizeRequired(input.Email, "邮箱不能为空。", 256, "邮箱不能超过 256 个字符。");

        // 邮箱全平台唯一：平台态全局定位账号，无需调用方提供租户范围
        using var platformScope = _currentTenant.Change(null);

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        // 防用户枚举：邮箱不存在时同样返回受理，不暴露账号是否存在
        if (user is null)
        {
            return new PasswordResetResultDto { Accepted = true };
        }

        var temporaryPassword = GenerateTemporaryPassword();
        try
        {
            await _userDomainService.ResetUserPasswordAsync(
                new UserPasswordResetCommand(user.BasicId, temporaryPassword, PasswordExpirationTime: null, Remark: "找回密码-临时密码"),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }

        // 认证审计：密码重置落登录日志
        await PublishSecurityAuditAsync(user.TenantId, user.BasicId, user.UserName, LoginResult.PasswordReset, "找回密码-重置为临时密码");

        // 已配置真实 SMTP：通过消息投递管道发送临时密码邮件，响应不回显；未配置（本地联调）：回显临时密码便于测试
        if (_emailSenderOptions.CurrentValue.IsConfigured)
        {
            await SendPasswordResetEmailAsync(email, user.BasicId, temporaryPassword, cancellationToken);
            return new PasswordResetResultDto { Accepted = true };
        }

        return new PasswordResetResultDto
        {
            Accepted = true,
            TemporaryPassword = temporaryPassword
        };
    }

    /// <inheritdoc />
    public async Task<PermissionInfoDto> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        using var tenantScope = _currentTenant.Change(_currentUser.TenantId, _currentUser.TenantId?.ToString());

        var now = DateTimeOffset.UtcNow;
        var snapshot = await _authorizationSnapshotQueryService.BuildAsync(userId, now, cancellationToken);
        var menus = await _menuRouteQueryService.GetRoutesAsync(snapshot, cancellationToken);

        return new PermissionInfoDto
        {
            Roles = snapshot.Roles,
            Permissions = snapshot.Permissions,
            Menus = menus
        };
    }

    /// <inheritdoc />
    public async Task<UserInfoDto> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        using var tenantScope = _currentTenant.Change(_currentUser.TenantId, _currentUser.TenantId?.ToString());

        return await _authContextQueryService.GetCurrentUserInfoAsync(
            userId,
            _currentUser.TenantId,
            _currentUser.Roles,
            cancellationToken);
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [UnitOfWork(true)]
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var login = NormalizeRequired(input.Username, "登录账号不能为空。", 256, "登录账号不能超过 256 个字符。");
        var password = NormalizeRequired(input.Password, "密码不能为空。", 200, "密码不能超过 200 个字符。");
        var now = DateTimeOffset.UtcNow;

        // 先登录后选租户：登录页不再选择租户，统一在平台态完成身份认证
        // （邮箱全平台唯一定位；平台账号可用用户名），登录成功后按成员关系决定落点
        using var platformScope = _currentTenant.Change(null);

        var authResult = await _authenticationDomainService.AuthenticatePasswordLoginAsync(
            login,
            password,
            tenantId: null,
            now,
            cancellationToken);

        if (authResult.RequiresTwoFactor)
        {
            var security = authResult.Security ?? throw new InvalidOperationException("用户双因素配置不存在。");
            var twoFactorUser = authResult.User ?? throw new InvalidOperationException("认证用户不存在。");
            var availableMethods = ResolveTwoFactorMethods(security.TwoFactorMethod);

            // 尚未提交验证码：进入方式选择 / 验证码下发阶段（不签发令牌）
            if (string.IsNullOrWhiteSpace(input.TwoFactorCode))
            {
                return await BuildTwoFactorChallengeAsync(twoFactorUser, availableMethods, input.TwoFactorMethod, tenantId: null, cancellationToken);
            }

            // 已提交验证码：按所选方式校验，未通过抛出（记录失败事件）；通过则继续往下签发令牌
            await VerifyTwoFactorCodeOrThrowAsync(twoFactorUser, security, availableMethods, input.TwoFactorMethod, input.TwoFactorCode, tenantId: null, now, login, cancellationToken);

            var twoFactorToken = await IssueLoginTokenWithLandingAsync(twoFactorUser, security, login, input.DeviceId, now, cancellationToken);
            return new LoginResponseDto
            {
                RequiresTwoFactor = false,
                Token = twoFactorToken
            };
        }

        if (!authResult.Succeeded)
        {
            var clientForFailure = _clientInfoProvider.GetCurrent();
            await _localEventBus.PublishAsync(
                new AuthLoginFailedDomainEvent(
                    null,
                    authResult.User?.BasicId,
                    login,
                    authResult.FailureResult,
                    authResult.ErrorMessage,
                    now,
                    _httpContextAccessor.HttpContext?.TraceIdentifier,
                    clientForFailure.IpAddress,
                    clientForFailure.UserAgent));
            throw new InvalidOperationException(authResult.ErrorMessage ?? "账号或密码错误。");
        }

        var user = authResult.User ?? throw new InvalidOperationException("认证用户不存在。");
        var token = await IssueLoginTokenWithLandingAsync(user, authResult.Security, login, input.DeviceId, now, cancellationToken);

        return new LoginResponseDto
        {
            RequiresTwoFactor = false,
            Token = token
        };
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [UnitOfWork(true)]
    public async Task<VerificationCodeResultDto> EmailLoginCodeAsync(EmailLoginCodeRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var email = NormalizeRequired(input.Email, "邮箱不能为空。", 256, "邮箱不能超过 256 个字符。");
        var now = DateTimeOffset.UtcNow;

        // 先登录后选租户：平台态按全平台唯一邮箱定位用户
        using var platformScope = _currentTenant.Change(null);

        // 复用邮箱登录的用户定位与账号可用性校验，确保仅向有效账号下发验证码
        var authResult = await _authenticationDomainService.AuthenticateEmailLoginAsync(email, tenantId: null, now, cancellationToken);
        if (!authResult.Succeeded)
        {
            throw new InvalidOperationException(authResult.ErrorMessage ?? "邮箱不可用。");
        }

        var code = await IssueAndSendEmailLoginCodeAsync(email, authResult.User?.BasicId, tenantId: null, cancellationToken);

        return new VerificationCodeResultDto
        {
            ExpiresInSeconds = _emailLoginCodeService.ExpiresInSeconds,
            // 已配置真实 SMTP 时不回显验证码；未配置（本地联调）时回显以便测试
            DebugCode = _emailSenderOptions.CurrentValue.IsConfigured ? null : code
        };
    }

    /// <inheritdoc />
    [AllowAnonymous]
    [UnitOfWork(true)]
    public async Task<LoginTokenDto> EmailLoginAsync(EmailLoginRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var email = NormalizeRequired(input.Email, "邮箱不能为空。", 256, "邮箱不能超过 256 个字符。");
        var code = NormalizeRequired(input.Code, "验证码不能为空。", 12, "验证码格式无效。");
        var now = DateTimeOffset.UtcNow;

        // 先登录后选租户：平台态验证 + 智能落点
        using var platformScope = _currentTenant.Change(null);

        if (!await _emailLoginCodeService.TryConsumeAsync(tenantId: null, email, code, cancellationToken))
        {
            throw new InvalidOperationException("验证码无效或已过期。");
        }

        var authResult = await _authenticationDomainService.AuthenticateEmailLoginAsync(email, tenantId: null, now, cancellationToken);
        if (!authResult.Succeeded)
        {
            var clientForFailure = _clientInfoProvider.GetCurrent();
            await _localEventBus.PublishAsync(
                new AuthLoginFailedDomainEvent(
                    null,
                    authResult.User?.BasicId,
                    email,
                    authResult.FailureResult,
                    authResult.ErrorMessage,
                    now,
                    _httpContextAccessor.HttpContext?.TraceIdentifier,
                    clientForFailure.IpAddress,
                    clientForFailure.UserAgent));
            throw new InvalidOperationException(authResult.ErrorMessage ?? "邮箱或验证码错误。");
        }

        var user = authResult.User ?? throw new InvalidOperationException("认证用户不存在。");
        return await IssueLoginTokenWithLandingAsync(user, authResult.Security, user.UserName, input.DeviceId, now, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<LoginTokenDto> SwitchTenantAsync(SwitchTenantRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var now = DateTimeOffset.UtcNow;

        // 归一目标租户：null 或 <=0 视为平台运维态（无租户上下文）
        var targetTenantId = input.TenantId is > 0 ? input.TenantId : null;
        var isSuperAdmin = _currentUser.IsInRole(SuperAdminRoleCode);

        // 跨租户读取当前用户的有效成员关系（忽略租户过滤）
        var memberships = await _tenantUserRepository.GetActiveByUserIdAsync(userId, now, cancellationToken);

        string? targetTenantName = null;
        if (targetTenantId is null)
        {
            // 平台运维态：仅超管或拥有平台管理员成员身份可进入
            var canEnterPlatform = isSuperAdmin || memberships.Any(member => member.MemberType == TenantMemberType.PlatformAdmin);
            if (!canEnterPlatform)
            {
                throw new InvalidOperationException("当前账号无权进入平台运维态。");
            }
        }
        else
        {
            // 切换到具体租户：超管可进入任意租户；否则必须是该租户的有效成员
            var isMember = memberships.Any(member => member.TenantId == targetTenantId.Value);
            if (!isMember && !isSuperAdmin)
            {
                throw new InvalidOperationException("当前账号不是目标租户的有效成员，无法切换。");
            }

            var tenant = await _authContextQueryService.GetLoginTenantOrThrowAsync(targetTenantId, now, cancellationToken)
                ?? throw new InvalidOperationException("目标租户不存在或不可用。");
            targetTenantId = tenant.TenantId;
            targetTenantName = tenant.TenantName;
        }

        var user = await _userRepository.GetByIdIgnoreTenantAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");

        // 在目标上下文内重建授权快照并签发新令牌（平台态不带 TenantId claim）
        using var tenantScope = _currentTenant.Change(targetTenantId, targetTenantName);
        return await IssueLoginTokenAsync(user, security: null, targetTenantId, user.UserName, input.DeviceId, now, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        using var tenantScope = _currentTenant.Change(_currentUser.TenantId, _currentUser.TenantId?.ToString());
        var sessionBusinessId = _currentUser.FindClaim(XiHanClaimTypes.SessionId)?.Value;
        if (string.IsNullOrWhiteSpace(sessionBusinessId))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var session = await _loginSessionDomainService.LogoutAsync(userId.Value, sessionBusinessId, now, cancellationToken);
        if (session is null)
        {
            return;
        }

        var userName = _currentUser.FindClaim(XiHanClaimTypes.UserName)?.Value;
        var client = _clientInfoProvider.GetCurrent();
        await _localEventBus.PublishAsync(
            new AuthLogoutDomainEvent(
                _currentUser.TenantId,
                userId.Value,
                userName,
                session.BasicId,
                sessionBusinessId,
                now,
                _httpContextAccessor.HttpContext?.TraceIdentifier,
                client.IpAddress,
                client.UserAgent));
    }

    /// <inheritdoc />
    [AllowAnonymous]
    public async Task<LoginTokenDto> RefreshTokenAsync(RefreshTokenRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(input.AccessToken) || string.IsNullOrWhiteSpace(input.RefreshToken))
        {
            throw new InvalidOperationException("刷新令牌参数不完整。");
        }

        var token = _authTokenIssueService.RefreshAccessToken(input.AccessToken, input.RefreshToken);

        // 认证审计：令牌刷新落登录日志（身份从旧令牌解析，仅用于审计归属）
        var identity = _authTokenIssueService.ResolveTokenIdentity(input.AccessToken);
        await PublishSecurityAuditAsync(
            identity?.TenantId,
            identity?.UserId,
            identity?.UserName,
            LoginResult.TokenRefreshed,
            "访问令牌刷新");

        return token;
    }

    private static string SanitizeUserNameBase(string provider)
    {
        var letters = new string((provider ?? "ext").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(letters))
        {
            return "ext";
        }

        return letters.Length > 20 ? letters[..20] : letters;
    }

    /// <summary>
    /// 构建欢迎邮件的 HTML 正文（与全局模板种子同款式，全内联样式兜底）
    /// </summary>
    private static string BuildWelcomeHtml(string userName, string brand)
    {
        return $@"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:480px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{brand}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>欢迎加入</h1>
      <p style='margin:0 0 8px;font-size:14px;line-height:1.7;color:#6b7280;'>{userName}，您好！</p>
      <p style='margin:0;font-size:14px;line-height:1.7;color:#6b7280;'>您的账号已创建成功，现在即可使用注册邮箱登录 {brand}。</p>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>
  </div>
</div>";
    }

    /// <summary>
    /// 生成满足默认密码策略的临时密码：含大小写/数字/特殊字符，
    /// 字母数字之间以特殊字符分隔，确保不出现连续序列或重复字符
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";  // 去除易混 I/O
        const string lowers = "abcdefghijkmnpqrstuvwxyz";  // 去除易混 l/o
        const string digits = "23456789";                  // 去除易混 0/1
        var bytes = Guid.NewGuid().ToByteArray();
        char Pick(string set, int i) => set[bytes[i] % set.Length];
        return string.Concat(
            Pick(uppers, 0), '@',
            Pick(lowers, 1), '#',
            Pick(digits, 2), '$',
            Pick(uppers, 3), '%',
            Pick(lowers, 4), '&',
            Pick(digits, 5));
    }

    /// <summary>
    /// 构建临时密码邮件的 HTML 正文（全内联样式，兼容主流邮件客户端）
    /// </summary>
    private static string BuildPasswordResetHtml(string temporaryPassword, string brand)
    {
        return $@"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:480px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{brand}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>临时登录密码</h1>
      <p style='margin:0 0 24px;font-size:14px;line-height:1.7;color:#6b7280;'>您申请了找回密码，请使用以下临时密码登录，并尽快在个人中心修改为新密码：</p>
      <div style='margin:0 0 24px;padding:18px 0;text-align:center;background:#f3f6ff;border:1px solid #e3e9ff;border-radius:12px;'>
        <span style='font-size:24px;font-weight:700;letter-spacing:4px;color:#3b5bdb;'>{temporaryPassword}</span>
      </div>
      <p style='margin:0;font-size:13px;line-height:1.7;color:#9ca3af;'>如非本人操作，请忽略本邮件并立即联系管理员。</p>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>
  </div>
</div>";
    }

    private static string RequireUserEmail(SysUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("账号未绑定邮箱，无法使用邮箱验证码。");
        }

        return user.Email.Trim();
    }

    private static string? NormalizeTwoFactorMethod(string? method)
    {
        return string.IsNullOrWhiteSpace(method) ? null : method.Trim().ToLowerInvariant();
    }

    private static string NormalizeRequired(string? value, string requiredMessage, int maxLength, string maxLengthMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(requiredMessage);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), maxLengthMessage);
        }

        return normalized;
    }

    /// <summary>
    /// 构建登录验证码邮件的 HTML 正文（全内联样式，兼容主流邮件客户端）
    /// </summary>
    private static string BuildEmailLoginCodeHtml(string code, int minutes, string brand)
    {
        return $@"<div style='margin:0;padding:24px;background:#f4f6fb;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:480px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 30px rgba(20,40,80,0.08);'>
    <div style='padding:28px 32px;background:linear-gradient(135deg,#4f7cff,#6f5bff);color:#ffffff;'>
      <div style='font-size:18px;font-weight:700;letter-spacing:.5px;'>{brand}</div>
    </div>
    <div style='padding:32px;'>
      <h1 style='margin:0 0 12px;font-size:20px;color:#1f2937;'>登录验证码</h1>
      <p style='margin:0 0 24px;font-size:14px;line-height:1.7;color:#6b7280;'>您正在登录 {brand}，请在登录页输入以下验证码完成验证：</p>
      <div style='margin:0 0 24px;padding:18px 0;text-align:center;background:#f3f6ff;border:1px solid #e3e9ff;border-radius:12px;'>
        <span style='font-size:32px;font-weight:700;letter-spacing:10px;color:#3b5bdb;'>{code}</span>
      </div>
      <p style='margin:0 0 8px;font-size:13px;line-height:1.7;color:#6b7280;'>验证码 <strong style='color:#374151;'>{minutes} 分钟</strong> 内有效，请勿向任何人泄露。</p>
      <p style='margin:0;font-size:13px;line-height:1.7;color:#9ca3af;'>如非本人操作，请忽略本邮件，您的账号仍然安全。</p>
    </div>
    <div style='padding:18px 32px;background:#fafbfc;border-top:1px solid #eef0f4;font-size:12px;color:#9ca3af;text-align:center;'>本邮件由系统自动发送，请勿直接回复。</div>
  </div>
</div>";
    }

    private static List<string> ResolveTwoFactorMethods(TwoFactorMethod method)
    {
        var methods = new List<string>();
        if (method.HasFlag(TwoFactorMethod.Totp))
        {
            methods.Add("totp");
        }

        if (method.HasFlag(TwoFactorMethod.Email))
        {
            methods.Add("email");
        }

        if (method.HasFlag(TwoFactorMethod.Phone))
        {
            methods.Add("phone");
        }

        return methods.Count == 0 ? ["totp"] : methods;
    }

    /// <summary>
    /// 第三方登录：按 (Provider, ProviderKey) 精确定位用户；未绑定则自动建号（不按邮箱并入既有账号，防冒用）。
    /// </summary>
    private async Task<ExternalLoginResultDto> LoginByExternalAsync(ExternalLoginInfo info, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        SysUser user;
        bool isNewUser;

        // 在默认注册租户范围内定位/创建绑定（与自助注册一致）
        using (_currentTenant.Change(DefaultRegistrationTenantId, DefaultRegistrationTenantId.ToString()))
        {
            var boundUserId = await _externalLoginStore.FindUserIdAsync(info.Provider, info.ProviderKey, DefaultRegistrationTenantId, cancellationToken);
            if (boundUserId is { } existingId && existingId > 0)
            {
                var existing = await _userRepository.GetByIdIgnoreTenantAsync(existingId, cancellationToken);
                if (existing is null)
                {
                    return ExternalLoginResultDto.Fail("user_not_found", "绑定的用户不存在。");
                }

                if (existing.Status != EnableStatus.Enabled)
                {
                    return ExternalLoginResultDto.Fail("disabled", "账号已被禁用，无法登录。");
                }

                user = existing;
                isNewUser = false;
            }
            else
            {
                // 首登自动建号 + 写入绑定（两步各自提交：本流程非事务，理由见 ExternalLoginAsync 注释）
                user = await CreateExternalUserAsync(info, cancellationToken);
                await _externalLoginStore.CreateAsync(user.BasicId, info, DefaultRegistrationTenantId, cancellationToken);
                isNewUser = true;
            }
        }

        // 平台态签发，落点（控制中心 / 唯一租户）由统一逻辑决定，与密码登录一致
        using var platformScope = _currentTenant.Change(null);
        var token = await IssueLoginTokenWithLandingAsync(user, security: null, user.UserName, deviceId: null, now, cancellationToken);
        _logger.LogInformation("第三方登录成功 provider={Provider} userId={UserId} 新用户={IsNewUser}", info.Provider, user.BasicId, isNewUser);
        return ExternalLoginResultDto.LoginSuccess(token);
    }

    /// <summary>
    /// 绑定第三方账号到指定用户（票据已校验身份）；已被他人绑定则拒绝。
    /// </summary>
    private async Task<ExternalLoginResultDto> BindExternalLoginAsync(ExternalLoginInfo info, long? bindUserId, CancellationToken cancellationToken)
    {
        if (bindUserId is not { } userId || userId <= 0)
        {
            return ExternalLoginResultDto.Fail("unauthenticated", "请先登录后再绑定第三方账号。");
        }

        var user = await _userRepository.GetByIdIgnoreTenantAsync(userId, cancellationToken);
        if (user is null)
        {
            return ExternalLoginResultDto.Fail("user_not_found", "用户不存在。");
        }

        var tenantId = user.TenantId;
        using var scope = _currentTenant.Change(tenantId, tenantId.ToString());

        var existing = await _externalLoginStore.FindUserIdAsync(info.Provider, info.ProviderKey, tenantId, cancellationToken);
        if (existing == userId)
        {
            // 幂等：已绑定到本人，直接成功
            return ExternalLoginResultDto.BindSuccess();
        }

        if (existing is not null)
        {
            return ExternalLoginResultDto.Fail("conflict", "该第三方账号已被其他用户绑定。");
        }

        await _externalLoginStore.CreateAsync(userId, info, tenantId, cancellationToken);
        _logger.LogInformation("第三方账号绑定成功 provider={Provider} userId={UserId}", info.Provider, userId);

        // 绑定成功通知（与登录一致：落站内信并尝试实时推送）；通知失败不影响绑定结果
        try
        {
            var content = string.Join(
                "\n",
                $"您已成功绑定第三方账号（{info.Provider}）。",
                $"时间：{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                "若非本人操作，请立即解除绑定并检查账号安全。");
            await _userNotificationDispatchService.DispatchToUserAsync(
                userId,
                "第三方账号绑定成功",
                content,
                NotificationType.User,
                businessType: "auth.external.bind",
                businessId: userId,
                link: "/workbench/profile",
                icon: "lucide:link",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "第三方账号绑定通知发送失败 userId={UserId}", userId);
        }

        return ExternalLoginResultDto.BindSuccess();
    }

    /// <summary>
    /// 第三方首登自动建号：随机强密码、不分配角色（与自助注册一致）；三方邮箱被占用则用占位邮箱，避免并入既有账号。
    /// </summary>
    private async Task<SysUser> CreateExternalUserAsync(ExternalLoginInfo info, CancellationToken cancellationToken)
    {
        var nickName = string.IsNullOrWhiteSpace(info.DisplayName) ? $"{info.Provider}用户" : info.DisplayName.Trim();
        if (nickName.Length > 50)
        {
            nickName = nickName[..50];
        }

        var userName = await GenerateUniqueUserNameAsync(info.Provider, cancellationToken);
        var email = await ResolveExternalEmailAsync(info, userName, cancellationToken);

        var command = new UserCreateCommand(
            UserName: userName,
            InitialPassword: GenerateTemporaryPassword(),
            RealName: null,
            NickName: nickName,
            Avatar: info.AvatarUrl,
            Email: email,
            Phone: null,
            Gender: UserGender.Unknown,
            Birthday: null,
            Status: EnableStatus.Enabled,
            TimeZone: null,
            Language: "zh-CN",
            Country: null,
            MemberType: TenantMemberType.Member,
            EffectiveTime: null,
            ExpirationTime: null,
            DisplayName: nickName,
            InviteRemark: null,
            Remark: $"第三方登录自动创建（{info.Provider}）",
            OperatorUserId: null);

        try
        {
            var result = await _userDomainService.CreateUserAsync(command, cancellationToken);
            return result.User;
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }
    }

    /// <summary>
    /// 生成唯一用户名：提供商名 + 随机后缀，冲突重试
    /// </summary>
    private async Task<string> GenerateUniqueUserNameAsync(string provider, CancellationToken cancellationToken)
    {
        var baseName = SanitizeUserNameBase(provider);
        for (var i = 0; i < 8; i++)
        {
            var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
            var candidate = $"{baseName}_{suffix}";
            if (!await _userRepository.ExistsUserNameAsync(candidate, null, cancellationToken))
            {
                return candidate;
            }
        }

        return $"{baseName}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 解析建号邮箱：三方邮箱未被占用才使用，否则用占位邮箱（不按邮箱并入既有账号）
    /// </summary>
    private async Task<string> ResolveExternalEmailAsync(ExternalLoginInfo info, string userName, CancellationToken cancellationToken)
    {
        var email = info.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && email.Contains('@')
            && !await _userRepository.ExistsEmailGloballyAsync(email, null, cancellationToken))
        {
            return email;
        }

        return $"{userName}@external.local";
    }

    /// <summary>
    /// 发送注册欢迎邮件（复用消息投递管道，模板优先、内置内容兜底）
    /// </summary>
    private async Task SendWelcomeEmailAsync(string email, SysUser user, CancellationToken cancellationToken)
    {
        var emailOptions = _emailSenderOptions.CurrentValue;
        var brand = string.IsNullOrWhiteSpace(emailOptions.FromName) ? "XiHan BasicApp" : emailOptions.FromName;
        var displayName = string.IsNullOrWhiteSpace(user.NickName) ? user.UserName : user.NickName;
        await _messageDeliveryService.CreateEmailAsync(
            new EmailCreateCommand(
                SendUserId: null,
                ReceiveUserId: user.BasicId,
                EmailType: EmailType.Notification,
                FromEmail: emailOptions.FromEmail,
                FromName: emailOptions.FromName,
                ToEmail: email,
                CcEmail: null,
                BccEmail: null,
                Subject: $"欢迎加入 {brand}",
                Content: BuildWelcomeHtml(displayName, brand),
                IsHtml: true,
                Attachments: null,
                // 模板优先：投递链路按编码查模板渲染，缺失回退上方内置内容
                TemplateCode: SaasMessageTemplateCodes.Auth.Welcome,
                TemplateParams: JsonSerializer.Serialize(new Dictionary<string, string> { ["user_name"] = displayName, ["brand"] = brand }),
                ScheduledTime: null,
                MaxRetryCount: 3,
                BusinessType: "auth.welcome",
                BusinessId: user.BasicId,
                Remark: null),
            cancellationToken);
    }

    /// <summary>
    /// 发送临时密码邮件（复用消息投递管道）
    /// </summary>
    private async Task SendPasswordResetEmailAsync(string email, long receiveUserId, string temporaryPassword, CancellationToken cancellationToken)
    {
        var emailOptions = _emailSenderOptions.CurrentValue;
        var brand = string.IsNullOrWhiteSpace(emailOptions.FromName) ? "XiHan BasicApp" : emailOptions.FromName;
        await _messageDeliveryService.CreateEmailAsync(
            new EmailCreateCommand(
                SendUserId: null,
                ReceiveUserId: receiveUserId,
                EmailType: EmailType.Verification,
                FromEmail: emailOptions.FromEmail,
                FromName: emailOptions.FromName,
                ToEmail: email,
                CcEmail: null,
                BccEmail: null,
                Subject: $"【{brand}】临时登录密码",
                Content: BuildPasswordResetHtml(temporaryPassword, brand),
                IsHtml: true,
                Attachments: null,
                // 模板优先：投递链路按编码查模板渲染，缺失回退上方内置内容
                TemplateCode: SaasMessageTemplateCodes.Auth.PasswordReset,
                TemplateParams: JsonSerializer.Serialize(new Dictionary<string, string> { ["temporary_password"] = temporaryPassword, ["brand"] = brand }),
                ScheduledTime: null,
                MaxRetryCount: 3,
                BusinessType: "auth.password-reset",
                BusinessId: receiveUserId,
                Remark: null),
            cancellationToken);
    }

    /// <summary>
    /// 决定登录落点租户并签发令牌（在落点租户上下文内重建快照）。
    /// </summary>
    /// <remarks>
    /// 落点策略（先登录后选租户）：
    /// - 平台账号（TenantId=0）或超管角色 → 平台态（前端落控制中心，可管理租户/用户/系统或选租户进入）
    /// - 恰好一个可用租户成员 → 直接进入该租户（免选择）
    /// - 零个或多个可用租户 → 平台态（前端落控制中心选择租户）
    /// 进入租户后可随时通过 SwitchTenant 切换。
    /// </remarks>
    private async Task<LoginTokenDto> IssueLoginTokenWithLandingAsync(
        SysUser user,
        SysUserSecurity? security,
        string loginName,
        string? deviceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var landing = await ResolveLoginLandingAsync(user, now, cancellationToken);
        using var landingScope = _currentTenant.Change(landing?.TenantId, landing?.TenantName);
        return await IssueLoginTokenAsync(user, security, landing?.TenantId, loginName, deviceId, now, cancellationToken);
    }

    /// <summary>
    /// 解析登录落点租户；返回 null 表示平台态（控制中心）。
    /// </summary>
    private async Task<LoginTenantContext?> ResolveLoginLandingAsync(SysUser user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // 平台账号（TenantId=0，如超管）恒落平台态
        if (user.TenantId == 0)
        {
            return null;
        }

        // 拥有超管角色（全局绑定）的账号同样恒落平台态（平台态快照仅含全局绑定，普通用户在此为空）
        var platformSnapshot = await _authorizationSnapshotQueryService.BuildAsync(user.BasicId, now, cancellationToken);
        if (platformSnapshot.Roles.Contains(SuperAdminRoleCode, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var memberships = await _tenantUserRepository.GetActiveByUserIdAsync(user.BasicId, now, cancellationToken);
        if (memberships.Count != 1)
        {
            return null;
        }

        // 唯一租户也要确认可用（正常/已配置/未过期），不可用则落控制中心由前端展示原因
        return await _authContextQueryService.FindAvailableLoginTenantAsync(memberships[0].TenantId, now, cancellationToken);
    }

    /// <summary>
    /// 为指定邮箱生成登录验证码并在已配置 SMTP 时发送邮件，返回生成的验证码（供本地联调回显）
    /// </summary>
    private async Task<string> IssueAndSendEmailLoginCodeAsync(string email, long? receiveUserId, long? tenantId, CancellationToken cancellationToken)
    {
        var code = await _emailLoginCodeService.IssueCodeAsync(tenantId, email, cancellationToken);
        var emailOptions = _emailSenderOptions.CurrentValue;

        // 已配置真实 SMTP：通过消息投递管道发送验证码邮件（失败会抛出，避免静默丢码）
        if (emailOptions.IsConfigured)
        {
            var minutes = Math.Max(1, _emailLoginCodeService.ExpiresInSeconds / 60);
            var brand = string.IsNullOrWhiteSpace(emailOptions.FromName) ? "XiHan BasicApp" : emailOptions.FromName;
            await _messageDeliveryService.CreateEmailAsync(
                new EmailCreateCommand(
                    SendUserId: null,
                    ReceiveUserId: receiveUserId,
                    EmailType: EmailType.Verification,
                    FromEmail: emailOptions.FromEmail,
                    FromName: emailOptions.FromName,
                    ToEmail: email,
                    CcEmail: null,
                    BccEmail: null,
                    Subject: $"【{brand}】登录验证码",
                    Content: BuildEmailLoginCodeHtml(code, minutes, brand),
                    IsHtml: true,
                    Attachments: null,
                    // 模板优先：投递链路按编码查模板渲染（租户可自定义覆盖全局），缺失回退上方内置内容
                    TemplateCode: SaasMessageTemplateCodes.Auth.EmailLoginCode,
                    TemplateParams: JsonSerializer.Serialize(new Dictionary<string, string> { ["code"] = code, ["minutes"] = minutes.ToString(), ["brand"] = brand }),
                    ScheduledTime: null,
                    MaxRetryCount: 3,
                    BusinessType: "auth.email-login",
                    BusinessId: receiveUserId,
                    Remark: null),
                cancellationToken);
        }

        return code;
    }

    /// <summary>
    /// 签发登录会话与令牌：构建授权快照、签发访问令牌、落地会话并发布登录成功事件
    /// </summary>
    private async Task<LoginTokenDto> IssueLoginTokenAsync(
        SysUser user,
        SysUserSecurity? security,
        long? effectiveTenantId,
        string userName,
        string? deviceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // 构建授权快照（角色 + 权限）
        var authSnapshot = await _authorizationSnapshotQueryService.BuildAsync(user.BasicId, now, cancellationToken);
        var client = _clientInfoProvider.GetCurrent();
        var sessionBusinessId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");

        // 具体权限码以服务端实时快照为准，token 不再冻结具体权限（避免变更后失效不及时与权限清单泄露）；
        // 仅保留通配 * 作为超管快路径
        IReadOnlyCollection<string> tokenPermissions = authSnapshot.Permissions.Contains("*") ? ["*"] : [];
        var tokenIssue = _authTokenIssueService.IssueAccessToken(
            new AuthAccessTokenIssueCommand(
                user,
                effectiveTenantId,
                sessionBusinessId,
                accessTokenJti,
                authSnapshot.Roles,
                tokenPermissions,
                deviceId));
        var sessionResult = await _loginSessionDomainService.IssuePasswordLoginAsync(
            user,
            security,
            effectiveTenantId,
            sessionBusinessId,
            accessTokenJti,
            tokenIssue.TokenResult,
            deviceId,
            client,
            now,
            cancellationToken);

        await _localEventBus.PublishAsync(
            new AuthLoginSucceededDomainEvent(
                effectiveTenantId,
                user.BasicId,
                userName,
                sessionResult.Session.BasicId,
                sessionBusinessId,
                now,
                _httpContextAccessor.HttpContext?.TraceIdentifier,
                client.IpAddress,
                client.UserAgent,
                client.Location,
                client.Browser,
                client.OperatingSystem,
                client.DeviceName));

        return tokenIssue.Token;
    }

    /// <summary>
    /// 发布认证安全审计事件（令牌刷新/密码重置等），失败不影响主流程
    /// </summary>
    private async Task PublishSecurityAuditAsync(long? tenantId, long? userId, string? userName, LoginResult auditResult, string message)
    {
        var client = _clientInfoProvider.GetCurrent();
        await _localEventBus.PublishAsync(
            new AuthSecurityAuditDomainEvent(
                tenantId,
                userId,
                userName,
                auditResult,
                message,
                DateTimeOffset.UtcNow,
                _httpContextAccessor.HttpContext?.TraceIdentifier,
                client.IpAddress,
                client.UserAgent));
    }

    /// <summary>
    /// 构建双因素验证挑战响应：解析当前应使用的方式，邮箱方式在进入验证码输入前先下发验证码
    /// </summary>
    private async Task<LoginResponseDto> BuildTwoFactorChallengeAsync(
        SysUser user,
        List<string> availableMethods,
        string? requestedMethod,
        long? tenantId,
        CancellationToken cancellationToken)
    {
        // 用户已明确选择某方式则采用其选择；否则仅在唯一方式时自动选中，多方式时交由前端展示选择
        var selected = NormalizeTwoFactorMethod(requestedMethod);
        var resolvedMethod = selected is not null && availableMethods.Contains(selected)
            ? selected
            : availableMethods.Count == 1 ? availableMethods[0] : null;

        // 邮箱方式需要在进入验证码输入前先下发验证码（TOTP 由认证器本地生成，无需下发）
        var codeSent = false;
        if (resolvedMethod == "email")
        {
            await SendEmailTwoFactorCodeAsync(user, tenantId, cancellationToken);
            codeSent = true;
        }

        return new LoginResponseDto
        {
            RequiresTwoFactor = true,
            AvailableTwoFactorMethods = availableMethods,
            TwoFactorMethod = resolvedMethod,
            CodeSent = codeSent,
            Token = null
        };
    }

    /// <summary>
    /// 校验已提交的双因素验证码，未通过时记录失败事件并抛出异常
    /// </summary>
    private async Task VerifyTwoFactorCodeOrThrowAsync(
        SysUser user,
        SysUserSecurity security,
        List<string> availableMethods,
        string? requestedMethod,
        string code,
        long? tenantId,
        DateTimeOffset now,
        string userName,
        CancellationToken cancellationToken)
    {
        // 解析待校验方式：优先使用前端提交的方式，缺省时回退到唯一可用方式
        var method = NormalizeTwoFactorMethod(requestedMethod);
        if (method is null || !availableMethods.Contains(method))
        {
            method = availableMethods.Count == 1 ? availableMethods[0] : null;
        }

        if (method is null)
        {
            throw new InvalidOperationException("请选择双因素验证方式。");
        }

        var trimmedCode = code.Trim();
        var verified = method switch
        {
            "totp" => VerifyTotpCode(security, trimmedCode),
            "email" => await _emailLoginCodeService.TryConsumeAsync(tenantId, RequireUserEmail(user), trimmedCode, cancellationToken),
            _ => throw new InvalidOperationException("不支持的双因素验证方式。")
        };

        if (verified)
        {
            return;
        }

        var client = _clientInfoProvider.GetCurrent();
        await _localEventBus.PublishAsync(
            new AuthLoginFailedDomainEvent(
                tenantId,
                user.BasicId,
                userName,
                LoginResult.RequiresTwoFactor,
                "双因素验证码无效。",
                now,
                _httpContextAccessor.HttpContext?.TraceIdentifier,
                client.IpAddress,
                client.UserAgent));
        throw new InvalidOperationException("双因素验证码无效或已过期。");
    }

    /// <summary>
    /// 校验 TOTP 认证器验证码
    /// </summary>
    private bool VerifyTotpCode(SysUserSecurity security, string code)
    {
        if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
        {
            throw new InvalidOperationException("账号尚未完成认证器初始化，无法使用认证器验证。");
        }

        return _otpService.VerifyTotpCode(security.TwoFactorSecret, code);
    }

    /// <summary>
    /// 向用户邮箱下发双因素登录验证码（复用邮箱登录验证码通道）
    /// </summary>
    private Task SendEmailTwoFactorCodeAsync(SysUser user, long? tenantId, CancellationToken cancellationToken)
    {
        return IssueAndSendEmailLoginCodeAsync(RequireUserEmail(user), user.BasicId, tenantId, cancellationToken);
    }
}
