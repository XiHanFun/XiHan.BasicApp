#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacRoleStore
// Guid:3f327f0d-95a8-4be0-bc49-746705530d87
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 17:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Globalization;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Roles;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Authorization;

/// <summary>
/// 基于 RBAC 数据库的角色存储实现
/// </summary>
public class RbacRoleStore : IRoleStore
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository">角色仓储</param>
    /// <param name="roleHierarchyRepository">角色层级仓储</param>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="currentTenant">当前租户</param>
    public RbacRoleStore(
        IRoleRepository roleRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IUserRepository userRepository,
        ISqlSugarDbContext dbContext,
        ICurrentTenant currentTenant)
    {
        _roleRepository = roleRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _userRepository = userRepository;
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<List<RoleDefinition>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(userId, out var parsedUserId))
        {
            return [];
        }

        var tenantId = _currentTenant.Id;
        var userRoles = await _userRepository.GetUserRolesAsync(parsedUserId, tenantId, cancellationToken);
        var roleIds = userRoles
            .Where(static mapping => mapping.Status == YesOrNo.Yes)
            .Select(static mapping => mapping.RoleId)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
        {
            return [];
        }

        var effectiveRoleIds = await _roleHierarchyRepository.GetInheritedRoleIdsAsync(
            roleIds,
            tenantId,
            cancellationToken);
        if (effectiveRoleIds.Count == 0)
        {
            return [];
        }

        var roles = await _roleRepository.GetByIdsAsync([.. effectiveRoleIds], cancellationToken);
        return roles
            .Where(role => role.Status == YesOrNo.Yes && IsTenantMatched(role.TenantId))
            .OrderBy(static role => role.Sort)
            .Select(MapToDefinition)
            .ToList();
    }

    /// <summary>
    /// 检查用户是否在角色中
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleName">角色名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在角色中</returns>
    public async Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        var roles = await GetUserRolesAsync(userId, cancellationToken);
        return roles.Any(role => string.Equals(role.Name, roleName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 将用户添加到角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleName">角色名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddUserToRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        var tenantId = _currentTenant.Id;
        var role = await _roleRepository.GetByRoleCodeAsync(roleName.Trim(), tenantId, cancellationToken)
            ?? throw new BusinessException(message: $"角色 '{roleName}' 不存在");

        var query = DbClient.Queryable<SysUserRole>()
            .Where(mapping => mapping.UserId == parsedUserId && mapping.RoleId == role.BasicId);
        query = ApplyTenantFilter(query, tenantId);

        var existing = await query.FirstAsync(cancellationToken);
        if (existing is null)
        {
            var mapping = new SysUserRole
            {
                TenantId = tenantId,
                UserId = parsedUserId,
                RoleId = role.BasicId,
                Status = YesOrNo.Yes
            };

            await DbClient.Insertable(mapping).ExecuteCommandAsync(cancellationToken);
            return;
        }

        if (existing.Status != YesOrNo.Yes)
        {
            existing.Status = YesOrNo.Yes;
            await DbClient.Updateable(existing).ExecuteCommandAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 从角色中移除用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleName">角色名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RemoveUserFromRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        var tenantId = _currentTenant.Id;
        var role = await _roleRepository.GetByRoleCodeAsync(roleName.Trim(), tenantId, cancellationToken);
        if (role is null)
        {
            return;
        }

        var deleteable = DbClient.Deleteable<SysUserRole>()
            .Where(mapping => mapping.UserId == parsedUserId && mapping.RoleId == role.BasicId);
        deleteable = ApplyTenantFilter(deleteable, tenantId);

        await deleteable.ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取全部角色
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<List<RoleDefinition>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.Id;
        IReadOnlyList<SysRole> roles = tenantId.HasValue
            ? await _roleRepository.GetListAsync(
                role => role.Status == YesOrNo.Yes && role.TenantId == tenantId.Value,
                cancellationToken)
            : await _roleRepository.GetListAsync(
                role => role.Status == YesOrNo.Yes && role.TenantId == null,
                cancellationToken);

        return roles
            .OrderBy(static role => role.Sort)
            .Select(MapToDefinition)
            .ToList();
    }

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色定义</returns>
    public async Task<RoleDefinition?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        var role = await _roleRepository.GetByRoleCodeAsync(roleName.Trim(), _currentTenant.Id, cancellationToken);
        if (role is null || role.Status != YesOrNo.Yes)
        {
            return null;
        }

        return MapToDefinition(role);
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色定义</returns>
    public async Task<RoleDefinition?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(roleId, out var parsedRoleId))
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(parsedRoleId, cancellationToken);
        if (role is null || role.Status != YesOrNo.Yes || !IsTenantMatched(role.TenantId))
        {
            return null;
        }

        return MapToDefinition(role);
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="role">角色定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CreateRoleAsync(RoleDefinition role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (string.IsNullOrWhiteSpace(role.Name))
        {
            throw new ArgumentException("角色名称不能为空", nameof(role));
        }

        var tenantId = _currentTenant.Id;
        var roleCode = role.Name.Trim();
        if (await _roleRepository.IsRoleCodeExistsAsync(roleCode, null, tenantId, cancellationToken))
        {
            throw new BusinessException(message: $"角色 '{roleCode}' 已存在");
        }

        var entity = new SysRole
        {
            TenantId = tenantId,
            RoleCode = roleCode,
            RoleName = string.IsNullOrWhiteSpace(role.DisplayName) ? roleCode : role.DisplayName.Trim(),
            RoleDescription = string.IsNullOrWhiteSpace(role.Description) ? null : role.Description.Trim(),
            RoleType = role.IsStatic ? RoleType.System : RoleType.Custom,
            Sort = role.Order,
            Status = role.IsEnabled ? YesOrNo.Yes : YesOrNo.No
        };

        await DbClient.Insertable(entity).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="role">角色定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateRoleAsync(RoleDefinition role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (!TryParseId(role.Id, out var parsedRoleId))
        {
            throw new ArgumentException("角色ID格式无效", nameof(role));
        }

        var entity = await _roleRepository.GetByIdAsync(parsedRoleId, cancellationToken)
            ?? throw new BusinessException(message: $"角色 '{role.Id}' 不存在");

        if (!IsTenantMatched(entity.TenantId))
        {
            throw new BusinessException(message: $"角色 '{role.Id}' 不在当前租户上下文中");
        }

        var roleCode = string.IsNullOrWhiteSpace(role.Name) ? entity.RoleCode : role.Name.Trim();
        if (!string.Equals(entity.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _roleRepository.IsRoleCodeExistsAsync(roleCode, entity.BasicId, _currentTenant.Id, cancellationToken);
            if (exists)
            {
                throw new BusinessException(message: $"角色 '{roleCode}' 已存在");
            }
        }

        entity.RoleCode = roleCode;
        entity.RoleName = string.IsNullOrWhiteSpace(role.DisplayName) ? roleCode : role.DisplayName.Trim();
        entity.RoleDescription = string.IsNullOrWhiteSpace(role.Description) ? null : role.Description.Trim();
        entity.RoleType = role.IsStatic ? RoleType.System : RoleType.Custom;
        entity.Sort = role.Order;
        entity.Status = role.IsEnabled ? YesOrNo.Yes : YesOrNo.No;

        await DbClient.Updateable(entity).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(roleId, out var parsedRoleId))
        {
            return;
        }

        var role = await _roleRepository.GetByIdAsync(parsedRoleId, cancellationToken);
        if (role is null || !IsTenantMatched(role.TenantId))
        {
            return;
        }

        role.IsDeleted = true;
        role.Status = YesOrNo.No;
        await DbClient.Updateable(role).ExecuteCommandAsync(cancellationToken);

        var tenantId = _currentTenant.Id;

        var userRoleDeleteable = DbClient.Deleteable<SysUserRole>().Where(mapping => mapping.RoleId == parsedRoleId);
        userRoleDeleteable = ApplyTenantFilter(userRoleDeleteable, tenantId);
        await userRoleDeleteable.ExecuteCommandAsync(cancellationToken);

        var rolePermissionDeleteable = DbClient.Deleteable<SysRolePermission>().Where(mapping => mapping.RoleId == parsedRoleId);
        rolePermissionDeleteable = ApplyTenantFilter(rolePermissionDeleteable, tenantId);
        await rolePermissionDeleteable.ExecuteCommandAsync(cancellationToken);

        var roleMenuDeleteable = DbClient.Deleteable<SysRoleMenu>().Where(mapping => mapping.RoleId == parsedRoleId);
        roleMenuDeleteable = ApplyTenantFilter(roleMenuDeleteable, tenantId);
        await roleMenuDeleteable.ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色中的用户ID列表
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户ID列表</returns>
    public async Task<List<string>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return [];
        }

        var role = await _roleRepository.GetByRoleCodeAsync(roleName.Trim(), _currentTenant.Id, cancellationToken);
        if (role is null)
        {
            return [];
        }

        var tenantId = _currentTenant.Id;
        var query = DbClient.Queryable<SysUserRole>()
            .Where(mapping => mapping.RoleId == role.BasicId && mapping.Status == YesOrNo.Yes);
        query = ApplyTenantFilter(query, tenantId);

        var userIds = await query
            .Select(static mapping => mapping.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return userIds
            .Select(id => id.ToString(CultureInfo.InvariantCulture))
            .ToList();
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

    private static RoleDefinition MapToDefinition(SysRole role)
    {
        return new RoleDefinition
        {
            Id = role.BasicId.ToString(CultureInfo.InvariantCulture),
            Name = role.RoleCode,
            DisplayName = role.RoleName,
            Description = role.RoleDescription,
            IsEnabled = role.Status == YesOrNo.Yes,
            IsStatic = role.RoleType == RoleType.System,
            Order = role.Sort
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
