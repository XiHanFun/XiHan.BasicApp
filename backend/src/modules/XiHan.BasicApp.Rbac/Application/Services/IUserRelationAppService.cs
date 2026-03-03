#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRelationAppService
// Guid:c8388127-13a6-4c63-ae26-5f11c48ca8a7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.UseCases.Commands;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.Services;

/// <summary>
/// 用户关系应用服务
/// </summary>
public interface IUserRelationAppService : IApplicationService
{
    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    Task<IReadOnlyList<UserRoleRelationDto>> GetUserRolesAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 获取用户权限关系
    /// </summary>
    Task<IReadOnlyList<UserPermissionRelationDto>> GetUserPermissionsAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    Task<IReadOnlyList<UserDepartmentRelationDto>> GetUserDepartmentsAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 分配用户角色
    /// </summary>
    Task AssignRolesAsync(AssignUserRolesCommand command);

    /// <summary>
    /// 分配用户权限
    /// </summary>
    Task AssignPermissionsAsync(AssignUserPermissionsCommand command);

    /// <summary>
    /// 分配用户部门
    /// </summary>
    Task AssignDepartmentsAsync(AssignUserDepartmentsCommand command);
}
