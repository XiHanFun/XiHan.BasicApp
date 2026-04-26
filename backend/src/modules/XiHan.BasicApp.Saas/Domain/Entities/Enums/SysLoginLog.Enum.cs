#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLog.Enum
// Guid:c4c53023-c1cd-497f-b8f7-eb9a2c9c4800
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 登录结果枚举
/// </summary>
public enum LoginResult
{
    /// <summary>
    /// 成功
    /// </summary>
    [Description("成功")]
    Success = 0,

    /// <summary>
    /// 失败 - 用户名或密码错误
    /// </summary>
    [Description("失败 - 用户名或密码错误")]
    InvalidCredentials = 1,

    /// <summary>
    /// 失败 - 账号已锁定
    /// </summary>
    [Description("失败 - 账号已锁定")]
    AccountLocked = 2,

    /// <summary>
    /// 失败 - 账号已禁用
    /// </summary>
    [Description("失败 - 账号已禁用")]
    AccountDisabled = 3,

    /// <summary>
    /// 失败 - 需要双因素认证
    /// </summary>
    [Description("失败 - 需要双因素认证")]
    RequiresTwoFactor = 4,

    /// <summary>
    /// 失败 - 双因素认证失败
    /// </summary>
    [Description("失败 - 双因素认证失败")]
    TwoFactorFailed = 5,

    /// <summary>
    /// 失败 - 其他错误
    /// </summary>
    [Description("失败 - 其他错误")]
    Failed = 99
}
