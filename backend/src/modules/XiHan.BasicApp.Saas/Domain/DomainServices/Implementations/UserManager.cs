#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserManager
// Guid:5cc875a2-3020-49b4-98bb-5b4685c3929c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:59:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.ValueObjects;
using XiHan.BasicApp.Saas.Constants.Basic;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 用户领域管理器实现
/// </summary>
public class UserManager : IUserManager
{
    private const string SuperAdminUserName = "superadmin";
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserManager(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="plainPassword">明文密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的用户实体</returns>
    public async Task<SysUser> CreateAsync(SysUser user, string plainPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

        if (string.Equals(user.UserName, SuperAdminUserName, StringComparison.OrdinalIgnoreCase)
            && !user.IsSystemAccount)
        {
            throw new BusinessException(message: "superadmin 为系统内置账号，禁止新增");
        }

        await EnsureUserNameUniqueAsync(user.UserName, null, user.TenantId, cancellationToken);

        var password = PasswordValueObject.Create(plainPassword, _passwordHasher);
        user.ChangePassword(password);
        user.MarkCreated();

        var created = await _userRepository.AddAsync(user, cancellationToken);
        await EnsureSecurityProfileAsync(created, cancellationToken);
        await EnsurePrimaryMembershipAsync(created, cancellationToken);
        return created;
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task ChangePasswordAsync(SysUser user, string newPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var password = PasswordValueObject.Create(newPassword, _passwordHasher);
        user.ChangePassword(password);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        if (security is null)
        {
            await EnsureSecurityProfileAsync(user, cancellationToken);
            return;
        }

        security.LastPasswordChangeTime = DateTimeOffset.UtcNow;
        security.PasswordExpiryTime = DateTimeOffset.UtcNow.AddDays(90);
        security.SecurityStamp = Guid.NewGuid().ToString("N");
        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 校验用户名唯一性
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task EnsureUserNameUniqueAsync(
        string userName,
        long? excludeUserId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var exists = await _userRepository.IsUserNameExistsAsync(userName, excludeUserId, tenantId, cancellationToken);
        if (exists)
        {
            throw new BusinessException(message: $"用户名 '{userName}' 已存在");
        }
    }

    /// <summary>
    /// 确保用户安全配置存在
    /// </summary>
    public async Task<SysUserSecurity> EnsureSecurityProfileAsync(SysUser user, CancellationToken cancellationToken = default)
    {
        var current = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        if (current is not null)
        {
            return current;
        }

        var security = new SysUserSecurity
        {
            TenantId = user.TenantId,
            UserId = user.BasicId,
            LastPasswordChangeTime = DateTimeOffset.UtcNow,
            PasswordExpiryTime = DateTimeOffset.UtcNow.AddDays(90),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsLocked = false
        };

        await _userRepository.SaveSecurityAsync(security, cancellationToken);
        return security;
    }

    private async Task EnsurePrimaryMembershipAsync(SysUser user, CancellationToken cancellationToken)
    {
        var membership = await _userRepository.GetTenantMembershipAsync(user.BasicId, user.TenantId, cancellationToken);
        if (membership is not null)
        {
            membership.MemberType = user.IsSystemAccount ? TenantMemberType.Owner : TenantMemberType.Member;
            membership.InviteStatus = TenantMemberInviteStatus.Accepted;
            membership.Status = YesOrNo.Yes;
            membership.EffectiveTime ??= DateTimeOffset.UtcNow;
            membership.RespondedTime ??= DateTimeOffset.UtcNow;
            await _userRepository.SaveTenantMembershipAsync(membership, cancellationToken);
            return;
        }

        await _userRepository.SaveTenantMembershipAsync(new SysTenantUser
        {
            TenantId = user.TenantId,
            UserId = user.BasicId,
            MemberType = user.IsSystemAccount ? TenantMemberType.Owner : TenantMemberType.Member,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            EffectiveTime = DateTimeOffset.UtcNow,
            RespondedTime = DateTimeOffset.UtcNow,
            Status = YesOrNo.Yes
        }, cancellationToken);
    }

    /// <summary>
    /// 处理密码验证失败
    /// </summary>
    public async Task HandlePasswordFailureAsync(SysUserSecurity security, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(security);

        security.FailedLoginAttempts++;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;

        if (security.FailedLoginAttempts >= MaxFailedAttempts)
        {
            security.IsLocked = true;
            security.LockoutTime = DateTimeOffset.UtcNow;
            security.LockoutEndTime = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
        }

        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 检查账户是否被锁定
    /// </summary>
    public Task<bool> IsAccountLockedAsync(SysUserSecurity security, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(security);

        if (!security.IsLocked)
            return Task.FromResult(false);

        if (security.LockoutEndTime.HasValue && security.LockoutEndTime.Value <= DateTimeOffset.UtcNow)
        {
            security.IsLocked = false;
            security.FailedLoginAttempts = 0;
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// 重置登录失败计数
    /// </summary>
    public async Task ResetFailedLoginAttemptsAsync(SysUserSecurity security, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(security);

        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;

        await _userRepository.SaveSecurityAsync(security, cancellationToken);
    }

    /// <summary>
    /// 解析默认角色ID
    /// </summary>
    public async Task<long?> ResolveDefaultRoleIdAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByRoleCodeAsync(RoleBasicConstants.TenantMemberRoleCode, tenantId, cancellationToken)
                   ?? await _roleRepository.GetByRoleCodeAsync(RoleBasicConstants.TenantMemberRoleCode, RoleBasicConstants.PlatformTenantId, cancellationToken)
                   ?? await _roleRepository.GetByRoleCodeAsync(RoleBasicConstants.TenantViewerRoleCode, tenantId, cancellationToken)
                   ?? await _roleRepository.GetByRoleCodeAsync(RoleBasicConstants.TenantViewerRoleCode, RoleBasicConstants.PlatformTenantId, cancellationToken);

        return role?.BasicId;
    }
}
