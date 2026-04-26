#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurity.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    None = 0,

    /// <summary>
    /// TOTP（时间型一次性密码，Authenticator App）
    /// </summary>
    Totp = 1,

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    Email = 2,

    /// <summary>
    /// 手机短信验证码
    /// </summary>
    Phone = 4
}

