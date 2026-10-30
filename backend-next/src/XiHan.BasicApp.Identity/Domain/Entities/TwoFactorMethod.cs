// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 一种双因素方式。
/// </summary>
/// <remarks>
/// JSON 上是字符串枚举。邮箱与短信要本人已验证的联系方式，码经 Notification 发出。
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
