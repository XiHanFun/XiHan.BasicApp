// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 登录日志记的身份事件；成败由调用结果决定，与事件类型分开记。
/// </summary>
public enum LoginEvent
{
    /// <summary>
    /// 登录。
    /// </summary>
    Login = 0,

    /// <summary>
    /// 用刷新令牌换新令牌。
    /// </summary>
    Refresh = 1,

    /// <summary>
    /// 退出登录。
    /// </summary>
    Logout = 2,

    /// <summary>
    /// 切换当前租户。
    /// </summary>
    SwitchTenant = 3,

    /// <summary>
    /// 开始模仿另一个用户。
    /// </summary>
    ImpersonationStart = 4,

    /// <summary>
    /// 结束模仿，回到发起人。
    /// </summary>
    ImpersonationStop = 5,

    /// <summary>
    /// 本人修改密码。
    /// </summary>
    ChangePassword = 6,

    /// <summary>
    /// 密码通过、账号开了双因素：签发两步验证票据，尚未开会话（成功即发出了挑战）。
    /// </summary>
    TwoFactorChallenge = 7,

    /// <summary>
    /// 本人开启一种双因素方式。
    /// </summary>
    TwoFactorEnable = 8,

    /// <summary>
    /// 本人关闭一种双因素方式。
    /// </summary>
    TwoFactorDisable = 9,

    /// <summary>
    /// 管理员重置用户的双因素设置（日志的用户是被重置的账号）。
    /// </summary>
    TwoFactorReset = 10,
}
