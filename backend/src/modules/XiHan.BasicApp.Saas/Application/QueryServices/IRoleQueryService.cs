#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleQueryService
// Guid:b8e4f2a1-3c6d-4e5f-9a1b-2c3d4e5f6a71
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 角色查询服务接口
/// </summary>
public interface IRoleQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取角色
    /// </summary>
    Task<RoleDto?> GetByIdAsync(long id);

    /// <summary>
    /// 根据编码获取角色。
    /// </summary>
    Task<RoleDto?> GetByCodeAsync(string roleCode, long? tenantId = null);

    /// <summary>
    /// 获取角色权限关系。
    /// </summary>
    Task<IReadOnlyList<RolePermissionRelationDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 获取角色自定义数据范围部门ID。
    /// </summary>
    Task<IReadOnlyCollection<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 获取角色直接父角色ID。
    /// </summary>
    Task<IReadOnlyCollection<long>> GetRoleParentRoleIdsAsync(long roleId, long? tenantId = null);
}
