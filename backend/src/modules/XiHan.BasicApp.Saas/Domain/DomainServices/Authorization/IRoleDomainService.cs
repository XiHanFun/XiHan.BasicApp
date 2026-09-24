// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// 批量变更角色权限（批量撤销 + 批量授予，底层走 UpdateRange/AddRange 单次提交）
    /// </summary>
    /// <returns>本次实际发生变化的授予/撤销权限ID（供审计发事件）</returns>
    Task<RolePermissionBatchUpdateResult> BatchUpdateRolePermissionsAsync(RolePermissionBatchUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限
    /// </summary>
    Task<RolePermissionCommandResult> UpdateRolePermissionAsync(RolePermissionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
    Task<RolePermissionCommandResult> UpdateRolePermissionStatusAsync(RolePermissionStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置角色数据范围：档位与自定义部门一次落地
    /// </summary>
    /// <returns>档位是否改变、本次实际变化的部门</returns>
    Task<DataScopeSetResult> SetRoleDataScopeAsync(RoleDataScopeSetCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量变更角色的直接父角色（一次性提交新增与移除，先移除后新增）
    /// </summary>
    /// <returns>本次实际发生变化的直接父角色</returns>
    Task<RoleHierarchyBatchUpdateResult> BatchUpdateRoleParentsAsync(RoleHierarchyBatchUpdateCommand command, CancellationToken cancellationToken = default);
}
