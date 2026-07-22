// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    [Description("密码错误")]
    InvalidCredentials = 1,

    /// <summary>
    /// 失败 - 账号已锁定
    /// </summary>
    [Description("账号锁定")]
    AccountLocked = 2,

    /// <summary>
    /// 失败 - 账号已禁用
    /// </summary>
    [Description("账号禁用")]
    AccountDisabled = 3,

    /// <summary>
    /// 失败 - 需要双因素认证
    /// </summary>
    [Description("需二次验证")]
    RequiresTwoFactor = 4,

    /// <summary>
    /// 失败 - 双因素认证失败
    /// </summary>
    [Description("二次验证失败")]
    TwoFactorFailed = 5,

    /// <summary>
    /// 正常登出
    /// </summary>
    [Description("正常登出")]
    Logout = 10,

    /// <summary>
    /// 令牌刷新（认证审计事件）
    /// </summary>
    [Description("令牌刷新")]
    TokenRefreshed = 11,

    /// <summary>
    /// 密码修改（认证审计事件）
    /// </summary>
    [Description("密码修改")]
    PasswordChanged = 12,

    /// <summary>
    /// 密码重置（认证审计事件）
    /// </summary>
    [Description("密码重置")]
    PasswordReset = 13,

    /// <summary>
    /// 绑定 MFA（认证审计事件）
    /// </summary>
    [Description("绑定MFA")]
    MfaBound = 14,

    /// <summary>
    /// 解绑 MFA（认证审计事件）
    /// </summary>
    [Description("解绑MFA")]
    MfaUnbound = 15,

    /// <summary>
    /// 切换租户（认证审计事件：复用会话轮换令牌，不算一次新登录）
    /// </summary>
    [Description("切换租户")]
    TenantSwitched = 16,

    /// <summary>
    /// 会话撤销（认证审计事件：本人在个人中心撤销，或管理员踢下线，具体原因见消息）
    /// </summary>
    [Description("会话撤销")]
    SessionRevoked = 17,

    /// <summary>
    /// 失败 - 其他错误
    /// </summary>
    [Description("其他失败")]
    Failed = 99
}
