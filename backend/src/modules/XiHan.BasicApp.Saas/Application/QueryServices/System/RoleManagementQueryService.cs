// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 角色管理页面查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色管理")]
public sealed class RoleManagementQueryService
    : SaasApplicationService, IRoleManagementQueryService
{
    private const int MaxGrantedUserCount = 100;

    private readonly IRoleRepository _roleRepository;

    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    private readonly IDepartmentRepository _departmentRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly IUserRepository _userRepository;

    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleManagementQueryService(
        IRoleRepository roleRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPermissionRepository permissionRepository,
        IRoleDataScopeRepository roleDataScopeRepository,
        IDepartmentRepository departmentRepository,
        IUserRoleRepository userRoleRepository,
        IUserRepository userRepository,
        ISuperAdminProtector superAdminProtector)
    {
        _roleRepository = roleRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _permissionRepository = permissionRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _departmentRepository = departmentRepository;
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _superAdminProtector = superAdminProtector;
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Role.Read)]
    public async Task<RoleManagementDetailDto?> GetRoleManagementDetailAsync(long roleId, CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 超管隐藏：非超管不得读取 super_admin 角色管理聚合（权限/授予用户等敏感数据），按 not-found 处理
        if (!_superAdminProtector.IsCurrentUserSuperAdmin()
            && await _superAdminProtector.IsProtectedRoleAsync(roleId, cancellationToken))
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        return new RoleManagementDetailDto
        {
            Role = RoleApplicationMapper.ToDetailDto(role),
            Ancestors = await GetHierarchiesAsync(role.BasicId, isAncestorQuery: true, cancellationToken),
            Descendants = await GetHierarchiesAsync(role.BasicId, isAncestorQuery: false, cancellationToken),
            Permissions = await GetPermissionsAsync(role.BasicId, cancellationToken),
            DataScopes = await GetDataScopesAsync(role.BasicId, cancellationToken),
            GrantedUsers = await GetGrantedUsersAsync(role.BasicId, now, cancellationToken),
            GeneratedTime = now
        };
    }

    private static RoleManagementGrantedUserDto ToGrantedUserDto(SysUserRole userRole, SysUser? user, DateTimeOffset now)
    {
        return new RoleManagementGrantedUserDto
        {
            BasicId = userRole.BasicId,
            UserId = userRole.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            Avatar = user?.Avatar,
            EffectiveTime = userRole.EffectiveTime,
            ExpirationTime = userRole.ExpirationTime,
            GrantReason = userRole.GrantReason,
            Status = userRole.Status,
            IsExpired = userRole.ExpirationTime.HasValue && userRole.ExpirationTime.Value <= now,
            Remark = userRole.Remark,
            CreatedTime = userRole.CreatedTime
        };
    }

    private async Task<List<RoleHierarchyListItemDto>> GetHierarchiesAsync(
            long roleId,
        bool isAncestorQuery,
        CancellationToken cancellationToken)
    {
        var hierarchies = isAncestorQuery
            ? await _roleHierarchyRepository.GetListAsync(
                item => item.DescendantId == roleId,
                item => item.Depth,
                cancellationToken)
            : await _roleHierarchyRepository.GetListAsync(
                item => item.AncestorId == roleId,
                item => item.Depth,
                cancellationToken);

        if (hierarchies.Count == 0)
        {
            return [];
        }

        var roleMap = await BuildRoleMapAsync(
            hierarchies.SelectMany(item => new[] { item.AncestorId, item.DescendantId }),
            cancellationToken);

        return [.. hierarchies
            .Select(item => RoleHierarchyApplicationMapper.ToListItemDto(
                item,
                roleMap.GetValueOrDefault(item.AncestorId),
                roleMap.GetValueOrDefault(item.DescendantId)))
            .OrderBy(item => item.Depth)
            .ThenBy(item => isAncestorQuery ? item.AncestorRoleCode : item.DescendantRoleCode)
            .ThenBy(item => isAncestorQuery ? item.AncestorId : item.DescendantId)];
    }

    private async Task<List<RolePermissionListItemDto>> GetPermissionsAsync(long roleId, CancellationToken cancellationToken)
    {
        var rolePermissions = await _rolePermissionRepository.GetListAsync(
            item => item.RoleId == roleId,
            item => item.CreatedTime,
            cancellationToken);

        if (rolePermissions.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(rolePermissions.Select(item => item.PermissionId), cancellationToken);

        return [.. rolePermissions
            .Select(item => RolePermissionApplicationMapper.ToListItemDto(
                item,
                permissionMap.GetValueOrDefault(item.PermissionId)))
            .OrderBy(item => item.PermissionCode)
            .ThenBy(item => item.PermissionId)];
    }

    private async Task<List<RoleDataScopeListItemDto>> GetDataScopesAsync(long roleId, CancellationToken cancellationToken)
    {
        var dataScopes = await _roleDataScopeRepository.GetListAsync(
            item => item.RoleId == roleId,
            item => item.CreatedTime,
            cancellationToken);

        if (dataScopes.Count == 0)
        {
            return [];
        }

        var departmentMap = await BuildDepartmentMapAsync(dataScopes.Select(item => item.DepartmentId), cancellationToken);

        return [.. dataScopes
            .Select(item => RoleDataScopeApplicationMapper.ToListItemDto(
                item,
                departmentMap.GetValueOrDefault(item.DepartmentId)))
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.DepartmentId)];
    }

    private async Task<List<RoleManagementGrantedUserDto>> GetGrantedUsersAsync(
        long roleId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.GetListAsync(
            item => item.RoleId == roleId,
            item => item.CreatedTime,
            cancellationToken);

        if (userRoles.Count == 0)
        {
            return [];
        }

        var userMap = await BuildUserMapAsync(userRoles.Select(item => item.UserId), cancellationToken);

        return [.. userRoles
            .Select(item => ToGrantedUserDto(item, userMap.GetValueOrDefault(item.UserId), now))
            .OrderBy(item => item.UserName)
            .ThenBy(item => item.UserId)
            .Take(MaxGrantedUserCount)];
    }

    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysDepartment>> BuildDepartmentMapAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken)
    {
        var ids = departmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysDepartment>();
        }

        var departments = await _departmentRepository.GetByIdsAsync(ids, cancellationToken);
        return departments.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysUser>> BuildUserMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUser>();
        }

        var users = await _userRepository.GetByIdsAsync(ids, cancellationToken);
        return users.ToDictionary(item => item.BasicId);
    }
}
