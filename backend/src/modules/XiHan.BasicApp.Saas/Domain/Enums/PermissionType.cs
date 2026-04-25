#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionType
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 权限类型
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// 资源操作权限（绑定 ResourceId + OperationId）
    /// </summary>
    [Description("资源操作权限")]
    ResourceBased = 0,

    /// <summary>
    /// 功能权限（不绑定资源/操作，仅通过 PermissionCode 标识）
    /// </summary>
    [Description("功能权限")]
    Functional = 1,

    /// <summary>
    /// 数据范围权限
    /// </summary>
    [Description("数据范围权限")]
    DataScope = 2
}
