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
}
