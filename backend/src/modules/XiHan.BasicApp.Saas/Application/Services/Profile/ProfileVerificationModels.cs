#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileVerificationModels
// Guid:ba19ab56-e583-4bf8-ab24-d0f767917b60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户验证码用途
/// </summary>
public enum ProfileVerificationPurpose
{
    /// <summary>
    /// 验证邮箱
    /// </summary>
    VerifyEmail,

    /// <summary>
    /// 验证手机
    /// </summary>
    VerifyPhone,

    /// <summary>
    /// 换绑邮箱
    /// </summary>
    ChangeEmail,

    /// <summary>
    /// 换绑手机
    /// </summary>
    ChangePhone,

    /// <summary>
    /// 邮箱两步验证
    /// </summary>
    TwoFactorEmail,

    /// <summary>
    /// 手机两步验证
    /// </summary>
    TwoFactorPhone
}
