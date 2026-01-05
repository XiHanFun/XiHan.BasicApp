#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAuthenticationService
// Guid:e5f6a7b8-c9d0-1234-5678-90abcdef0123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/06 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Claims;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.BasicApp.Rbac.Repositories.UserSecurities;
using XiHan.BasicApp.Rbac.Services.Users;
using XiHan.Framework.Authentication;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Authentication.Password;

namespace XiHan.BasicApp.Rbac.Adapters.Authentication;

/// <summary>
/// RBAC 认证服务实现
/// </summary>
public class RbacAuthenticationService : IAuthenticationService
{
    private readonly ISysUserRepository _userRepository;
    private readonly ISysUserSecurityRepository _userSecurityRepository;
    private readonly ISysUserService _userService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacAuthenticationService(
        ISysUserRepository userRepository,
        ISysUserSecurityRepository userSecurityRepository,
        ISysUserService userService,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOtpService otpService)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userService = userService;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
    }

    /// <summary>
    /// 用户名密码认证
    /// </summary>
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // 获取用户
        var user = await _userRepository.GetByUserNameAsync(username);
        if (user == null)
        {
            await RecordFailedLoginAttemptAsync(username, cancellationToken);
            return AuthenticationResult.Failure("用户名或密码错误");
        }

        // 检查账户状态
        if (user.Status != YesOrNo.Yes)
        {
            return AuthenticationResult.Failure("账户已被禁用");
        }

        // 检查账户是否被锁定
        if (await IsAccountLockedAsync(username, cancellationToken))
        {
            var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId);
            return AuthenticationResult.LockedOut(security?.LockoutEndTime?.DateTime);
        }

        // 验证密码
        if (!_passwordHasher.VerifyPassword(user.Password, password))
        {
            await RecordFailedLoginAttemptAsync(username, cancellationToken);
            return AuthenticationResult.Failure("用户名或密码错误");
        }

        // 检查是否需要双因素认证
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(user.BasicId);
        if (userSecurity?.TwoFactorEnabled == true)
        {
            return AuthenticationResult.RequiresTwoFactorAuthentication(user.BasicId.ToString(), user.UserName);
        }

        // 重置失败次数
        await ResetFailedLoginAttemptsAsync(username, cancellationToken);

        // 更新最后登录时间
        user.LastLoginTime = DateTimeOffset.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 生成 JWT Token
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.BasicId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        // 添加角色声明
        var userPermissions = await _userService.GetUserPermissionsAsync(user.BasicId);
        claims.AddRange(userPermissions.Select(p => new Claim("Permission", p)));

        var tokenResult = _jwtTokenService.GenerateAccessToken(claims);

        return AuthenticationResult.Success(user.BasicId.ToString(), user.UserName, tokenResult);
    }

    /// <summary>
    /// 验证密码强度
    /// </summary>
    public Task<PasswordValidationResult> ValidatePasswordStrengthAsync(string password, IEnumerable<string>? customBlacklist = null)
    {
        // 这里应该调用 PasswordStrengthChecker，暂时简化实现
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            return Task.FromResult(PasswordValidationResult.Failure("密码长度不足8位", 0));
        }

        return Task.FromResult(PasswordValidationResult.Success(80));
    }

    /// <summary>
    /// 更改密码
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(userIdLong, cancellationToken);
        if (user == null)
        {
            return false;
        }

        // 验证当前密码
        if (!_passwordHasher.VerifyPassword(user.Password, currentPassword))
        {
            return false;
        }

        // 哈希新密码
        user.Password = _passwordHasher.HashPassword(newPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 更新安全信息
        var security = await _userSecurityRepository.GetByUserIdAsync(userIdLong);
        if (security != null)
        {
            security.LastPasswordChangeTime = DateTimeOffset.UtcNow;
            await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    public async Task<bool> ResetPasswordAsync(string userId, string newPassword, string resetToken, CancellationToken cancellationToken = default)
    {
        // 实际应用中需要验证重置令牌
        if (!long.TryParse(userId, out var userIdLong))
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(userIdLong, cancellationToken);
        if (user == null)
        {
            return false;
        }

        // 哈希新密码
        user.Password = _passwordHasher.HashPassword(newPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return true;
    }

    /// <summary>
    /// 启用双因素认证
    /// </summary>
    public async Task<TwoFactorSetupResult> EnableTwoFactorAuthenticationAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            throw new ArgumentException("无效的用户ID");
        }

        var user = await _userRepository.GetByIdAsync(userIdLong, cancellationToken) ?? throw new InvalidOperationException("用户不存在");

        // 生成TOTP密钥
        var secret = _otpService.GenerateTotpSecret();
        var qrCodeUri = _otpService.GenerateTotpUri(secret, "XiHanBasicApp", user.UserName);
        var recoveryCodes = _otpService.GenerateRecoveryCodes();

        // 保存到数据库
        var security = await _userSecurityRepository.GetByUserIdAsync(userIdLong);
        if (security != null)
        {
            security.TwoFactorEnabled = true;
            security.TwoFactorSecret = secret;
            await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        }

        return new TwoFactorSetupResult
        {
            Secret = secret,
            QrCodeUri = qrCodeUri,
            RecoveryCodes = recoveryCodes,
            ManualEntryKey = secret
        };
    }

    /// <summary>
    /// 验证双因素认证代码
    /// </summary>
    public async Task<bool> VerifyTwoFactorCodeAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            return false;
        }

        var security = await _userSecurityRepository.GetByUserIdAsync(userIdLong);
        if (security?.TwoFactorSecret == null)
        {
            return false;
        }

        return _otpService.VerifyTotpCode(security.TwoFactorSecret, code);
    }

    /// <summary>
    /// 禁用双因素认证
    /// </summary>
    public async Task<bool> DisableTwoFactorAuthenticationAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            return false;
        }

        var security = await _userSecurityRepository.GetByUserIdAsync(userIdLong);
        if (security != null)
        {
            security.TwoFactorEnabled = false;
            security.TwoFactorSecret = null;
            await _userSecurityRepository.UpdateAsync(security, cancellationToken);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 生成备用恢复码
    /// </summary>
    public Task<List<string>> GenerateRecoveryCodesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_otpService.GenerateRecoveryCodes());
    }

    /// <summary>
    /// 验证恢复码
    /// </summary>
    public Task<bool> VerifyRecoveryCodeAsync(string userId, string recoveryCode, CancellationToken cancellationToken = default)
    {
        // 实际应用中需要从数据库中验证恢复码
        return Task.FromResult(false);
    }

    /// <summary>
    /// 记录登录失败
    /// </summary>
    public async Task<bool> RecordFailedLoginAttemptAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(username);
        if (user == null)
        {
            return false;
        }

        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId);
        if (security == null)
        {
            return false;
        }

        security.FailedLoginAttempts++;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;

        // 如果失败次数超过限制，锁定账户
        if (security.FailedLoginAttempts >= 5)
        {
            security.IsLocked = true;
            security.LockoutTime = DateTimeOffset.UtcNow;
            security.LockoutEndTime = DateTimeOffset.UtcNow.AddMinutes(30);
        }

        await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return security.IsLocked;
    }

    /// <summary>
    /// 重置登录失败次数
    /// </summary>
    public async Task ResetFailedLoginAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(username);
        if (user == null)
        {
            return;
        }

        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId);
        if (security != null)
        {
            security.FailedLoginAttempts = 0;
            security.LastFailedLoginTime = null;
            security.IsLocked = false;
            security.LockoutTime = null;
            security.LockoutEndTime = null;
            await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        }
    }

    /// <summary>
    /// 检查账户是否被锁定
    /// </summary>
    public async Task<bool> IsAccountLockedAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(username);
        if (user == null)
        {
            return false;
        }

        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId);
        if (security == null)
        {
            return false;
        }

        // 检查是否锁定且锁定时间未过期
        if (security.IsLocked && security.LockoutEndTime.HasValue)
        {
            if (security.LockoutEndTime.Value > DateTimeOffset.UtcNow)
            {
                return true;
            }

            // 锁定时间已过期，自动解锁
            security.IsLocked = false;
            security.LockoutTime = null;
            security.LockoutEndTime = null;
            security.FailedLoginAttempts = 0;
            await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        }

        return false;
    }
}
