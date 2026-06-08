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
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Otp;
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
        IHttpContextAccessor httpContextAccessor)
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
    }

    /// <inheritdoc />
    [AllowAnonymous]
    public async Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _saasConfigurationService.GetLoginConfigAsync(cancellationToken);
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

        var userName = NormalizeRequired(input.Username, "用户名不能为空。", 50, "用户名不能超过 50 个字符。");
        var password = NormalizeRequired(input.Password, "密码不能为空。", 200, "密码不能超过 200 个字符。");
        var now = DateTimeOffset.UtcNow;
        var tenant = await _authContextQueryService.GetLoginTenantOrThrowAsync(input.TenantId, now, cancellationToken);
        var effectiveTenantId = tenant?.TenantId;
        using var tenantScope = _currentTenant.Change(effectiveTenantId, tenant?.TenantName);

        var authResult = await _authenticationDomainService.AuthenticatePasswordLoginAsync(
            userName,
            password,
            effectiveTenantId,
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
                return await BuildTwoFactorChallengeAsync(twoFactorUser, availableMethods, input.TwoFactorMethod, effectiveTenantId, cancellationToken);
            }

            // 已提交验证码：按所选方式校验，未通过抛出（记录失败事件）；通过则继续往下签发令牌
            await VerifyTwoFactorCodeOrThrowAsync(twoFactorUser, security, availableMethods, input.TwoFactorMethod, input.TwoFactorCode, effectiveTenantId, now, userName, cancellationToken);

            var twoFactorToken = await IssueLoginTokenAsync(twoFactorUser, security, effectiveTenantId, userName, input.DeviceId, now, cancellationToken);
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
                    effectiveTenantId,
                    authResult.User?.BasicId,
                    userName,
                    authResult.FailureResult,
                    authResult.ErrorMessage,
                    now,
                    _httpContextAccessor.HttpContext?.TraceIdentifier,
                    clientForFailure.IpAddress,
                    clientForFailure.UserAgent));
            throw new InvalidOperationException(authResult.ErrorMessage ?? "用户名或密码错误。");
        }

        var user = authResult.User ?? throw new InvalidOperationException("认证用户不存在。");
        var token = await IssueLoginTokenAsync(user, authResult.Security, effectiveTenantId, userName, input.DeviceId, now, cancellationToken);

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
        var tenant = await _authContextQueryService.GetLoginTenantOrThrowAsync(input.TenantId, now, cancellationToken);
        var effectiveTenantId = tenant?.TenantId;
        using var tenantScope = _currentTenant.Change(effectiveTenantId, tenant?.TenantName);

        // 复用邮箱登录的用户定位与账号可用性校验，确保仅向有效账号下发验证码
        var authResult = await _authenticationDomainService.AuthenticateEmailLoginAsync(email, effectiveTenantId, now, cancellationToken);
        if (!authResult.Succeeded)
        {
            throw new InvalidOperationException(authResult.ErrorMessage ?? "邮箱不可用。");
        }

        var code = await IssueAndSendEmailLoginCodeAsync(email, authResult.User?.BasicId, effectiveTenantId, cancellationToken);

        return new VerificationCodeResultDto
        {
            ExpiresInSeconds = _emailLoginCodeService.ExpiresInSeconds,
            // 已配置真实 SMTP 时不回显验证码；未配置（本地联调）时回显以便测试
            DebugCode = _emailSenderOptions.CurrentValue.IsConfigured ? null : code
        };
    }

    /// <summary>
    /// 为指定邮箱生成登录验证码并在已配置 SMTP 时发送邮件，返回生成的验证码（供本地联调回显）
    /// </summary>
    private async Task<string> IssueAndSendEmailLoginCodeAsync(string email, long? receiveUserId, long? tenantId, CancellationToken cancellationToken)
    {
        var code = _emailLoginCodeService.IssueCode(tenantId, email);
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
                    TemplateId: null,
                    TemplateParams: null,
                    ScheduledTime: null,
                    MaxRetryCount: 3,
                    BusinessType: "auth.email-login",
                    BusinessId: receiveUserId,
                    Remark: null),
                cancellationToken);
        }

        return code;
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
        var tenant = await _authContextQueryService.GetLoginTenantOrThrowAsync(input.TenantId, now, cancellationToken);
        var effectiveTenantId = tenant?.TenantId;
        using var tenantScope = _currentTenant.Change(effectiveTenantId, tenant?.TenantName);

        if (!_emailLoginCodeService.TryConsume(effectiveTenantId, email, code))
        {
            throw new InvalidOperationException("验证码无效或已过期。");
        }

        var authResult = await _authenticationDomainService.AuthenticateEmailLoginAsync(email, effectiveTenantId, now, cancellationToken);
        if (!authResult.Succeeded)
        {
            var clientForFailure = _clientInfoProvider.GetCurrent();
            await _localEventBus.PublishAsync(
                new AuthLoginFailedDomainEvent(
                    effectiveTenantId,
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
        return await IssueLoginTokenAsync(user, authResult.Security, effectiveTenantId, user.UserName, input.DeviceId, now, cancellationToken);
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
    public Task<LoginTokenDto> RefreshTokenAsync(RefreshTokenRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(input.AccessToken) || string.IsNullOrWhiteSpace(input.RefreshToken))
        {
            throw new InvalidOperationException("刷新令牌参数不完整。");
        }

        return Task.FromResult(_authTokenIssueService.RefreshAccessToken(input.AccessToken, input.RefreshToken));
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
            "email" => _emailLoginCodeService.TryConsume(tenantId, RequireUserEmail(user), trimmedCode),
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

}
