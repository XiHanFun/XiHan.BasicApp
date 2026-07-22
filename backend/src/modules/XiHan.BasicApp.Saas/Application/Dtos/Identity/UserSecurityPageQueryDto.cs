// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户安全分页查询 DTO
/// </summary>
public sealed class UserSecurityPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool? IsLocked { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    public bool? TwoFactorEnabled { get; set; }

    /// <summary>
    /// 双因素认证方式
    /// </summary>
    public TwoFactorMethod? TwoFactorMethod { get; set; }

    /// <summary>
    /// 邮箱是否验证
    /// </summary>
    public bool? EmailVerified { get; set; }

    /// <summary>
    /// 手机号是否验证
    /// </summary>
    public bool? PhoneVerified { get; set; }

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    public bool? AllowMultiLogin { get; set; }
}
