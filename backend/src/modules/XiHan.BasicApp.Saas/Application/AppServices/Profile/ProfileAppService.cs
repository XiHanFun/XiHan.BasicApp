#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService
// Guid:d74d16af-1221-45b8-8d0d-b0ab6cc144cc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Password;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户个人中心应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "个人中心")]
public sealed partial class ProfileAppService
    : SaasApplicationService, IProfileAppService
{
    private const int UserNameChangeIntervalDays = 90;
    private const string VerificationCodeBusinessType = "profile.verification-code";
    private const int VerificationCodeSeconds = 600;
    private static readonly ConcurrentDictionary<string, VerificationCodeState> VerificationCodes = new();

    private readonly IAuthenticationService _authenticationService;
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentUser _currentUser;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly ILocalEventBus _localEventBus;
    private readonly IUserNotificationDispatchService _notificationDispatchService;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileAppService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserSessionRepository userSessionRepository,
        IExternalLoginRepository externalLoginRepository,
        ITenantUserRepository tenantUserRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationService authenticationService,
        IOtpService otpService,
        ILocalEventBus localEventBus,
        IUserNotificationDispatchService notificationDispatchService,
        ISqlSugarClientResolver clientResolver,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userSessionRepository = userSessionRepository;
        _externalLoginRepository = externalLoginRepository;
        _tenantUserRepository = tenantUserRepository;
        _passwordHasher = passwordHasher;
        _authenticationService = authenticationService;
        _otpService = otpService;
        _localEventBus = localEventBus;
        _notificationDispatchService = notificationDispatchService;
        _clientResolver = clientResolver;
        _currentUser = currentUser;
    }

    private enum VerificationPurpose
    {
        VerifyEmail,
        VerifyPhone,
        ChangeEmail,
        ChangePhone,
        TwoFactorEmail,
        TwoFactorPhone
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ChangePasswordAsync(ProfileChangePasswordDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(input.OldPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.NewPassword);

        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        if (input.UserId != user.BasicId)
        {
            throw new InvalidOperationException("只能修改当前用户密码。");
        }

        EnsurePasswordMatches(security, input.OldPassword);
        await EnsurePasswordMeetsPolicyAsync(user, input.NewPassword, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.Password = _passwordHasher.HashPassword(input.NewPassword);
        security.LastPasswordChangeTime = now;
        security.PasswordExpiryTime = null;
        security.SecurityStamp = NewSecurityStamp();
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);

        await _notificationDispatchService.DispatchToUserAsync(
            user.BasicId,
            "密码已修改",
            "您的账号密码已更新，如非本人操作，请立即联系管理员。",
            NotificationType.Warning,
            "profile.password.changed",
            user.BasicId,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ChangeUserNameAsync(ProfileChangeUserNameDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var newUserName = NormalizeRequired(input.UserName, "用户名不能为空。", 50, "用户名不能超过 50 个字符。");
        if (!UserNameRegex().IsMatch(newUserName))
        {
            throw new InvalidOperationException("用户名仅支持 3-30 位字母、数字和下划线。");
        }

        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不可修改用户名。");
        }

        if (!CanChangeUserName(security, DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("用户名 90 天内只能修改一次。");
        }

        EnsurePasswordMatches(security, input.Password);
        if (await _userRepository.ExistsUserNameAsync(newUserName, user.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("用户名已存在。");
        }

        user.UserName = newUserName;
        security.LastUserNameChangeTime = DateTimeOffset.UtcNow;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userRepository.UpdateAsync(user, cancellationToken);
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);

        await _notificationDispatchService.DispatchToUserAsync(
            user.BasicId,
            "用户名已修改",
            $"您的账号用户名已修改为 {newUserName}。",
            NotificationType.User,
            "profile.username.changed",
            user.BasicId,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ConfirmChangeEmailAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        var pendingEmail = ConsumeVerificationCode(user.BasicId, VerificationPurpose.ChangeEmail, input.Code);
        user.Email = pendingEmail;
        security.EmailVerified = true;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userRepository.UpdateAsync(user, cancellationToken);
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ConfirmChangePhoneAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        var pendingPhone = ConsumeVerificationCode(user.BasicId, VerificationPurpose.ChangePhone, input.Code);
        user.Phone = pendingPhone;
        security.PhoneVerified = true;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userRepository.UpdateAsync(user, cancellationToken);
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task DeactivateAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default)
    {
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        EnsurePasswordMatches(security, input.Password);
        EnsureAccountCanBeClosed(user);
        await EnsureCurrentUserIsNotTenantOwnerAsync(user, cancellationToken);

        user.Status = EnableStatus.Disabled;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userRepository.UpdateAsync(user, cancellationToken);
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        await RevokeCurrentUserSessionsAsync(user.BasicId, "用户在个人中心停用账号", cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task DeleteAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default)
    {
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        EnsurePasswordMatches(security, input.Password);
        EnsureAccountCanBeClosed(user);
        await EnsureCurrentUserIsNotTenantOwnerAsync(user, cancellationToken);

        security.IsDeleted = true;
        security.DeletedTime = DateTimeOffset.UtcNow;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        await _userRepository.SoftDeleteAsync(user, cancellationToken);
        await RevokeCurrentUserSessionsAsync(user.BasicId, "用户在个人中心注销账号", cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task Disable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        if (!security.TwoFactorMethod.HasFlag(method))
        {
            return;
        }

        await EnsureTwoFactorCodeValidAsync(user, security, method, input.Code, enabling: false, cancellationToken);
        security.TwoFactorMethod &= ~method;
        security.TwoFactorEnabled = security.TwoFactorMethod != TwoFactorMethod.None;
        if (!security.TwoFactorMethod.HasFlag(TwoFactorMethod.Totp))
        {
            security.TwoFactorSecret = null;
        }

        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task Enable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        await EnsureTwoFactorCodeValidAsync(user, security, method, input.Code, enabling: true, cancellationToken);

        security.TwoFactorMethod |= method;
        security.TwoFactorEnabled = security.TwoFactorMethod != TwoFactorMethod.None;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ProfileExternalLoginDto>> GetLinkedAccountsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();
        var accounts = await _externalLoginRepository.GetListAsync(item => item.UserId == userId, cancellationToken);
        return [.. accounts
            .OrderBy(item => item.Provider)
            .Select(item => new ProfileExternalLoginDto
            {
                Provider = item.Provider,
                ProviderDisplayName = item.ProviderDisplayName,
                Email = item.Email,
                AvatarUrl = item.AvatarUrl,
                LastLoginTime = item.LastLoginTime
            })];
    }

    /// <inheritdoc />
    public async Task<ProfileLoginLogPageDto> GetLoginLogsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();
        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = DbClient.Queryable<SysLoginLog>()
            .Where(log => log.UserId == userId)
            .SplitTable()
            .OrderBy(log => log.LoginTime, OrderByType.Desc);

        RefAsync<int> total = 0;
        var logs = await query.ToPageListAsync(page, pageSize, total, cancellationToken);
        return new ProfileLoginLogPageDto
        {
            Items = [.. logs.Select(log => new ProfileLoginLogItemDto
            {
                LoginTime = log.LoginTime,
                LoginIp = log.LoginIp,
                LoginLocation = log.LoginLocation,
                Browser = log.Browser,
                Os = log.Os,
                LoginResult = (int)log.LoginResult,
                Message = log.Message
            })],
            Total = total
        };
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        return ToProfileDto(user, security);
    }

    /// <inheritdoc />
    public async Task<List<ProfileSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();
        var currentSessionId = GetCurrentSessionId();
        var now = DateTimeOffset.UtcNow;
        var expireFallback = now.AddYears(100);
        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == userId &&
                       !session.IsRevoked &&
                       SqlFunc.IsNull(session.ExpiresAt, expireFallback) > now,
            cancellationToken);

        return [.. sessions
            .OrderByDescending(session => string.Equals(session.UserSessionId, currentSessionId, StringComparison.Ordinal))
            .ThenByDescending(session => session.LastActivityTime)
            .Select(session => new ProfileSessionDto
            {
                SessionId = session.UserSessionId,
                DeviceName = session.DeviceName,
                DeviceType = (int)session.DeviceType,
                Browser = session.Browser,
                OperatingSystem = session.OperatingSystem,
                IpAddress = session.IpAddress,
                Location = session.Location,
                LoginTime = session.LoginTime,
                LastActivityTime = session.LastActivityTime,
                IsCurrent = string.Equals(session.UserSessionId, currentSessionId, StringComparison.Ordinal)
            })];
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();
        var currentSessionId = GetCurrentSessionId();
        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == userId &&
                       !session.IsRevoked &&
                       session.UserSessionId != currentSessionId,
            cancellationToken);
        if (sessions.Count == 0)
        {
            return;
        }

        foreach (var session in sessions)
        {
            RevokeSession(session, "用户在个人中心撤销其他会话");
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        foreach (var session in sessions)
        {
            await PublishSessionRevokedAsync(session, revokeAll: false, "用户在个人中心撤销其他会话", cancellationToken);
        }
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task RevokeSessionAsync(ProfileSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var sessionId = NormalizeRequired(input.SessionId, "会话标识不能为空。", 100, "会话标识不能超过 100 个字符。");
        var currentSessionId = GetCurrentSessionId();
        if (string.Equals(sessionId, currentSessionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("不能在个人中心踢下线当前会话。");
        }

        var session = await _userSessionRepository.GetFirstAsync(
            item => item.UserId == userId && item.UserSessionId == sessionId,
            cancellationToken)
            ?? throw new InvalidOperationException("会话不存在。");

        RevokeSession(session, "用户在个人中心撤销会话");
        _ = await _userSessionRepository.UpdateAsync(session, cancellationToken);
        await PublishSessionRevokedAsync(session, revokeAll: false, "用户在个人中心撤销会话", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> Send2FASetupCodeAsync(ProfileTwoFactorMethodDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, _) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        return method switch
        {
            TwoFactorMethod.Email => await SendVerificationCodeAsync(user, VerificationPurpose.TwoFactorEmail, user.Email, "邮箱两步验证", cancellationToken),
            TwoFactorMethod.Phone => await SendVerificationCodeAsync(user, VerificationPurpose.TwoFactorPhone, user.Phone, "手机两步验证", cancellationToken),
            _ => throw new InvalidOperationException("该双因素方式不需要发送验证码。")
        };
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendChangeEmailCodeAsync(ProfileChangeEmailDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var newEmail = NormalizeRequired(input.NewEmail, "新邮箱不能为空。", 100, "邮箱不能超过 100 个字符。");
        if (!EmailRegex().IsMatch(newEmail))
        {
            throw new InvalidOperationException("邮箱格式无效。");
        }

        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        EnsurePasswordMatches(security, input.Password);
        return await SendVerificationCodeAsync(user, VerificationPurpose.ChangeEmail, newEmail, "邮箱换绑", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendChangePhoneCodeAsync(ProfileChangePhoneDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var newPhone = NormalizeRequired(input.NewPhone, "新手机号不能为空。", 20, "手机号不能超过 20 个字符。");
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        EnsurePasswordMatches(security, input.Password);
        return await SendVerificationCodeAsync(user, VerificationPurpose.ChangePhone, newPhone, "手机换绑", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendEmailVerifyCodeAsync(CancellationToken cancellationToken = default)
    {
        var (user, _) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("当前账号未绑定邮箱。");
        }

        return await SendVerificationCodeAsync(user, VerificationPurpose.VerifyEmail, user.Email, "邮箱验证", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendPhoneVerifyCodeAsync(CancellationToken cancellationToken = default)
    {
        var (user, _) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            throw new InvalidOperationException("当前账号未绑定手机号。");
        }

        return await SendVerificationCodeAsync(user, VerificationPurpose.VerifyPhone, user.Phone, "手机验证", cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<ProfileTwoFactorSetupDto> Setup2FAAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        var secret = string.IsNullOrWhiteSpace(security.TwoFactorSecret)
            ? _otpService.GenerateTotpSecret()
            : security.TwoFactorSecret;

        security.TwoFactorSecret = secret;
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);

        return new ProfileTwoFactorSetupDto
        {
            SharedKey = secret,
            AuthenticatorUri = _otpService.GenerateTotpUri(secret, "XiHan BasicApp", user.UserName)
        };
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task UnlinkAccountAsync(ProfileUnlinkAccountDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = NormalizeRequired(input.Provider, "第三方提供商不能为空。", 50, "第三方提供商不能超过 50 个字符。");
        var userId = GetCurrentUserIdOrThrow();
        var account = await _externalLoginRepository.GetFirstAsync(
            item => item.UserId == userId && item.Provider == provider,
            cancellationToken)
            ?? throw new InvalidOperationException("第三方账号绑定不存在。");

        account.IsDeleted = true;
        account.DeletedTime = DateTimeOffset.UtcNow;
        _ = await _externalLoginRepository.UpdateAsync(account, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<UserProfileDto> UpdateProfileAsync(ProfileUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateProfileUpdate(input);

        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        user.NickName = NormalizeNullable(input.NickName, 50);
        user.RealName = NormalizeNullable(input.RealName, 50);
        user.Avatar = NormalizeNullable(input.Avatar, 500);
        user.Gender = input.Gender.HasValue ? (UserGender)input.Gender.Value : user.Gender;
        user.Birthday = input.Birthday;
        user.TimeZone = NormalizeNullable(input.TimeZone, 50);
        user.Language = NormalizeNullable(input.Language, 10);
        user.Country = NormalizeNullable(input.Country, 50);
        user.Remark = NormalizeNullable(input.Remark, 500);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        return ToProfileDto(savedUser, security);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task VerifyEmailAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        ConsumeVerificationCode(user.BasicId, VerificationPurpose.VerifyEmail, input.Code);
        security.EmailVerified = true;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task VerifyPhoneAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        var (user, security) = await GetCurrentUserSecurityOrThrowAsync(cancellationToken);
        ConsumeVerificationCode(user.BasicId, VerificationPurpose.VerifyPhone, input.Code);
        security.PhoneVerified = true;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    private static string BuildVerificationKey(long userId, VerificationPurpose purpose)
    {
        return $"{userId}:{purpose}";
    }

    private static bool CanChangeUserName(SysUserSecurity security, DateTimeOffset now)
    {
        return !security.LastUserNameChangeTime.HasValue ||
               security.LastUserNameChangeTime.Value.AddDays(UserNameChangeIntervalDays) <= now;
    }

    private static string ConsumeVerificationCode(long userId, VerificationPurpose purpose, string? code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var key = BuildVerificationKey(userId, purpose);
        if (!VerificationCodes.TryRemove(key, out var state) ||
            state.ExpiresAt <= DateTimeOffset.UtcNow ||
            !string.Equals(state.Code, code.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("验证码无效或已过期。");
        }

        return state.PendingValue;
    }

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
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

    private static void RevokeSession(SysUserSession session, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        session.IsOnline = false;
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.RevokedReason = NormalizeNullable(reason, 200);
        session.LogoutTime ??= now;
        session.LastActivityTime = now;
    }

    private static TwoFactorMethod ToTwoFactorMethod(int method)
    {
        return method switch
        {
            (int)TwoFactorMethod.Totp => TwoFactorMethod.Totp,
            (int)TwoFactorMethod.Email => TwoFactorMethod.Email,
            (int)TwoFactorMethod.Phone => TwoFactorMethod.Phone,
            _ => throw new ArgumentOutOfRangeException(nameof(method), "双因素方式无效。")
        };
    }

    [GeneratedRegex("^[A-Za-z0-9_]{3,30}$", RegexOptions.Compiled)]
    private static partial Regex UserNameRegex();

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateProfileUpdate(ProfileUpdateDto input)
    {
        ValidateOptionalLength(input.NickName, 50, nameof(input.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(input.RealName, 50, nameof(input.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(input.Avatar, 500, nameof(input.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(input.TimeZone, 50, nameof(input.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(input.Language, 10, nameof(input.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(input.Country, 50, nameof(input.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        if (input.Gender.HasValue && !Enum.IsDefined(typeof(UserGender), input.Gender.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input.Gender), "性别枚举值无效。");
        }
    }

    private void EnsureAccountCanBeClosed(SysUser user)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能在个人中心自助关闭。");
        }
    }

    private async Task EnsureCurrentUserIsNotTenantOwnerAsync(SysUser user, CancellationToken cancellationToken)
    {
        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能在个人中心自助关闭。");
        }
    }

    private void EnsurePasswordMatches(SysUserSecurity security, string? password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (!_passwordHasher.VerifyPassword(security.Password, password))
        {
            throw new InvalidOperationException("当前密码不正确。");
        }
    }

    private async Task EnsurePasswordMeetsPolicyAsync(SysUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blacklist = new[] { user.UserName, user.RealName, user.NickName, user.Email, user.Phone }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();
        var result = await _authenticationService.ValidatePasswordStrengthAsync(password, blacklist);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors.Count > 0 ? string.Join("；", result.Errors) : result.Message;
        throw new InvalidOperationException($"新密码不符合安全要求：{errors}");
    }

    private async Task EnsureTwoFactorCodeValidAsync(
        SysUser user,
        SysUserSecurity security,
        TwoFactorMethod method,
        string? code,
        bool enabling,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (method == TwoFactorMethod.Totp)
        {
            if (string.IsNullOrWhiteSpace(security.TwoFactorSecret))
            {
                throw new InvalidOperationException("请先初始化 TOTP 双因素认证。");
            }

            if (!_otpService.VerifyTotpCode(security.TwoFactorSecret, code.Trim()))
            {
                throw new InvalidOperationException("TOTP 验证码无效。");
            }

            return;
        }

        var purpose = method == TwoFactorMethod.Email
            ? VerificationPurpose.TwoFactorEmail
            : VerificationPurpose.TwoFactorPhone;
        _ = ConsumeVerificationCode(user.BasicId, purpose, code);

        if (enabling && method == TwoFactorMethod.Email && !security.EmailVerified)
        {
            throw new InvalidOperationException("启用邮箱两步验证前必须先验证邮箱。");
        }

        if (enabling && method == TwoFactorMethod.Phone && !security.PhoneVerified)
        {
            throw new InvalidOperationException("启用手机两步验证前必须先验证手机号。");
        }
    }

    private string? GetCurrentSessionId()
    {
        return _currentUser.FindClaim(XiHanClaimTypes.SessionId)?.Value;
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }

    private async Task<(SysUser User, SysUserSecurity Security)> GetCurrentUserSecurityOrThrowAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserIdOrThrow();
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == user.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("用户安全记录不存在。");
        return (user, security);
    }

    private async Task PublishSessionRevokedAsync(SysUserSession session, bool revokeAll, string reason, CancellationToken cancellationToken)
    {
        await _localEventBus.PublishAsync(
            new UserSessionRevokedDomainEvent(
                session.TenantId,
                session.UserId,
                session.BasicId,
                session.UserSessionId,
                session.CurrentAccessTokenJti,
                revokeAll,
                _currentUser.UserId,
                reason));
    }

    private async Task RevokeCurrentUserSessionsAsync(long userId, string reason, CancellationToken cancellationToken)
    {
        var sessions = await _userSessionRepository.GetListAsync(session => session.UserId == userId && !session.IsRevoked, cancellationToken);
        if (sessions.Count == 0)
        {
            return;
        }

        foreach (var session in sessions)
        {
            RevokeSession(session, reason);
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        await PublishSessionRevokedAsync(sessions[0], revokeAll: true, reason, cancellationToken);
    }

    private async Task<ProfileVerificationCodeResultDto> SendVerificationCodeAsync(
        SysUser user,
        VerificationPurpose purpose,
        string? target,
        string title,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("当前账号缺少可接收验证码的联系方式。");
        }

        var code = GenerateVerificationCode();
        VerificationCodes[BuildVerificationKey(user.BasicId, purpose)] = new VerificationCodeState(
            code,
            target.Trim(),
            DateTimeOffset.UtcNow.AddSeconds(VerificationCodeSeconds));

        await _notificationDispatchService.DispatchToUserAsync(
            user.BasicId,
            $"{title}验证码",
            $"验证码：{code}，10 分钟内有效。",
            NotificationType.User,
            VerificationCodeBusinessType,
            user.BasicId,
            link: "/workbench/profile",
            icon: "lucide:shield-check",
            cancellationToken: cancellationToken);

        return new ProfileVerificationCodeResultDto { ExpiresInSeconds = VerificationCodeSeconds };
    }

    private UserProfileDto ToProfileDto(SysUser user, SysUserSecurity security)
    {
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
            TenantId = _currentUser.TenantId,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
            IsSystemAccount = user.IsSystemAccount,
            TwoFactorEnabled = security.TwoFactorEnabled,
            TwoFactorMethod = (int)security.TwoFactorMethod,
            EmailVerified = security.EmailVerified,
            PhoneVerified = security.PhoneVerified,
            LastPasswordChangeTime = security.LastPasswordChangeTime,
            LastUserNameChangeTime = security.LastUserNameChangeTime,
            CanChangeUserName = !user.IsSystemAccount && CanChangeUserName(security, DateTimeOffset.UtcNow)
        };
    }

    private sealed record VerificationCodeState(string Code, string PendingValue, DateTimeOffset ExpiresAt);
}
