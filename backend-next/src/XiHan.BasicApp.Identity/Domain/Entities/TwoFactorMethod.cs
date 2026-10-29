// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 一种双因素方式。
/// </summary>
/// <remarks>
/// JSON 上是字符串枚举。邮箱与短信要等 Notification 能发码，目前只有认证器可以开启。
/// </remarks>
public enum TwoFactorMethod
{
    /// <summary>
    /// 认证器（TOTP，RFC 6238）。
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
