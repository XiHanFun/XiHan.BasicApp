// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的错误码：<c>ACCESS_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class AccessErrors
{
    /// <summary>
    /// 角色不存在（或不在当前主体的作用侧）。
    /// </summary>
    public const string RoleNotFound = "ACCESS_NOTFOUND_001";

    /// <summary>
    /// 用户不存在或不在角色所属的租户。
    /// </summary>
    public const string MemberNotFound = "ACCESS_NOTFOUND_002";

    /// <summary>
    /// 角色码在该作用侧已被占用。
    /// </summary>
    public const string RoleCodeTaken = "ACCESS_CONFLICT_001";

    /// <summary>
    /// 内建角色不能删除。
    /// </summary>
    public const string BuiltInRole = "ACCESS_CONFLICT_002";

    /// <summary>
    /// 权限码不在权限目录里。
    /// </summary>
    public const string PermissionUnknown = "ACCESS_VALIDATION_001";

    /// <summary>
    /// 权限的作用侧与角色所在侧不符（平台角色只能授平台 / 两侧权限，租户角色只能授租户 / 两侧权限）。
    /// </summary>
    public const string PermissionScopeMismatch = "ACCESS_VALIDATION_002";

    /// <summary>
    /// 列表的筛选或排序字段不受支持。
    /// </summary>
    public const string QueryUnsupported = "ACCESS_VALIDATION_003";

    /// <summary>
    /// 平台角色的数据范围只能是全部、不能带自定义部门。
    /// </summary>
    public const string DataScopeNotApplicable = "ACCESS_VALIDATION_004";

    /// <summary>
    /// 自定义数据范围至少要有一个部门。
    /// </summary>
    public const string CustomScopeEmpty = "ACCESS_VALIDATION_005";

    /// <summary>
    /// 自定义数据范围里的部门不在当前租户。
    /// </summary>
    public const string DepartmentNotFound = "ACCESS_NOTFOUND_003";

    /// <summary>
    /// 租户主体要看平台侧的权限目录。
    /// </summary>
    public const string PlatformSideDenied = "ACCESS_DENIED_001";
}
