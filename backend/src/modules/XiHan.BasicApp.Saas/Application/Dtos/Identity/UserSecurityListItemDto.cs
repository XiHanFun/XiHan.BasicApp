#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityListItemDto
// Guid:2a52c25f-4718-4d41-8685-b010a1d34cc0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户安全列表项 DTO
/// </summary>
public sealed class UserSecurityListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 最后修改密码时间
    /// </summary>
    public DateTimeOffset? LastPasswordChangeTime { get; set; }

    /// <summary>
    /// 密码过期时间
    /// </summary>
    public DateTimeOffset? PasswordExpiryTime { get; set; }

    /// <summary>
    /// 密码是否已过期
    /// </summary>
    public bool IsPasswordExpired { get; set; }

    /// <summary>
    /// 失败登录次数
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTimeOffset? LockoutEndTime { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 双因素认证方式
    /// </summary>
    public TwoFactorMethod TwoFactorMethod { get; set; }

    /// <summary>
    /// 邮箱是否验证
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 手机号是否验证
    /// </summary>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    public bool AllowMultiLogin { get; set; }

    /// <summary>
    /// 最大登录设备数
    /// </summary>
    public int MaxLoginDevices { get; set; }

    /// <summary>
    /// 上次安全检查时间
    /// </summary>
    public DateTimeOffset? LastSecurityCheckTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
