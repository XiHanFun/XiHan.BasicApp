#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileDomainService
// Guid:638ed01e-4892-423a-958b-a25a776f0694
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.RegularExpressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 当前用户个人中心领域服务实现
/// </summary>
public sealed class ProfileDomainService
    : IProfileDomainService
{
    private const int UserNameChangeIntervalDays = 90;
    private static readonly Regex EmailPattern = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);
    private static readonly Regex UserNamePattern = new("^[A-Za-z0-9_]{3,30}$", RegexOptions.Compiled);

    private readonly IAuthenticationService _authenticationService;

    private readonly IExternalLoginRepository _externalLoginRepository;

    private readonly IOtpService _otpService;

    private readonly IPasswordHasher _passwordHasher;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileDomainService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserSessionRepository userSessionRepository,
        IExternalLoginRepository externalLoginRepository,
        ITenantUserRepository tenantUserRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationService authenticationService,
        IOtpService otpService)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userSessionRepository = userSessionRepository;
        _externalLoginRepository = externalLoginRepository;
        _tenantUserRepository = tenantUserRepository;
        _passwordHasher = passwordHasher;
        _authenticationService = authenticationService;
        _otpService = otpService;
    }

    /// <inheritdoc />
    public async Task<ProfileUserSecurityResult> ChangePasswordAsync(ProfileChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(command.OldPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.NewPassword);

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        if (command.InputUserId != user.BasicId)
        {
            throw new InvalidOperationException("只能修改当前用户密码。");
        }

        EnsurePasswordMatches(security, command.OldPassword);
        await EnsurePasswordMeetsPolicyAsync(user, command.NewPassword, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.Password = _passwordHasher.HashPassword(command.NewPassword);
        security.LastPasswordChangeTime = now;
        security.PasswordExpirationTime = null;
        security.SecurityStamp = NewSecurityStamp();
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new ProfileUserSecurityResult(user, savedSecurity);
    }

    /// <inheritdoc />
    public async Task<ProfileUserSecurityResult> ChangeUserNameAsync(ProfileChangeUserNameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var newUserName = NormalizeRequired(command.UserName, "用户名不能为空。", 50, "用户名不能超过 50 个字符。");
        if (!UserNamePattern.IsMatch(newUserName))
        {
            throw new InvalidOperationException("用户名仅支持 3-30 位字母、数字和下划线。");
        }

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不可修改用户名。");
        }

        if (!CanChangeUserName(security, DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("用户名 90 天内只能修改一次。");
        }

        EnsurePasswordMatches(security, command.Password);
        if (await _userRepository.ExistsUserNameAsync(newUserName, user.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("用户名已存在。");
        }

        user.UserName = newUserName;
        security.LastUserNameChangeTime = DateTimeOffset.UtcNow;
        security.SecurityStamp = NewSecurityStamp();

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new ProfileUserSecurityResult(savedUser, savedSecurity);
    }

    /// <inheritdoc />
    public async Task<ProfileUserSecurityResult> ConfirmContactAsync(ProfileConfirmContactCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        var target = NormalizeContact(command.ContactKind, command.Target);
        if (command.ContactKind == ProfileContactKind.Email)
        {
            user.Email = target;
            security.EmailVerified = true;
        }
        else
        {
            user.Phone = target;
            security.PhoneVerified = true;
        }

        security.SecurityStamp = NewSecurityStamp();
        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new ProfileUserSecurityResult(savedUser, savedSecurity);
    }

    /// <inheritdoc />
    public async Task<ProfileSessionRevokeResult> DeactivateAccountAsync(ProfilePasswordConfirmCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        EnsurePasswordMatches(security, command.Password);
        EnsureAccountCanBeClosed(user);
        await EnsureCurrentUserIsNotTenantOwnerAsync(user, cancellationToken);

        user.Status = EnableStatus.Disabled;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userRepository.UpdateAsync(user, cancellationToken);
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);

        return await RevokeCurrentUserSessionsAsync(user.BasicId, "用户在个人中心停用账号", command.OperatorUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileSessionRevokeResult> DeleteAccountAsync(ProfilePasswordConfirmCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        EnsurePasswordMatches(security, command.Password);
        EnsureAccountCanBeClosed(user);
        await EnsureCurrentUserIsNotTenantOwnerAsync(user, cancellationToken);

        security.IsDeleted = true;
        security.DeletedTime = DateTimeOffset.UtcNow;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        await _userRepository.SoftDeleteAsync(user, cancellationToken);

        return await RevokeCurrentUserSessionsAsync(user.BasicId, "用户在个人中心注销账号", command.OperatorUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DisableTwoFactorAsync(ProfileTwoFactorCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateSingleTwoFactorMethod(command.Method);
        var (_, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        if (!security.TwoFactorMethod.HasFlag(command.Method))
        {
            return;
        }

        security.TwoFactorMethod &= ~command.Method;
        security.TwoFactorEnabled = security.TwoFactorMethod != TwoFactorMethod.None;
        if (!security.TwoFactorMethod.HasFlag(TwoFactorMethod.Totp))
        {
            security.TwoFactorSecret = null;
        }

        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnableTwoFactorAsync(ProfileTwoFactorCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateSingleTwoFactorMethod(command.Method);
        var (_, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        if (command.Method == TwoFactorMethod.Totp && string.IsNullOrWhiteSpace(security.TwoFactorSecret))
        {
            throw new InvalidOperationException("请先初始化 TOTP 双因素认证。");
        }

        if (command.Method == TwoFactorMethod.Email && !security.EmailVerified)
        {
            throw new InvalidOperationException("启用邮箱两步验证前必须先验证邮箱。");
        }

        if (command.Method == TwoFactorMethod.Phone && !security.PhoneVerified)
        {
            throw new InvalidOperationException("启用手机两步验证前必须先验证手机号。");
        }

        security.TwoFactorMethod |= command.Method;
        security.TwoFactorEnabled = security.TwoFactorMethod != TwoFactorMethod.None;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileContactPrepareResult> PrepareChangeContactAsync(ProfileChangeContactPrepareCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var target = NormalizeContact(command.ContactKind, command.Target);
        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        EnsurePasswordMatches(security, command.Password);
        return new ProfileContactPrepareResult(user, target);
    }

    /// <inheritdoc />
    public async Task<ProfileSessionRevokeResult> RevokeOtherSessionsAsync(ProfileOtherSessionsRevokeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == command.UserId &&
                       session.Status != SessionStatus.Revoked &&
                       session.UserSessionId != command.CurrentSessionId,
            cancellationToken);
        if (sessions.Count == 0)
        {
            return new ProfileSessionRevokeResult([]);
        }

        const string reason = "用户在个人中心撤销其他会话";
        foreach (var session in sessions)
        {
            RevokeSession(session, reason);
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        return new ProfileSessionRevokeResult([.. sessions.Select(session => BuildSessionRevokedEvent(session, false, command.OperatorUserId, reason))]);
    }

    /// <inheritdoc />
    public async Task<ProfileSessionRevokeResult> RevokeSessionAsync(ProfileSessionRevokeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        var sessionId = NormalizeRequired(command.SessionId, "会话标识不能为空。", 100, "会话标识不能超过 100 个字符。");
        if (string.Equals(sessionId, command.CurrentSessionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("不能在个人中心踢下线当前会话。");
        }

        var session = await _userSessionRepository.GetFirstAsync(
            item => item.UserId == command.UserId && item.UserSessionId == sessionId,
            cancellationToken)
            ?? throw new InvalidOperationException("会话不存在。");

        const string reason = "用户在个人中心撤销会话";
        RevokeSession(session, reason);
        session = await _userSessionRepository.UpdateAsync(session, cancellationToken);
        return new ProfileSessionRevokeResult([BuildSessionRevokedEvent(session, false, command.OperatorUserId, reason)]);
    }

    /// <inheritdoc />
    public async Task<ProfileTwoFactorSetupResult> SetupTwoFactorAsync(ProfileTwoFactorSetupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        var secret = string.IsNullOrWhiteSpace(security.TwoFactorSecret)
            ? _otpService.GenerateTotpSecret()
            : security.TwoFactorSecret;

        security.TwoFactorSecret = secret;
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);

        return new ProfileTwoFactorSetupResult(secret, _otpService.GenerateTotpUri(secret, command.Issuer, user.UserName));
    }

    /// <inheritdoc />
    public async Task UnlinkAccountAsync(ProfileUnlinkAccountCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var provider = NormalizeRequired(command.Provider, "第三方提供商不能为空。", 50, "第三方提供商不能超过 50 个字符。");
        var account = await _externalLoginRepository.GetFirstAsync(
            item => item.UserId == command.UserId && item.Provider == provider,
            cancellationToken)
            ?? throw new InvalidOperationException("第三方账号绑定不存在。");

        account.IsDeleted = true;
        account.DeletedTime = DateTimeOffset.UtcNow;
        _ = await _externalLoginRepository.UpdateAsync(account, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileUserSecurityResult> UpdateProfileAsync(ProfileUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateProfileUpdate(command);

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        user.NickName = NormalizeNullable(command.NickName, 50);
        user.RealName = NormalizeNullable(command.RealName, 50);
        user.Avatar = NormalizeNullable(command.Avatar, 500);
        user.Gender = command.Gender.HasValue ? (UserGender)command.Gender.Value : user.Gender;
        user.Birthday = command.Birthday;
        user.TimeZone = NormalizeNullable(command.TimeZone, 50);
        user.Language = NormalizeNullable(command.Language, 10);
        user.Country = NormalizeNullable(command.Country, 50);
        user.Remark = NormalizeNullable(command.Remark, 500);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        return new ProfileUserSecurityResult(savedUser, security);
    }

    /// <inheritdoc />
    public async Task<ProfileUserSecurityResult> VerifyContactAsync(ProfileVerifyContactCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        if (command.ContactKind == ProfileContactKind.Email)
        {
            security.EmailVerified = true;
        }
        else if (command.ContactKind == ProfileContactKind.Phone)
        {
            security.PhoneVerified = true;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(command), "联系方式类型无效。");
        }

        security.SecurityStamp = NewSecurityStamp();
        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new ProfileUserSecurityResult(user, savedSecurity);
    }

    private static UserSessionRevokedDomainEvent BuildSessionRevokedEvent(
        SysUserSession session,
        bool revokeAllUserSessions,
        long? operatorUserId,
        string reason)
    {
        return new UserSessionRevokedDomainEvent(
            session.TenantId,
            session.UserId,
            session.BasicId,
            session.UserSessionId,
            session.CurrentAccessTokenJti,
            revokeAllUserSessions,
            operatorUserId,
            reason);
    }

    private static UserSessionRevokedDomainEvent BuildUserSessionsRevokedEvent(
        SysUser user,
        long sessionTenantKey,
        long? operatorUserId,
        string reason)
    {
        return new UserSessionRevokedDomainEvent(
            sessionTenantKey,
            user.BasicId,
            sessionId: null,
            userSessionId: null,
            accessTokenJti: null,
            revokeAllUserSessions: true,
            operatorUserId,
            reason);
    }

    private static bool CanChangeUserName(SysUserSecurity security, DateTimeOffset now)
    {
        return !security.LastUserNameChangeTime.HasValue ||
               security.LastUserNameChangeTime.Value.AddDays(UserNameChangeIntervalDays) <= now;
    }

    private static List<string> BuildPasswordBlacklist(SysUser user)
    {
        return [.. new[] { user.UserName, user.RealName, user.NickName, user.Email, user.Phone }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())];
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

    private static string NormalizeContact(ProfileContactKind contactKind, string? target)
    {
        if (contactKind == ProfileContactKind.Email)
        {
            var email = NormalizeRequired(target, "新邮箱不能为空。", 100, "邮箱不能超过 100 个字符。");
            if (!EmailPattern.IsMatch(email))
            {
                throw new InvalidOperationException("邮箱格式无效。");
            }

            return email;
        }

        if (contactKind == ProfileContactKind.Phone)
        {
            return NormalizeRequired(target, "新手机号不能为空。", 20, "手机号不能超过 20 个字符。");
        }

        throw new ArgumentOutOfRangeException(nameof(contactKind), "联系方式类型无效。");
    }

    private static void RevokeSession(SysUserSession session, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        session.Status = SessionStatus.Revoked;
        session.RevokedTime = now;
        session.RevokedReason = NormalizeNullable(reason, 200);
        session.LogoutTime ??= now;
        session.LastActivityTime = now;
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateProfileUpdate(ProfileUpdateCommand command)
    {
        ValidateOptionalLength(command.NickName, 50, nameof(command.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(command.RealName, 50, nameof(command.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(command.Avatar, 500, nameof(command.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(command.TimeZone, 50, nameof(command.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(command.Language, 10, nameof(command.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(command.Country, 50, nameof(command.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
        if (command.Gender.HasValue && !Enum.IsDefined(typeof(UserGender), command.Gender.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(command.Gender), "性别枚举值无效。");
        }
    }

    private static void ValidateSingleTwoFactorMethod(TwoFactorMethod method)
    {
        if (method is not TwoFactorMethod.Totp and not TwoFactorMethod.Email and not TwoFactorMethod.Phone)
        {
            throw new ArgumentOutOfRangeException(nameof(method), "双因素方式无效。");
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

        var blacklist = BuildPasswordBlacklist(user);
        var result = await _authenticationService.ValidatePasswordStrengthAsync(password, blacklist);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors.Count > 0 ? string.Join("；", result.Errors) : result.Message;
        throw new InvalidOperationException($"新密码不符合安全要求：{errors}");
    }

    private async Task<(SysUser User, SysUserSecurity Security)> GetUserSecurityOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前用户不存在。");
        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("用户安全记录不存在。");

        return (user, security);
    }

    private async Task<ProfileSessionRevokeResult> RevokeCurrentUserSessionsAsync(
        long userId,
        string reason,
        long? operatorUserId,
        CancellationToken cancellationToken)
    {
        var (user, _) = await GetUserSecurityOrThrowAsync(userId, cancellationToken);
        var sessions = await _userSessionRepository.GetListAsync(session => session.UserId == user.BasicId && session.Status != SessionStatus.Revoked, cancellationToken);
        if (sessions.Count == 0)
        {
            return new ProfileSessionRevokeResult([]);
        }

        foreach (var session in sessions)
        {
            RevokeSession(session, reason);
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        return new ProfileSessionRevokeResult([BuildUserSessionsRevokedEvent(user, sessions[0].TenantId, operatorUserId, reason)]);
    }
}
