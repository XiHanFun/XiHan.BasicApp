#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacRoleStore
// Guid:b2c3d4e5-f6a7-8901-2345-6789 0abcdef0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/06 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.Framework.Authorization.Roles;

namespace XiHan.BasicApp.Rbac.Adapters.Authorization;

/// <summary>
/// RBAC 角色存储适配器
/// </summary>
public class RbacRoleStore : IRoleStore
{
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISysUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacRoleStore(
        ISysRoleRepository roleRepository,
        ISysUserRepository userRepository)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    public async Task<IEnumerable<RoleDefinition>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            return [];
        }

        var roles = await _roleRepository.GetByUserIdAsync(userIdLong);

        return roles.Select(r => new RoleDefinition
        {
            Id = r.BasicId.ToString(),
            Name = r.RoleCode,
            DisplayName = r.RoleName,
            Description = r.RoleDescription,
            IsEnabled = r.Status == Enums.YesOrNo.Yes,
            Order = r.Sort,
            CreatedTime = r.CreatedTime.DateTime,
            LastModifiedTime = r.ModifiedTime?.DateTime
        });
    }

    /// <summary>
    /// 检查用户是否在指定角色中
    /// </summary>
    public async Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            return false;
        }

        var role = await _roleRepository.GetByRoleCodeAsync(roleName);
        if (role == null)
        {
            return false;
        }

        var userRoleIds = await _userRepository.GetUserRoleIdsAsync(userIdLong);
        return userRoleIds.Contains(role.BasicId);
    }

    /// <summary>
    /// 将用户添加到角色
    /// </summary>
    public async Task AddUserToRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            throw new ArgumentException("无效的用户ID", nameof(userId));
        }

        var role = await _roleRepository.GetByRoleCodeAsync(roleName) ?? throw new InvalidOperationException($"角色不存在: {roleName}");
        await _roleRepository.AddUserRoleAsync(userIdLong, role.BasicId);
    }

    /// <summary>
    /// 从角色中移除用户
    /// </summary>
    public async Task RemoveUserFromRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            throw new ArgumentException("无效的用户ID", nameof(userId));
        }

        var role = await _roleRepository.GetByRoleCodeAsync(roleName);
        if (role == null)
        {
            return; // 角色不存在，无需移除
        }

        await _roleRepository.RemoveUserRoleAsync(userIdLong, role.BasicId);
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    public async Task<IEnumerable<RoleDefinition>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);

        return roles.Select(r => new RoleDefinition
        {
            Id = r.BasicId.ToString(),
            Name = r.RoleCode,
            DisplayName = r.RoleName,
            Description = r.RoleDescription,
            IsEnabled = r.Status == Enums.YesOrNo.Yes,
            Order = r.Sort,
            CreatedTime = r.CreatedTime.DateTime,
            LastModifiedTime = r.ModifiedTime?.DateTime
        });
    }

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    public async Task<RoleDefinition?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByRoleCodeAsync(roleName);
        if (role == null)
        {
            return null;
        }

        return new RoleDefinition
        {
            Id = role.BasicId.ToString(),
            Name = role.RoleCode,
            DisplayName = role.RoleName,
            Description = role.RoleDescription,
            IsEnabled = role.Status == Enums.YesOrNo.Yes,
            Order = role.Sort,
            CreatedTime = role.CreatedTime.DateTime,
            LastModifiedTime = role.ModifiedTime?.DateTime
        };
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    public async Task<RoleDefinition?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(roleId, out var roleIdLong))
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(roleIdLong, cancellationToken);
        if (role == null)
        {
            return null;
        }

        return new RoleDefinition
        {
            Id = role.BasicId.ToString(),
            Name = role.RoleCode,
            DisplayName = role.RoleName,
            Description = role.RoleDescription,
            IsEnabled = role.Status == Enums.YesOrNo.Yes,
            Order = role.Sort,
            CreatedTime = role.CreatedTime.DateTime,
            LastModifiedTime = role.ModifiedTime?.DateTime
        };
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task CreateRoleAsync(RoleDefinition role, CancellationToken cancellationToken = default)
    {
        var entity = new SysRole
        {
            RoleCode = role.Name,
            RoleName = role.DisplayName,
            RoleDescription = role.Description,
            Status = role.IsEnabled ? Enums.YesOrNo.Yes : Enums.YesOrNo.No,
            Sort = role.Order
        };

        await _roleRepository.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public async Task UpdateRoleAsync(RoleDefinition role, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(role.Id, out var roleIdLong))
        {
            throw new ArgumentException("无效的角色ID", nameof(role.Id));
        }

        var entity = await _roleRepository.GetByIdAsync(roleIdLong, cancellationToken) ?? throw new InvalidOperationException($"角色不存在: {role.Id}");
        entity.RoleCode = role.Name;
        entity.RoleName = role.DisplayName;
        entity.RoleDescription = role.Description;
        entity.Status = role.IsEnabled ? Enums.YesOrNo.Yes : Enums.YesOrNo.No;
        entity.Sort = role.Order;

        await _roleRepository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public async Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(roleId, out var roleIdLong))
        {
            throw new ArgumentException("无效的角色ID", nameof(roleId));
        }

        await _roleRepository.DeleteByIdAsync(roleIdLong, cancellationToken);
    }

    /// <summary>
    /// 获取角色中的用户ID列表
    /// </summary>
    public async Task<IEnumerable<string>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByRoleCodeAsync(roleName);
        if (role == null)
        {
            return [];
        }

        var userIds = await _roleRepository.GetUsersInRoleAsync(role.BasicId);
        return userIds.Select(id => id.ToString());
    }
}
