#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataPermissionScope
// Guid:9c2b3c4d-5e6f-7890-abcd-ef12345678be
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 07:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 数据权限范围（数值越大范围越广，多角色取 Max 即可得最宽范围）
/// </summary>
/// <remarks>
/// 多部门用户约定：DepartmentAndChildren/DepartmentOnly 以用户主部门（SysUserDepartment.IsMain=true）为基准。
/// 多角色用户约定：取所有角色中最大的 DataScope 值（Max），即 All 胜出。
/// 若存在 Custom 则合并 Custom 指定的部门与其他角色的范围取并集。
/// </remarks>
public enum DataPermissionScope
{
    /// <summary>
    /// 仅本人数据权限
    /// 只能看到自己创建的数据
    /// </summary>
    SelfOnly = 0,

    /// <summary>
    /// 仅本部门数据权限
    /// 以用户主部门为基准，仅限该部门
    /// </summary>
    DepartmentOnly = 1,

    /// <summary>
    /// 本部门及子部门数据权限
    /// 以用户主部门为基准，包含其所有下级部门
    /// </summary>
    DepartmentAndChildren = 2,

    /// <summary>
    /// 全部数据权限
    /// 可以看到所有数据
    /// </summary>
    All = 3,

    /// <summary>
    /// 自定义数据权限
    /// 通过 SysRoleDataScope 指定可访问的部门列表（支持 IncludeChildren 自动包含子部门）
    /// </summary>
    Custom = 99
}
