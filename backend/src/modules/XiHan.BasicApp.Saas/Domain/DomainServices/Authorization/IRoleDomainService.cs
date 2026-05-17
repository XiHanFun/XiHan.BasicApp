#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleDomainService
// Guid:72202bf7-6b3e-4ac5-862f-677a8a5afd16
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 角色领域服务
/// </summary>
public interface IRoleDomainService
{
    /// <summary>
    /// 创建角色
    /// </summary>
    Task<RoleCommandResult> CreateRoleAsync(RoleCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<RoleCommandResult> UpdateRoleAsync(RoleUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色状态
    /// </summary>
    Task<RoleCommandResult> UpdateRoleStatusAsync(RoleStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予角色权限
    /// </summary>
    Task<RolePermissionCommandResult> CreateRolePermissionAsync(RolePermissionGrantCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限
    /// </summary>
    Task<RolePermissionCommandResult> UpdateRolePermissionAsync(RolePermissionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
    Task<RolePermissionCommandResult> UpdateRolePermissionStatusAsync(RolePermissionStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予角色数据范围
    /// </summary>
    Task<RoleDataScopeCommandResult> CreateRoleDataScopeAsync(RoleDataScopeGrantCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色数据范围
    /// </summary>
    Task<RoleDataScopeCommandResult> UpdateRoleDataScopeAsync(RoleDataScopeUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色数据范围状态
    /// </summary>
    Task<RoleDataScopeCommandResult> UpdateRoleDataScopeStatusAsync(RoleDataScopeStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销角色数据范围
    /// </summary>
    Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建角色直接继承关系
    /// </summary>
    Task<RoleHierarchyCommandResult> CreateRoleHierarchyAsync(RoleHierarchyCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default);
}
