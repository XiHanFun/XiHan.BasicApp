#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAuthenticationService
// Guid:b4c5d6e7-f8a9-bcde-f123-4567890abcde
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Domain.Services.Implementations;

/// <summary>
/// 用户认证领域服务实现
/// </summary>
public class UserAuthenticationService : DomainService, IUserAuthenticationService
{
    private readonly ISysUserRepository _userRepository;
    private readonly ISysUserSecurityRepository _userSecurityRepository;
    private readonly IPasswordHasher _passwordHasher;
    
    /// <summary>
    /// 最大失败登录次数（超过此次数将锁定账户）
    /// </summary>
    private const int MaxFailedLoginAttempts = 5;
    
    /// <summary>
    /// 账户锁定时长（分钟）
    /// </summary>
    private const int LockoutDurationMinutes = 30;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserAuthenticationService(
        ISysUserRepository userRepository,
        ISysUserSecurityRepository userSecurityRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 验证用户凭证
    /// </summary>
    public async Task<SysUser?> AuthenticateAsync(string userName, string password, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(userName, tenantId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // 检查用户状态
        if (!IsUserActive(user))
        {
            return null;
        }

        // 验证密码
        if (!VerifyPassword(user, password))
        {
            return null;
        }

        // 检查租户归属
        if (tenantId.HasValue && !BelongsToTenant(user, tenantId.Value))
        {
            return null;
        }

        return user;
    }

    /// <summary>
    /// 验证用户密码
    /// </summary>
    public bool VerifyPassword(SysUser user, string password)
    {
        if (user == null || string.IsNullOrEmpty(user.Password))
        {
            return false;
        }

        return _passwordHasher.VerifyPassword(user.Password, password);
    }

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(password);
    }

    /// <summary>
    /// 检查用户是否被锁定
    /// </summary>
    public bool IsUserLocked(SysUser user)
    {
        var userSecurity = _userSecurityRepository.GetByUserIdAsync(user.BasicId).GetAwaiter().GetResult();
        
        if (userSecurity == null)
        {
            return false;
        }

        // 检查是否被标记为锁定
        if (!userSecurity.IsLocked)
        {
            return false;
        }

        // 检查锁定是否已过期
        if (userSecurity.LockoutEndTime.HasValue && userSecurity.LockoutEndTime.Value <= DateTimeOffset.Now)
        {
            // 锁定已过期，自动解锁
            _userSecurityRepository.UnlockUserAsync(user.BasicId).GetAwaiter().GetResult();
            return false;
        }

        return true;
    }

    /// <summary>
    /// 记录登录失败并检查是否需要锁定账户
    /// </summary>
    public async Task<bool> RecordFailedLoginAttemptAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _userSecurityRepository.IncrementFailedLoginAttemptsAsync(userId, cancellationToken);
        
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(userId, cancellationToken);
        if (userSecurity != null && userSecurity.FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            // 失败次数超过限制，锁定账户
            var lockoutEndTime = DateTimeOffset.Now.AddMinutes(LockoutDurationMinutes);
            await _userSecurityRepository.LockUserAsync(userId, lockoutEndTime, cancellationToken);
            return true; // 已锁定
        }

        return false; // 未锁定
    }

    /// <summary>
    /// 重置登录失败次数
    /// </summary>
    public async Task ResetFailedLoginAttemptsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _userSecurityRepository.ResetFailedLoginAttemptsAsync(userId, cancellationToken);
    }

    /// <summary>
    /// 检查用户是否处于活跃状态
    /// </summary>
    public bool IsUserActive(SysUser user)
    {
        if (user == null)
        {
            return false;
        }

        // 检查用户状态
        if (user.Status != YesOrNo.Yes)
        {
            return false;
        }

        // 检查是否被锁定
        if (IsUserLocked(user))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证用户是否属于指定租户
    /// </summary>
    public bool BelongsToTenant(SysUser user, long tenantId)
    {
        if (user == null)
        {
            return false;
        }

        return user.TenantId == tenantId;
    }
}
