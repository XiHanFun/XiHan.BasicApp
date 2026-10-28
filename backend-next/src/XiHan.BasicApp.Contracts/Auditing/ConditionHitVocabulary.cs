// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 条件命中的来源。
/// </summary>
public enum ConditionHitSource
{
    /// <summary>
    /// 访问策略。
    /// </summary>
    AccessPolicy = 0,

    /// <summary>
    /// 权限条件（ABAC）。
    /// </summary>
    PermissionCondition = 1,
}

/// <summary>
/// 命中的规则要求的动作。
/// </summary>
public enum ConditionHitAction
{
    /// <summary>
    /// 只记录。
    /// </summary>
    LogOnly = 0,

    /// <summary>
    /// 拒绝。
    /// </summary>
    Deny = 1,

    /// <summary>
    /// 放行。
    /// </summary>
    Allow = 2,
}

/// <summary>
/// 规则求值的时机。
/// </summary>
public enum ConditionHitPhase
{
    /// <summary>
    /// 认证之前。
    /// </summary>
    BeforeAuthentication = 0,

    /// <summary>
    /// 认证之后。
    /// </summary>
    AfterAuthentication = 1,

    /// <summary>
    /// 登录时。
    /// </summary>
    Login = 2,

    /// <summary>
    /// 授权判定（权限条件）。
    /// </summary>
    Authorization = 3,
}
