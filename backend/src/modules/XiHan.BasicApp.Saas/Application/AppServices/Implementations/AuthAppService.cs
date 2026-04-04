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
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.OAuth;
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
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

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
    private readonly IAuthTokenCacheHelper _authTokenCacheHelper;
    private readonly IDistributedCache<AuthVerificationCodeCacheItem> _verificationCodeCache;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly OAuthOptions _oauthOptions;
    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotifier;

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
    /// <param name="authTokenCacheHelper"></param>
    /// <param name="verificationCodeCache"></param>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="currentUser"></param>
    /// <param name="clientInfoProvider"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="unitOfWorkManager"></param>
    /// <param name="externalLoginRepository"></param>
    /// <param name="oauthOptions"></param>
    /// <param name="realtimeNotifier"></param>
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
        IAuthTokenCacheHelper authTokenCacheHelper,
        IDistributedCache<AuthVerificationCodeCacheItem> verificationCodeCache,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        IExternalLoginRepository externalLoginRepository,
        IOptions<OAuthOptions> oauthOptions,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotifier)
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
        _authTokenCacheHelper = authTokenCacheHelper;
        _verificationCodeCache = verificationCodeCache;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWorkManager = unitOfWorkManager;
        _externalLoginRepository = externalLoginRepository;
        _oauthOptions = oauthOptions.Value;
        _realtimeNotifier = realtimeNotifier;
    }

    /// <summary>
    /// 获取登录配置
    /// </summary>
    public Task<LoginConfigDto> GetLoginConfigAsync()
    {
        var loginMethods = _configuration.GetSection("XiHan:Authentication:LoginMethods").Get<string[]>();
        var oauth = _oauthOptions;

        var providers = oauth is { Enabled: true }
            ? oauth.Providers
                .Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.ClientId))
                .DistinctBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => new OAuthProviderItemDto
                {
                    Name = p.Name.ToLowerInvariant(),
                    DisplayName = p.DisplayName ?? p.Name
                })
                .ToList()
            : [];

        var response = new LoginConfigDto
        {
            TenantEnabled = _configuration.GetValue("XiHan:MultiTenancy:Enabled", true),
            LoginMethods = loginMethods is { Length: > 0 } ? [.. loginMethods] : ["password"],
            OauthProviders = providers
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
            // 计算用户已启用的所有 2FA 方式
            var availableMethods = new List<string>();
            if (security.TwoFactorMethod.HasFlag(TwoFactorMethod.Totp)
                && !string.IsNullOrWhiteSpace(security.TwoFactorSecret))
            {
                availableMethods.Add("totp");
            }

            if (security.TwoFactorMethod.HasFlag(TwoFactorMethod.Email)
                && !string.IsNullOrWhiteSpace(user.Email))
            {
                availableMethods.Add("email");
            }

            if (security.TwoFactorMethod.HasFlag(TwoFactorMethod.Phone)
                && !string.IsNullOrWhiteSpace(user.Phone))
            {
                availableMethods.Add("phone");
            }

            if (availableMethods.Count == 0)
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "无可用双因素认证方式");
                await uow.CompleteAsync();
                throw new BusinessException(message: "账号双因素认证配置异常，请联系管理员");
            }

            var chosenMethod = command.TwoFactorMethod?.Trim().ToLowerInvariant();
            var twoFactorCode = command.TwoFactorCode?.Trim();

            // 阶段1：未选择方式 → 返回可用方式列表
            if (string.IsNullOrWhiteSpace(chosenMethod))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.RequiresTwoFactor, clientInfo, "需要双因素认证");
                await uow.CompleteAsync();
                return new LoginResponseDto
                {
                    RequiresTwoFactor = true,
                    AvailableTwoFactorMethods = availableMethods
                };
            }

            // 校验用户选择的方式是否在可用列表中
            if (!availableMethods.Contains(chosenMethod))
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "不支持的双因素方式");
                await uow.CompleteAsync();
                throw new BusinessException(message: "所选认证方式不可用");
            }

            // 阶段2：已选方式但未提供验证码 → 对邮箱/手机发送验证码
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                var codeSent = false;
                if (chosenMethod == "email")
                {
                    await Send2FALoginCodeAsync(user.TenantId, user.Email!, "2FA-Login-Email");
                    codeSent = true;
                }
                else if (chosenMethod == "phone")
                {
                    await Send2FALoginCodeAsync(user.TenantId, user.Phone!, "2FA-Login-Phone");
                    codeSent = true;
                }

                await WriteLoginLogAsync(user.BasicId, command, LoginResult.RequiresTwoFactor, clientInfo, $"等待 {chosenMethod} 验证码");
                await uow.CompleteAsync();
                return new LoginResponseDto
                {
                    RequiresTwoFactor = true,
                    AvailableTwoFactorMethods = availableMethods,
                    TwoFactorMethod = chosenMethod,
                    CodeSent = codeSent
                };
            }

            // 阶段3：已选方式且提供了验证码 → 校验
            var codeValid = chosenMethod switch
            {
                "totp" => _otpService.VerifyTotpCode(security.TwoFactorSecret!, twoFactorCode),
                "email" => await Verify2FALoginCodeAsync(user.TenantId, user.Email!, "2FA-Login-Email", twoFactorCode),
                "phone" => await Verify2FALoginCodeAsync(user.TenantId, user.Phone!, "2FA-Login-Phone", twoFactorCode),
                _ => false
            };

            if (!codeValid)
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
            await _authTokenCacheHelper.RemoveSessionTokenAsync(revokedSessionId);
        }

        // 多端策略踢掉的旧会话，通过 SignalR 推送强制下线
        if (revokedSessionIds.Count > 0)
        {
            await NotifyForceLogoutAsync(user.BasicId.ToString(), "您的账号已在其他设备登录", revokedSessionIds);
        }

        // 向已有在线设备推送新设备登录通知
        await NotifyNewDeviceLoginAsync(user, clientInfo);

        await _authTokenCacheHelper.SaveRefreshTokenAsync(refreshToken, user, sessionId);

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
            await _authTokenCacheHelper.RemoveSessionTokenAsync(revokedSessionId);
        }

        // 向已有在线设备推送新设备登录通知
        await NotifyNewDeviceLoginAsync(user, clientInfo);

        await _authTokenCacheHelper.SaveRefreshTokenAsync(refreshToken, user, sessionId);

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
        var cachedToken = await _authTokenCacheHelper.GetRefreshTokenAsync(refreshToken);
        if (cachedToken is null || cachedToken.ExpireAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("刷新令牌已失效，请重新登录");
        }

        var user = await _userRepository.GetByIdAsync(cachedToken.UserId);
        if (user is null || user.Status != YesOrNo.Yes)
        {
            await _authTokenCacheHelper.RemoveRefreshTokenDirectAsync(refreshToken);
            await _authTokenCacheHelper.RemoveSessionTokenMapDirectAsync(cachedToken.SessionId);
            throw new UnauthorizedAccessException("登录状态已失效，请重新登录");
        }

        var existingSession = await _userSessionRepository.GetBySessionIdAsync(cachedToken.SessionId, user.TenantId);
        if (existingSession is null || existingSession.IsRevoked || !existingSession.IsOnline)
        {
            await _authTokenCacheHelper.RemoveRefreshTokenDirectAsync(refreshToken);
            await _authTokenCacheHelper.RemoveSessionTokenMapDirectAsync(cachedToken.SessionId);
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

        await _authTokenCacheHelper.SaveRefreshTokenAsync(tokenResult.RefreshToken, user, sessionId);
        if (!string.Equals(refreshToken, tokenResult.RefreshToken, StringComparison.Ordinal))
        {
            await _authTokenCacheHelper.RemoveRefreshTokenDirectAsync(refreshToken);
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
        var user = await ResolveCurrentUserEntityAsync() ?? throw new UnauthorizedAccessException("未登录或登录已过期");
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
        var user = await ResolveCurrentUserEntityAsync() ?? throw new UnauthorizedAccessException("未登录或登录已过期");
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
            await _authTokenCacheHelper.RemoveSessionTokenAsync(sessionId);
        }

        // 向其他在线设备推送登出通知
        var clientInfo = _clientInfoProvider.GetCurrent();
        await NotifyDeviceLogoutAsync(userId.ToString(), clientInfo);
    }

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query));
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
            throw new ArgumentException("用户 ID 无效", nameof(query));
        }

        var departmentIds = await _authorizationCacheService.GetUserDataScopeDepartmentIdsAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserDataScopeDepartmentIdsAsync(query.UserId, query.TenantId, token));
        return departmentIds;
    }

    /// <summary>
    /// 处理第三方登录（查找或自动注册用户，签发令牌）
    /// </summary>
    public async Task<AuthTokenDto> ExternalLoginAsync(ExternalLoginCommand command)
    {
        command.ValidateAnnotations();
        var provider = command.Provider.Trim().ToLowerInvariant();
        var providerKey = command.ProviderKey.Trim();
        var tenantId = NormalizeTenantId(command.TenantId);
        var clientInfo = _clientInfoProvider.GetCurrent();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        // 查找已绑定的用户
        var existingBinding = await _externalLoginRepository.FindByProviderAsync(provider, providerKey, tenantId);
        SysUser? user = null;

        if (existingBinding is not null)
        {
            user = await _userRepository.GetByIdAsync(existingBinding.UserId);

            // 更新三方登录记录的最后登录时间
            existingBinding.LastLoginTime = DateTimeOffset.UtcNow;
            existingBinding.ProviderDisplayName = command.DisplayName ?? existingBinding.ProviderDisplayName;
            existingBinding.AvatarUrl = command.AvatarUrl ?? existingBinding.AvatarUrl;
            existingBinding.Email = command.Email ?? existingBinding.Email;
            await _externalLoginRepository.UpdateAsync(existingBinding);
        }

        // 未绑定：尝试通过邮箱匹配已有用户，或自动创建新用户
        if (user is null && !string.IsNullOrWhiteSpace(command.Email))
        {
            user = await _userRepository.GetByEmailAsync(command.Email, tenantId);
        }

        if (user is null)
        {
            // 自动创建用户
            var userName = GenerateExternalUserName(provider, providerKey);
            var tempPassword = GenerateTemporaryPassword();
            user = new SysUser
            {
                TenantId = tenantId,
                UserName = userName,
                NickName = command.DisplayName ?? userName,
                Email = command.Email,
                Avatar = command.AvatarUrl,
                Status = YesOrNo.Yes,
                Language = "zh-CN"
            };
            user = await _userManager.CreateAsync(user, tempPassword);

            var defaultRoleId = await ResolveDefaultRoleIdAsync(tenantId);
            if (defaultRoleId.HasValue)
            {
                await _userRepository.ReplaceUserRolesAsync(user.BasicId, [defaultRoleId.Value], tenantId);
            }
        }

        if (user.Status != YesOrNo.Yes)
        {
            await WriteLoginLogAsync(user.BasicId, user.UserName, tenantId, LoginResult.AccountDisabled, clientInfo, $"第三方登录({provider})失败：账号已禁用");
            await uow.CompleteAsync();
            throw new BusinessException(message: "账号已禁用");
        }

        // 创建绑定记录（如果还不存在）
        if (existingBinding is null)
        {
            var newBinding = new SysExternalLogin
            {
                TenantId = tenantId,
                UserId = user.BasicId,
                Provider = provider,
                ProviderKey = providerKey,
                ProviderDisplayName = command.DisplayName,
                Email = command.Email,
                AvatarUrl = command.AvatarUrl,
                LastLoginTime = DateTimeOffset.UtcNow
            };
            await _externalLoginRepository.AddAsync(newBinding);
        }

        var security = await EnsureSecurityProfileAsync(user);
        var revokedSessionIds = await EnforceSessionPolicyAsync(user, security);

        user.LastLoginTime = DateTimeOffset.UtcNow;
        user.LastLoginIp = clientInfo.IpAddress;
        await _userRepository.UpdateAsync(user);

        var roleCodes = await GetUserRoleCodesAsync(user.BasicId, user.TenantId);
        var sessionId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var tokenResult = _jwtTokenService.GenerateAccessToken(BuildUserClaims(user, roleCodes, sessionId, accessTokenJti));

        await SaveOrUpdateSessionAsync(user, sessionId, accessTokenJti);
        await WriteLoginLogAsync(user.BasicId, user.UserName, tenantId, LoginResult.Success, clientInfo, $"第三方登录({provider})成功");
        await uow.CompleteAsync();

        foreach (var revokedSessionId in revokedSessionIds)
        {
            await _authTokenCacheHelper.RemoveSessionTokenAsync(revokedSessionId);
        }

        // 向已有在线设备推送新设备登录通知
        await NotifyNewDeviceLoginAsync(user, clientInfo);

        await _authTokenCacheHelper.SaveRefreshTokenAsync(tokenResult.RefreshToken, user, sessionId);

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
    private static List<AuthMenuRouteDto> BuildMenuRoutes(
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
                Link = menu.IsExternal ? menu.ExternalUrl : null,
                Badge = menu.Badge,
                BadgeType = menu.BadgeType,
                Dot = menu.BadgeDot
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
    /// 为第三方登录用户生成唯一用户名
    /// </summary>
    private static string GenerateExternalUserName(string provider, string providerKey)
    {
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{provider}:{providerKey}")))[..8].ToLowerInvariant();
        return $"{provider}_{hash}";
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
    /// 通过 SignalR 向用户已有设备推送新设备上线通知
    /// </summary>
    private async Task NotifyNewDeviceLoginAsync(SysUser user, ClientInfo clientInfo)
    {
        try
        {
            var deviceDesc = !string.IsNullOrWhiteSpace(clientInfo.Browser)
                ? $"{clientInfo.Browser} ({clientInfo.OperatingSystem})"
                : clientInfo.OperatingSystem ?? "未知设备";

            await _realtimeNotifier.SendToUserAsync(
                user.BasicId.ToString(),
                SignalRConstants.ClientMethods.ReceiveNotification,
                new
                {
                    Type = "Warning",
                    Title = "新设备登录",
                    Content = $"您的账号刚刚在新设备上登录：{deviceDesc}，IP：{clientInfo.IpAddress ?? "未知"}。如非本人操作，请及时修改密码。"
                });
        }
        catch
        {
            // 推送失败不影响登录流程
        }
    }

    /// <summary>
    /// 通过 SignalR 向用户其他设备推送登出通知
    /// </summary>
    private async Task NotifyDeviceLogoutAsync(string userId, ClientInfo clientInfo)
    {
        try
        {
            var deviceDesc = !string.IsNullOrWhiteSpace(clientInfo.Browser)
                ? $"{clientInfo.Browser} ({clientInfo.OperatingSystem})"
                : clientInfo.OperatingSystem ?? "未知设备";

            await _realtimeNotifier.SendToUserAsync(
                userId,
                SignalRConstants.ClientMethods.ReceiveNotification,
                new
                {
                    Type = "Info",
                    Title = "设备已登出",
                    Content = $"您的账号已在一台设备上登出：{deviceDesc}，IP：{clientInfo.IpAddress ?? "未知"}。"
                });
        }
        catch
        {
            // 推送失败不影响登出流程
        }
    }

    /// <summary>
    /// 通过 SignalR 向指定用户推送强制下线消息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">下线原因</param>
    /// <param name="targetSessionIds">需要被踢的会话ID列表，前端据此判断是否应处理；为空则全部处理</param>
    private async Task NotifyForceLogoutAsync(string userId, string reason, IReadOnlyCollection<string>? targetSessionIds = null)
    {
        try
        {
            await _realtimeNotifier.SendToUserAsync(
                userId,
                SignalRConstants.ClientMethods.ForceLogout,
                new { Reason = reason, Timestamp = DateTimeOffset.UtcNow, TargetSessionIds = targetSessionIds });
        }
        catch
        {
            // SignalR 推送失败不影响主流程（用户可能不在线）
        }
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
    /// 发送 2FA 登录验证码（邮箱/手机方式）
    /// </summary>
    private async Task Send2FALoginCodeAsync(long? tenantId, string target, string purpose)
    {
        var expiresInSeconds = Math.Clamp(
            _configuration.GetValue("XiHan:Authentication:TwoFactorCodeExpiresInSeconds", 300), 60, 1800);

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        var tenantSegment = tenantId?.ToString() ?? "0";
        var cacheKey = $"auth:2fa:{purpose}:{tenantSegment}:{target.ToLowerInvariant()}";

        await _verificationCodeCache.SetAsync(
            cacheKey,
            new AuthVerificationCodeCacheItem
            {
                Purpose = purpose,
                Target = target,
                TenantId = tenantId,
                Code = code,
                ExpireAt = expireAt
            },
            options: new DistributedCacheEntryOptions { AbsoluteExpiration = expireAt },
            hideErrors: true);

        // TODO: 接入实际邮件/短信发送服务
    }

    /// <summary>
    /// 验证 2FA 登录验证码
    /// </summary>
    private async Task<bool> Verify2FALoginCodeAsync(long? tenantId, string target, string purpose, string code)
    {
        var tenantSegment = tenantId?.ToString() ?? "0";
        var cacheKey = $"auth:2fa:{purpose}:{tenantSegment}:{target.ToLowerInvariant()}";

        var cached = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);
        if (cached is null || cached.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cached.Code, code.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);
        return true;
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
