#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignPermissionsToRoleDto
// Guid:1742de52-20e7-4b4d-ac21-bc7fa0c51e08
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.ApplicationServices.Roles.Dtos;

/// <summary>
/// 角色分配权限DTO
/// </summary>
public class AssignPermissionsToRoleDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<long> PermissionIds { get; set; } = [];
}
