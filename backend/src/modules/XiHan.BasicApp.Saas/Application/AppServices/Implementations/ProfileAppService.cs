#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567890
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
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenCacheHelper _authTokenCacheHelper;
    private readonly IDistributedCache<AuthVerificationCodeCacheItem> _verificationCodeCache;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRealtimeNotificationService<Hubs.BasicAppNotificationHub> _realtimeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IUserSessionRepository userSessionRepository,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IAuthTokenCacheHelper authTokenCacheHelper,
        IDistributedCache<AuthVerificationCodeCacheItem> verificationCodeCache,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWorkManager unitOfWorkManager,
        IRealtimeNotificationService<Hubs.BasicAppNotificationHub> realtimeNotifier)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _userSessionRepository = userSessionRepository;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _authTokenCacheHelper = authTokenCacheHelper;
        _verificationCodeCache = verificationCodeCache;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWorkManager = unitOfWorkManager;
        _realtimeNotifier = realtimeNotifier;
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
    /// 修改密码（当前用户自助）
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var currentPassword = PasswordValueObject.FromHash(user.Password);
        if (!currentPassword.Verify(command.OldPassword, _passwordHasher))
        {
            throw new BusinessException(message: "原密码错误");
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
            await _authTokenCacheHelper.RemoveSessionTokenAsync(sessionId);
        }

        await NotifyForceLogoutAsync(user.BasicId.ToString(), "您的其他会话已全部下线", otherSessionIds);
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
            _configuration.GetValue("XiHan:Authentication:EmailCodeExpiresInSeconds", 300), 60, 1800);
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
    /// 停用当前账号
    /// </summary>
    public async Task DeactivateAccountAsync(DeactivateAccountCommand command)
    {
        command.ValidateAnnotations();

        var user = await ResolveCurrentUserEntityAsync()
                   ?? throw new UnauthorizedAccessException("未登录或登录已过期");

        var currentPassword = PasswordValueObject.FromHash(user.Password);
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

        var currentPassword = PasswordValueObject.FromHash(user.Password);
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
               || _configuration.GetValue("XiHan:Authentication:ExposeDebugSecrets", false);
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
        var tenantSegment = tenantId?.ToString() ?? "0";
        return $"auth:email-verify:{tenantSegment}:{email.ToLowerInvariant()}";
    }

    #endregion
}
