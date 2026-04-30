#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityAppService
// Guid:78f860b4-44ec-417e-8e86-8ae5fc05ef64
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户安全命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户安全")]
public sealed class UserSecurityAppService(
    IUserRepository userRepository,
    IUserSecurityRepository userSecurityRepository,
    ITenantUserRepository tenantUserRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationService authenticationService)
    : SaasApplicationService, IUserSecurityAppService
{
    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 用户安全仓储
    /// </summary>
    private readonly IUserSecurityRepository _userSecurityRepository = userSecurityRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 密码哈希服务
    /// </summary>
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// 认证服务
    /// </summary>
    private readonly IAuthenticationService _authenticationService = authenticationService;

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="input">密码重置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetPassword)]
    public async Task<UserSecurityDetailDto> ResetUserPasswordAsync(UserPasswordResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePasswordResetInput(input);

        var (user, security) = await GetUserSecurityOrThrowAsync(input.UserId, cancellationToken);
        await EnsurePasswordMeetsPolicyAsync(user, input.NewPassword, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.Password = _passwordHasher.HashPassword(input.NewPassword);
        security.LastPasswordChangeTime = now;
        security.PasswordExpiryTime = input.PasswordExpiryTime;
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(input.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(savedSecurity, user, now);
    }

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    /// <param name="input">锁定状态参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Lock)]
    public async Task<UserSecurityDetailDto> UpdateUserLockAsync(UserLockUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateLockInput(input);

        var (user, security) = await GetUserSecurityOrThrowAsync(input.UserId, cancellationToken);
        if (input.IsLocked)
        {
            await EnsureUserCanBeLockedAsync(user, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        if (input.IsLocked)
        {
            security.IsLocked = true;
            security.LockoutTime = now;
            security.LockoutEndTime = input.LockoutEndTime;
        }
        else
        {
            security.IsLocked = false;
            security.LockoutTime = null;
            security.LockoutEndTime = null;
            security.FailedLoginAttempts = 0;
            security.LastFailedLoginTime = null;
        }

        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(input.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(savedSecurity, user, now);
    }

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    /// <param name="input">登录策略参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.LoginPolicy)]
    public async Task<UserSecurityDetailDto> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateLoginPolicyInput(input);

        var (user, security) = await GetUserSecurityOrThrowAsync(input.UserId, cancellationToken);
        security.AllowMultiLogin = input.AllowMultiLogin;
        security.MaxLoginDevices = input.MaxLoginDevices;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(input.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(savedSecurity, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 获取用户与安全记录，不存在时抛出异常
    /// </summary>
    private async Task<(SysUser User, SysUserSecurity Security)> GetUserSecurityOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == user.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("用户安全记录不存在。");

        return (user, security);
    }

    /// <summary>
    /// 校验用户能否被锁定
    /// </summary>
    private async Task EnsureUserCanBeLockedAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能通过用户安全服务锁定。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能通过用户安全服务锁定。");
        }
    }

    /// <summary>
    /// 校验密码策略
    /// </summary>
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

    /// <summary>
    /// 构建密码黑名单
    /// </summary>
    private static List<string> BuildPasswordBlacklist(SysUser user)
    {
        return
        [
            .. new[]
            {
                user.UserName,
                user.RealName,
                user.NickName,
                user.Email,
                user.Phone
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
        ];
    }

    /// <summary>
    /// 校验密码重置参数
    /// </summary>
    private static void ValidatePasswordResetInput(UserPasswordResetDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(input.NewPassword);
        if (input.PasswordExpiryTime.HasValue && input.PasswordExpiryTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "密码过期时间必须晚于当前时间。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验锁定参数
    /// </summary>
    private static void ValidateLockInput(UserLockUpdateDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.IsLocked && input.LockoutEndTime.HasValue && input.LockoutEndTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "锁定结束时间必须晚于当前时间。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验登录策略参数
    /// </summary>
    private static void ValidateLoginPolicyInput(UserLoginPolicyUpdateDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.MaxLoginDevices < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "最大登录设备数不能小于 0。");
        }

        if (!input.AllowMultiLogin && input.MaxLoginDevices == 0)
        {
            throw new InvalidOperationException("禁用多端登录时最大登录设备数必须大于 0。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 创建安全戳
    /// </summary>
    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }
}
