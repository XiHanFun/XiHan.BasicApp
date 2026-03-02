#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRelationRepository
// Guid:7e5b141c-9cbf-4e87-9bd9-6b949d62b543
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 14:26:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户关系仓储接口（用户-角色/权限/部门）
/// </summary>
public interface IUserRelationRepository
{
    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetUserRolesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户直授权限关系
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetUserPermissionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetUserDepartmentsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换用户角色关系
    /// </summary>
    Task ReplaceUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换用户直授权限关系
    /// </summary>
    Task ReplaceUserPermissionsAsync(long userId, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换用户部门关系
    /// </summary>
    Task ReplaceUserDepartmentsAsync(
        long userId,
        IReadOnlyCollection<long> departmentIds,
        long? mainDepartmentId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default);
}
