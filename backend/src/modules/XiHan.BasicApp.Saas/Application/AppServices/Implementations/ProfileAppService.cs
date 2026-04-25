#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService
// Guid:1c6ed31d-0e67-4484-8dd6-57c435cedfae
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Constants.Settings;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;
using IAuthenticationService = XiHan.Framework.Authentication.Users.IAuthenticationService;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 个人中心应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class ProfileAppService : ApplicationServiceBase, IProfileAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserManager _userManager;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly ILoginLogSplitRepository _loginLogRepository;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenCacheHelper _authTokenCacheHelper;
    private readonly IDistributedCache<AuthVerificationCodeCacheItem> _verificationCodeCache;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAuthenticationService _authenticationService;
    private readonly IRealtimeNotificationService<Hubs.BasicAppNotificationHub> _realtimeNotifier;
    private readonly ITenantAccessContextService _tenantAccessContextService;

    /// <summary>
    /// 用户名修改冷却天数
    /// </summary>
    private const int UserNameChangeCooldownDays = 90;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IUserSessionRepository userSessionRepository,
        IExternalLoginRepository externalLoginRepository,
        ILoginLogSplitRepository loginLogRepository,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IAuthTokenCacheHelper authTokenCacheHelper,
        IDistributedCache<AuthVerificationCodeCacheItem> verificationCodeCache,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        IAuthenticationService authenticationService,
        IRealtimeNotificationService<Hubs.BasicAppNotificationHub> realtimeNotifier,
        ITenantAccessContextService tenantAccessContextService)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _userSessionRepository = userSessionRepository;
        _externalLoginRepository = externalLoginRepository;
        _loginLogRepository = loginLogRepository;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _authTokenCacheHelper = authTokenCacheHelper;
        _verificationCodeCache = verificationCodeCache;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWorkManager = unitOfWorkManager;
        _authenticationService = authenticationService;
        _realtimeNotifier = realtimeNotifier;
        _tenantAccessContextService = tenantAccessContextService;
    }

    /// <summary>
    /// 获取当前用户完整档案
    /// </summary>
    public async Task<UserProfileDto> GetProfileAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId);

        var canChangeUserName = !user.IsSystemAccount
            && (security?.LastUserNameChangeTime is null
                || security.LastUserNameChangeTime.Value.AddDays(UserNameChangeCooldownDays) <= DateTimeOffset.UtcNow);

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
            IsSystemAccount = user.IsSystemAccount,
            TwoFactorEnabled = security?.TwoFactorEnabled ?? false,
            TwoFactorMethod = (int)(security?.TwoFactorMethod ?? TwoFactorMethod.None),
            EmailVerified = security?.EmailVerified ?? false,
            PhoneVerified = security?.PhoneVerified ?? false,
            LastPasswordChangeTime = security?.LastPasswordChangeTime,
            LastUserNameChangeTime = security?.LastUserNameChangeTime,
            CanChangeUserName = canChangeUserName
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

        if (command.NickName is not null)
        {
            user.NickName = command.NickName.Trim();
        }

        if (command.RealName is not null)
        {
            user.RealName = command.RealName.Trim();
        }

        if (command.Avatar is not null)
        {
            user.Avatar = command.Avatar.Trim();
        }

        if (command.Gender.HasValue)
        {
            user.Gender = command.Gender.Value;
        }

        if (command.Birthday.HasValue)
        {
            user.Birthday = command.Birthday.Value;
        }

        if (command.TimeZone is not null)
        {
            user.TimeZone = command.TimeZone.Trim();
        }

        if (command.Language is not null)
        {
            user.Language = command.Language.Trim();
        }

        if (command.Country is not null)
        {
            user.Country = command.Country.Trim();
        }

        if (command.Remark is not null)
        {
            user.Remark = command.Remark.Trim();
        }

        await _userRepository.UpdateAsync(user);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 修改密码（当前用户自助）
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId)
                       ?? throw new BusinessException(message: "用户安全信息不存在");
        var currentPassword = PasswordValueObject.FromHash(security.Password);
        if (!currentPassword.Verify(command.OldPassword, _passwordHasher))
        {
            throw new BusinessException(message: "原密码错误");
        }

        // 密码策略校验
        var validationResult = await _authenticationService.ValidatePasswordStrengthAsync(command.NewPassword);
        if (!validationResult.IsValid)
        {
            throw new BusinessException(message: string.Join("；", validationResult.Errors));
        }

        // 新旧密码不能相同
        if (currentPassword.Verify(command.NewPassword, _passwordHasher))
        {
            throw new BusinessException(message: "新密码不能与当前密码相同");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _userManager.ChangePasswordAsync(user, command.NewPassword);
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
        var currentTenantId = await ResolveCurrentTenantIdAsync();
        var sessions = await _userSessionRepository.GetOnlineSessionsAsync(user.BasicId, currentTenantId);

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

        var currentTenantId = await ResolveCurrentTenantIdAsync();
        var session = await _userSessionRepository.GetBySessionIdAsync(command.SessionId, currentTenantId);
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
        await MarkSessionRevokedAsync(command.SessionId, currentTenantId, "用户在个人中心主动撤销");
        await uow.CompleteAsync();

        await _authTokenCacheHelper.RemoveSessionTokenAsync(command.SessionId);
        await NotifyForceLogoutAsync(user.BasicId.ToString(), "您的会话已被撤销", [command.SessionId]);
    }

    /// <summary>
    /// 撤销当前用户其他所有会话
    /// </summary>
    public async Task RevokeOtherSessionsAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var currentSessionId = _httpContextAccessor.HttpContext?.User.FindSessionId();
        var currentTenantId = await ResolveCurrentTenantIdAsync();
        var sessions = await _userSessionRepository.GetOnlineSessionsAsync(user.BasicId, currentTenantId);
        var otherSessionIds = sessions
            .Where(s => !string.Equals(s.UserSessionId, currentSessionId, StringComparison.Ordinal))
            .Select(s => s.UserSessionId)
            .ToArray();

        if (otherSessionIds.Length == 0)
        {
            return;
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _userSessionRepository.RevokeSessionsAsync(otherSessionIds, "用户在个人中心撤销所有其他会话", currentTenantId);
        await uow.CompleteAsync();

        foreach (var sessionId in otherSessionIds)
        {
            await _authTokenCacheHelper.RemoveSessionTokenAsync(sessionId);
        }

        await NotifyForceLogoutAsync(user.BasicId.ToString(), "您的其他会话已全部下线", otherSessionIds);
    }

    /// <summary>
    /// 初始化 TOTP 双因素认证（生成密钥和二维码URI）
    /// </summary>
    public async Task<TwoFactorSetupResultDto> Setup2FAAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await EnsureSecurityProfileAsync(user);
        if (security.TwoFactorMethod.HasFlag(TwoFactorMethod.Totp))
        {
            throw new BusinessException(message: "TOTP 认证已启用，请先禁用再重新设置");
        }

        var secret = _otpService.GenerateTotpSecret();
        var uri = _otpService.GenerateTotpUri(secret, "XiHan", user.UserName);

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
    /// 发送 2FA 设置验证码（邮箱/手机方式）
    /// </summary>
    public async Task<AuthVerificationCodeDto> Send2FASetupCodeAsync(Send2FAVerifyCodeCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        if (command.Method == TwoFactorMethod.Email)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new BusinessException(message: "请先设置邮箱地址");
            }

            var security = await EnsureSecurityProfileAsync(user);
            if (!security.EmailVerified)
            {
                throw new BusinessException(message: "请先验证邮箱");
            }
        }
        else if (command.Method == TwoFactorMethod.Phone)
        {
            if (string.IsNullOrWhiteSpace(user.Phone))
            {
                throw new BusinessException(message: "请先设置手机号");
            }

            var security = await EnsureSecurityProfileAsync(user);
            if (!security.PhoneVerified)
            {
                throw new BusinessException(message: "请先验证手机号");
            }
        }
        else
        {
            throw new BusinessException(message: "该方式不需要发送验证码，请使用 TOTP 初始化接口");
        }

        var target = command.Method == TwoFactorMethod.Email ? user.Email! : user.Phone!;
        var purpose = $"2FA-Setup-{command.Method}";
        return await SendVerificationCodeInternalAsync(user.TenantId, target, purpose);
    }

    /// <summary>
    /// 验证并启用指定的双因素认证方式（按位添加，可同时启用多种）
    /// </summary>
    public async Task Enable2FAAsync(Enable2FACommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await EnsureSecurityProfileAsync(user);

        // 检查该方式是否已启用
        if (security.TwoFactorMethod.HasFlag(command.Method))
        {
            throw new BusinessException(message: "该认证方式已启用");
        }

        switch (command.Method)
        {
            case TwoFactorMethod.Totp:
                if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
                {
                    throw new BusinessException(message: "请先调用初始化接口获取密钥");
                }

                if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, command.Code.Trim()))
                {
                    throw new BusinessException(message: "验证码错误，请检查后重试");
                }

                break;

            case TwoFactorMethod.Email:
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    throw new BusinessException(message: "请先设置邮箱地址");
                }

                await VerifyCachedCodeAsync(user.TenantId, user.Email, $"2FA-Setup-{TwoFactorMethod.Email}", command.Code);
                break;

            case TwoFactorMethod.Phone:
                if (string.IsNullOrWhiteSpace(user.Phone))
                {
                    throw new BusinessException(message: "请先设置手机号");
                }

                await VerifyCachedCodeAsync(user.TenantId, user.Phone, $"2FA-Setup-{TwoFactorMethod.Phone}", command.Code);
                break;

            default:
                throw new BusinessException(message: "不支持的认证方式");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        security.TwoFactorMethod |= command.Method;
        security.TwoFactorEnabled = security.TwoFactorMethod != TwoFactorMethod.None;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 验证并禁用指定的双因素认证方式（按位移除）
    /// </summary>
    public async Task Disable2FAAsync(Disable2FACommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await EnsureSecurityProfileAsync(user);

        // 检查该方式是否已启用
        if (!security.TwoFactorMethod.HasFlag(command.Method))
        {
            throw new BusinessException(message: "该认证方式未启用");
        }

        switch (command.Method)
        {
            case TwoFactorMethod.Totp:
                if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
                {
                    throw new BusinessException(message: "双因素认证配置异常，请联系管理员");
                }

                if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, command.Code.Trim()))
                {
                    throw new BusinessException(message: "验证码错误，请检查后重试");
                }

                break;

            case TwoFactorMethod.Email:
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    throw new BusinessException(message: "邮箱未设置");
                }

                await VerifyCachedCodeAsync(user.TenantId, user.Email, $"2FA-Setup-{TwoFactorMethod.Email}", command.Code);
                break;

            case TwoFactorMethod.Phone:
                if (string.IsNullOrWhiteSpace(user.Phone))
                {
                    throw new BusinessException(message: "手机号未设置");
                }

                await VerifyCachedCodeAsync(user.TenantId, user.Phone, $"2FA-Setup-{TwoFactorMethod.Phone}", command.Code);
                break;

            default:
                throw new BusinessException(message: "双因素认证配置异常");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        security.TwoFactorMethod &= ~command.Method;
        security.TwoFactorEnabled = security.TwoFactorMethod != TwoFactorMethod.None;
        // 仅当 TOTP 被移除时清除密钥
        if (command.Method == TwoFactorMethod.Totp)
        {
            security.TwoFactorSecret = null;
        }

        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    public async Task<AuthVerificationCodeDto> SendEmailVerifyCodeAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new BusinessException(message: "请先设置邮箱地址");
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.EmailVerified)
        {
            throw new BusinessException(message: "邮箱已验证，无需重复操作");
        }

        var expiresInSeconds = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Auth.EmailCodeExpiresInSeconds, 300), 60, 1800);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        await _verificationCodeCache.SetAsync(
            BuildEmailVerifyCodeCacheKey(user.TenantId, user.Email),
            new AuthVerificationCodeCacheItem
            {
                Purpose = "EmailVerify",
                Target = user.Email,
                TenantId = user.TenantId,
                Code = code,
                ExpireAt = expireAt
            },
            options: new DistributedCacheEntryOptions { AbsoluteExpiration = expireAt },
            hideErrors: true);

        // TODO: 接入实际邮件发送服务，将验证码发送到用户邮箱

        return new AuthVerificationCodeDto
        {
            ExpiresInSeconds = expiresInSeconds,
            DebugCode = exposeDebugSecrets ? code : null
        };
    }

    /// <summary>
    /// 验证邮箱
    /// </summary>
    public async Task VerifyEmailAsync(VerifyEmailCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new BusinessException(message: "请先设置邮箱地址");
        }

        var cacheKey = BuildEmailVerifyCodeCacheKey(user.TenantId, user.Email);
        var cachedCode = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);

        if (cachedCode is null
            || cachedCode.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cachedCode.Code, command.Code.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessException(message: "验证码错误或已过期");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var security = await EnsureSecurityProfileAsync(user);
        security.EmailVerified = true;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 发送手机验证码（验证当前已保存的手机号）
    /// </summary>
    public async Task<AuthVerificationCodeDto> SendPhoneVerifyCodeAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            throw new BusinessException(message: "请先设置手机号");
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.PhoneVerified)
        {
            throw new BusinessException(message: "手机号已验证，无需重复操作");
        }

        var expiresInSeconds = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Auth.PhoneCodeExpiresInSeconds, 300), 60, 1800);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        await _verificationCodeCache.SetAsync(
            BuildPhoneVerifyCodeCacheKey(user.TenantId, user.Phone),
            new AuthVerificationCodeCacheItem
            {
                Purpose = "PhoneVerify",
                Target = user.Phone,
                TenantId = user.TenantId,
                Code = code,
                ExpireAt = expireAt
            },
            options: new DistributedCacheEntryOptions { AbsoluteExpiration = expireAt },
            hideErrors: true);

        // TODO: 接入实际短信发送服务

        return new AuthVerificationCodeDto
        {
            ExpiresInSeconds = expiresInSeconds,
            DebugCode = exposeDebugSecrets ? code : null
        };
    }

    /// <summary>
    /// 验证手机
    /// </summary>
    public async Task VerifyPhoneAsync(VerifyPhoneCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            throw new BusinessException(message: "请先设置手机号");
        }

        var cacheKey = BuildPhoneVerifyCodeCacheKey(user.TenantId, user.Phone);
        var cachedCode = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);

        if (cachedCode is null
            || cachedCode.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cachedCode.Code, command.Code.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessException(message: "验证码错误或已过期");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var security = await EnsureSecurityProfileAsync(user);
        security.PhoneVerified = true;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 换绑邮箱 — 第一步：验证密码、发送验证码到新邮箱
    /// </summary>
    public async Task<AuthVerificationCodeDto> SendChangeEmailCodeAsync(ChangeEmailCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId)
                       ?? throw new BusinessException(message: "用户安全信息不存在");
        var currentPassword = PasswordValueObject.FromHash(security.Password);
        if (!currentPassword.Verify(command.Password, _passwordHasher))
        {
            throw new BusinessException(message: "密码错误");
        }

        var newEmail = command.NewEmail.Trim();
        if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(message: "新邮箱与当前邮箱相同");
        }

        var existingByEmail = await _userRepository.GetByEmailAsync(newEmail, user.TenantId);
        if (existingByEmail is not null && existingByEmail.BasicId != user.BasicId)
        {
            throw new BusinessException(message: "该邮箱已被其他用户使用");
        }

        // 缓存中同时保存新邮箱地址，确认时使用
        var expiresInSeconds = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Auth.EmailCodeExpiresInSeconds, 300), 60, 1800);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        await _verificationCodeCache.SetAsync(
            BuildChangeContactCacheKey(user.TenantId, user.BasicId, "ChangeEmail"),
            new AuthVerificationCodeCacheItem
            {
                Purpose = "ChangeEmail",
                Target = newEmail,
                TenantId = user.TenantId,
                Code = code,
                ExpireAt = expireAt
            },
            options: new DistributedCacheEntryOptions { AbsoluteExpiration = expireAt },
            hideErrors: true);

        // TODO: 接入实际邮件发送服务，将验证码发送到 newEmail

        return new AuthVerificationCodeDto
        {
            ExpiresInSeconds = expiresInSeconds,
            DebugCode = exposeDebugSecrets ? code : null
        };
    }

    /// <summary>
    /// 换绑邮箱 — 第二步：验证码校验后落库
    /// </summary>
    public async Task ConfirmChangeEmailAsync(ConfirmChangeEmailCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var cacheKey = BuildChangeContactCacheKey(user.TenantId, user.BasicId, "ChangeEmail");
        var cached = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);

        if (cached is null || cached.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cached.Code, command.Code.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessException(message: "验证码错误或已过期");
        }

        var newEmail = cached.Target;

        // 二次唯一性校验
        var existingByEmail = await _userRepository.GetByEmailAsync(newEmail, user.TenantId);
        if (existingByEmail is not null && existingByEmail.BasicId != user.BasicId)
        {
            throw new BusinessException(message: "该邮箱已被其他用户使用");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        user.Email = newEmail;
        await _userRepository.UpdateAsync(user);

        // 验证码验证通过即表明新邮箱归属确认，直接标记已验证
        var security = await EnsureSecurityProfileAsync(user);
        security.EmailVerified = true;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 换绑手机 — 第一步：验证密码、发送验证码到新手机
    /// </summary>
    public async Task<AuthVerificationCodeDto> SendChangePhoneCodeAsync(ChangePhoneCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId)
                       ?? throw new BusinessException(message: "用户安全信息不存在");
        var currentPassword = PasswordValueObject.FromHash(security.Password);
        if (!currentPassword.Verify(command.Password, _passwordHasher))
        {
            throw new BusinessException(message: "密码错误");
        }

        var newPhone = command.NewPhone.Trim();
        if (string.Equals(newPhone, user.Phone, StringComparison.Ordinal))
        {
            throw new BusinessException(message: "新手机号与当前手机号相同");
        }

        var existingByPhone = await _userRepository.GetByPhoneAsync(newPhone, user.TenantId);
        if (existingByPhone is not null && existingByPhone.BasicId != user.BasicId)
        {
            throw new BusinessException(message: "该手机号已被其他用户使用");
        }

        var expiresInSeconds = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Auth.PhoneCodeExpiresInSeconds, 300), 60, 1800);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        await _verificationCodeCache.SetAsync(
            BuildChangeContactCacheKey(user.TenantId, user.BasicId, "ChangePhone"),
            new AuthVerificationCodeCacheItem
            {
                Purpose = "ChangePhone",
                Target = newPhone,
                TenantId = user.TenantId,
                Code = code,
                ExpireAt = expireAt
            },
            options: new DistributedCacheEntryOptions { AbsoluteExpiration = expireAt },
            hideErrors: true);

        // TODO: 接入实际短信发送服务，将验证码发送到 newPhone

        return new AuthVerificationCodeDto
        {
            ExpiresInSeconds = expiresInSeconds,
            DebugCode = exposeDebugSecrets ? code : null
        };
    }

    /// <summary>
    /// 换绑手机 — 第二步：验证码校验后落库
    /// </summary>
    public async Task ConfirmChangePhoneAsync(ConfirmChangePhoneCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var cacheKey = BuildChangeContactCacheKey(user.TenantId, user.BasicId, "ChangePhone");
        var cached = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);

        if (cached is null || cached.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cached.Code, command.Code.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessException(message: "验证码错误或已过期");
        }

        var newPhone = cached.Target;

        var existingByPhone = await _userRepository.GetByPhoneAsync(newPhone, user.TenantId);
        if (existingByPhone is not null && existingByPhone.BasicId != user.BasicId)
        {
            throw new BusinessException(message: "该手机号已被其他用户使用");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        user.Phone = newPhone;
        await _userRepository.UpdateAsync(user);

        var security = await EnsureSecurityProfileAsync(user);
        security.PhoneVerified = true;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 修改用户名
    /// </summary>
    public async Task ChangeUserNameAsync(ChangeUserNameCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        if (user.IsSystemAccount)
        {
            throw new BusinessException(message: "系统内置账号不允许修改用户名");
        }

        // 密码验证
        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId)
                       ?? throw new BusinessException(message: "用户安全信息不存在");
        var currentPassword = PasswordValueObject.FromHash(security.Password);
        if (!currentPassword.Verify(command.Password, _passwordHasher))
        {
            throw new BusinessException(message: "密码错误");
        }

        var securityProfile = await EnsureSecurityProfileAsync(user);

        // 90 天冷却期校验
        if (securityProfile.LastUserNameChangeTime.HasValue)
        {
            var nextAllowed = securityProfile.LastUserNameChangeTime.Value.AddDays(UserNameChangeCooldownDays);
            if (nextAllowed > DateTimeOffset.UtcNow)
            {
                var remaining = (int)Math.Ceiling((nextAllowed - DateTimeOffset.UtcNow).TotalDays);
                throw new BusinessException(message: $"用户名修改冷却期未过，还需等待 {remaining} 天");
            }
        }

        var newUserName = command.UserName.Trim();

        // 不允许与当前相同
        if (string.Equals(newUserName, user.UserName, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(message: "新用户名与当前用户名相同");
        }

        // 租户内唯一性
        var existingByName = await _userRepository.GetByUserNameAsync(newUserName, user.TenantId);
        if (existingByName is not null)
        {
            throw new BusinessException(message: "该用户名已被其他用户使用");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        user.UserName = newUserName;
        await _userRepository.UpdateAsync(user);

        securityProfile.LastUserNameChangeTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(securityProfile);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 获取当前用户的第三方登录绑定列表
    /// </summary>
    public async Task<IReadOnlyList<ExternalLoginItemDto>> GetLinkedAccountsAsync()
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var logins = await _externalLoginRepository.GetByUserIdAsync(user.BasicId, user.TenantId);

        return [.. logins.Select(l => new ExternalLoginItemDto
        {
            Provider = l.Provider,
            ProviderDisplayName = l.ProviderDisplayName,
            Email = l.Email,
            AvatarUrl = l.AvatarUrl,
            LastLoginTime = l.LastLoginTime
        })];
    }

    /// <summary>
    /// 解除第三方登录绑定
    /// </summary>
    public async Task UnlinkExternalLoginAsync(UnlinkExternalLoginCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        // 确保解绑后用户仍有可用登录方式（有密码或至少保留一个绑定）
        var allLogins = await _externalLoginRepository.GetByUserIdAsync(user.BasicId, user.TenantId);
        var securityForCheck = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId);
        var hasPassword = !string.IsNullOrWhiteSpace(securityForCheck?.Password);
        if (!hasPassword && allLogins.Count <= 1)
        {
            throw new BusinessException(message: "至少需要保留一种登录方式（密码或第三方账号）");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _externalLoginRepository.DeleteAsync(user.BasicId, command.Provider.Trim());
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 获取当前用户的登录日志（分页）
    /// </summary>
    public async Task<LoginLogPageDto> GetLoginLogsAsync(int pageIndex = 1, int pageSize = 20)
    {
        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _loginLogRepository.GetPagedByUserIdAsync(
            user.BasicId, user.TenantId, pageIndex, pageSize);

        return new LoginLogPageDto
        {
            Total = total,
            Items = [.. items.Select(log => new LoginLogItemDto
            {
                LoginTime = log.LoginTime,
                LoginIp = log.LoginIp,
                LoginLocation = log.LoginLocation,
                Browser = log.Browser,
                Os = log.Os,
                LoginResult = (int)log.LoginResult,
                Message = log.Message
            })]
        };
    }

    /// <summary>
    /// 停用当前账号
    /// </summary>
    public async Task DeactivateAccountAsync(DeactivateAccountCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId)
                       ?? throw new BusinessException(message: "用户安全信息不存在");
        var currentPassword = PasswordValueObject.FromHash(security.Password);
        if (!currentPassword.Verify(command.Password, _passwordHasher))
        {
            throw new BusinessException(message: "密码错误");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        user.Status = YesOrNo.No;
        await _userRepository.UpdateAsync(user);

        await _userSessionRepository.RevokeUserSessionsAsync(user.BasicId, "账号停用", user.TenantId);

        await uow.CompleteAsync();
    }

    /// <summary>
    /// 注销当前账号（软删除）
    /// </summary>
    public async Task DeleteAccountAsync(DeleteAccountCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId)
                       ?? throw new BusinessException(message: "用户安全信息不存在");
        var currentPassword = PasswordValueObject.FromHash(security.Password);
        if (!currentPassword.Verify(command.Password, _passwordHasher))
        {
            throw new BusinessException(message: "密码错误");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        await _userSessionRepository.RevokeUserSessionsAsync(user.BasicId, "账号注销", user.TenantId);
        await _userRepository.DeleteAsync(user);

        await uow.CompleteAsync();
    }

    #region Private Helpers

    private async Task<SysUser?> ResolveCurrentUserEntityAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value);
        return user is not null && user.Status == YesOrNo.Yes ? user : null;
    }

    private async Task<long?> ResolveCurrentTenantIdAsync()
    {
        var context = await _tenantAccessContextService.GetCurrentContextAsync();
        return context?.CurrentTenantId;
    }

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
            // SignalR 推送失败不影响主流程
        }
    }

    private bool ShouldExposeDebugSecrets()
    {
        return _hostEnvironment.IsDevelopment()
               || _configuration.GetValue(SaasSettingKeys.Auth.ExposeDebugSecrets, false);
    }

    private static string GenerateNumericCode(int length)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        var max = (int)Math.Pow(10, Math.Min(length, 9));
        return RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");
    }

    private static string BuildEmailVerifyCodeCacheKey(long? tenantId, string email)
    {
        return SaasCacheKeys.AuthEmailVerifyCode(tenantId, email);
    }

    private static string BuildPhoneVerifyCodeCacheKey(long? tenantId, string phone)
    {
        return SaasCacheKeys.AuthPhoneVerifyCode(tenantId, phone);
    }

    /// <summary>
    /// 换绑联系方式的缓存键（按用户ID + purpose 维度，防止多次请求冲突）
    /// </summary>
    private static string BuildChangeContactCacheKey(long? tenantId, long userId, string purpose)
    {
        return SaasCacheKeys.AuthChangeContact(tenantId, userId, purpose);
    }

    private static string Build2FAVerifyCodeCacheKey(long? tenantId, string target, string purpose)
    {
        return SaasCacheKeys.AuthTwoFactorCode(tenantId, purpose, target);
    }

    /// <summary>
    /// 通用验证码发送（供 2FA 设置和登录挑战共用）
    /// </summary>
    private async Task<AuthVerificationCodeDto> SendVerificationCodeInternalAsync(long? tenantId, string target, string purpose)
    {
        var expiresInSeconds = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Auth.TwoFactorCodeExpiresInSeconds, 300), 60, 1800);
        var exposeDebugSecrets = ShouldExposeDebugSecrets();

        var code = GenerateNumericCode(6);
        var expireAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        await _verificationCodeCache.SetAsync(
            Build2FAVerifyCodeCacheKey(tenantId, target, purpose),
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

        // TODO: 根据 purpose 接入实际邮件/短信发送服务

        return new AuthVerificationCodeDto
        {
            ExpiresInSeconds = expiresInSeconds,
            DebugCode = exposeDebugSecrets ? code : null
        };
    }

    /// <summary>
    /// 通用缓存验证码校验
    /// </summary>
    private async Task VerifyCachedCodeAsync(long? tenantId, string target, string purpose, string code)
    {
        var cacheKey = Build2FAVerifyCodeCacheKey(tenantId, target, purpose);
        var cached = await _verificationCodeCache.GetAsync(cacheKey, hideErrors: true);

        if (cached is null || cached.ExpireAt <= DateTimeOffset.UtcNow
            || !string.Equals(cached.Code, code.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessException(message: "验证码错误或已过期");
        }

        await _verificationCodeCache.RemoveAsync(cacheKey, hideErrors: true);
    }

    #endregion
}
