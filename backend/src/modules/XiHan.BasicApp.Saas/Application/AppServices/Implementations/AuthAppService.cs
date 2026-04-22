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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Helpers;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Constants.Claims;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Constants.Settings;
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
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Web.Api.Constants;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 认证应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
public class AuthAppService : ApplicationServiceBase, IAuthAppService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;
    private readonly IUserManager _userManager;
    private readonly IAuthorizationContextService _authorizationContextService;
    private readonly ILoginLogRepository _loginLogRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenCacheHelper _authTokenCacheHelper;
    private readonly IDistributedCache<AuthVerificationCodeCacheItem> _verificationCodeCache;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly OAuthOptions _oauthOptions;
    private readonly IAuthSessionManager _authSessionManager;
    private readonly IAuthNotificationService _authNotificationService;
    private readonly ITenantAccessContextService _tenantAccessContextService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IAuthorizationContextService authorizationContextService,
        ILoginLogRepository loginLogRepository,
        IJwtTokenService jwtTokenService,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IAuthTokenCacheHelper authTokenCacheHelper,
        IDistributedCache<AuthVerificationCodeCacheItem> verificationCodeCache,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        IExternalLoginRepository externalLoginRepository,
        IOptions<OAuthOptions> oauthOptions,
        IAuthSessionManager authSessionManager,
        IAuthNotificationService authNotificationService,
        ITenantAccessContextService tenantAccessContextService)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _authorizationContextService = authorizationContextService;
        _loginLogRepository = loginLogRepository;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _authTokenCacheHelper = authTokenCacheHelper;
        _verificationCodeCache = verificationCodeCache;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWorkManager = unitOfWorkManager;
        _externalLoginRepository = externalLoginRepository;
        _oauthOptions = oauthOptions.Value;
        _authSessionManager = authSessionManager;
        _authNotificationService = authNotificationService;
        _tenantAccessContextService = tenantAccessContextService;
    }

    /// <summary>
    /// 获取登录配置
    /// </summary>
    [AllowAnonymous]
    public Task<LoginConfigDto> GetLoginConfigAsync()
    {
        var loginMethods = _configuration.GetSection(SaasSettingKeys.Auth.LoginMethods).Get<string[]>();
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
            TenantEnabled = _configuration.GetValue(SaasSettingKeys.MultiTenancy.Enabled, true),
            LoginMethods = loginMethods is { Length: > 0 } ? [.. loginMethods] : ["password"],
            OauthProviders = providers
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 登录（返回令牌或双因素验证挑战）
    /// </summary>
    [AllowAnonymous]
    public async Task<LoginResponseDto> LoginAsync(UserLoginCommand command)
    {
        command.ValidateAnnotations();
        command.UserName = command.UserName.Trim();
        var clientInfo = _clientInfoProvider.GetCurrent();
        var effectiveTenantId = await _tenantAccessContextService.ResolveTargetTenantIdAsync(command.TargetTenantId, command.TargetTenantCode);

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByUserNameAsync(command.UserName, effectiveTenantId);
        if (user is null)
        {
            await WriteLoginLogAsync(0, command, LoginResult.InvalidCredentials, clientInfo, "用户名或密码错误");
            await uow.CompleteAsync();
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        await _tenantAccessContextService.EnsureTenantAccessAsync(user.BasicId, effectiveTenantId);

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
                    await Send2FALoginCodeAsync(effectiveTenantId, user.Email!, "2FA-Login-Email");
                    codeSent = true;
                }
                else if (chosenMethod == "phone")
                {
                    await Send2FALoginCodeAsync(effectiveTenantId, user.Phone!, "2FA-Login-Phone");
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
                "email" => await Verify2FALoginCodeAsync(effectiveTenantId, user.Email!, "2FA-Login-Email", twoFactorCode),
                "phone" => await Verify2FALoginCodeAsync(effectiveTenantId, user.Phone!, "2FA-Login-Phone", twoFactorCode),
                _ => false
            };

            if (!codeValid)
            {
                await WriteLoginLogAsync(user.BasicId, command, LoginResult.TwoFactorFailed, clientInfo, "双因素验证码错误");
                await uow.CompleteAsync();
                throw new BusinessException(message: "双因素验证码错误或已过期，请重新输入");
            }
        }

        var token = await IssueTokenAfterAuthAsync(user, effectiveTenantId, security, clientInfo, uow, command.UserName, effectiveTenantId, "登录成功");

        return new LoginResponseDto
        {
            RequiresTwoFactor = false,
            Token = token
        };
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [AllowAnonymous]
    public async Task RegisterAsync(UserRegisterCommand command)
    {
        command.ValidateAnnotations();
        command.UserName = command.UserName.Trim();
        command.Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        command.Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim();
        command.NickName = string.IsNullOrWhiteSpace(command.NickName) ? null : command.NickName.Trim();
        var tenantId = await _tenantAccessContextService.ResolveTargetTenantIdAsync(command.TargetTenantId, command.TargetTenantCode);

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
            TenantId = tenantId ?? 0,
            UserName = command.UserName,
            RealName = command.UserName,
            NickName = command.NickName ?? command.UserName,
            Email = command.Email,
            Phone = command.Phone,
            Status = YesOrNo.Yes,
            Language = "zh-CN"
        };

        var created = await _userManager.CreateAsync(user, command.Password);
        var defaultRoleId = await _userManager.ResolveDefaultRoleIdAsync(created.TenantId);
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
    [AllowAnonymous]
    public async Task<AuthVerificationCodeDto> SendPhoneLoginCodeAsync(SendPhoneLoginCodeCommand command)
    {
        command.ValidateAnnotations();
        var phone = command.Phone.Trim();
        var tenantId = NormalizeTenantId(command.TenantId);
        var expiresInSeconds = Math.Clamp(_configuration.GetValue(SaasSettingKeys.Auth.PhoneCodeExpiresInSeconds, 300), 60, 1800);
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
    [AllowAnonymous]
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

        await _tenantAccessContextService.EnsureTenantAccessAsync(user.BasicId, tenantId);
        return await IssueTokenAfterAuthAsync(user, tenantId, security, clientInfo, uow, user.UserName, tenantId, "手机验证码登录成功");
    }

    /// <summary>
    /// 申请重置密码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [AllowAnonymous]
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
    [AllowAnonymous]
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

        var sessionValid = await _authSessionManager.IsSessionValidAsync(cachedToken.SessionId, cachedToken.TenantId);
        if (!sessionValid)
        {
            await _authTokenCacheHelper.RemoveRefreshTokenDirectAsync(refreshToken);
            await _authTokenCacheHelper.RemoveSessionTokenMapDirectAsync(cachedToken.SessionId);
            throw new UnauthorizedAccessException("会话已失效，请重新登录");
        }

        await _tenantAccessContextService.EnsureTenantAccessAsync(user.BasicId, cachedToken.TenantId);
        var roleCodes = await _authorizationContextService.GetUserRoleCodesAsync(user.BasicId, cachedToken.TenantId);
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var sessionId = string.IsNullOrWhiteSpace(cachedToken.SessionId) ? Guid.NewGuid().ToString("N") : cachedToken.SessionId;
        var tokenResult = _jwtTokenService.GenerateAccessToken(
            BuildUserClaims(user, cachedToken.TenantId, roleCodes, sessionId, accessTokenJti));

        var clientInfo = _clientInfoProvider.GetCurrent();
        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _authSessionManager.SaveOrUpdateSessionAsync(user, cachedToken.TenantId, sessionId, accessTokenJti, clientInfo);
        await uow.CompleteAsync();

        await _authTokenCacheHelper.SaveRefreshTokenAsync(tokenResult.RefreshToken, user, cachedToken.TenantId, sessionId);
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
            ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt),
            CurrentTenantId = cachedToken.TenantId,
            HomeTenantId = user.TenantId,
            SessionId = sessionId
        };
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<CurrentUserDto> GetCurrentUserAsync()
    {
        var context = await _authorizationContextService.GetCurrentContextAsync() ?? throw new UnauthorizedAccessException("未登录或登录已过期");
        var user = context.User;
        return new CurrentUserDto
        {
            BasicId = user.BasicId,
            UserName = user.UserName,
            NickName = user.NickName,
            Avatar = user.Avatar,
            Email = user.Email,
            Phone = user.Phone,
            CurrentTenantId = context.CurrentTenantId,
            HomeTenantId = user.TenantId,
            SessionId = context.SessionId
        };
    }

    /// <summary>
    /// 获取权限上下文
    /// </summary>
    public async Task<AuthPermissionDto> GetPermissionsAsync()
    {
        var context = await _authorizationContextService.GetCurrentContextAsync() ?? throw new UnauthorizedAccessException("未登录或登录已过期");
        return await _authorizationContextService.BuildPermissionContextAsync(context.User);
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    public async Task LogoutAsync()
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.UserId.HasValue)
        {
            return;
        }

        var userId = CurrentUser.UserId.Value;
        var tenantId = CurrentUser.TenantId;
        var sessionId = _httpContextAccessor.HttpContext?.User.FindSessionId();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _authSessionManager.MarkSessionRevokedAsync(sessionId, tenantId, "用户主动退出");
        }
        else
        {
            await _authSessionManager.RevokeUserSessionsAsync(userId, "用户主动退出", tenantId);
        }

        await uow.CompleteAsync();

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _authTokenCacheHelper.RemoveSessionTokenAsync(sessionId);
        }

        var clientInfo = _clientInfoProvider.GetCurrent();
        await _authNotificationService.NotifyDeviceLogoutAsync(userId.ToString(), clientInfo);
    }

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    [PermissionAuthorize("permission:read", "same_tenant")]
    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query));
        }

        return await _authorizationContextService.GetUserPermissionCodesAsync(query.UserId, query.TenantId);
    }

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    [PermissionAuthorize("department:read", "same_tenant")]
    public async Task<IReadOnlyCollection<long>> GetDataScopeDepartmentIdsAsync(UserDataScopeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query));
        }

        return await _authorizationContextService.GetUserDataScopeDepartmentIdsAsync(query.UserId, query.TenantId);
    }

    /// <summary>
    /// 处理第三方登录（查找或自动注册用户，签发令牌）
    /// </summary>
    public async Task<AuthTokenDto> ExternalLoginAsync(ExternalLoginCommand command)
    {
        command.ValidateAnnotations();
        var provider = command.Provider.Trim().ToLowerInvariant();
        var providerKey = command.ProviderKey.Trim();
        var tenantId = await _tenantAccessContextService.ResolveTargetTenantIdAsync(command.TargetTenantId, command.TargetTenantCode);
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
                TenantId = tenantId ?? 0,
                UserName = userName,
                NickName = command.DisplayName ?? userName,
                Email = command.Email,
                Avatar = command.AvatarUrl,
                Status = YesOrNo.Yes,
                Language = "zh-CN"
            };
            user = await _userManager.CreateAsync(user, tempPassword);

            var defaultRoleId = await _userManager.ResolveDefaultRoleIdAsync(tenantId);
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

        await _tenantAccessContextService.EnsureTenantAccessAsync(user.BasicId, tenantId);

        // 创建绑定记录（如果还不存在）
        if (existingBinding is null)
        {
            var newBinding = new SysExternalLogin
            {
                TenantId = tenantId ?? 0,
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
        return await IssueTokenAfterAuthAsync(user, tenantId, security, clientInfo, uow, user.UserName, tenantId, $"第三方登录({provider})成功");
    }

    /// <summary>
    /// 发起第三方登录（验证提供商并通过 ChallengeAsync 重定向到授权页）
    /// </summary>
    [AllowAnonymous]
    public async Task GetExternalLoginAuthorizeAsync(string provider, long? targetTenantId = null, string? targetTenantCode = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new BusinessException(message: "提供商名称不能为空");
        }

        var providerConfig = _oauthOptions.Providers
            .FirstOrDefault(p => string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase) && p.Enabled)
            ?? throw new BusinessException(message: $"不支持的登录提供商: {provider}");

        var httpContext = _httpContextAccessor.HttpContext
                         ?? throw new InvalidOperationException("无法获取 HTTP 上下文");

        var request = httpContext.Request;
        var currentPath = request.Path.Value ?? string.Empty;
        var lastSlashIndex = currentPath.LastIndexOf('/');
        var currentSegment = lastSlashIndex >= 0 ? currentPath[(lastSlashIndex + 1)..] : string.Empty;
        var callbackSegment = currentSegment.Replace("Authorize", "Callback", StringComparison.OrdinalIgnoreCase);
        var callbackPath = lastSlashIndex >= 0
            ? $"{currentPath[..(lastSlashIndex + 1)]}{callbackSegment}"
            : $"/api/Auth/{callbackSegment}";

        var queryParts = new List<string> { $"provider={Uri.EscapeDataString(provider)}" };
        if (targetTenantId.HasValue)
        {
            queryParts.Add($"tenantId={targetTenantId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(targetTenantCode))
        {
            queryParts.Add($"tenantCode={Uri.EscapeDataString(targetTenantCode)}");
        }

        var callbackUrl = $"{request.Scheme}://{request.Host}{callbackPath}?{string.Join("&", queryParts)}";

        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
            Items =
            {
                ["provider"] = provider,
                ["tenantId"] = targetTenantId?.ToString() ?? string.Empty,
                ["tenantCode"] = targetTenantCode ?? string.Empty
            }
        };

        await httpContext.ChallengeAsync(providerConfig.Name, properties);
        await httpContext.Response.CompleteAsync();
    }

    /// <summary>
    /// 处理第三方登录回调（从外部 Cookie 读取认证结果，签发令牌，重定向到前端）
    /// </summary>
    [AllowAnonymous]
    public async Task GetExternalLoginCallbackAsync(string provider, long? targetTenantId = null, string? targetTenantCode = null)
    {
        var httpContext = _httpContextAccessor.HttpContext
                         ?? throw new InvalidOperationException("无法获取 HTTP 上下文");

        var authResult = await httpContext.AuthenticateAsync("ExternalCookie");
        if (authResult?.Succeeded != true || authResult.Principal is null)
        {
            httpContext.Response.Redirect(BuildFrontendErrorUrl("第三方登录认证失败"));
            await httpContext.Response.CompleteAsync();
            return;
        }

        var claims = authResult.Principal.Claims.ToList();
        var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            httpContext.Response.Redirect(BuildFrontendErrorUrl("无法获取第三方用户标识"));
            await httpContext.Response.CompleteAsync();
            return;
        }

        var displayName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                          ?? claims.FirstOrDefault(c => c.Type == "urn:github:name")?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var avatarUrl = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value
                        ?? claims.FirstOrDefault(c => c.Type == "urn:github:avatar_url")?.Value
                        ?? claims.FirstOrDefault(c => c.Type == "urn:qq:figureurl_qq_2")?.Value;

        await httpContext.SignOutAsync("ExternalCookie");

        try
        {
            var command = new ExternalLoginCommand
            {
                Provider = provider,
                ProviderKey = providerKey,
                DisplayName = displayName,
                Email = email,
                AvatarUrl = avatarUrl,
                TargetTenantId = targetTenantId,
                TargetTenantCode = targetTenantCode
            };

            var tokenDto = await ExternalLoginAsync(command);

            var frontendUrl = _oauthOptions.FrontendCallbackUrl;
            var redirectUrl = $"{frontendUrl}?accessToken={Uri.EscapeDataString(tokenDto.AccessToken)}" +
                              $"&refreshToken={Uri.EscapeDataString(tokenDto.RefreshToken)}" +
                              $"&expiresIn={tokenDto.ExpiresIn}" +
                              $"&provider={Uri.EscapeDataString(provider)}";

            httpContext.Response.Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            httpContext.Response.Redirect(BuildFrontendErrorUrl(ex.Message));
        }

        await httpContext.Response.CompleteAsync();
    }

    /// <summary>
    /// 构建用户声明
    /// </summary>
    /// <param name="user"></param>
    /// <param name="roleCodes"></param>
    /// <param name="sessionId"></param>
    /// <param name="accessTokenJti"></param>
    /// <returns></returns>
    private static List<Claim> BuildUserClaims(SysUser user, long? effectiveTenantId, IReadOnlyCollection<string> roleCodes, string sessionId, string accessTokenJti)
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

        if (effectiveTenantId.HasValue)
        {
            claims.Add(new Claim(XiHanClaimTypes.TenantId, effectiveTenantId.Value.ToString()));
        }

        if (user.TenantId > 0)
        {
            claims.Add(new Claim(SaasClaimTypes.HomeTenantId, user.TenantId.ToString()));
        }

        foreach (var roleCode in roleCodes.Where(static roleCode => !string.IsNullOrWhiteSpace(roleCode)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(XiHanClaimTypes.Role, roleCode));
        }

        return claims;
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
        return SaasCacheKeys.AuthPhoneLoginCode(tenantId, phone);
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
    /// 异地登录风险判定
    /// 综合子网、网络类型等因素判断是否应标记为风险登录
    /// </summary>
    private static bool DetermineRiskLogin(string? previousIp, string? currentIp)
    {
        if (string.IsNullOrWhiteSpace(previousIp) || string.IsNullOrWhiteSpace(currentIp))
        {
            return false;
        }

        if (string.Equals(previousIp, currentIp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 双方都是回环地址
        if (IsLoopback(previousIp) && IsLoopback(currentIp))
        {
            return false;
        }

        // 双方都是私网地址
        if (IsPrivateNetwork(previousIp) && IsPrivateNetwork(currentIp))
        {
            return false;
        }

        // 一方私网一方公网 → 风险
        if (IsPrivateNetwork(previousIp) != IsPrivateNetwork(currentIp))
        {
            return true;
        }

        // 同一 /16 子网（大概率同城同运营商）→ 不标记风险
        if (IsSameSubnet16(previousIp, currentIp))
        {
            return false;
        }

        return true;
    }

    private static bool IsLoopback(string ip) =>
            ip is "127.0.0.1" or "::1" || ip.StartsWith("127.", StringComparison.Ordinal);

    private static bool IsPrivateNetwork(string ip)
    {
        if (ip.Contains(':'))
        {
            return ip.StartsWith("fc", StringComparison.OrdinalIgnoreCase)
                   || ip.StartsWith("fd", StringComparison.OrdinalIgnoreCase)
                   || ip.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase)
                   || ip is "::1";
        }

        var parts = ip.Split('.');
        if (parts.Length != 4 || !byte.TryParse(parts[0], out var a) || !byte.TryParse(parts[1], out var b))
        {
            return false;
        }

        return a == 10
               || (a == 172 && b >= 16 && b <= 31)
               || (a == 192 && b == 168)
               || a == 127;
    }

    private static bool IsSameSubnet16(string ip1, string ip2)
    {
        var parts1 = ip1.Split('.');
        var parts2 = ip2.Split('.');
        if (parts1.Length != 4 || parts2.Length != 4)
        {
            return false;
        }

        return parts1[0] == parts2[0] && parts1[1] == parts2[1];
    }

    /// <summary>
    /// 认证通过后的统一令牌签发流程（密码登录、手机登录、第三方登录共用）
    /// </summary>
    private async Task<AuthTokenDto> IssueTokenAfterAuthAsync(
        SysUser user,
        long? effectiveTenantId,
        SysUserSecurity security,
        ClientInfo clientInfo,
        IUnitOfWork uow,
        string loginIdentifier,
        long? tenantId,
        string successMessage)
    {
        var revokedSessionIds = await _authSessionManager.EnforceSessionPolicyAsync(user, security, effectiveTenantId);

        security.FailedLoginAttempts = 0;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;
        security.LastFailedLoginTime = null;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        var previousLoginIp = user.LastLoginIp;
        user.LastLoginTime = DateTimeOffset.UtcNow;
        user.LastLoginIp = clientInfo.IpAddress;
        await _userRepository.UpdateAsync(user);

        var roleCodes = await _authorizationContextService.GetUserRoleCodesAsync(user.BasicId, effectiveTenantId);
        var sessionId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var tokenResult = _jwtTokenService.GenerateAccessToken(BuildUserClaims(user, effectiveTenantId, roleCodes, sessionId, accessTokenJti));

        await _authSessionManager.SaveOrUpdateSessionAsync(user, effectiveTenantId, sessionId, accessTokenJti, clientInfo);
        await WriteLoginLogAsync(user.BasicId, loginIdentifier, tenantId, LoginResult.Success, clientInfo, successMessage, sessionId, previousLoginIp);
        await uow.CompleteAsync();

        foreach (var revokedSessionId in revokedSessionIds)
        {
            await _authTokenCacheHelper.RemoveSessionTokenAsync(revokedSessionId);
        }

        if (revokedSessionIds.Count > 0)
        {
            await _authNotificationService.NotifyForceLogoutAsync(
                user.BasicId.ToString(), "您的账号已在其他设备登录", revokedSessionIds);
        }

        await _authNotificationService.NotifyNewDeviceLoginAsync(user.BasicId.ToString(), clientInfo);
        await _authTokenCacheHelper.SaveRefreshTokenAsync(tokenResult.RefreshToken, user, effectiveTenantId, sessionId);

        return new AuthTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = new DateTimeOffset(tokenResult.IssuedAt),
            ExpiresAt = new DateTimeOffset(tokenResult.ExpiresAt),
            CurrentTenantId = effectiveTenantId,
            HomeTenantId = user.TenantId,
            SessionId = sessionId
        };
    }

    /// <summary>
    /// 写入登录日志
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="command"></param>
    /// <param name="loginResult"></param>
    /// <param name="clientInfo"></param>
    /// <param name="message"></param>
    /// <param name="sessionId">登录成功时传入新创建的会话ID，失败时为 null</param>
    /// <param name="previousLoginIp">上一次登录的 IP，用于异地登录判定</param>
    /// <returns></returns>
    private async Task WriteLoginLogAsync(long userId, UserLoginCommand command, LoginResult loginResult, ClientInfo clientInfo, string message, string? sessionId = null, string? previousLoginIp = null)
    {
        await WriteLoginLogAsync(userId, command.UserName, command.TargetTenantId, loginResult, clientInfo, message, sessionId, previousLoginIp, command.DeviceId);
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
    /// <param name="sessionId">登录成功时传入新创建的会话ID，失败时为 null</param>
    /// <param name="previousLoginIp">上一次登录的 IP，用于异地登录判定</param>
    /// <param name="explicitDeviceId">显式传入的设备标识</param>
    /// <returns></returns>
    private async Task WriteLoginLogAsync(
        long userId, string userName, long? tenantId,
        LoginResult loginResult, ClientInfo clientInfo, string message,
        string? sessionId = null, string? previousLoginIp = null, string? explicitDeviceId = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var traceId = httpContext?.Items[XiHanWebApiConstants.TraceIdItemKey]?.ToString()
                      ?? httpContext?.TraceIdentifier;

        // 设备标识优先级：显式传入 → 请求头 X-Device-Id
        var deviceId = explicitDeviceId;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = httpContext?.Request.Headers["X-Device-Id"].FirstOrDefault();
        }

        var isRiskLogin = DetermineRiskLogin(previousLoginIp, clientInfo.IpAddress);

        var log = new SysLoginLog
        {
            TenantId = tenantId ?? 0,
            UserId = userId,
            UserName = userName,
            TraceId = traceId is { Length: > 64 } ? traceId[..64] : traceId,
            SessionId = sessionId is { Length: > 100 } ? sessionId[..100] : sessionId,
            LoginIp = clientInfo.IpAddress,
            LoginLocation = clientInfo.Location,
            Browser = clientInfo.Browser,
            Os = clientInfo.OperatingSystem,
            UserAgent = clientInfo.UserAgent is { Length: > 500 } ? clientInfo.UserAgent[..500] : clientInfo.UserAgent,
            Device = clientInfo.DeviceName is { Length: > 50 } ? clientInfo.DeviceName[..50] : clientInfo.DeviceName,
            DeviceId = deviceId is { Length: > 200 } ? deviceId[..200] : deviceId,
            IsRiskLogin = isRiskLogin,
            LoginResult = loginResult,
            Message = message,
            LoginTime = DateTimeOffset.UtcNow
        };

        await _loginLogRepository.AddAsync(log);
    }

    /// <summary>
    /// 是否返回调试信息
    /// </summary>
    /// <returns></returns>
    private bool ShouldExposeDebugSecrets()
    {
        return _hostEnvironment.IsDevelopment()
               || _configuration.GetValue(SaasSettingKeys.Auth.ExposeDebugSecrets, false);
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
            _configuration.GetValue(SaasSettingKeys.Auth.TwoFactorCodeExpiresInSeconds, 300), 60, 1800);

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        var cacheKey = SaasCacheKeys.AuthTwoFactorCode(tenantId, purpose, target);

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
        var cacheKey = SaasCacheKeys.AuthTwoFactorCode(tenantId, purpose, target);

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

    private string BuildFrontendErrorUrl(string error)
    {
        var callbackUrl = _oauthOptions.FrontendCallbackUrl;
        return $"{callbackUrl}?error={Uri.EscapeDataString(error)}";
    }
}
