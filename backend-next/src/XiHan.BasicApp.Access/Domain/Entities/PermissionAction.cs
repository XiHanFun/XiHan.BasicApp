// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 授予规则的动作。JSON 上是字符串枚举。
/// </summary>
public enum PermissionAction
{
    /// <summary>
    /// 授予（规则行只带有效期与原因，授予本身在框架授予存储）。
    /// </summary>
    Grant = 0,

    /// <summary>
    /// 拒绝：用户上的拒绝压过一切授予；角色上的拒绝只拿走这个角色自己的授予。
    /// </summary>
    Deny = 1,
}

/// <summary>
/// 授予规则的主体类型。JSON 上是字符串枚举。
/// </summary>
public enum GrantSubjectKind
{
    /// <summary>
    /// 角色。
    /// </summary>
    Role = 0,

    /// <summary>
    /// 用户（直接授权）。
    /// </summary>
    User = 1,
}
