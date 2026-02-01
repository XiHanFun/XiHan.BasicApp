#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionType
// Guid:ad28152c-d6e9-4396-addb-b479254bad30
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 权限类型枚举
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// 菜单权限
    /// </summary>
    Menu = 0,

    /// <summary>
    /// 按钮权限
    /// </summary>
    Button = 1,

    /// <summary>
    /// API权限
    /// </summary>
    Api = 2,

    /// <summary>
    /// 数据权限
    /// </summary>
    Data = 3
}
