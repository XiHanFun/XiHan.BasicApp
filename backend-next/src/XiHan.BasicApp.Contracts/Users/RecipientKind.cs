// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Users;

/// <summary>
/// 按什么找接收人。
/// </summary>
public enum RecipientKind
{
    /// <summary>
    /// 当前一侧的全部正常账号（平台：平台账号；租户：注册地在该租户的用户与租户成员）。
    /// </summary>
    All = 0,

    /// <summary>
    /// 持有这些角色（此刻有效的指派）的用户。
    /// </summary>
    Role = 1,

    /// <summary>
    /// 这些部门及其下级部门的成员（只在租户一侧）。
    /// </summary>
    Department = 2,

    /// <summary>
    /// 指定的用户。
    /// </summary>
    User = 3,
}
