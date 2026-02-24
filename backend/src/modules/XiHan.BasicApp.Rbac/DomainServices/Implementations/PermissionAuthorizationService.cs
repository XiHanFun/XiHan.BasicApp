#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAuthorizationService
// Guid:b2fd6f17-5e0d-430a-9a27-cab6b7bdd14d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.DomainServices;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.DomainServices.Implementations;

/// <summary>
/// 权限授权领域服务实现
/// </summary>
public class PermissionAuthorizationService : DomainService, IPermissionAuthorizationService
{
    private readonly ISysPermissionRepository _permissionRepository;
    private readonly ISysRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionAuthorizationService(ISysPermissionRepository permissionRepository, ISysRoleRepository roleRepository)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId, cancellationToken);
        return permissions.Any(p => p.PermissionCode == permissionCode && p.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 检查用户是否拥有多个权限中的任意一个
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(long userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId, cancellationToken);
        var userPermissionCodes = permissions.Where(p => p.Status == YesOrNo.Yes).Select(p => p.PermissionCode).ToHashSet();
        return permissionCodes.Any(code => userPermissionCodes.Contains(code));
    }

    /// <summary>
    /// 检查用户是否拥有所有指定权限
    /// </summary>
    public async Task<bool> HasAllPermissionsAsync(long userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId, cancellationToken);
        var userPermissionCodes = permissions.Where(p => p.Status == YesOrNo.Yes).Select(p => p.PermissionCode).ToHashSet();
        return permissionCodes.All(code => userPermissionCodes.Contains(code));
    }

    /// <summary>
    /// 检查用户是否可以访问指定资源
    /// </summary>
    public async Task<bool> CanAccessResourceAsync(long userId, string resourceCode, string operationCode, CancellationToken cancellationToken = default)
    {
        var permissionCode = $"{resourceCode}:{operationCode}";
        return await HasPermissionAsync(userId, permissionCode, cancellationToken);
    }

    /// <summary>
    /// 检查角色是否拥有指定权限
    /// </summary>
    public async Task<bool> RoleHasPermissionAsync(long roleId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId, cancellationToken);
        return permissions.Any(p => p.PermissionCode == permissionCode && p.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 获取用户的所有权限编码
    /// </summary>
    public async Task<List<string>> GetUserPermissionCodesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId, cancellationToken);
        return permissions.Where(p => p.Status == YesOrNo.Yes).Select(p => p.PermissionCode).Distinct().ToList();
    }

    /// <summary>
    /// 检查权限是否处于活跃状态
    /// </summary>
    public async Task<bool> IsPermissionActiveAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionCode, cancellationToken);
        return permission != null && permission.Status == YesOrNo.Yes;
    }
}
