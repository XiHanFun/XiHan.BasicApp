#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleManager
// Guid:7392c3e6-e5f7-4e94-8dcd-f9425c3f0513
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:59:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

/// <summary>
/// 角色领域管理器实现
/// </summary>
public class RoleManager : IRoleManager
{
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository">角色仓储</param>
    public RoleManager(
        IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// 校验角色编码唯一性
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeRoleId">排除的角色ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    /// <exception cref="InvalidOperationException">角色编码已存在</exception>
    public async Task EnsureRoleCodeUniqueAsync(
        string roleCode,
        long? excludeRoleId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var exists = await _roleRepository.IsRoleCodeExistsAsync(roleCode, excludeRoleId, tenantId, cancellationToken);
        if (exists)
        {
            throw new BusinessException(message: $"角色编码 '{roleCode}' 已存在");
        }
    }

    /// <summary>
    /// 分配角色权限
    /// </summary>
    /// <param name="role">角色</param>
    /// <param name="permissionIds">权限ID集合</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    /// <exception cref="ArgumentNullException">角色为空</exception>
    public async Task AssignPermissionsAsync(
        SysRole role,
        IReadOnlyCollection<long> permissionIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        await _roleRepository.ReplaceRolePermissionsAsync(
            role.BasicId,
            permissionIds,
            tenantId ?? role.TenantId,
            cancellationToken);

        role.MarkPermissionsChanged(permissionIds.Distinct().ToArray());
        await _roleRepository.UpdateAsync(role, cancellationToken);
    }

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID集合</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    /// <exception cref="ArgumentNullException">角色为空</exception>
    public async Task AssignMenusAsync(
        long roleId,
        IReadOnlyCollection<long> menuIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await _roleRepository.ReplaceRoleMenusAsync(
            roleId,
            menuIds,
            tenantId,
            cancellationToken);
    }
}
