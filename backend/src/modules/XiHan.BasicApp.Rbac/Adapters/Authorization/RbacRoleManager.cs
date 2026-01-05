#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacRoleManager
// Guid:f6a7b8c9-d0e1-2345-6789-0abcdef01234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/06 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.Framework.Authorization.Roles;

namespace XiHan.BasicApp.Rbac.Adapters.Authorization;

/// <summary>
/// RBAC 角色管理器适配器
/// </summary>
public class RbacRoleManager : IRoleManager
{
    private readonly IRoleStore _roleStore;
    private readonly ISysRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacRoleManager(IRoleStore roleStore, ISysRoleRepository roleRepository)
    {
        _roleStore = roleStore;
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task<RoleOperationResult> CreateRoleAsync(RoleDefinition role, CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查角色是否已存在
            var existingRole = await _roleStore.GetRoleByNameAsync(role.Name, cancellationToken);
            if (existingRole != null)
            {
                return RoleOperationResult.Failure($"角色已存在: {role.Name}", "DuplicateRoleName");
            }

            await _roleStore.CreateRoleAsync(role, cancellationToken);
            return RoleOperationResult.Success(role);
        }
        catch (Exception ex)
        {
            return RoleOperationResult.Failure($"创建角色失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public async Task<RoleOperationResult> UpdateRoleAsync(RoleDefinition role, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingRole = await _roleStore.GetRoleByIdAsync(role.Id, cancellationToken);
            if (existingRole == null)
            {
                return RoleOperationResult.Failure($"角色不存在: {role.Id}", "RoleNotFound");
            }

            await _roleStore.UpdateRoleAsync(role, cancellationToken);
            return RoleOperationResult.Success(role);
        }
        catch (Exception ex)
        {
            return RoleOperationResult.Failure($"更新角色失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public async Task<RoleOperationResult> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleStore.GetRoleByIdAsync(roleId, cancellationToken);
            if (role == null)
            {
                return RoleOperationResult.Failure($"角色不存在: {roleId}", "RoleNotFound");
            }

            // 检查角色是否为静态角色
            if (role.IsStatic)
            {
                return RoleOperationResult.Failure("不能删除系统角色", "CannotDeleteStaticRole");
            }

            await _roleStore.DeleteRoleAsync(roleId, cancellationToken);
            return RoleOperationResult.Success();
        }
        catch (Exception ex)
        {
            return RoleOperationResult.Failure($"删除角色失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    public async Task<IEnumerable<RoleDefinition>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _roleStore.GetAllRolesAsync(cancellationToken);
    }

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    public async Task<RoleDefinition?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _roleStore.GetRoleByNameAsync(roleName, cancellationToken);
    }

    /// <summary>
    /// 检查角色是否存在
    /// </summary>
    public async Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleStore.GetRoleByNameAsync(roleName, cancellationToken);
        return role != null;
    }

    /// <summary>
    /// 将用户添加到角色
    /// </summary>
    public async Task<RoleOperationResult> AddUserToRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleStore.GetRoleByNameAsync(roleName, cancellationToken);
            if (role == null)
            {
                return RoleOperationResult.Failure($"角色不存在: {roleName}", "RoleNotFound");
            }

            var isInRole = await _roleStore.IsInRoleAsync(userId, roleName, cancellationToken);
            if (isInRole)
            {
                return RoleOperationResult.Failure($"用户已在角色中: {roleName}", "UserAlreadyInRole");
            }

            await _roleStore.AddUserToRoleAsync(userId, roleName, cancellationToken);
            return RoleOperationResult.Success();
        }
        catch (Exception ex)
        {
            return RoleOperationResult.Failure($"添加用户到角色失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从角色中移除用户
    /// </summary>
    public async Task<RoleOperationResult> RemoveUserFromRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var isInRole = await _roleStore.IsInRoleAsync(userId, roleName, cancellationToken);
            if (!isInRole)
            {
                return RoleOperationResult.Failure($"用户不在角色中: {roleName}", "UserNotInRole");
            }

            await _roleStore.RemoveUserFromRoleAsync(userId, roleName, cancellationToken);
            return RoleOperationResult.Success();
        }
        catch (Exception ex)
        {
            return RoleOperationResult.Failure($"从角色中移除用户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户的所有角色
    /// </summary>
    public async Task<IEnumerable<RoleDefinition>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _roleStore.GetUserRolesAsync(userId, cancellationToken);
    }

    /// <summary>
    /// 检查用户是否在指定角色中
    /// </summary>
    public async Task<bool> IsUserInRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        return await _roleStore.IsInRoleAsync(userId, roleName, cancellationToken);
    }
}
