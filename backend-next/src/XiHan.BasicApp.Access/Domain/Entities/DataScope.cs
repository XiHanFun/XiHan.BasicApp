// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色的数据范围。
/// </summary>
/// <remarks>
/// 见方案 5.6。一个用户有多个角色时取并集，任一为 <see cref="All"/> 即不受限；平台侧角色只能是 <see cref="All"/>（平台没有部门）。
/// </remarks>
public enum DataScope
{
    /// <summary>
    /// 只看自己（新建租户角色的缺省）。
    /// </summary>
    SelfOnly = 0,

    /// <summary>
    /// 自己所在的部门（全部归属）。
    /// </summary>
    DepartmentOnly = 1,

    /// <summary>
    /// 自己所在的部门及其全部子部门。
    /// </summary>
    DepartmentAndChildren = 2,

    /// <summary>
    /// 不受限。
    /// </summary>
    All = 3,

    /// <summary>
    /// 自定义：<see cref="SysRoleDataScope"/> 列出的部门（可含子部门）。
    /// </summary>
    Custom = 4,
}
