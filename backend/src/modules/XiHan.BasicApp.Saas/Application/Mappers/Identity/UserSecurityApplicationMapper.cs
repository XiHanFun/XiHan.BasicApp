#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityApplicationMapper
// Guid:4cf5d7cc-a4ea-4ebf-a2d7-2326c3bdff2d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 用户安全应用层映射器
/// </summary>
public static class UserSecurityApplicationMapper
{
    /// <summary>
    /// 映射用户安全列表项
    /// </summary>
    /// <param name="security">用户安全实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户安全列表项 DTO</returns>
    public static UserSecurityListItemDto ToListItemDto(SysUserSecurity security, SysUser? user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(security);

        return new UserSecurityListItemDto
        {
            BasicId = security.BasicId,
            UserId = security.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            LastPasswordChangeTime = security.LastPasswordChangeTime,
            PasswordExpiryTime = security.PasswordExpiryTime,
            IsPasswordExpired = IsPasswordExpired(security, now),
            FailedLoginAttempts = security.FailedLoginAttempts,
            IsLocked = security.IsLocked,
            LockoutEndTime = security.LockoutEndTime,
            TwoFactorEnabled = security.TwoFactorEnabled,
            TwoFactorMethod = security.TwoFactorMethod,
            EmailVerified = security.EmailVerified,
            PhoneVerified = security.PhoneVerified,
            AllowMultiLogin = security.AllowMultiLogin,
            MaxLoginDevices = security.MaxLoginDevices,
            LastSecurityCheckTime = security.LastSecurityCheckTime,
            CreatedTime = security.CreatedTime,
            ModifiedTime = security.ModifiedTime
        };
    }

    /// <summary>
    /// 映射用户安全详情
    /// </summary>
    /// <param name="security">用户安全实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>用户安全详情 DTO</returns>
    public static UserSecurityDetailDto ToDetailDto(SysUserSecurity security, SysUser user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(security);
        ArgumentNullException.ThrowIfNull(user);

        return new UserSecurityDetailDto
        {
            BasicId = security.BasicId,
            UserId = security.UserId,
            UserName = user.UserName,
            RealName = user.RealName,
            NickName = user.NickName,
            LastPasswordChangeTime = security.LastPasswordChangeTime,
            PasswordExpiryTime = security.PasswordExpiryTime,
            IsPasswordExpired = IsPasswordExpired(security, now),
            FailedLoginAttempts = security.FailedLoginAttempts,
            LastFailedLoginTime = security.LastFailedLoginTime,
            IsLocked = security.IsLocked,
            LockoutTime = security.LockoutTime,
            LockoutEndTime = security.LockoutEndTime,
            TwoFactorEnabled = security.TwoFactorEnabled,
            TwoFactorMethod = security.TwoFactorMethod,
            LastUserNameChangeTime = security.LastUserNameChangeTime,
            EmailVerified = security.EmailVerified,
            PhoneVerified = security.PhoneVerified,
            AllowMultiLogin = security.AllowMultiLogin,
            MaxLoginDevices = security.MaxLoginDevices,
            LastSecurityCheckTime = security.LastSecurityCheckTime,
            Remark = security.Remark,
            CreatedTime = security.CreatedTime,
            CreatedId = security.CreatedId,
            CreatedBy = security.CreatedBy,
            ModifiedTime = security.ModifiedTime,
            ModifiedId = security.ModifiedId,
            ModifiedBy = security.ModifiedBy
        };
    }

    /// <summary>
    /// 判断密码是否过期
    /// </summary>
    private static bool IsPasswordExpired(SysUserSecurity security, DateTimeOffset now)
    {
        return security.PasswordExpiryTime.HasValue && security.PasswordExpiryTime.Value <= now;
    }
}
