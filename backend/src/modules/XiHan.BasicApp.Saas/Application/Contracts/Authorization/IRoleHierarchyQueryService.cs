// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色继承查询应用服务接口
/// </summary>
public interface IRoleHierarchyQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色的全部上级（不含自身）
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上级链，按继承深度排列</returns>
    Task<IReadOnlyList<RoleInheritanceItemDto>> GetRoleAncestorsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的全部下级（不含自身）
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下级链，按继承深度排列</returns>
    Task<IReadOnlyList<RoleInheritanceItemDto>> GetRoleDescendantsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色从生效的上级继承来的权限绑定
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>继承来的权限绑定，同一权限来自多个上级时逐条列出</returns>
    Task<IReadOnlyList<RoleInheritedPermissionDto>> GetRoleInheritedPermissionsAsync(long roleId, CancellationToken cancellationToken = default);
}
