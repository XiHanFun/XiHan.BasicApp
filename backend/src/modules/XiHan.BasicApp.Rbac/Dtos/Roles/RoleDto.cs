#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDto
// Guid:3a2b3c4d-5e6f-7890-abcd-ef1234567892
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Dtos.Roles;

/// <summary>
/// 角色 DTO
/// </summary>
public class RoleDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 角色详情 DTO
/// </summary>
public class RoleDetailDto : RoleDto
{
    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> MenuIds { get; set; } = [];

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> PermissionIds { get; set; } = [];

    /// <summary>
    /// 用户数量
    /// </summary>
    public int UserCount { get; set; }
}

/// <summary>
/// 创建角色 DTO
/// </summary>
public class CreateRoleDto : RbacCreationDtoBase
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; } = RoleType.System;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> MenuIds { get; set; } = [];

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> PermissionIds { get; set; } = [];

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新角色 DTO
/// </summary>
public class UpdateRoleDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType? RoleType { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 分配菜单 DTO
/// </summary>
public class AssignRoleMenusDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public XiHanBasicAppIdType RoleId { get; set; }

    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> MenuIds { get; set; } = [];
}

/// <summary>
/// 分配权限 DTO
/// </summary>
public class AssignRolePermissionsDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public XiHanBasicAppIdType RoleId { get; set; }

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> PermissionIds { get; set; } = [];
}
