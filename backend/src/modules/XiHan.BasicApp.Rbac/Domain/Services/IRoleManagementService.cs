#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleManagementService
// Guid:d6e7f8a9-bcde-f123-4567-890abcdef123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Domain.Services;

/// <summary>
/// 角色管理领域服务接口
/// </summary>
public interface IRoleManagementService : IDomainService
{
    /// <summary>
    /// 为用户分配角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AssignRolesToUserAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveRolesFromUserAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AssignPermissionsToRoleAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除角色的权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemovePermissionsFromRoleAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为角色分配菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AssignMenusToRoleAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除角色的菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveMenusFromRoleAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否拥有指定角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleCode">角色编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有角色</returns>
    Task<bool> UserHasRoleAsync(long userId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否拥有多个角色中的任意一个
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleCodes">角色编码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有任意角色</returns>
    Task<bool> UserHasAnyRoleAsync(long userId, IEnumerable<string> roleCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证角色是否可以被删除
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    Task<bool> CanDeleteRoleAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色编码是否重复
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="excludeRoleId">排除的角色ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重复</returns>
    Task<bool> IsRoleCodeDuplicateAsync(string roleCode, long? tenantId = null, long? excludeRoleId = null, CancellationToken cancellationToken = default);
}
