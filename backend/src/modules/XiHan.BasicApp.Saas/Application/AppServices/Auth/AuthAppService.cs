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
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Application.Attributes;
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
            return BuildTwoFactorChallenge(authResult.Security ?? throw new InvalidOperationException("用户双因素配置不存在。"));
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
        var security = authResult.Security;

        // 构建授权快照（角色 + 权限）
        var authSnapshot = await _authorizationSnapshotQueryService.BuildAsync(user.BasicId, now, cancellationToken);
        var client = _clientInfoProvider.GetCurrent();
        var sessionBusinessId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");

        var tokenIssue = _authTokenIssueService.IssueAccessToken(
            new AuthAccessTokenIssueCommand(
                user,
                effectiveTenantId,
                sessionBusinessId,
                accessTokenJti,
                authSnapshot.Roles,
                authSnapshot.Permissions,
                input.DeviceId));
        var sessionResult = await _loginSessionDomainService.IssuePasswordLoginAsync(
            user,
            security,
            effectiveTenantId,
            sessionBusinessId,
            accessTokenJti,
            tokenIssue.TokenResult,
            input.DeviceId,
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

        return new LoginResponseDto
        {
            RequiresTwoFactor = false,
            Token = tokenIssue.Token
        };
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

    private static LoginResponseDto BuildTwoFactorChallenge(SysUserSecurity security)
    {
        var methods = ResolveTwoFactorMethods(security.TwoFactorMethod);
        return new LoginResponseDto
        {
            RequiresTwoFactor = true,
            AvailableTwoFactorMethods = methods,
            TwoFactorMethod = methods.Count == 1 ? methods[0] : null,
            CodeSent = false,
            Token = null
        };
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
