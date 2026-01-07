#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDomainService
// Guid:c3d4e5f6-a7b8-4c5d-9e0f-2a3b4c5d6e7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 权限领域服务
/// 处理权限相关的业务逻辑（权限计算、权限继承等）
/// </summary>
public class PermissionDomainService : DomainService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionDomainService(
        IPermissionRepository permissionRepository,
        IResourceRepository resourceRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _permissionRepository = permissionRepository;
        _resourceRepository = resourceRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// 获取用户的所有权限（包括角色权限和直接权限）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    public async Task<List<SysPermission>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetUserPermissionsAsync), new { userId });

        var allPermissions = new HashSet<SysPermission>();

        // 1. 获取用户直接权限
        var directPermissions = await _permissionRepository.GetByUserIdAsync(userId, cancellationToken);
        foreach (var permission in directPermissions)
        {
            allPermissions.Add(permission);
        }

        // 2. 获取用户角色权限
        var user = await _userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user != null)
        {
            // 通过用户角色关系获取角色权限
            var rolePermissions = await _permissionRepository.GetByUserIdAsync(userId, cancellationToken);
            foreach (var permission in rolePermissions)
            {
                allPermissions.Add(permission);
            }
        }

        Logger.LogInformation("用户 {UserId} 共有权限: {PermissionCount}", userId, allPermissions.Count);
        return allPermissions.ToList();
    }

    /// <summary>
    /// 获取用户的权限编码列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限编码列表</returns>
    public async Task<List<string>> GetUserPermissionCodesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Select(p => p.PermissionCode).Distinct().ToList();
    }

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有权限</returns>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissionCodes = await GetUserPermissionCodesAsync(userId, cancellationToken);
        return permissionCodes.Contains(permissionCode);
    }

    /// <summary>
    /// 检查用户是否拥有任一权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有任一权限</returns>
    public async Task<bool> HasAnyPermissionAsync(long userId, List<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var userPermissionCodes = await GetUserPermissionCodesAsync(userId, cancellationToken);
        return permissionCodes.Any(p => userPermissionCodes.Contains(p));
    }

    /// <summary>
    /// 检查用户是否拥有所有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有所有权限</returns>
    public async Task<bool> HasAllPermissionsAsync(long userId, List<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var userPermissionCodes = await GetUserPermissionCodesAsync(userId, cancellationToken);
        return permissionCodes.All(p => userPermissionCodes.Contains(p));
    }

    /// <summary>
    /// 检查权限编码唯一性
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="excludePermissionId">排除的权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsPermissionCodeUniqueAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _permissionRepository.ExistsByPermissionCodeAsync(permissionCode, excludePermissionId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 获取资源的所有权限
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    public async Task<List<SysPermission>> GetResourcePermissionsAsync(long resourceId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetResourcePermissionsAsync), new { resourceId });

        var permissions = await _permissionRepository.GetByResourceIdAsync(resourceId, cancellationToken);

        Logger.LogInformation("资源 {ResourceId} 共有权限: {PermissionCount}", resourceId, permissions.Count);
        return permissions;
    }

    /// <summary>
    /// 检查权限编码格式是否合法
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <returns>是否合法</returns>
    /// <remarks>
    /// 业务规则：权限编码格式为 Module.Action，如 User.Create
    /// </remarks>
    public bool IsValidPermissionCode(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var parts = permissionCode.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        return parts.All(p => !string.IsNullOrWhiteSpace(p) &&
                             System.Text.RegularExpressions.Regex.IsMatch(p, @"^[a-zA-Z0-9_]+$"));
    }

    /// <summary>
    /// 检查权限是否可以删除
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanDeleteAsync), new { permissionId });

        // 检查权限是否存在
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
        if (permission == null)
        {
            throw new KeyNotFoundException($"权限 {permissionId} 不存在");
        }

        // 检查权限是否已被删除
        if (permission.IsDeleted)
        {
            throw new InvalidOperationException("权限已被删除");
        }

        // TODO: 可以添加更多业务规则检查
        // - 检查权限是否被角色使用
        // - 检查权限是否被用户直接使用
        // - 检查是否为系统保留权限

        Logger.LogInformation("权限 {PermissionId} 可以删除", permissionId);
        return true;
    }
}
