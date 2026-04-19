#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacPermissionStore
// Guid:4cd2ea24-b8f4-41df-a6de-29d06c78aa96
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 17:18:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Globalization;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Authorization;

/// <summary>
/// 基于 RBAC 数据库的权限存储实现
/// </summary>
public class RbacPermissionStore : IPermissionStore
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="roleRepository">角色仓储</param>
    /// <param name="permissionRepository">权限仓储</param>
    /// <param name="clientResolver"></param>
    /// <param name="currentTenant">当前租户</param>
    public RbacPermissionStore(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    public async Task<List<PermissionDefinition>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(userId, out var parsedUserId))
        {
            return [];
        }

        var tenantId = _currentTenant.Id;
        var permissions = await _permissionRepository.GetUserPermissionsAsync(parsedUserId, tenantId, cancellationToken);
        return [.. permissions
            .Where(permission => permission.Status == YesOrNo.Yes && IsTenantMatched(permission.TenantId))
            .OrderBy(static permission => permission.Sort)
            .Select(MapToDefinition)];
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    public async Task<List<PermissionDefinition>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(roleId, out var parsedRoleId))
        {
            return [];
        }

        var tenantId = _currentTenant.Id;
        var permissions = await _permissionRepository.GetRolePermissionsAsync(parsedRoleId, tenantId, cancellationToken);
        return [.. permissions
            .Where(permission => permission.Status == YesOrNo.Yes && IsTenantMatched(permission.TenantId))
            .OrderBy(static permission => permission.Sort)
            .Select(MapToDefinition)];
    }

    /// <summary>
    /// 授予用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionName">权限名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task GrantPermissionToUserAsync(string userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(permissionName))
        {
            return;
        }

        var tenantId = _currentTenant.Id;
        var user = await _userRepository.GetByIdAsync(parsedUserId, cancellationToken);
        if (user is null || !IsTenantMatched(user.TenantId))
        {
            throw new BusinessException(message: $"用户 '{userId}' 不存在");
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName.Trim(), tenantId, cancellationToken)
            ?? throw new BusinessException(message: $"权限 '{permissionName}' 不存在");

        var query = DbClient.Queryable<SysUserPermission>()
            .Where(mapping => mapping.UserId == parsedUserId && mapping.PermissionId == permission.BasicId);
        query = ApplyTenantFilter(query, tenantId);
        var existing = await query.FirstAsync(cancellationToken);

        if (existing is null)
        {
            var mapping = new SysUserPermission
            {
                TenantId = tenantId ?? 0,
                UserId = parsedUserId,
                PermissionId = permission.BasicId,
                PermissionAction = PermissionAction.Grant,
                Status = YesOrNo.Yes
            };

            await DbClient.Insertable(mapping).ExecuteCommandAsync(cancellationToken);
            return;
        }

        existing.PermissionAction = PermissionAction.Grant;
        existing.Status = YesOrNo.Yes;
        existing.EffectiveTime = null;
        existing.ExpirationTime = null;
        await DbClient.Updateable(existing).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 撤销用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionName">权限名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RevokePermissionFromUserAsync(string userId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(permissionName))
        {
            return;
        }

        var tenantId = _currentTenant.Id;
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName.Trim(), tenantId, cancellationToken);
        if (permission is null)
        {
            return;
        }

        var deleteable = DbClient.Deleteable<SysUserPermission>()
            .Where(mapping => mapping.UserId == parsedUserId && mapping.PermissionId == permission.BasicId);
        deleteable = ApplyTenantFilter(deleteable, tenantId);

        await deleteable.ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 授予角色权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionName">权限名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task GrantPermissionToRoleAsync(string roleId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(roleId, out var parsedRoleId) || string.IsNullOrWhiteSpace(permissionName))
        {
            return;
        }

        var tenantId = _currentTenant.Id;
        var role = await _roleRepository.GetByIdAsync(parsedRoleId, cancellationToken);
        if (role is null || !IsTenantMatched(role.TenantId))
        {
            throw new BusinessException(message: $"角色 '{roleId}' 不存在");
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName.Trim(), tenantId, cancellationToken)
            ?? throw new BusinessException(message: $"权限 '{permissionName}' 不存在");

        var query = DbClient.Queryable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == parsedRoleId && mapping.PermissionId == permission.BasicId);
        query = ApplyTenantFilter(query, tenantId);
        var existing = await query.FirstAsync(cancellationToken);

        if (existing is null)
        {
            var mapping = new SysRolePermission
            {
                TenantId = tenantId ?? 0,
                RoleId = parsedRoleId,
                PermissionId = permission.BasicId,
                Status = YesOrNo.Yes
            };

            await DbClient.Insertable(mapping).ExecuteCommandAsync(cancellationToken);
            return;
        }

        existing.Status = YesOrNo.Yes;
        await DbClient.Updateable(existing).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionName">权限名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RevokePermissionFromRoleAsync(string roleId, string permissionName, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(roleId, out var parsedRoleId) || string.IsNullOrWhiteSpace(permissionName))
        {
            return;
        }

        var tenantId = _currentTenant.Id;
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName.Trim(), tenantId, cancellationToken);
        if (permission is null)
        {
            return;
        }

        var deleteable = DbClient.Deleteable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == parsedRoleId && mapping.PermissionId == permission.BasicId);
        deleteable = ApplyTenantFilter(deleteable, tenantId);

        await deleteable.ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有权限定义
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限定义列表</returns>
    public async Task<List<PermissionDefinition>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.Id;
        IReadOnlyList<SysPermission> permissions = tenantId.HasValue
            ? await _permissionRepository.GetListAsync(
                permission => permission.Status == YesOrNo.Yes && permission.TenantId == tenantId.Value,
                cancellationToken)
            : await _permissionRepository.GetListAsync(
                permission => permission.Status == YesOrNo.Yes && permission.TenantId == 0,
                cancellationToken);

        return [.. permissions
            .OrderBy(static permission => permission.Sort)
            .Select(MapToDefinition)];
    }

    /// <summary>
    /// 根据名称获取权限定义
    /// </summary>
    /// <param name="permissionName">权限名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限定义</returns>
    public async Task<PermissionDefinition?> GetPermissionByNameAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return null;
        }

        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionName.Trim(), _currentTenant.Id, cancellationToken);
        if (permission is null || permission.Status != YesOrNo.Yes || !IsTenantMatched(permission.TenantId))
        {
            return null;
        }

        return MapToDefinition(permission);
    }

    private bool IsTenantMatched(long? tenantId)
    {
        var currentTenantId = _currentTenant.Id;
        return currentTenantId.HasValue ? tenantId == currentTenantId : tenantId is null;
    }

    private static bool TryParseId(string? value, out long parsedId)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedId);
    }

    private static PermissionDefinition MapToDefinition(SysPermission permission)
    {
        return new PermissionDefinition
        {
            Name = permission.PermissionCode,
            DisplayName = permission.PermissionName,
            Description = permission.PermissionDescription,
            Tag = permission.Tags,
            IsEnabled = permission.Status == YesOrNo.Yes,
            Order = permission.Sort
        };
    }

    private static ISugarQueryable<T> ApplyTenantFilter<T>(ISugarQueryable<T> query, long? tenantId)
        where T : class, new()
    {
        return tenantId.HasValue
            ? query.Where("TenantId = @tenantId", new { tenantId })
            : query.Where("TenantId IS NULL");
    }

    private static IDeleteable<T> ApplyTenantFilter<T>(IDeleteable<T> deleteable, long? tenantId)
        where T : class, new()
    {
        return tenantId.HasValue
            ? deleteable.Where("TenantId = @tenantId", new { tenantId })
            : deleteable.Where("TenantId IS NULL");
    }
}
