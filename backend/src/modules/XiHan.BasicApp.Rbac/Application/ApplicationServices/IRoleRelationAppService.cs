#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleRelationAppService
// Guid:6e31c13d-e899-442d-8f3c-fadf884f6d11
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 角色关系应用服务
/// </summary>
public interface IRoleRelationAppService : IApplicationService
{
    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    Task<IReadOnlyList<RolePermissionRelationDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    Task<IReadOnlyList<RoleMenuRelationDto>> GetRoleMenusAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 分配角色权限
    /// </summary>
    Task AssignPermissionsAsync(AssignRolePermissionsCommand command);

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    Task AssignMenusAsync(AssignRoleMenusCommand command);
}
