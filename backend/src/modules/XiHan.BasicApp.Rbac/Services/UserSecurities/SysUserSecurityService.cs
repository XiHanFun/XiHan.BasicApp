#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurityService
// Guid:ed2b3c4d-5e6f-7890-abcd-ef12345678a3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.UserSecurities;
using XiHan.BasicApp.Rbac.Services.UserSecurities.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.UserSecurities;

/// <summary>
/// 系统用户安全服务实现
/// </summary>
public class SysUserSecurityService : CrudApplicationServiceBase<SysUserSecurity, UserSecurityDto, XiHanBasicAppIdType, CreateUserSecurityDto, UpdateUserSecurityDto>, ISysUserSecurityService
{
    private readonly ISysUserSecurityRepository _userSecurityRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserSecurityService(ISysUserSecurityRepository userSecurityRepository) : base(userSecurityRepository)
    {
        _userSecurityRepository = userSecurityRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取用户安全信息
    /// </summary>
    public async Task<UserSecurityDto?> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var userSecurity = await _userSecurityRepository.GetByUserIdAsync(userId);
        return userSecurity?.ToDto();
    }

    /// <summary>
    /// 获取锁定的用户列表
    /// </summary>
    public async Task<List<UserSecurityDto>> GetLockedUsersAsync()
    {
        var userSecurities = await _userSecurityRepository.GetLockedUsersAsync();
        return userSecurities.ToDto();
    }

    /// <summary>
    /// 获取密码过期的用户列表
    /// </summary>
    public async Task<List<UserSecurityDto>> GetPasswordExpiredUsersAsync()
    {
        var userSecurities = await _userSecurityRepository.GetPasswordExpiredUsersAsync();
        return userSecurities.ToDto();
    }

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    public async Task<bool> IncrementFailedLoginAttemptsAsync(XiHanBasicAppIdType userId)
    {
        return await _userSecurityRepository.IncrementFailedLoginAttemptsAsync(userId);
    }

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    public async Task<bool> ResetFailedLoginAttemptsAsync(XiHanBasicAppIdType userId)
    {
        return await _userSecurityRepository.ResetFailedLoginAttemptsAsync(userId);
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    public async Task<bool> LockUserAsync(XiHanBasicAppIdType userId, DateTimeOffset? lockoutEndTime = null)
    {
        return await _userSecurityRepository.LockUserAsync(userId, lockoutEndTime);
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    public async Task<bool> UnlockUserAsync(XiHanBasicAppIdType userId)
    {
        return await _userSecurityRepository.UnlockUserAsync(userId);
    }

    /// <summary>
    /// 更新安全戳（强制重新登录）
    /// </summary>
    public async Task<bool> UpdateSecurityStampAsync(XiHanBasicAppIdType userId)
    {
        return await _userSecurityRepository.UpdateSecurityStampAsync(userId);
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<UserSecurityDto> MapToEntityDtoAsync(SysUserSecurity entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 UserSecurityDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysUserSecurity> MapToEntityAsync(UserSecurityDto dto)
    {
        var entity = new SysUserSecurity
        {
            UserId = dto.UserId,
            LastPasswordChangeTime = dto.LastPasswordChangeTime,
            PasswordExpiryTime = dto.PasswordExpiryTime,
            FailedLoginAttempts = dto.FailedLoginAttempts,
            LastFailedLoginTime = dto.LastFailedLoginTime,
            IsLocked = dto.IsLocked,
            LockoutTime = dto.LockoutTime,
            LockoutEndTime = dto.LockoutEndTime,
            TwoFactorEnabled = dto.TwoFactorEnabled,
            TwoFactorSecret = dto.TwoFactorSecret,
            SecurityStamp = dto.SecurityStamp,
            EmailVerified = dto.EmailVerified,
            PhoneVerified = dto.PhoneVerified,
            AllowMultiLogin = dto.AllowMultiLogin,
            MaxLoginDevices = dto.MaxLoginDevices,
            LastSecurityCheckTime = dto.LastSecurityCheckTime,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 UserSecurityDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(UserSecurityDto dto, SysUserSecurity entity)
    {
        entity.UserId = dto.UserId;
        entity.LastPasswordChangeTime = dto.LastPasswordChangeTime;
        entity.PasswordExpiryTime = dto.PasswordExpiryTime;
        entity.FailedLoginAttempts = dto.FailedLoginAttempts;
        entity.LastFailedLoginTime = dto.LastFailedLoginTime;
        entity.IsLocked = dto.IsLocked;
        entity.LockoutTime = dto.LockoutTime;
        entity.LockoutEndTime = dto.LockoutEndTime;
        entity.TwoFactorEnabled = dto.TwoFactorEnabled;
        entity.TwoFactorSecret = dto.TwoFactorSecret;
        entity.SecurityStamp = dto.SecurityStamp;
        entity.EmailVerified = dto.EmailVerified;
        entity.PhoneVerified = dto.PhoneVerified;
        entity.AllowMultiLogin = dto.AllowMultiLogin;
        entity.MaxLoginDevices = dto.MaxLoginDevices;
        entity.LastSecurityCheckTime = dto.LastSecurityCheckTime;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysUserSecurity> MapToEntityAsync(CreateUserSecurityDto createDto)
    {
        var entity = new SysUserSecurity
        {
            UserId = createDto.UserId,
            PasswordExpiryTime = createDto.PasswordExpiryTime,
            AllowMultiLogin = createDto.AllowMultiLogin,
            MaxLoginDevices = createDto.MaxLoginDevices,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateUserSecurityDto updateDto, SysUserSecurity entity)
    {
        if (updateDto.PasswordExpiryTime.HasValue) entity.PasswordExpiryTime = updateDto.PasswordExpiryTime;
        if (updateDto.TwoFactorEnabled.HasValue) entity.TwoFactorEnabled = updateDto.TwoFactorEnabled.Value;
        if (updateDto.AllowMultiLogin.HasValue) entity.AllowMultiLogin = updateDto.AllowMultiLogin.Value;
        if (updateDto.MaxLoginDevices.HasValue) entity.MaxLoginDevices = updateDto.MaxLoginDevices.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
