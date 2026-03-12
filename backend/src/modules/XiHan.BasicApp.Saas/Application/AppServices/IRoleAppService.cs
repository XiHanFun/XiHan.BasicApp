#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleAppService
// Guid:fb5f77dd-0653-4987-be28-f559db7f6ca2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:46:36
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 角色应用服务
/// </summary>
public interface IRoleAppService
    : ICrudApplicationService<SysRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<RoleDto?> GetByCodeAsync(RoleByCodeQuery query);

    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    Task<IReadOnlyList<RolePermissionRelationDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    Task<IReadOnlyList<RoleMenuRelationDto>> GetRoleMenusAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 获取角色自定义数据范围部门ID
    /// </summary>
    Task<IReadOnlyCollection<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 分配角色权限
    /// </summary>
    Task AssignPermissionsAsync(AssignRolePermissionsCommand command);

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    Task AssignMenusAsync(AssignRoleMenusCommand command);

    /// <summary>
    /// 分配角色自定义数据范围
    /// </summary>
    Task AssignDataScopeAsync(AssignRoleDataScopeCommand command);
}
