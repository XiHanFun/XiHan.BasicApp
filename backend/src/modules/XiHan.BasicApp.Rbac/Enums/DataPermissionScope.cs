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

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 数据权限范围
/// </summary>
public enum DataPermissionScope
{
    /// <summary>
    /// 全部数据权限
    /// 可以看到所有数据
    /// </summary>
    All = 0,

    /// <summary>
    /// 本部门及子部门数据权限
    /// 可以看到本部门及其子部门的数据
    /// </summary>
    DepartmentAndChildren = 1,

    /// <summary>
    /// 仅本部门数据权限
    /// 只能看到本部门的数据
    /// </summary>
    DepartmentOnly = 2,

    /// <summary>
    /// 仅本人数据权限
    /// 只能看到自己创建的数据
    /// </summary>
    SelfOnly = 3,

    /// <summary>
    /// 自定义数据权限
    /// 通过自定义规则过滤数据
    /// </summary>
    Custom = 99
}
