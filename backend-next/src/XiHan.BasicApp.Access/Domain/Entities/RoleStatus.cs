// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色状态。
/// </summary>
public enum RoleStatus
{
    /// <summary>
    /// 生效：成员的令牌带该角色。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：保留成员与授予，但不进入令牌。
    /// </summary>
    Disabled = 1,
}
