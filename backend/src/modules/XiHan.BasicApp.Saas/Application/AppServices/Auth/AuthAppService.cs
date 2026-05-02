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

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 认证应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "认证", RouteTemplate = "api/Auth")]
public sealed class AuthAppService(
    IUserRepository userRepository,
    IUserSecurityRepository userSecurityRepository,
    ITenantRepository tenantRepository,
    ITenantUserRepository tenantUserRepository,
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    IPermissionRepository permissionRepository,
    IMenuRepository menuRepository,
    IUserSessionRepository userSessionRepository,
    IOAuthTokenRepository oauthTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor)
    : SaasApplicationService, IAuthAppService
{
    private const string SuperAdminRoleCode = "super_admin";
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserSecurityRepository _userSecurityRepository = userSecurityRepository;
    private readonly ITenantRepository _tenantRepository = tenantRepository;
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;
    private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;
    private readonly IPermissionRepository _permissionRepository = permissionRepository;
    private readonly IMenuRepository _menuRepository = menuRepository;
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository = oauthTokenRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    [AllowAnonymous]
    public Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new LoginConfigDto());
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
        var tenant = await GetLoginTenantOrThrowAsync(input.TenantId, cancellationToken);
        var effectiveTenantId = tenant?.BasicId;
        using var tenantScope = _currentTenant.Change(effectiveTenantId, tenant?.TenantName);

        var now = DateTimeOffset.UtcNow;
        var user = await _userRepository.GetByUserNameAsync(userName, cancellationToken)
            ?? throw new InvalidOperationException("用户名或密码错误。");
        if (user.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("用户已被禁用。");
        }

        await EnsureTenantMembershipValidAsync(user.BasicId, effectiveTenantId, now, cancellationToken);

        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == user.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("用户安全记录不存在。");
        await EnsureSecurityCanLoginAsync(security, now, cancellationToken);

        if (!_passwordHasher.VerifyPassword(security.Password, password))
        {
            await RecordFailedLoginAsync(security, now, cancellationToken);
            throw new InvalidOperationException("用户名或密码错误。");
        }

        if (security.PasswordExpiryTime.HasValue && security.PasswordExpiryTime.Value <= now)
        {
            throw new InvalidOperationException("密码已过期，请联系管理员重置密码。");
        }

        if (security.TwoFactorEnabled && security.TwoFactorMethod != TwoFactorMethod.None)
        {
            return BuildTwoFactorChallenge(security);
        }

        var authSnapshot = await BuildAuthorizationSnapshotAsync(user.BasicId, now, cancellationToken);
        var client = ResolveClientInfo();
        var sessionBusinessId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var claims = BuildClaims(user, effectiveTenantId, sessionBusinessId, accessTokenJti, authSnapshot.Roles, input.DeviceId);
        var tokenResult = _jwtTokenService.GenerateAccessToken(claims);

        user.LastLoginTime = now;
        user.LastLoginIp = client.IpAddress;
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;
        security.LastSecurityCheckTime = now;

        var session = await CreateLoginSessionAsync(user, sessionBusinessId, accessTokenJti, tokenResult, input.DeviceId, client, now, cancellationToken);
        await CreateOAuthTokenAsync(user, session, accessTokenJti, tokenResult, now, cancellationToken);
        _ = await _userRepository.UpdateAsync(user, cancellationToken);
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        await TouchTenantMembershipAsync(user.BasicId, effectiveTenantId, now, cancellationToken);

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

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");

        return new UserInfoDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            NickName = user.NickName ?? user.RealName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            TenantId = _currentUser.TenantId,
            Roles = [.. _currentUser.Roles]
        };
    }

    /// <inheritdoc />
    public async Task<PermissionInfoDto> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        using var tenantScope = _currentTenant.Change(_currentUser.TenantId, _currentUser.TenantId?.ToString());

        var now = DateTimeOffset.UtcNow;
        var snapshot = await BuildAuthorizationSnapshotAsync(userId, now, cancellationToken);
        var menus = await BuildMenuRoutesAsync(snapshot, cancellationToken);

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
        var session = await _userSessionRepository.GetFirstAsync(
            item => item.UserId == userId.Value && item.UserSessionId == sessionBusinessId,
            cancellationToken);
        if (session is null)
        {
            return;
        }

        session.IsOnline = false;
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.RevokedReason = "用户主动退出";
        session.LogoutTime = now;
        _ = await _userSessionRepository.UpdateAsync(session, cancellationToken);

        var tokens = await _oauthTokenRepository.GetListAsync(item => item.SessionId == session.BasicId && !item.IsRevoked, cancellationToken);
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedTime = now;
        }

        if (tokens.Count > 0)
        {
            _ = await _oauthTokenRepository.UpdateRangeAsync(tokens, cancellationToken);
        }
    }

    private async Task<SysTenant?> GetLoginTenantOrThrowAsync(long? tenantId, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue || tenantId.Value <= 0)
        {
            return null;
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken)
            ?? throw new InvalidOperationException("租户不存在。");
        if (tenant.TenantStatus != TenantStatus.Normal)
        {
            throw new InvalidOperationException("租户当前不可登录。");
        }

        if (tenant.ConfigStatus is not TenantConfigStatus.Configured)
        {
            throw new InvalidOperationException("租户尚未完成初始化配置。");
        }

        if (tenant.ExpireTime.HasValue && tenant.ExpireTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("租户已过期。");
        }

        return tenant;
    }

    private async Task EnsureTenantMembershipValidAsync(long userId, long? tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue)
        {
            return;
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("用户不是当前租户成员。");
        if (membership.InviteStatus != TenantMemberInviteStatus.Accepted || membership.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("用户当前租户成员身份无效。");
        }

        if (membership.EffectiveTime.HasValue && membership.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("用户当前租户成员身份尚未生效。");
        }

        if (membership.ExpirationTime.HasValue && membership.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("用户当前租户成员身份已过期。");
        }
    }

    private async Task EnsureSecurityCanLoginAsync(SysUserSecurity security, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!security.IsLocked)
        {
            return;
        }

        if (security.LockoutEndTime.HasValue && security.LockoutEndTime.Value <= now)
        {
            security.IsLocked = false;
            security.LockoutTime = null;
            security.LockoutEndTime = null;
            security.FailedLoginAttempts = 0;
            security.LastFailedLoginTime = null;
            _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
            return;
        }

        throw new InvalidOperationException("账号已被锁定，请稍后再试。");
    }

    private async Task RecordFailedLoginAsync(SysUserSecurity security, DateTimeOffset now, CancellationToken cancellationToken)
    {
        security.FailedLoginAttempts++;
        security.LastFailedLoginTime = now;

        if (security.FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            security.IsLocked = true;
            security.LockoutTime = now;
            security.LockoutEndTime = now.Add(LockoutDuration);
        }

        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
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

    private async Task<AuthorizationSnapshot> BuildAuthorizationSnapshotAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken);
        var roles = await _roleRepository.GetEnabledByIdsAsync(userRoles.Select(item => item.RoleId), cancellationToken);
        var roleCodes = roles
            .Select(role => role.RoleCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var isSuperAdmin = roleCodes.Contains(SuperAdminRoleCode, StringComparer.OrdinalIgnoreCase);

        if (isSuperAdmin)
        {
            var allPermissions = await _permissionRepository.GetListAsync(permission => permission.Status == EnableStatus.Enabled, cancellationToken);
            var permissionIds = allPermissions.Select(permission => permission.BasicId).ToHashSet();
            var superAdminPermissionCodes = allPermissions
                .Select(permission => permission.PermissionCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();
            superAdminPermissionCodes.Insert(0, "*");
            return new AuthorizationSnapshot(roleCodes, superAdminPermissionCodes, permissionIds);
        }

        var rolePermissions = await _rolePermissionRepository.GetValidByRoleIdsAsync(roles.Select(role => role.BasicId), now, cancellationToken);
        var roleGrantIds = rolePermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Grant)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        var roleDenyIds = rolePermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Deny)
            .Select(permission => permission.PermissionId)
            .ToHashSet();

        roleGrantIds.ExceptWith(roleDenyIds);

        var userPermissions = await _userPermissionRepository.GetValidByUserIdAsync(userId, now, cancellationToken);
        var userGrantIds = userPermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Grant)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        var userDenyIds = userPermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Deny)
            .Select(permission => permission.PermissionId)
            .ToHashSet();

        var finalPermissionIds = roleGrantIds;
        finalPermissionIds.UnionWith(userGrantIds);
        finalPermissionIds.ExceptWith(userDenyIds);

        var permissions = await _permissionRepository.GetByIdsAsync(finalPermissionIds, cancellationToken);
        var permissionCodes = permissions
            .Where(permission => permission.Status == EnableStatus.Enabled)
            .Select(permission => permission.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthorizationSnapshot(roleCodes, permissionCodes, finalPermissionIds);
    }

    private async Task<List<MenuRouteDto>> BuildMenuRoutesAsync(AuthorizationSnapshot snapshot, CancellationToken cancellationToken)
    {
        var hasAllPermissions = snapshot.Permissions.Contains("*", StringComparer.OrdinalIgnoreCase);
        var allPermissions = await _permissionRepository.GetListAsync(permission => permission.Status == EnableStatus.Enabled, cancellationToken);
        var permissionCodeMap = allPermissions.ToDictionary(permission => permission.BasicId, permission => permission.PermissionCode);
        var menus = await _menuRepository.GetListAsync(
            menu => menu.Status == EnableStatus.Enabled && menu.MenuType != MenuType.Button,
            cancellationToken);
        var visibleMenus = menus
            .Where(menu => menu.IsVisible)
            .Where(menu => !menu.PermissionId.HasValue || hasAllPermissions || snapshot.PermissionIds.Contains(menu.PermissionId.Value))
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.BasicId)
            .ToList();

        if (visibleMenus.Count == 0)
        {
            return [BuildFallbackDashboardRoute()];
        }

        var routeMap = visibleMenus.ToDictionary(menu => menu.BasicId, menu => ToMenuRoute(menu, permissionCodeMap));
        var roots = new List<MenuRouteDto>();
        foreach (var menu in visibleMenus)
        {
            var route = routeMap[menu.BasicId];
            if (menu.ParentId.HasValue && routeMap.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children ??= [];
                parent.Children.Add(route);
            }
            else
            {
                roots.Add(route);
            }
        }

        return roots.Count == 0 ? [BuildFallbackDashboardRoute()] : roots;
    }

    private async Task<SysUserSession> CreateLoginSessionAsync(
        SysUser user,
        string sessionBusinessId,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        string? deviceId,
        ClientInfo client,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = new SysUserSession
        {
            UserId = user.BasicId,
            CurrentAccessTokenJti = accessTokenJti,
            UserSessionId = sessionBusinessId,
            DeviceType = DeviceType.Web,
            DeviceName = "Web",
            DeviceId = NormalizeNullable(deviceId, 200),
            Browser = client.UserAgent,
            OperatingSystem = client.UserAgent,
            IpAddress = client.IpAddress,
            LoginTime = now,
            LastActivityTime = now,
            IsOnline = true,
            IsRevoked = false,
            ExpiresAt = ToDateTimeOffset(tokenResult.ExpiresAt)
        };

        return await _userSessionRepository.AddAsync(session, cancellationToken);
    }

    private async Task CreateOAuthTokenAsync(
        SysUser user,
        SysUserSession session,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var oauthToken = new SysOAuthToken
        {
            SessionId = session.BasicId,
            AccessTokenJti = accessTokenJti,
            AccessToken = null,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ClientId = "basicapp-web",
            UserId = user.BasicId,
            GrantType = GrantType.Password,
            Scopes = "basicapp",
            Status = EnableStatus.Enabled,
            AccessTokenExpiresTime = ToDateTimeOffset(tokenResult.ExpiresAt),
            RefreshTokenExpiresTime = now.AddDays(7),
            IsRevoked = false
        };

        _ = await _oauthTokenRepository.AddAsync(oauthToken, cancellationToken);
    }

    private async Task TouchTenantMembershipAsync(long userId, long? tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!tenantId.HasValue)
        {
            return;
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken);
        if (membership is null)
        {
            return;
        }

        membership.LastActiveTime = now;
        _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
    }

    private List<Claim> BuildClaims(
        SysUser user,
        long? tenantId,
        string sessionBusinessId,
        string accessTokenJti,
        IReadOnlyCollection<string> roles,
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

        return claims;
    }

    private ClientInfo ResolveClientInfo()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        return new ClientInfo(
            NormalizeNullable(ipAddress, 50),
            NormalizeNullable(userAgent, 100));
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

    private static MenuRouteDto ToMenuRoute(SysMenu menu, IReadOnlyDictionary<long, string> permissionCodeMap)
    {
        var permissionCodes = menu.PermissionId.HasValue && permissionCodeMap.TryGetValue(menu.PermissionId.Value, out var code)
            ? new List<string> { code }
            : null;

        return new MenuRouteDto
        {
            BasicId = menu.BasicId.ToString(),
            Path = string.IsNullOrWhiteSpace(menu.Path) ? $"/{menu.MenuCode}" : menu.Path.Trim(),
            Name = string.IsNullOrWhiteSpace(menu.RouteName) ? ToRouteName(menu.MenuCode) : menu.RouteName.Trim(),
            Component = menu.IsExternal ? null : NormalizeComponent(menu.Component),
            Redirect = NormalizeNullable(menu.Redirect, 200),
            Meta = new MenuMetaDto
            {
                Title = string.IsNullOrWhiteSpace(menu.Title) ? menu.MenuName : menu.Title.Trim(),
                Icon = NormalizeNullable(menu.Icon, 100),
                Hidden = !menu.IsVisible,
                KeepAlive = menu.IsCache,
                AffixTab = menu.IsAffix,
                Permissions = permissionCodes,
                Order = menu.Sort,
                Badge = NormalizeNullable(menu.Badge, 50),
                BadgeType = NormalizeNullable(menu.BadgeType, 20),
                Dot = menu.BadgeDot,
                Link = menu.IsExternal ? NormalizeNullable(menu.ExternalUrl, 500) : null
            }
        };
    }

    private static MenuRouteDto BuildFallbackDashboardRoute()
    {
        return new MenuRouteDto
        {
            Path = "/workbench/dashboard",
            Name = "Dashboard",
            Component = "workbench/dashboard/index",
            Meta = new MenuMetaDto
            {
                Title = "menu.dashboard",
                Icon = "lucide:layout-dashboard",
                AffixTab = true,
                Order = 1
            }
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

    private static string? NormalizeComponent(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Replace('\\', '/').TrimStart('/').Replace(".vue", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToRouteName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "DynamicRoute";
        }

        var chars = value
            .Split([':', '-', '_', '.', '/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(ToPascalSegment);
        var routeName = string.Concat(chars);
        return string.IsNullOrWhiteSpace(routeName) ? "DynamicRoute" : routeName;
    }

    private static string ToPascalSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 1
            ? trimmed.ToUpperInvariant()
            : char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
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

    private sealed record AuthorizationSnapshot(
        List<string> Roles,
        List<string> Permissions,
        HashSet<long> PermissionIds);

    private sealed record ClientInfo(string? IpAddress, string? UserAgent);
}
