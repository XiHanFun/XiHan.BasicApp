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
/// 数据权限范围
/// </summary>
/// <remarks>
/// 重要：本枚举不承诺“数值越大范围越广”，禁止以数值大小作为权限合并依据。
/// 多角色合并必须显式按语义处理：
/// - 任一角色为 All：最终结果为全部数据；
/// - DepartmentOnly / DepartmentAndChildren：基于用户当前租户上下文下全部有效部门归属求并集；
/// - Custom：与其它范围按并集叠加；
/// - 若仅存在 SelfOnly：仅返回本人数据。
/// 这样可以避免把 Custom=99 误当成“比 All 更宽”的数值语义。
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
    /// 以用户当前租户上下文下的全部有效部门归属为基准，仅限这些部门
    /// </summary>
    DepartmentOnly = 1,

    /// <summary>
    /// 本部门及子部门数据权限
    /// 以用户当前租户上下文下的全部有效部门归属为基准，包含这些部门的所有下级部门
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
