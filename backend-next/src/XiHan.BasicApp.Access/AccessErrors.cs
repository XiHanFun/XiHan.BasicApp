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

    /// <summary>
    /// 菜单只能在平台侧管理。
    /// </summary>
    public const string MenuPlatformOnly = "ACCESS_DENIED_002";

    /// <summary>
    /// 菜单节点不存在。
    /// </summary>
    public const string MenuNotFound = "ACCESS_NOTFOUND_004";

    /// <summary>
    /// 菜单编码已被占用。
    /// </summary>
    public const string MenuCodeTaken = "ACCESS_CONFLICT_003";

    /// <summary>
    /// 页面菜单由页面登记生成，不能删除。
    /// </summary>
    public const string MenuBuiltIn = "ACCESS_CONFLICT_004";

    /// <summary>
    /// 目录下还有节点，不能删除。
    /// </summary>
    public const string MenuHasChildren = "ACCESS_CONFLICT_005";

    /// <summary>
    /// 上级节点不合规。
    /// </summary>
    public const string MenuParentInvalid = "ACCESS_VALIDATION_006";

    /// <summary>
    /// 外链地址不合规（外链须是 http / https 绝对地址，目录不能有）。
    /// </summary>
    public const string MenuUrlInvalid = "ACCESS_VALIDATION_007";

    /// <summary>
    /// 页面菜单的来源字段（新建、路由、组件、权限、外链）归页面登记。
    /// </summary>
    public const string MenuPageManaged = "ACCESS_VALIDATION_008";

    /// <summary>
    /// 目录与外链必须有标题。
    /// </summary>
    public const string MenuTitleRequired = "ACCESS_VALIDATION_009";

    /// <summary>
    /// 只有页面菜单能恢复默认。
    /// </summary>
    public const string MenuNotPage = "ACCESS_VALIDATION_010";
}
