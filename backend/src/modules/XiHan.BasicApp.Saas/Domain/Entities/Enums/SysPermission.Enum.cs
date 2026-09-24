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

/// <summary>
/// 权限作用侧（在哪个上下文生效）
/// </summary>
/// <remarks>
/// 平台就是 0 号租户：平台上下文只生效平台侧权限，业务租户上下文只生效租户侧权限，两侧在两边都生效、各管各的数据。
/// 生效权限 = 已分配权限 ∩ 当前上下文允许的作用侧 ∩ 套餐白名单（仅租户上下文）；套餐白名单只能含租户侧与两侧。
/// </remarks>
[Flags]
public enum PermissionSide
{
    /// <summary>
    /// 平台侧：只在平台上下文生效（租户、版本、权限目录维护、运维、跨租户操作等）
    /// </summary>
    [Description("平台")]
    Platform = 1,

    /// <summary>
    /// 租户侧：只在业务租户上下文生效（组织、成员、数据范围、审批与工作流等）
    /// </summary>
    [Description("租户")]
    Tenant = 2,

    /// <summary>
    /// 两侧：两边都生效，各管各的数据（用户、角色、授权、日志、配置、文件等）
    /// </summary>
    [Description("两侧")]
    Both = Platform | Tenant
}
