#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleRepository
// Guid:b2c3d4e5-f6a7-4b5c-8d9e-1f2a3b4c5d6e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository : IAggregateRootRepository<SysRole, long>
{
    /// <summary>
    /// 根据角色编码查询角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    Task<SysRole?> GetByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeRoleId">排除的角色ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeRoleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色及其权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体（包含权限）</returns>
    Task<SysRole?> GetWithPermissionsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色及其菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体（包含菜单）</returns>
    Task<SysRole?> GetWithMenusAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的所有用户
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<List<SysUser>> GetUsersByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的祖先角色（被继承的角色）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>祖先角色列表</returns>
    Task<List<SysRole>> GetAncestorRolesAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的后代角色（继承此角色的角色）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>后代角色列表</returns>
    Task<List<SysRole>> GetDescendantRolesAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID获取角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<List<SysRole>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设置父角色是否会形成循环继承
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否会形成循环</returns>
    Task<bool> WouldCreateCycleAsync(long roleId, long parentRoleId, CancellationToken cancellationToken = default);
}
