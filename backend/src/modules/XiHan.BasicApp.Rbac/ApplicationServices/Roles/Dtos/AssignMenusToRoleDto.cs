#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignMenusToRoleDto
// Guid:f4027aea-497f-4593-a10e-d650bbd551d2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.ApplicationServices.Roles.Dtos;

/// <summary>
/// 角色分配菜单DTO
/// </summary>
public class AssignMenusToRoleDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<long> MenuIds { get; set; } = [];
}
