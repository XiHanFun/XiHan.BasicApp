#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRoleRepository
// Guid:b2c3d4e5-f6a7-8901-2345-67890abcdef1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统角色仓储接口
/// </summary>
public interface ISysRoleRepository : IAggregateRootRepository<SysRole, long>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeRoleId">排除的角色ID（用于更新时检查）</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsRoleCodeExistsAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的角色列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="isActive">是否启用（null 表示不过滤）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<List<SysRole>> GetRolesByTenantAsync(long tenantId, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<List<SysRole>> GetRolesByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存角色
    /// </summary>
    /// <param name="role">角色实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的角色实体</returns>
    Task<SysRole> SaveAsync(SysRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableRoleAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableRoleAsync(long roleId, CancellationToken cancellationToken = default);
}
