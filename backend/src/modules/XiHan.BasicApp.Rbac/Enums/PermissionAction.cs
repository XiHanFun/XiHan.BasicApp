#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAction
// Guid:ad28152c-d6e9-4396-addb-b479254bad30
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 权限操作枚举
/// </summary>
public enum PermissionAction
{
    /// <summary>
    /// 授予权限
    /// </summary>
    Grant = 0,

    /// <summary>
    /// 禁用权限
    /// </summary>
    Deny = 1
}
