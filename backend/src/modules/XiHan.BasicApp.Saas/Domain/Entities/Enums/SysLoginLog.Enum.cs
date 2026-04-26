#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLog.Enum
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
    Success = 0,

    /// <summary>
    /// 失败 - 用户名或密码错误
    /// </summary>
    InvalidCredentials = 1,

    /// <summary>
    /// 失败 - 账号已锁定
    /// </summary>
    AccountLocked = 2,

    /// <summary>
    /// 失败 - 账号已禁用
    /// </summary>
    AccountDisabled = 3,

    /// <summary>
    /// 失败 - 需要双因素认证
    /// </summary>
    RequiresTwoFactor = 4,

    /// <summary>
    /// 失败 - 双因素认证失败
    /// </summary>
    TwoFactorFailed = 5,

    /// <summary>
    /// 失败 - 其他错误
    /// </summary>
    Failed = 99
}

