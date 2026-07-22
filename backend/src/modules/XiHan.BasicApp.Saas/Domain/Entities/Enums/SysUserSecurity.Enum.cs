// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 双因素认证方式枚举（可按位组合，支持同时启用多种方式）
/// </summary>
[Flags]
public enum TwoFactorMethod
{
    /// <summary>
    /// 未启用
    /// </summary>
    [Description("未启用")]
    None = 0,

    /// <summary>
    /// TOTP（时间型一次性密码，Authenticator App）
    /// </summary>
    [Description("TOTP")]
    Totp = 1,

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    [Description("邮箱验证码")]
    Email = 2,

    /// <summary>
    /// 手机短信验证码
    /// </summary>
    [Description("短信验证码")]
    Phone = 4
}
