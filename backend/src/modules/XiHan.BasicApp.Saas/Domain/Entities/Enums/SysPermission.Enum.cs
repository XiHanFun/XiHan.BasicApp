#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermission.Enum
// Guid:1a7a13e0-8f5c-49e2-8290-72d7a79fa9f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限类型枚举
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// 资源操作权限（绑定 ResourceId + OperationId）
    /// </summary>
    [Description("资源操作")]
    ResourceBased = 0,

    /// <summary>
    /// 功能权限（不绑定资源/操作，仅通过 PermissionCode 标识）
    /// </summary>
    [Description("功能")]
    Functional = 1,

    /// <summary>
    /// 数据范围权限
    /// </summary>
    [Description("数据范围")]
    DataScope = 2
}
