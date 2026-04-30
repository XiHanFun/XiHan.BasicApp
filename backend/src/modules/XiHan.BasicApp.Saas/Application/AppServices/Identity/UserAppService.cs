#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAppService
// Guid:ce9953f4-f7d4-4ebe-96cc-e282947f11ab
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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserAppService(
    IUserRepository userRepository,
    IUserSecurityRepository userSecurityRepository,
    ITenantUserRepository tenantUserRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationService authenticationService,
    ICurrentUser currentUser)
    : SaasApplicationService, IUserAppService
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
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Create)]
    public async Task<UserDetailDto> CreateUserAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var userName = input.UserName.Trim();
        if (await _userRepository.ExistsUserNameAsync(userName, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("用户名已存在。");
        }

        await EnsurePasswordMeetsPolicyAsync(input, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = new SysUser
        {
            UserName = userName,
            RealName = NormalizeNullable(input.RealName),
            NickName = NormalizeNullable(input.NickName),
            Avatar = NormalizeNullable(input.Avatar),
            Email = NormalizeNullable(input.Email),
            Phone = NormalizeNullable(input.Phone),
            Gender = input.Gender,
            Birthday = input.Birthday,
            Status = input.Status,
            TimeZone = NormalizeNullable(input.TimeZone),
            Language = NormalizeNullable(input.Language) ?? "zh-CN",
            Country = NormalizeNullable(input.Country),
            IsSystemAccount = false,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedUser = await _userRepository.AddAsync(user, cancellationToken);
        await CreateUserSecurityAsync(savedUser.BasicId, input.InitialPassword, now, input.Remark, cancellationToken);
        await CreateTenantMembershipAsync(savedUser.BasicId, input, now, cancellationToken);

        return UserApplicationMapper.ToDetailDto(savedUser);
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Update)]
    public async Task<UserDetailDto> UpdateUserAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var user = await GetUserOrThrowAsync(input.BasicId, cancellationToken);
        var normalizedEmail = NormalizeNullable(input.Email);
        var normalizedPhone = NormalizeNullable(input.Phone);
        var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var phoneChanged = !string.Equals(user.Phone, normalizedPhone, StringComparison.Ordinal);

        user.RealName = NormalizeNullable(input.RealName);
        user.NickName = NormalizeNullable(input.NickName);
        user.Avatar = NormalizeNullable(input.Avatar);
        user.Email = normalizedEmail;
        user.Phone = normalizedPhone;
        user.Gender = input.Gender;
        user.Birthday = input.Birthday;
        user.TimeZone = NormalizeNullable(input.TimeZone);
        user.Language = NormalizeNullable(input.Language);
        user.Country = NormalizeNullable(input.Country);
        user.Remark = NormalizeNullable(input.Remark);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        if (emailChanged || phoneChanged)
        {
            await ResetContactVerificationAsync(savedUser.BasicId, emailChanged, phoneChanged, cancellationToken);
        }

        return UserApplicationMapper.ToDetailDto(savedUser);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Status)]
    public async Task<UserDetailDto> UpdateUserStatusAsync(UserStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var user = await GetUserOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == EnableStatus.Disabled)
        {
            await EnsureUserCanBeDisabledAsync(user, cancellationToken);
        }

        user.Status = input.Status;
        user.Remark = NormalizeNullable(input.Remark);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        if (input.Status == EnableStatus.Disabled)
        {
            await RefreshSecurityStampAsync(savedUser.BasicId, cancellationToken);
        }

        return UserApplicationMapper.ToDetailDto(savedUser);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Delete)]
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await GetUserOrThrowAsync(id, cancellationToken);
        var membership = await EnsureUserCanBeDeletedAsync(user, cancellationToken);

        if (membership is not null)
        {
            membership.InviteStatus = TenantMemberInviteStatus.Revoked;
            membership.Status = ValidityStatus.Invalid;
            membership.RespondedTime ??= DateTimeOffset.UtcNow;
            _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
        }

        await SoftDeleteUserSecurityAsync(user.BasicId, cancellationToken);
        await _userRepository.SoftDeleteAsync(user, cancellationToken);
    }

    /// <summary>
    /// 创建用户安全记录
    /// </summary>
    private async Task CreateUserSecurityAsync(long userId, string password, DateTimeOffset now, string? remark, CancellationToken cancellationToken)
    {
        var userSecurity = new SysUserSecurity
        {
            UserId = userId,
            Password = _passwordHasher.HashPassword(password),
            LastPasswordChangeTime = now,
            FailedLoginAttempts = 0,
            IsLocked = false,
            TwoFactorEnabled = false,
            TwoFactorMethod = TwoFactorMethod.None,
            SecurityStamp = NewSecurityStamp(),
            EmailVerified = false,
            PhoneVerified = false,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            LastSecurityCheckTime = now,
            Remark = NormalizeNullable(remark)
        };

        _ = await _userSecurityRepository.AddAsync(userSecurity, cancellationToken);
    }

    /// <summary>
    /// 创建当前租户成员记录
    /// </summary>
    private async Task CreateTenantMembershipAsync(long userId, UserCreateDto input, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = new SysTenantUser
        {
            UserId = userId,
            MemberType = input.MemberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            InvitedBy = _currentUser.UserId,
            InvitedTime = now,
            RespondedTime = now,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            DisplayName = NormalizeNullable(input.DisplayName),
            InviteRemark = NormalizeNullable(input.InviteRemark),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        _ = await _tenantUserRepository.AddAsync(tenantMember, cancellationToken);
    }

    /// <summary>
    /// 获取用户，不存在时抛出异常
    /// </summary>
    private async Task<SysUser> GetUserOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户主键必须大于 0。");
        }

        return await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
    }

    /// <summary>
    /// 校验用户能否停用
    /// </summary>
    private async Task EnsureUserCanBeDisabledAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能停用。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能直接停用。");
        }
    }

    /// <summary>
    /// 校验用户能否删除
    /// </summary>
    private async Task<SysTenantUser?> EnsureUserCanBeDeletedAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能删除。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能直接删除。");
        }

        return membership;
    }

    /// <summary>
    /// 重置联系方式验证状态
    /// </summary>
    private async Task ResetContactVerificationAsync(long userId, bool emailChanged, bool phoneChanged, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == userId, cancellationToken);
        if (security is null)
        {
            return;
        }

        if (emailChanged)
        {
            security.EmailVerified = false;
        }

        if (phoneChanged)
        {
            security.PhoneVerified = false;
        }

        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <summary>
    /// 刷新安全戳
    /// </summary>
    private async Task RefreshSecurityStampAsync(long userId, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == userId, cancellationToken);
        if (security is null)
        {
            return;
        }

        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <summary>
    /// 软删除用户安全记录
    /// </summary>
    private async Task SoftDeleteUserSecurityAsync(long userId, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == userId, cancellationToken);
        if (security is null)
        {
            return;
        }

        security.IsDeleted = true;
        security.DeletedTime = DateTimeOffset.UtcNow;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(UserCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.InitialPassword);

        ValidateRequiredLength(input.UserName, 50, nameof(input.UserName), "用户名不能超过 50 个字符。");
        ValidateOptionalLength(input.RealName, 50, nameof(input.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(input.NickName, 50, nameof(input.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(input.Avatar, 500, nameof(input.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(input.Email, 100, nameof(input.Email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(input.Phone, 20, nameof(input.Phone), "手机号不能超过 20 个字符。");
        ValidateOptionalLength(input.TimeZone, 50, nameof(input.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(input.Language, 10, nameof(input.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(input.Country, 50, nameof(input.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(input.DisplayName, 100, nameof(input.DisplayName), "租户内显示名不能超过 100 个字符。");
        ValidateOptionalLength(input.InviteRemark, 500, nameof(input.InviteRemark), "邀请备注不能超过 500 个字符。");
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        ValidateEnum(input.Gender, nameof(input.Gender));
        ValidateEnum(input.Status, nameof(input.Status));
        ValidateEnum(input.MemberType, nameof(input.MemberType));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
        EnsureMemberTypeCanBeCreated(input.MemberType);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(UserUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ValidateOptionalLength(input.RealName, 50, nameof(input.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(input.NickName, 50, nameof(input.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(input.Avatar, 500, nameof(input.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(input.Email, 100, nameof(input.Email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(input.Phone, 20, nameof(input.Phone), "手机号不能超过 20 个字符。");
        ValidateOptionalLength(input.TimeZone, 50, nameof(input.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(input.Language, 10, nameof(input.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(input.Country, 50, nameof(input.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        ValidateEnum(input.Gender, nameof(input.Gender));
    }

    /// <summary>
    /// 校验密码策略
    /// </summary>
    private async Task EnsurePasswordMeetsPolicyAsync(UserCreateDto input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blacklist = BuildPasswordBlacklist(input);
        var result = await _authenticationService.ValidatePasswordStrengthAsync(input.InitialPassword, blacklist);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors.Count > 0 ? string.Join("；", result.Errors) : result.Message;
        throw new InvalidOperationException($"初始密码不符合安全要求：{errors}");
    }

    /// <summary>
    /// 构建密码黑名单
    /// </summary>
    private static List<string> BuildPasswordBlacklist(UserCreateDto input)
    {
        return
        [
            .. new[]
            {
                input.UserName,
                input.RealName,
                input.NickName,
                input.Email,
                input.Phone
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
        ];
    }

    /// <summary>
    /// 校验成员类型可由用户创建流程分配
    /// </summary>
    private static void EnsureMemberTypeCanBeCreated(TenantMemberType memberType)
    {
        if (memberType is TenantMemberType.Owner or TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("租户所有者和平台管理员成员身份必须通过专项流程维护。");
        }
    }

    /// <summary>
    /// 校验成员有效期
    /// </summary>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("成员失效时间必须晚于生效时间。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验必填字符串长度
    /// </summary>
    private static void ValidateRequiredLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
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
