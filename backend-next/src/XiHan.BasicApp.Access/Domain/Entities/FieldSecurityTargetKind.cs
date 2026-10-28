// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 字段安全规则的目标类型。
/// </summary>
public enum FieldSecurityTargetKind
{
    /// <summary>
    /// 角色：此刻持有该角色（指派有效、角色启用）的用户；只认直接持有。
    /// </summary>
    Role = 0,

    /// <summary>
    /// 用户本人。
    /// </summary>
    User = 1,

    /// <summary>
    /// 部门（只在租户侧）：该部门及其下级部门的成员；停用的部门不算。
    /// </summary>
    Department = 2,
}

/// <summary>
/// 字段安全规则的状态。
/// </summary>
public enum FieldRuleStatus
{
    /// <summary>
    /// 启用。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：不参与判定。
    /// </summary>
    Disabled = 1,
}
