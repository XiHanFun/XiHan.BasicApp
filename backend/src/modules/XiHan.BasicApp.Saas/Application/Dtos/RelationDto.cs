#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RelationDto
// Guid:7e01b473-fe7c-4b8e-bca5-f29cff3cf257
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户角色关系 DTO
/// </summary>
public class UserRoleRelationDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 用户权限关系 DTO
/// </summary>
public class UserPermissionRelationDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 权限操作
    /// </summary>
    public PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 用户部门关系 DTO
/// </summary>
public class UserDepartmentRelationDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 是否主部门
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 角色权限关系 DTO
/// </summary>
public class RolePermissionRelationDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 角色菜单关系 DTO
/// </summary>
public class RoleMenuRelationDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
