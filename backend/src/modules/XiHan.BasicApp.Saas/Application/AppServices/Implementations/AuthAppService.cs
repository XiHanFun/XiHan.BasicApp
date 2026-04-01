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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 认证应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
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
    private readonly IDistributedCache<AuthVerificationCodeCacheItem> _verificationCodeCache;
    private readonly IConfiguration _configuration;
    private readonly JwtOptions _jwtOptions;
    private readonly IHostEnvironment _hostEnvironment;
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
    /// <param name="verificationCodeCache"></param>
    /// <param name="configuration"></param>
    /// <param name="jwtOptions"></param>
    /// <param name="hostEnvironment"></param>
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
        IDistributedCache<AuthVerificationCodeCacheItem> verificationCodeCache,
        IConfiguration configuration,
        IOptions<JwtOptions> jwtOptions,
        IHostEnvironment hostEnvironment,
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
        _verificationCodeCache = verificationCodeCache;
        _configuration = configuration;
        _jwtOptions = jwtOptions.Value;
        _hostEnvironment = hostEnvironment;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 获取登录配置
    /// </summary>
    public Task<LoginConfigDto> GetLoginConfigAsync()
    {
        var loginMethods = _configuration.GetSection("XiHan:Authentication:LoginMethods").Get<string[]>();
        var oauthProviders = _configuration.GetSection("XiHan:Authentication:OAuth:Providers").Get<string[]>();

        var response = new LoginConfigDto
        {
            TenantEnabled = _configuration.GetValue("XiHan:MultiTenancy:Enabled", true),
            LoginMethods = loginMethods is { Length: > 0 } ? [.. loginMethods] : ["password"],
            OauthProviders = oauthProviders is { Length: > 0 } ? [.. oauthProviders] : []
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 登录（返回令牌或双因素验证挑战）
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(UserLoginCommand command)
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
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        if (user.Status != YesOrNo.Yes)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountDisabled, clientInfo, "账号已禁用");
            await uow.CompleteAsync();
            throw new BusinessException(message: "账号已禁用");
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.IsLocked && security.LockoutEndTime.HasValue && security.LockoutEndTime > DateTimeOffset.UtcNow)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountLocked, clientInfo, "账号已锁定");
            await uow.CompleteAsync();
            throw new BusinessException(message: $"账号已锁定，请 {security.LockoutEndTime:HH:mm} 后重试");
        }

        var password = PasswordValueObject.FromHash(user.Password);
        if (!password.Verify(command.Password, _passwordHasher))
        {
            await HandlePasswordFailureAsync(security);
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.InvalidCredentials, clientInfo, "用户名或密码错误");
            await uow.CompleteAsync();
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        if (security.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "账号未配置双因素密钥");
                await uow.CompleteAsync();
                throw new BusinessException(message: "账号双因素认证配置异常，请联系管理员");
            }

            var twoFactorCode = command.TwoFactorCode?.Trim();

            // 未提供验证码 → HTTP 200 + requiresTwoFactor，前端据此展示 OTP 输入
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.RequiresTwoFactor, clientInfo, "需要双因素认证");
                await uow.CompleteAsync();
                return new LoginResponseDto { RequiresTwoFactor = true };
            }

            // 验证码错误 → BusinessException(400)，与凭据错误的 401 区分
            if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, twoFactorCode))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "双因素验证码错误");
                await uow.CompleteAsync();
                throw new BusinessException(message: "双因素验证码错误或已过期，请重新输入");
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

        return new LoginResponseDto
        {
            RequiresTwoFactor = false,
            Token = new AuthTokenDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                TokenType = tokenResult.TokenType,
                ExpiresIn = tokenResult.ExpiresIn,
                IssuedAt = new DateTimeOffset(tokenResult.IssuedAt),
                ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt)
            }
        };
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task RegisterAsync(UserRegisterCommand command)
    {
        command.ValidateAnnotations();
        command.UserName = command.UserName.Trim();
        command.Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        command.Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim();
        command.NickName = string.IsNullOrWhiteSpace(command.NickName) ? null : command.NickName.Trim();
        var tenantId = NormalizeTenantId(command.TenantId);

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var existingByEmail = await _userRepository.GetByEmailAsync(command.Email, tenantId);
            if (existingByEmail is not null)
            {
                throw new BusinessException(message: "邮箱已被占用");
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            var existingByPhone = await _userRepository.GetByPhoneAsync(command.Phone, tenantId);
            if (existingByPhone is not null)
            {
                throw new BusinessException(message: "手机号已被占用");
            }
        }

        var user = new SysUser
        {
            TenantId = tenantId,
            UserName = command.UserName,
            RealName = command.UserName,
            NickName = command.NickName ?? command.UserName,
            Email = command.Email,
            Phone = command.Phone,
            Status = YesOrNo.Yes,
            Language = "zh-CN"
        };

        var created = await _userManager.CreateAsync(user, command.Password);
        var defaultRoleId = await ResolveDefaultRoleIdAsync(created.TenantId);
        if (defaultRoleId.HasValue)
        {
            await _userRepository.ReplaceUserRolesAsync(created.BasicId, [defaultRoleId.Value], created.TenantId);
        }

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 发送手机登录验证码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<AuthVerificationCodeDto> SendPhoneLoginCodeAsync(SendPhoneLoginCodeCommand command)
    {
        command.ValidateAnnotations();
        var phone = command.Phone.Trim();
        var tenantId = NormalizeTenantId(command.TenantId);
        var expiresInSeconds = Math.Clamp(_configuration.GetValue("XiHan:Authentication:PhoneCodeExpiresInSeconds", 300), 60, 1800);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        var user = await _userRepository.GetByPhoneAsync(phone, tenantId);
        if (user is not null && user.Status == YesOrNo.Yes)
        {
            var code = GenerateNumericCode(6);
            var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
            await _verificationCodeCache.SetAsync(
                BuildPhoneLoginCodeCacheKey(tenantId, phone),
                new AuthVerificationCodeCacheItem
                {
                    Purpose = "PhoneLogin",
                    Target = phone,
                    TenantId = tenantId,
                    Code = code,
                    ExpireAt = expireAt
                },
                options: new DistributedCacheEntryOptions { AbsoluteExpiration = expireAt },
                hideErrors: true);

            return new AuthVerificationCodeDto
            {
                ExpiresInSeconds = expiresInSeconds,
                DebugCode = exposeDebugSecrets ? code : null
            };
        }

        return new AuthVerificationCodeDto
        {
            ExpiresInSeconds = expiresInSeconds
        };
    }

    /// <summary>
    /// 手机验证码登录
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<AuthTokenDto> PhoneLoginAsync(PhoneLoginCommand command)
    {
        command.ValidateAnnotations();
        var phone = command.Phone.Trim();
        var code = command.Code.Trim();
        var tenantId = NormalizeTenantId(command.TenantId);
        var clientInfo = _clientInfoProvider.GetCurrent();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var cacheKey = BuildPhoneLoginCodeCacheKey(tenantId, phone);
        var cachedCode = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);
        if (cachedCode is null
            || cachedCode.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cachedCode.Code, code, StringComparison.Ordinal))
        {
            await WriteLoginLogAsync(0, phone, tenantId, LoginResult.InvalidCredentials, clientInfo, "手机号或验证码错误");
            await uow.CompleteAsync();
            throw new UnauthorizedAccessException("手机号或验证码错误");
        }

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);

        var user = await _userRepository.GetByPhoneAsync(phone, tenantId);
        if (user is null)
        {
            await WriteLoginLogAsync(0, phone, tenantId, LoginResult.InvalidCredentials, clientInfo, "手机号或验证码错误");
            await uow.CompleteAsync();
            throw new UnauthorizedAccessException("手机号或验证码错误");
        }

        if (user.Status != YesOrNo.Yes)
        {
            await WriteLoginLogAsync(user.BasicId, user.UserName, tenantId, LoginResult.AccountDisabled, clientInfo, "账号已禁用");
            await uow.CompleteAsync();
            throw new BusinessException(message: "账号已禁用");
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.IsLocked && security.LockoutEndTime.HasValue && security.LockoutEndTime > DateTimeOffset.UtcNow)
        {
            await WriteLoginLogAsync(user.BasicId, user.UserName, tenantId, LoginResult.AccountLocked, clientInfo, "账号已锁定");
            await uow.CompleteAsync();
            throw new BusinessException(message: $"账号已锁定，请 {security.LockoutEndTime:HH:mm} 后重试");
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
        await WriteLoginLogAsync(user.BasicId, user.UserName, tenantId, LoginResult.Success, clientInfo, "手机验证码登录成功");
        await uow.CompleteAsync();

        foreach (var revokedSessionId in revokedSessionIds)
        {
            await RemoveSessionTokenAsync(revokedSessionId);
        }

        await SaveRefreshTokenAsync(refreshToken, user, sessionId);

        return new AuthTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = new DateTimeOffset(tokenResult.IssuedAt),
            ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt)
        };
    }

    /// <summary>
    /// 申请重置密码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<PasswordResetResultDto> RequestPasswordResetAsync(RequestPasswordResetCommand command)
    {
        command.ValidateAnnotations();
        var email = command.Email.Trim();
        var tenantId = NormalizeTenantId(command.TenantId);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByEmailAsync(email, tenantId);
        if (user is null || user.Status != YesOrNo.Yes)
        {
            await uow.CompleteAsync();
            return new PasswordResetResultDto { Accepted = true };
        }

        var temporaryPassword = GenerateTemporaryPassword();
        await _userManager.ChangePasswordAsync(user, temporaryPassword);
        await uow.CompleteAsync();

        return new PasswordResetResultDto
        {
            Accepted = true,
            TemporaryPassword = exposeDebugSecrets ? temporaryPassword : null
        };
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public async Task<AuthTokenDto> RefreshTokenAsync(RefreshTokenCommand command)
    {
        command.ValidateAnnotations();
        var refreshToken = command.RefreshToken.Trim();
        var oldRefreshTokenCacheKey = BuildRefreshTokenCacheKey(refreshToken);
        var cachedToken = await _refreshTokenCache.GetAsync(oldRefreshTokenCacheKey, hideErrors: true);
        if (cachedToken is null || cachedToken.ExpireAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("刷新令牌已失效，请重新登录");
        }

        var user = await _userRepository.GetByIdAsync(cachedToken.UserId);
        if (user is null || user.Status != YesOrNo.Yes)
        {
            await _refreshTokenCache.RemoveAsync(oldRefreshTokenCacheKey, hideErrors: true);
            await _sessionTokenMapCache.RemoveAsync(BuildSessionTokenMapCacheKey(cachedToken.SessionId), hideErrors: true);
            throw new UnauthorizedAccessException("登录状态已失效，请重新登录");
        }

        var existingSession = await _userSessionRepository.GetBySessionIdAsync(cachedToken.SessionId, user.TenantId);
        if (existingSession is null || existingSession.IsRevoked || !existingSession.IsOnline)
        {
            await _refreshTokenCache.RemoveAsync(oldRefreshTokenCacheKey, hideErrors: true);
            await _sessionTokenMapCache.RemoveAsync(BuildSessionTokenMapCacheKey(cachedToken.SessionId), hideErrors: true);
            throw new UnauthorizedAccessException("会话已失效，请重新登录");
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

        return new AuthTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = new DateTimeOffset(tokenResult.IssuedAt),
            ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt)
        };
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<CurrentUserDto> GetCurrentUserAsync()
    {
        var user = await ResolveCurrentUserEntityAsync();
        if (user is null)
        {
            throw new UnauthorizedAccessException("未登录或登录已过期");
        }

        return new CurrentUserDto
        {
            UserId = user.BasicId,
            UserName = user.UserName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            TenantId = user.TenantId
        };
    }

    /// <summary>
    /// 获取权限上下文
    /// </summary>
    public async Task<AuthPermissionDto> GetPermissionsAsync()
    {
        var user = await ResolveCurrentUserEntityAsync();
        if (user is null)
        {
            throw new UnauthorizedAccessException("未登录或登录已过期");
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

        return new AuthPermissionDto
        {
            Roles = [.. roleCodes],
            Permissions = [.. permissionCodeSet.OrderBy(static code => code)],
            Menus = BuildMenuRoutes(userMenus, resourcePermissionMap)
        };
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    public async Task LogoutAsync()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return;
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
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordCommand command)
    {
        command.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var currentPassword = PasswordValueObject.FromHash(user.Password);
        if (!currentPassword.Verify(command.OldPassword, _passwordHasher))
        {
            throw new BusinessException(message: "原密码错误");
        }

        await _userManager.ChangePasswordAsync(user, command.NewPassword);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query.UserId));
        }

        var permissionCodes = await _authorizationCacheService.GetUserPermissionCodesAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserPermissionCodesAsync(query.UserId, query.TenantId, token));
        return permissionCodes;
    }

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    public async Task<IReadOnlyCollection<long>> GetDataScopeDepartmentIdsAsync(UserDataScopeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query.UserId));
        }

        var departmentIds = await _authorizationCacheService.GetUserDataScopeDepartmentIdsAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserDataScopeDepartmentIdsAsync(query.UserId, query.TenantId, token));
        return departmentIds;
    }

    /// <summary>
    /// 获取当前用户完整档案
    /// </summary>
    public async Task<UserProfileDto> GetProfileAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId);

        return new UserProfileDto
        {
            UserId = user.BasicId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            Gender = (int)user.Gender,
            Birthday = user.Birthday,
            TimeZone = user.TimeZone,
            Language = user.Language,
            Country = user.Country,
            Remark = user.Remark,
            TenantId = user.TenantId,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
            TwoFactorEnabled = security?.TwoFactorEnabled ?? false,
            EmailVerified = security?.EmailVerified ?? false,
            PhoneVerified = security?.PhoneVerified ?? false,
            LastPasswordChangeTime = security?.LastPasswordChangeTime
        };
    }

    /// <summary>
    /// 更新当前用户个人资料
    /// </summary>
    public async Task UpdateProfileAsync(UpdateProfileCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        if (command.NickName is not null) user.NickName = command.NickName.Trim();
        if (command.RealName is not null) user.RealName = command.RealName.Trim();
        if (command.Avatar is not null) user.Avatar = command.Avatar.Trim();
        if (command.Gender.HasValue) user.Gender = command.Gender.Value;
        if (command.Birthday.HasValue) user.Birthday = command.Birthday.Value;
        if (command.TimeZone is not null) user.TimeZone = command.TimeZone.Trim();
        if (command.Language is not null) user.Language = command.Language.Trim();
        if (command.Country is not null) user.Country = command.Country.Trim();
        if (command.Remark is not null) user.Remark = command.Remark.Trim();

        // 邮箱/手机号变更需要重新验证
        if (command.Email is not null && !string.Equals(command.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingByEmail = await _userRepository.GetByEmailAsync(command.Email.Trim(), user.TenantId);
            if (existingByEmail is not null && existingByEmail.BasicId != user.BasicId)
            {
                throw new BusinessException(message: "该邮箱已被其他用户使用");
            }

            user.Email = command.Email.Trim();
            var security = await EnsureSecurityProfileAsync(user);
            security.EmailVerified = false;
            await _userRepository.SaveSecurityAsync(security);
        }

        if (command.Phone is not null && !string.Equals(command.Phone.Trim(), user.Phone, StringComparison.Ordinal))
        {
            var existingByPhone = await _userRepository.GetByPhoneAsync(command.Phone.Trim(), user.TenantId);
            if (existingByPhone is not null && existingByPhone.BasicId != user.BasicId)
            {
                throw new BusinessException(message: "该手机号已被其他用户使用");
            }

            user.Phone = command.Phone.Trim();
            var security = await EnsureSecurityProfileAsync(user);
            security.PhoneVerified = false;
            await _userRepository.SaveSecurityAsync(security);
        }

        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 获取当前用户活跃会话列表
    /// </summary>
    public async Task<IReadOnlyList<UserSessionItemDto>> GetSessionsAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var currentSessionId = _httpContextAccessor.HttpContext?.User.FindSessionId();
        var sessions = await _userSessionRepository.GetOnlineSessionsAsync(user.BasicId, user.TenantId);

        return [.. sessions
            .OrderByDescending(s => s.LastActivityTime)
            .Select(s => new UserSessionItemDto
            {
                SessionId = s.UserSessionId,
                DeviceName = s.DeviceName,
                DeviceType = (int)s.DeviceType,
                Browser = s.Browser,
                OperatingSystem = s.OperatingSystem,
                IpAddress = s.IpAddress,
                Location = s.Location,
                LoginTime = s.LoginTime,
                LastActivityTime = s.LastActivityTime,
                IsCurrent = string.Equals(s.UserSessionId, currentSessionId, StringComparison.Ordinal)
            })];
    }

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    public async Task RevokeSessionAsync(RevokeSessionCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var session = await _userSessionRepository.GetBySessionIdAsync(command.SessionId, user.TenantId);
        if (session is null || session.UserId != user.BasicId)
        {
            throw new BusinessException(message: "会话不存在或无权操作");
        }

        var currentSessionId = _httpContextAccessor.HttpContext?.User.FindSessionId();
        if (string.Equals(session.UserSessionId, currentSessionId, StringComparison.Ordinal))
        {
            throw new BusinessException(message: "不能撤销当前会话，请使用退出登录");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await MarkSessionRevokedAsync(command.SessionId, user.TenantId, "用户在个人中心主动撤销");
        await uow.CompleteAsync();

        await RemoveSessionTokenAsync(command.SessionId);
    }

    /// <summary>
    /// 撤销当前用户其他所有会话
    /// </summary>
    public async Task RevokeOtherSessionsAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var currentSessionId = _httpContextAccessor.HttpContext?.User.FindSessionId();
        var sessions = await _userSessionRepository.GetOnlineSessionsAsync(user.BasicId, user.TenantId);
        var otherSessionIds = sessions
            .Where(s => !string.Equals(s.UserSessionId, currentSessionId, StringComparison.Ordinal))
            .Select(s => s.UserSessionId)
            .ToArray();

        if (otherSessionIds.Length == 0) return;

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _userSessionRepository.RevokeSessionsAsync(otherSessionIds, "用户在个人中心撤销所有其他会话", user.TenantId);
        await uow.CompleteAsync();

        foreach (var sessionId in otherSessionIds)
        {
            await RemoveSessionTokenAsync(sessionId);
        }
    }

    /// <summary>
    /// 初始化双因素认证
    /// </summary>
    public async Task<TwoFactorSetupResultDto> Setup2FAAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await EnsureSecurityProfileAsync(user);
        if (security.TwoFactorEnabled)
        {
            throw new BusinessException(message: "双因素认证已启用，请先禁用再重新设置");
        }

        var secret = _otpService.GenerateTotpSecret();
        var uri = _otpService.GenerateTotpUri(secret, "XiHan", user.UserName);

        // 暂存密钥，等待用户验证后正式启用
        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        security.TwoFactorSecret = secret;
        await _userRepository.SaveSecurityAsync(security);
        await uow.CompleteAsync();

        return new TwoFactorSetupResultDto
        {
            SharedKey = secret,
            AuthenticatorUri = uri
        };
    }

    /// <summary>
    /// 验证并启用双因素认证
    /// </summary>
    public async Task Enable2FAAsync(Enable2FACommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await EnsureSecurityProfileAsync(user);
        if (security.TwoFactorEnabled)
        {
            throw new BusinessException(message: "双因素认证已处于启用状态");
        }

        if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
        {
            throw new BusinessException(message: "请先调用初始化接口获取密钥");
        }

        if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, command.Code.Trim()))
        {
            throw new BusinessException(message: "验证码错误，请检查后重试");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        security.TwoFactorEnabled = true;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 验证并禁用双因素认证
    /// </summary>
    public async Task Disable2FAAsync(Disable2FACommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await EnsureSecurityProfileAsync(user);
        if (!security.TwoFactorEnabled)
        {
            throw new BusinessException(message: "双因素认证未启用");
        }

        if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
        {
            throw new BusinessException(message: "双因素认证配置异常，请联系管理员");
        }

        if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, command.Code.Trim()))
        {
            throw new BusinessException(message: "验证码错误，请检查后重试");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        security.TwoFactorEnabled = false;
        security.TwoFactorSecret = null;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);
        await uow.CompleteAsync();
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
    /// 构建手机登录验证码缓存键
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="phone"></param>
    /// <returns></returns>
    private static string BuildPhoneLoginCodeCacheKey(long? tenantId, string phone)
    {
        var tenantSegment = tenantId?.ToString() ?? "0";
        return $"auth:phone-code:{tenantSegment}:{phone}";
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
        await WriteLoginLogAsync(userId, command.UserName, command.TenantId, loginResult, clientInfo, message);
    }

    /// <summary>
    /// 写入登录日志
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <param name="loginResult"></param>
    /// <param name="clientInfo"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task WriteLoginLogAsync(long userId, string userName, long? tenantId, LoginResult loginResult, ClientInfo clientInfo, string message)
    {
        var log = new SysLoginLog
        {
            TenantId = tenantId,
            UserId = userId,
            UserName = userName,
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
    /// 标准化租户ID（非正数视为 null）
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    private static long? NormalizeTenantId(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;
    }

    /// <summary>
    /// 生成数字验证码
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    private static string GenerateNumericCode(int length)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        var max = (int)Math.Pow(10, Math.Min(length, 9));
        return RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");
    }

    /// <summary>
    /// 生成临时密码
    /// </summary>
    /// <returns></returns>
    private static string GenerateTemporaryPassword()
    {
        return $"Tmp@{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
    }

    /// <summary>
    /// 是否返回调试信息
    /// </summary>
    /// <returns></returns>
    private bool ShouldExposeDebugSecrets()
    {
        return _hostEnvironment.IsDevelopment()
               || _configuration.GetValue("XiHan:Authentication:ExposeDebugSecrets", false);
    }

    /// <summary>
    /// 解析注册默认角色ID
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    private async Task<long?> ResolveDefaultRoleIdAsync(long? tenantId)
    {
        var role = await _roleRepository.GetByRoleCodeAsync("employee", tenantId);
        role ??= await _roleRepository.GetByRoleCodeAsync("guest", tenantId);

        if (role is null && tenantId.HasValue)
        {
            role = await _roleRepository.GetByRoleCodeAsync("employee", null);
            role ??= await _roleRepository.GetByRoleCodeAsync("guest", null);
        }

        return role?.BasicId;
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
