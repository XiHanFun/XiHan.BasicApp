// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 账号开启的双因素方式（按位组合，存库）；接口上按 <see cref="TwoFactorMethod"/> 的列表出入。
/// </summary>
[Flags]
public enum TwoFactorMethods
{
    /// <summary>
    /// 没有开启任何方式。
    /// </summary>
    None = 0,

    /// <summary>
    /// 认证器（TOTP）。
    /// </summary>
    Totp = 1,

    /// <summary>
    /// 邮箱验证码。
    /// </summary>
    Email = 2,

    /// <summary>
    /// 手机短信验证码。
    /// </summary>
    Phone = 4,
}
