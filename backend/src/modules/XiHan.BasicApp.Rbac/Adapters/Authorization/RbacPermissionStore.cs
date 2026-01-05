#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacPermissionStore
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/06 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Repositories.Permissions;
using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.BasicApp.Rbac.Repositories.UserPermissions;
using XiHan.Framework.Authorization.Permissions;

namespace XiHan.BasicApp.Rbac.Adapters.Authorization;

/// <summary>
/// RBAC 权限存储适配器
/// </summary>
public class RbacPermissionStore : IPermissionStore
{
    private readonly ISysPermissionRepository _permissionRepository;
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISysUserPermissionRepository _userPermissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacPermissionStore(
        ISysPermissionRepository permissionRepository,
        ISysRoleRepository roleRepository,
        ISysUserPermissionRepository userPermissionRepository)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userPermissionRepository = userPermissionRepository;
    }

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    public async Task<IEnumerable<PermissionDefinition>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            return [];
        }

        var permissions = await _permissionRepository.GetByUserIdAsync(userIdLong);

        return permissions.Select(p => new PermissionDefinition
        {
            Name = p.PermissionCode,
            DisplayName = p.PermissionName,
            Description = p.PermissionDescription,
            IsEnabled = p.Status == Enums.YesOrNo.Yes,
            Order = p.Sort
        });
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    public async Task<IEnumerable<PermissionDefinition>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(roleId, out var roleIdLong))
        {
            return [];
        }

        var permissions = await _permissionRepository.GetByRoleIdAsync(roleIdLong);

        return permissions.Select(p => new PermissionDefinition
        {
            Name = p.PermissionCode,
            DisplayName = p.PermissionName,
            Description = p.PermissionDescription,
            IsEnabled = p.Status == Enums.YesOrNo.Yes,
            Order = p.Sort
        });
    }

    /// <summary>
    /// 授予用户权限
    /// </summary>
    public async Task GrantPermissionToUserAsync(string userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            throw new ArgumentException("无效的用户ID", nameof(userId));
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName) ?? throw new InvalidOperationException($"权限不存在: {permissionName}");
        await _userPermissionRepository.AddUserPermissionAsync(userIdLong, permission.BasicId);
    }

    /// <summary>
    /// 撤销用户权限
    /// </summary>
    public async Task RevokePermissionFromUserAsync(string userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            throw new ArgumentException("无效的用户ID", nameof(userId));
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName);
        if (permission == null)
        {
            return; // 权限不存在，无需撤销
        }

        await _userPermissionRepository.RemoveUserPermissionAsync(userIdLong, permission.BasicId);
    }

    /// <summary>
    /// 授予角色权限
    /// </summary>
    public async Task GrantPermissionToRoleAsync(string roleId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(roleId, out var roleIdLong))
        {
            throw new ArgumentException("无效的角色ID", nameof(roleId));
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName) ?? throw new InvalidOperationException($"权限不存在: {permissionName}");
        await _roleRepository.AddRolePermissionAsync(roleIdLong, permission.BasicId);
    }

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    public async Task RevokePermissionFromRoleAsync(string roleId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(roleId, out var roleIdLong))
        {
            throw new ArgumentException("无效的角色ID", nameof(roleId));
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName);
        if (permission == null)
        {
            return; // 权限不存在，无需撤销
        }

        await _roleRepository.RemoveRolePermissionAsync(roleIdLong, permission.BasicId);
    }

    /// <summary>
    /// 获取所有权限定义
    /// </summary>
    public async Task<IEnumerable<PermissionDefinition>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);

        return permissions.Select(p => new PermissionDefinition
        {
            Name = p.PermissionCode,
            DisplayName = p.PermissionName,
            Description = p.PermissionDescription,
            IsEnabled = p.Status == Enums.YesOrNo.Yes,
            Order = p.Sort
        });
    }

    /// <summary>
    /// 根据名称获取权限定义
    /// </summary>
    public async Task<PermissionDefinition?> GetPermissionByNameAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName);
        if (permission == null)
        {
            return null;
        }

        return new PermissionDefinition
        {
            Name = permission.PermissionCode,
            DisplayName = permission.PermissionName,
            Description = permission.PermissionDescription,
            IsEnabled = permission.Status == Enums.YesOrNo.Yes,
            Order = permission.Sort
        };
    }
}
