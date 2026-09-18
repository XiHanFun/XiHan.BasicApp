// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    TwoFactorPhone,

    /// <summary>
    /// 手机验证码登录发码（匿名端点；发送阶段已按手机号定位到具体用户，故仍可按用户维度计入日配额/封禁）
    /// </summary>
    PhoneLoginCode
}
