// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 访问策略动作
/// </summary>
public enum AccessPolicyAction
{
    /// <summary>
    /// 只记录：命中只写命中记录，不拦截
    /// </summary>
    [Description("只记录")]
    LogOnly = 0,

    /// <summary>
    /// 拒绝
    /// </summary>
    [Description("拒绝")]
    Deny = 1,

    /// <summary>
    /// 放行：同一层里压过拒绝（白名单例外）
    /// </summary>
    [Description("放行")]
    Allow = 2
}

/// <summary>
/// 条件命中的求值时机
/// </summary>
public enum AccessPolicyPhase
{
    /// <summary>
    /// 认证前：管线最前，只有请求本身的属性
    /// </summary>
    [Description("认证前")]
    BeforeAuthentication = 1,

    /// <summary>
    /// 认证后：认证与租户解析之后、授权之前
    /// </summary>
    [Description("认证后")]
    AfterAuthentication = 2,

    /// <summary>
    /// 登录时：核对凭据之后、签发令牌之前
    /// </summary>
    [Description("登录时")]
    Login = 3,

    /// <summary>
    /// 鉴权时：权限条件随权限判定求值
    /// </summary>
    [Description("鉴权时")]
    Authorization = 4
}

/// <summary>
/// 命中记录的来源
/// </summary>
public enum AccessPolicyHitSource
{
    /// <summary>
    /// 访问策略
    /// </summary>
    [Description("访问策略")]
    AccessPolicy = 0,

    /// <summary>
    /// 权限条件
    /// </summary>
    [Description("权限条件")]
    PermissionCondition = 1
}
