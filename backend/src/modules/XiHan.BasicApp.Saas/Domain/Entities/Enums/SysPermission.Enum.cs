// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
