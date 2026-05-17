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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Jwt;
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
    /// 构造函数
    /// </summary>
    public AuthAppService(
        IAuthenticationDomainService authenticationDomainService,
        ILoginSessionDomainService loginSessionDomainService,
        IAuthContextQueryService authContextQueryService,
        IAuthorizationSnapshotQueryService authorizationSnapshotQueryService,
        IMenuRouteQueryService menuRouteQueryService,
        ISaasConfigurationService saasConfigurationService,
        IJwtTokenService jwtTokenService,
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
        _jwtTokenService = jwtTokenService;
        _localEventBus = localEventBus;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private readonly IAuthenticationDomainService _authenticationDomainService;
    private readonly ILoginSessionDomainService _loginSessionDomainService;
    private readonly IAuthContextQueryService _authContextQueryService;
    private readonly IAuthorizationSnapshotQueryService _authorizationSnapshotQueryService;
    private readonly IMenuRouteQueryService _menuRouteQueryService;
    private readonly ISaasConfigurationService _saasConfigurationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILocalEventBus _localEventBus;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

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

        // 生成包含应用级声明的 JWT
        var claims = BuildClaims(user, effectiveTenantId, sessionBusinessId, accessTokenJti, authSnapshot.Roles, authSnapshot.Permissions, input.DeviceId);
        var tokenResult = _jwtTokenService.GenerateAccessToken(claims);
        var sessionResult = await _loginSessionDomainService.IssuePasswordLoginAsync(
            user,
            security,
            effectiveTenantId,
            sessionBusinessId,
            accessTokenJti,
            tokenResult,
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
            Token = ToLoginTokenDto(tokenResult)
        };
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

        var tokenResult = _jwtTokenService.RefreshAccessToken(input.AccessToken.Trim(), input.RefreshToken.Trim())
            ?? throw new InvalidOperationException("刷新令牌无效或已过期。");
        return Task.FromResult(ToLoginTokenDto(tokenResult));
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

    private static LoginTokenDto ToLoginTokenDto(JwtTokenResult tokenResult)
    {
        return new LoginTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = tokenResult.IssuedAt,
            ExpiresAt = tokenResult.ExpiresAt
        };
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

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }

    private List<Claim> BuildClaims(
        SysUser user,
        long? tenantId,
        string sessionBusinessId,
        string accessTokenJti,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        string? deviceId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.BasicId.ToString()),
            new(JwtRegisteredClaimNames.Jti, accessTokenJti),
            new(XiHanClaimTypes.UserId, user.BasicId.ToString()),
            new(XiHanClaimTypes.UserName, user.UserName),
            new(XiHanClaimTypes.SessionId, sessionBusinessId)
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(XiHanClaimTypes.TenantId, tenantId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(XiHanClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim(XiHanClaimTypes.PhoneNumber, user.Phone));
        }

        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            claims.Add(new Claim(XiHanClaimTypes.Picture, user.Avatar));
        }

        var normalizedDeviceId = NormalizeNullable(deviceId, 200);
        if (!string.IsNullOrWhiteSpace(normalizedDeviceId))
        {
            claims.Add(new Claim(XiHanClaimTypes.DeviceFingerprint, normalizedDeviceId));
        }

        foreach (var role in roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(XiHanClaimTypes.Role, role));
        }

        var permissionClaims = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (permissionClaims.Contains("*", StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(XiHanClaimTypes.Permission, "*"));
        }
        else
        {
            foreach (var permission in permissionClaims)
            {
                claims.Add(new Claim(XiHanClaimTypes.Permission, permission));
            }
        }

        return claims;
    }

}
