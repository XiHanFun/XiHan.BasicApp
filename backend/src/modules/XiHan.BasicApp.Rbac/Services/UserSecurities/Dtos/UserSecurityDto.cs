#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityDto
// Guid:cd2b3c4d-5e6f-7890-abcd-ef12345678a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.UserSecurities.Dtos;

/// <summary>
/// 用户安全 DTO
/// </summary>
public class UserSecurityDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 最后修改密码时间
    /// </summary>
    public DateTimeOffset? LastPasswordChangeTime { get; set; }

    /// <summary>
    /// 密码过期时间
    /// </summary>
    public DateTimeOffset? PasswordExpiryTime { get; set; }

    /// <summary>
    /// 失败登录次数
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// 最后失败登录时间
    /// </summary>
    public DateTimeOffset? LastFailedLoginTime { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 锁定时间
    /// </summary>
    public DateTimeOffset? LockoutTime { get; set; }

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTimeOffset? LockoutEndTime { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// 双因素认证密钥
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 安全戳（用于强制重新登录）
    /// </summary>
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// 邮箱是否验证
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// 手机号是否验证
    /// </summary>
    public bool PhoneVerified { get; set; } = false;

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    public bool AllowMultiLogin { get; set; } = true;

    /// <summary>
    /// 最大登录设备数（0表示不限制）
    /// </summary>
    public int MaxLoginDevices { get; set; } = 0;

    /// <summary>
    /// 上次安全检查时间
    /// </summary>
    public DateTimeOffset? LastSecurityCheckTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建用户安全 DTO
/// </summary>
public class CreateUserSecurityDto : RbacCreationDtoBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 密码过期时间
    /// </summary>
    public DateTimeOffset? PasswordExpiryTime { get; set; }

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    public bool AllowMultiLogin { get; set; } = true;

    /// <summary>
    /// 最大登录设备数（0表示不限制）
    /// </summary>
    public int MaxLoginDevices { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新用户安全 DTO
/// </summary>
public class UpdateUserSecurityDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 密码过期时间
    /// </summary>
    public DateTimeOffset? PasswordExpiryTime { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    public bool? TwoFactorEnabled { get; set; }

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    public bool? AllowMultiLogin { get; set; }

    /// <summary>
    /// 最大登录设备数（0表示不限制）
    /// </summary>
    public int? MaxLoginDevices { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

