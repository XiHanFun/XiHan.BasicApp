#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthAppService
// Guid:f3f91e56-9d5b-4f30-8f75-f6f84b714442
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.UseCases.Commands;
using XiHan.BasicApp.Rbac.Application.UseCases.Queries;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.ValueObjects;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Contracts.Dtos;
using XiHan.Framework.Application.Contracts.Enums;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 认证应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
[DynamicApi(RouteTemplate = "api/auth")]
public class AuthAppService : ApplicationServiceBase, IAuthAppService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;
    private readonly IUserManager _userManager;
    private readonly IAuthorizationDomainService _authorizationDomainService;
    private readonly IRbacAuthorizationCacheService _authorizationCacheService;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ILoginLogRepository _loginLogRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDistributedCache<AuthRefreshTokenCacheItem> _refreshTokenCache;
    private readonly IDistributedCache<AuthSessionTokenMapCacheItem> _sessionTokenMapCache;
    private readonly IConfiguration _configuration;
    private readonly JwtOptions _jwtOptions;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="userManager"></param>
    /// <param name="authorizationDomainService"></param>
    /// <param name="authorizationCacheService"></param>
    /// <param name="roleRepository"></param>
    /// <param name="permissionRepository"></param>
    /// <param name="menuRepository"></param>
    /// <param name="userSessionRepository"></param>
    /// <param name="loginLogRepository"></param>
    /// <param name="jwtTokenService"></param>
    /// <param name="otpService"></param>
    /// <param name="passwordHasher"></param>
    /// <param name="refreshTokenCache"></param>
    /// <param name="sessionTokenMapCache"></param>
    /// <param name="configuration"></param>
    /// <param name="jwtOptions"></param>
    /// <param name="currentUser"></param>
    /// <param name="clientInfoProvider"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="unitOfWorkManager"></param>
    public AuthAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IAuthorizationDomainService authorizationDomainService,
        IRbacAuthorizationCacheService authorizationCacheService,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        IUserSessionRepository userSessionRepository,
        ILoginLogRepository loginLogRepository,
        IJwtTokenService jwtTokenService,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IDistributedCache<AuthRefreshTokenCacheItem> refreshTokenCache,
        IDistributedCache<AuthSessionTokenMapCacheItem> sessionTokenMapCache,
        IConfiguration configuration,
        IOptions<JwtOptions> jwtOptions,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _authorizationDomainService = authorizationDomainService;
        _authorizationCacheService = authorizationCacheService;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _userSessionRepository = userSessionRepository;
        _loginLogRepository = loginLogRepository;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _refreshTokenCache = refreshTokenCache;
        _sessionTokenMapCache = sessionTokenMapCache;
        _configuration = configuration;
        _jwtOptions = jwtOptions.Value;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 获取登录配置
    /// </summary>
    [DynamicApi(Name = "login-config")]
    public Task<ApiResponse> GetLoginConfigAsync()
    {
        var loginMethods = _configuration.GetSection("XiHan:Authentication:LoginMethods").Get<string[]>();
        var oauthProviders = _configuration.GetSection("XiHan:Authentication:OAuth:Providers").Get<string[]>();

        var response = new LoginConfigDto
        {
            TenantEnabled = _configuration.GetValue("XiHan:MultiTenancy:Enabled", true),
            LoginMethods = loginMethods is { Length: > 0 } ? [.. loginMethods] : ["password"],
            OauthProviders = oauthProviders is { Length: > 0 } ? [.. oauthProviders] : []
        };

        return Task.FromResult(Success(response));
    }

    /// <summary>
    /// 登录
    /// </summary>
    [DynamicApi(Name = "login")]
    public async Task<ApiResponse> LoginAsync(UserLoginCommand command)
    {
        command.ValidateAnnotations();
        command.UserName = command.UserName.Trim();
        var clientInfo = _clientInfoProvider.GetCurrent();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByUserNameAsync(command.UserName, command.TenantId);
        if (user is null)
        {
            await WriteLoginLogAsync(0, command, LoginResult.InvalidCredentials, clientInfo, "用户名或密码错误");
            await uow.CompleteAsync();
            return Error(ApiResponseCodes.Unauthorized, "用户名或密码错误");
        }

        if (user.Status != YesOrNo.Yes)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountDisabled, clientInfo, "账号已禁用");
            await uow.CompleteAsync();
            return Error(ApiResponseCodes.Forbidden, "账号已禁用");
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.IsLocked && security.LockoutEndTime.HasValue && security.LockoutEndTime > DateTimeOffset.UtcNow)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountLocked, clientInfo, "账号已锁定");
            await uow.CompleteAsync();
            return Error(ApiResponseCodes.Forbidden, $"账号已锁定，请 {security.LockoutEndTime:HH:mm} 后重试");
        }

        var password = PasswordValueObject.FromHash(user.Password);
        if (!password.Verify(command.Password, _passwordHasher))
        {
            await HandlePasswordFailureAsync(security);
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.InvalidCredentials, clientInfo, "用户名或密码错误");
            await uow.CompleteAsync();
            return Error(ApiResponseCodes.Unauthorized, "用户名或密码错误");
        }

        if (security.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "账号未配置双因素密钥");
                await uow.CompleteAsync();
                return Error(ApiResponseCodes.Forbidden, "账号双因素认证配置异常，请联系管理员");
            }

            var twoFactorCode = command.TwoFactorCode?.Trim();
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.RequiresTwoFactor, clientInfo, "需要双因素认证");
                await uow.CompleteAsync();
                return Error(ApiResponseCodes.Unauthorized, "请输入双因素验证码", new { RequiresTwoFactor = true });
            }

            if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, twoFactorCode))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "双因素验证码错误");
                await uow.CompleteAsync();
                return Error(ApiResponseCodes.Unauthorized, "双因素验证码错误");
            }
        }

        var revokedSessionIds = await EnforceSessionPolicyAsync(user, security);

        security.FailedLoginAttempts = 0;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;
        security.LastFailedLoginTime = null;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        user.LastLoginTime = DateTimeOffset.UtcNow;
        user.LastLoginIp = clientInfo.IpAddress;
        await _userRepository.UpdateAsync(user);

        var roleCodes = await GetUserRoleCodesAsync(user.BasicId, user.TenantId);
        var sessionId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var tokenResult = _jwtTokenService.GenerateAccessToken(BuildUserClaims(user, roleCodes, sessionId, accessTokenJti));
        var refreshToken = tokenResult.RefreshToken;

        await SaveOrUpdateSessionAsync(user, sessionId, accessTokenJti);
        await WriteLoginLogAsync(user.BasicId, command, LoginResult.Success, clientInfo, "登录成功");
        await uow.CompleteAsync();

        foreach (var revokedSessionId in revokedSessionIds)
        {
            await RemoveSessionTokenAsync(revokedSessionId);
        }

        await SaveRefreshTokenAsync(refreshToken, user, sessionId);

        var response = new AuthTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = new DateTimeOffset(tokenResult.IssuedAt),
            ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt)
        };

        return Success(response);
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [DynamicApi(Name = "refresh-token")]
    public async Task<ApiResponse> RefreshTokenAsync(RefreshTokenCommand command)
    {
        command.ValidateAnnotations();
        var refreshToken = command.RefreshToken.Trim();
        var oldRefreshTokenCacheKey = BuildRefreshTokenCacheKey(refreshToken);
        var cachedToken = await _refreshTokenCache.GetAsync(oldRefreshTokenCacheKey, hideErrors: true);
        if (cachedToken is null || cachedToken.ExpireAt <= DateTimeOffset.UtcNow)
        {
            return Error(ApiResponseCodes.Unauthorized, "刷新令牌已失效，请重新登录");
        }

        var user = await _userRepository.GetByIdAsync(cachedToken.UserId);
        if (user is null || user.Status != YesOrNo.Yes)
        {
            await _refreshTokenCache.RemoveAsync(oldRefreshTokenCacheKey, hideErrors: true);
            await _sessionTokenMapCache.RemoveAsync(BuildSessionTokenMapCacheKey(cachedToken.SessionId), hideErrors: true);
            return Error(ApiResponseCodes.Unauthorized, "登录状态已失效，请重新登录");
        }

        var existingSession = await _userSessionRepository.GetBySessionIdAsync(cachedToken.SessionId, user.TenantId);
        if (existingSession is null || existingSession.IsRevoked || !existingSession.IsOnline)
        {
            await _refreshTokenCache.RemoveAsync(oldRefreshTokenCacheKey, hideErrors: true);
            await _sessionTokenMapCache.RemoveAsync(BuildSessionTokenMapCacheKey(cachedToken.SessionId), hideErrors: true);
            return Error(ApiResponseCodes.Unauthorized, "会话已失效，请重新登录");
        }

        var roleCodes = await GetUserRoleCodesAsync(user.BasicId, user.TenantId);
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var sessionId = string.IsNullOrWhiteSpace(cachedToken.SessionId) ? Guid.NewGuid().ToString("N") : cachedToken.SessionId;
        var tokenResult = _jwtTokenService.GenerateAccessToken(
            BuildUserClaims(user, roleCodes, sessionId, accessTokenJti));

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await SaveOrUpdateSessionAsync(user, sessionId, accessTokenJti);
        await uow.CompleteAsync();

        await SaveRefreshTokenAsync(tokenResult.RefreshToken, user, sessionId);
        if (!string.Equals(oldRefreshTokenCacheKey, BuildRefreshTokenCacheKey(tokenResult.RefreshToken), StringComparison.Ordinal))
        {
            await _refreshTokenCache.RemoveAsync(oldRefreshTokenCacheKey, hideErrors: true);
        }

        var response = new AuthTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = new DateTimeOffset(tokenResult.IssuedAt),
            ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt)
        };

        return Success(response);
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [DynamicApi(Name = "me")]
    public async Task<ApiResponse> GetCurrentUserAsync()
    {
        var user = await ResolveCurrentUserEntityAsync();
        if (user is null)
        {
            return Error(ApiResponseCodes.Unauthorized, "未登录或登录已过期");
        }

        var response = new CurrentUserDto
        {
            UserId = user.BasicId,
            UserName = user.UserName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            TenantId = user.TenantId
        };
        return Success(response);
    }

    /// <summary>
    /// 获取权限上下文
    /// </summary>
    [DynamicApi(Name = "permissions")]
    public async Task<ApiResponse> GetPermissionsAsync()
    {
        var user = await ResolveCurrentUserEntityAsync();
        if (user is null)
        {
            return Error(ApiResponseCodes.Unauthorized, "未登录或登录已过期");
        }

        var roleCodes = await GetUserRoleCodesAsync(user.BasicId, user.TenantId);
        var permissionCodes = await _authorizationCacheService.GetUserPermissionCodesAsync(
            user.BasicId,
            user.TenantId,
            token => _authorizationDomainService.GetUserPermissionCodesAsync(user.BasicId, user.TenantId, token));
        var userPermissions = await _permissionRepository.GetUserPermissionsAsync(user.BasicId, user.TenantId);
        var userMenus = await _menuRepository.GetUserMenusAsync(user.BasicId, user.TenantId);

        var permissionCodeSet = permissionCodes
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var resourcePermissionMap = userPermissions
            .Where(permission =>
                permission.ResourceId > 0 &&
                !string.IsNullOrWhiteSpace(permission.PermissionCode) &&
                permissionCodeSet.Contains(permission.PermissionCode))
            .GroupBy(permission => permission.ResourceId)
            .ToDictionary(group => group.Key, group => group.Select(static permission => permission.PermissionCode).First());

        var response = new AuthPermissionDto
        {
            Roles = [.. roleCodes],
            Permissions = [.. permissionCodeSet.OrderBy(static code => code)],
            Menus = BuildMenuRoutes(userMenus, resourcePermissionMap)
        };

        return Success(response);
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [DynamicApi(Name = "logout")]
    public async Task<ApiResponse> LogoutAsync()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return Success();
        }

        var userId = _currentUser.UserId.Value;
        var tenantId = _currentUser.TenantId;
        var sessionId = _httpContextAccessor.HttpContext?.User.FindSessionId();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await MarkSessionRevokedAsync(sessionId, tenantId, "用户主动退出");
        }
        else
        {
            await _userSessionRepository.RevokeUserSessionsAsync(userId, "用户主动退出", tenantId);
        }

        await uow.CompleteAsync();

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await RemoveSessionTokenAsync(sessionId);
        }

        return Success();
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [DynamicApi(Name = "change-password")]
    public async Task<ApiResponse> ChangePasswordAsync(ChangePasswordCommand command)
    {
        command.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var currentPassword = PasswordValueObject.FromHash(user.Password);
        if (!currentPassword.Verify(command.OldPassword, _passwordHasher))
        {
            throw new InvalidOperationException("原密码错误");
        }

        await _userManager.ChangePasswordAsync(user, command.NewPassword);
        await uow.CompleteAsync();

        return Success();
    }

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    [DynamicApi(Name = "permission-codes")]
    public async Task<ApiResponse> GetPermissionCodesAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var permissionCodes = await _authorizationCacheService.GetUserPermissionCodesAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserPermissionCodesAsync(query.UserId, query.TenantId, token));
        return Success(permissionCodes);
    }

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    [DynamicApi(Name = "data-scope-department-ids")]
    public async Task<ApiResponse> GetDataScopeDepartmentIdsAsync(UserDataScopeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var departmentIds = await _authorizationCacheService.GetUserDataScopeDepartmentIdsAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserDataScopeDepartmentIdsAsync(query.UserId, query.TenantId, token));
        return Success(departmentIds);
    }

    /// <summary>
    /// 构建用户声明
    /// </summary>
    /// <param name="user"></param>
    /// <param name="roleCodes"></param>
    /// <param name="sessionId"></param>
    /// <param name="accessTokenJti"></param>
    /// <returns></returns>
    private static List<Claim> BuildUserClaims(SysUser user, IReadOnlyCollection<string> roleCodes, string sessionId, string accessTokenJti)
    {
        var claims = new List<Claim>
        {
            new(XiHanClaimTypes.UserId, user.BasicId.ToString()),
            new(XiHanClaimTypes.UserName, user.UserName),
            new(XiHanClaimTypes.SessionId, sessionId),
            new(JwtRegisteredClaimNames.Jti, accessTokenJti)
        };

        if (!string.IsNullOrWhiteSpace(user.NickName))
        {
            claims.Add(new Claim(XiHanClaimTypes.Name, user.NickName));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(XiHanClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim(XiHanClaimTypes.PhoneNumber, user.Phone));
        }

        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim(XiHanClaimTypes.TenantId, user.TenantId.Value.ToString()));
        }

        foreach (var roleCode in roleCodes.Where(static roleCode => !string.IsNullOrWhiteSpace(roleCode)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(XiHanClaimTypes.Role, roleCode));
        }

        return claims;
    }

    /// <summary>
    /// 构建菜单树
    /// </summary>
    /// <param name="menus"></param>
    /// <param name="resourcePermissionMap"></param>
    /// <returns></returns>
    private static IReadOnlyList<AuthMenuRouteDto> BuildMenuRoutes(
        IReadOnlyList<SysMenu> menus,
        IReadOnlyDictionary<long, string> resourcePermissionMap)
    {
        var routeMenus = menus
            .Where(menu => menu.MenuType != MenuType.Button)
            .OrderBy(static menu => menu.Sort)
            .ThenBy(static menu => menu.BasicId)
            .ToList();
        var menuMap = routeMenus.ToDictionary(static menu => menu.BasicId, menu => MapMenuRoute(menu, resourcePermissionMap));
        var rootMenus = new List<AuthMenuRouteDto>();

        foreach (var menu in routeMenus)
        {
            var route = menuMap[menu.BasicId];
            if (menu.ParentId.HasValue && menuMap.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children.Add(route);
            }
            else
            {
                rootMenus.Add(route);
            }
        }

        SortMenuTree(rootMenus);
        return rootMenus;
    }

    /// <summary>
    /// 映射菜单路由
    /// </summary>
    /// <param name="menu"></param>
    /// <param name="resourcePermissionMap"></param>
    /// <returns></returns>
    private static AuthMenuRouteDto MapMenuRoute(SysMenu menu, IReadOnlyDictionary<long, string> resourcePermissionMap)
    {
        var permissionCode = menu.ResourceId.HasValue && resourcePermissionMap.TryGetValue(menu.ResourceId.Value, out var permission)
            ? permission
            : null;
        return new AuthMenuRouteDto
        {
            Name = !string.IsNullOrWhiteSpace(menu.RouteName)
                ? menu.RouteName
                : !string.IsNullOrWhiteSpace(menu.MenuCode)
                    ? menu.MenuCode
                    : $"menu_{menu.BasicId}",
            Path = !string.IsNullOrWhiteSpace(menu.Path) ? menu.Path : BuildFallbackMenuPath(menu),
            Component = menu.Component,
            Redirect = menu.Redirect,
            Permission = permissionCode,
            Meta = new AuthMenuMetaDto
            {
                Title = !string.IsNullOrWhiteSpace(menu.Title) ? menu.Title : menu.MenuName,
                Icon = menu.Icon,
                Hidden = !menu.IsVisible,
                KeepAlive = menu.IsCache,
                AffixTab = menu.IsAffix,
                Permissions = string.IsNullOrWhiteSpace(permissionCode) ? [] : [permissionCode],
                Order = menu.Sort,
                Link = menu.IsExternal ? menu.ExternalUrl : null
            }
        };
    }

    /// <summary>
    /// 排序菜单树
    /// </summary>
    /// <param name="menus"></param>
    private static void SortMenuTree(List<AuthMenuRouteDto> menus)
    {
        menus.Sort((left, right) => left.Meta.Order.CompareTo(right.Meta.Order));
        foreach (var menu in menus.Where(static menu => menu.Children.Count > 0))
        {
            SortMenuTree(menu.Children);
        }
    }

    /// <summary>
    /// 构建兜底菜单路径
    /// </summary>
    /// <param name="menu"></param>
    /// <returns></returns>
    private static string BuildFallbackMenuPath(SysMenu menu)
    {
        var seed = string.IsNullOrWhiteSpace(menu.MenuCode) ? $"menu-{menu.BasicId}" : menu.MenuCode;
        var normalized = seed
            .Replace("_", "-", StringComparison.Ordinal)
            .Replace(":", "-", StringComparison.Ordinal)
            .ToLowerInvariant();
        return menu.ParentId.HasValue ? normalized : $"/{normalized}";
    }

    /// <summary>
    /// 构建刷新令牌缓存键
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    private static string BuildRefreshTokenCacheKey(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var tokenHash = Convert.ToHexString(SHA256.HashData(tokenBytes));
        return $"auth:refresh:{tokenHash}";
    }

    /// <summary>
    /// 构建会话映射缓存键
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    private static string BuildSessionTokenMapCacheKey(string sessionId)
    {
        return $"auth:session:{sessionId}";
    }

    /// <summary>
    /// 写入登录日志
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="command"></param>
    /// <param name="loginResult"></param>
    /// <param name="clientInfo"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task WriteLoginLogAsync(long userId, UserLoginCommand command, LoginResult loginResult, ClientInfo clientInfo, string message)
    {
        var log = new SysLoginLog
        {
            TenantId = command.TenantId,
            UserId = userId,
            UserName = command.UserName,
            LoginIp = clientInfo.IpAddress,
            LoginLocation = clientInfo.Location,
            Browser = clientInfo.Browser,
            Os = clientInfo.OperatingSystem,
            LoginResult = loginResult,
            Message = message,
            LoginTime = DateTimeOffset.UtcNow
        };

        await _loginLogRepository.AddAsync(log);
    }

    /// <summary>
    /// 获取当前用户实体
    /// </summary>
    /// <returns></returns>
    private async Task<SysUser?> ResolveCurrentUserEntityAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value);
        return user is not null && user.Status == YesOrNo.Yes ? user : null;
    }

    /// <summary>
    /// 获取用户角色编码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    private async Task<IReadOnlyCollection<string>> GetUserRoleCodesAsync(long userId, long? tenantId)
    {
        var userRoles = await _userRepository.GetUserRolesAsync(userId, tenantId);
        var roleIds = userRoles
            .Where(role => role.Status == YesOrNo.Yes)
            .Select(static role => role.RoleId)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
        {
            return [];
        }

        var roles = await _roleRepository.GetByIdsAsync(roleIds);
        return [.. roles
            .Where(role => role.Status == YesOrNo.Yes && !string.IsNullOrWhiteSpace(role.RoleCode))
            .OrderBy(static role => role.Sort)
            .Select(static role => role.RoleCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// 按安全策略限制会话数量
    /// </summary>
    /// <param name="user"></param>
    /// <param name="security"></param>
    /// <returns>被撤销的会话ID集合</returns>
    private async Task<IReadOnlyList<string>> EnforceSessionPolicyAsync(SysUser user, SysUserSecurity security)
    {
        var onlineSessions = await _userSessionRepository.GetOnlineSessionsAsync(user.BasicId, user.TenantId);
        if (onlineSessions.Count == 0)
        {
            return [];
        }

        List<string> toRevokeSessionIds;
        if (!security.AllowMultiLogin)
        {
            toRevokeSessionIds = [.. onlineSessions.Select(session => session.UserSessionId)];
        }
        else if (security.MaxLoginDevices > 0 && onlineSessions.Count >= security.MaxLoginDevices)
        {
            var overflowCount = onlineSessions.Count - security.MaxLoginDevices + 1;
            toRevokeSessionIds = [..
                onlineSessions
                    .OrderBy(session => session.LastActivityTime)
                    .ThenBy(session => session.LoginTime)
                    .Take(overflowCount)
                    .Select(session => session.UserSessionId)];
        }
        else
        {
            return [];
        }

        if (toRevokeSessionIds.Count == 0)
        {
            return [];
        }

        await _userSessionRepository.RevokeSessionsAsync(toRevokeSessionIds, "触发多端登录策略", user.TenantId);
        return toRevokeSessionIds;
    }

    /// <summary>
    /// 保存或更新会话
    /// </summary>
    /// <param name="user"></param>
    /// <param name="sessionId"></param>
    /// <param name="accessTokenJti"></param>
    /// <returns></returns>
    private async Task SaveOrUpdateSessionAsync(SysUser user, string sessionId, string accessTokenJti)
    {
        var now = DateTimeOffset.UtcNow;
        var clientInfo = _clientInfoProvider.GetCurrent();
        var userSession = await _userSessionRepository.GetBySessionIdAsync(sessionId, user.TenantId);
        if (userSession is null)
        {
            userSession = new SysUserSession
            {
                TenantId = user.TenantId,
                UserId = user.BasicId,
                CurrentAccessTokenJti = accessTokenJti,
                UserSessionId = sessionId,
                DeviceType = DeviceType.Web,
                DeviceName = clientInfo.DeviceName ?? "Web Browser",
                Browser = clientInfo.Browser,
                OperatingSystem = clientInfo.OperatingSystem,
                IpAddress = clientInfo.IpAddress,
                Location = clientInfo.Location,
                LoginTime = now,
                LastActivityTime = now,
                IsOnline = true,
                IsRevoked = false
            };
            await _userSessionRepository.AddAsync(userSession);
            return;
        }

        userSession.CurrentAccessTokenJti = accessTokenJti;
        userSession.LastActivityTime = now;
        userSession.IsOnline = true;
        userSession.IsRevoked = false;
        userSession.RevokedAt = null;
        userSession.RevokedReason = null;
        userSession.LogoutTime = null;
        userSession.DeviceName = clientInfo.DeviceName ?? userSession.DeviceName;
        userSession.IpAddress = clientInfo.IpAddress;
        userSession.Browser = clientInfo.Browser;
        userSession.OperatingSystem = clientInfo.OperatingSystem;
        userSession.Location = clientInfo.Location;
        await _userSessionRepository.UpdateAsync(userSession);
    }

    /// <summary>
    /// 标记会话已撤销
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="tenantId"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    private async Task MarkSessionRevokedAsync(string sessionId, long? tenantId, string reason)
    {
        var session = await _userSessionRepository.GetBySessionIdAsync(sessionId, tenantId);
        if (session is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.IsOnline = false;
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.LogoutTime = now;
        session.RevokedReason = reason;
        await _userSessionRepository.UpdateAsync(session);
    }

    /// <summary>
    /// 保存刷新令牌
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <param name="user"></param>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    private async Task SaveRefreshTokenAsync(string refreshToken, SysUser user, string sessionId)
    {
        var refreshTokenCacheKey = BuildRefreshTokenCacheKey(refreshToken);
        var expireAt = DateTimeOffset.UtcNow.AddDays(Math.Max(1, _jwtOptions.RefreshTokenExpirationDays));
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expireAt
        };

        await _refreshTokenCache.SetAsync(
            refreshTokenCacheKey,
            new AuthRefreshTokenCacheItem
            {
                UserId = user.BasicId,
                UserName = user.UserName,
                TenantId = user.TenantId,
                SessionId = sessionId,
                ExpireAt = expireAt
            },
            options: cacheOptions,
            hideErrors: true);

        await _sessionTokenMapCache.SetAsync(
            BuildSessionTokenMapCacheKey(sessionId),
            new AuthSessionTokenMapCacheItem
            {
                RefreshTokenCacheKey = refreshTokenCacheKey
            },
            options: cacheOptions,
            hideErrors: true);
    }

    /// <summary>
    /// 移除会话令牌
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    private async Task RemoveSessionTokenAsync(string sessionId)
    {
        var sessionTokenMapCacheKey = BuildSessionTokenMapCacheKey(sessionId);
        var tokenMap = await _sessionTokenMapCache.GetAsync(sessionTokenMapCacheKey, hideErrors: true);
        if (tokenMap is not null && !string.IsNullOrWhiteSpace(tokenMap.RefreshTokenCacheKey))
        {
            await _refreshTokenCache.RemoveAsync(tokenMap.RefreshTokenCacheKey, hideErrors: true);
        }

        await _sessionTokenMapCache.RemoveAsync(sessionTokenMapCacheKey, hideErrors: true);
    }

    /// <summary>
    /// 成功响应
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private ApiResponse Success(object? data = null)
    {
        return ApiResponse.Success(data, _httpContextAccessor.HttpContext?.TraceIdentifier);
    }

    /// <summary>
    /// 失败响应
    /// </summary>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private ApiResponse Error(ApiResponseCodes code, string message, object? data = null)
    {
        return new ApiResponse
        {
            Code = code,
            Message = message,
            Data = data,
            TraceId = _httpContextAccessor.HttpContext?.TraceIdentifier
        };
    }

    /// <summary>
    /// 确保安全配置文件
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private async Task<SysUserSecurity> EnsureSecurityProfileAsync(SysUser user)
    {
        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId);
        if (security is not null)
        {
            return security;
        }

        security = new SysUserSecurity
        {
            TenantId = user.TenantId,
            UserId = user.BasicId,
            FailedLoginAttempts = 0,
            IsLocked = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        return await _userRepository.SaveSecurityAsync(security);
    }

    /// <summary>
    /// 处理密码失败
    /// </summary>
    /// <param name="security"></param>
    /// <returns></returns>
    private async Task HandlePasswordFailureAsync(SysUserSecurity security)
    {
        security.FailedLoginAttempts += 1;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;

        if (security.FailedLoginAttempts >= MaxFailedAttempts)
        {
            security.IsLocked = true;
            security.LockoutTime = DateTimeOffset.UtcNow;
            security.LockoutEndTime = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
        }

        await _userRepository.SaveSecurityAsync(security);
    }
}
