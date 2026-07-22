// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 授权来源
/// </summary>
public enum AuthorizationGrantSource
{
    /// <summary>
    /// 用户直授
    /// </summary>
    [Description("用户直授")]
    User = 0,

    /// <summary>
    /// 角色授权
    /// </summary>
    [Description("角色授权")]
    Role = 1,

    /// <summary>
    /// 租户版本
    /// </summary>
    [Description("租户版本")]
    TenantEdition = 2,

    /// <summary>
    /// 权限委派
    /// </summary>
    [Description("权限委派")]
    Delegation = 3
}
