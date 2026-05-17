#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationSnapshotQueryService
// Guid:c6bf376e-36ed-409d-b6d9-3b6d45211c41
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照查询服务实现
/// </summary>
public sealed class AuthorizationSnapshotQueryService
    : IAuthorizationSnapshotQueryService
{
    private const string SuperAdminRoleCode = "super_admin";

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IUserPermissionRepository _userPermissionRepository;

    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationSnapshotQueryService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
    }

    /// <inheritdoc />
    public async Task<AuthorizationSnapshot> BuildAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userRoles = await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken);
        var roles = await _roleRepository.GetEnabledByIdsAsync(userRoles.Select(item => item.RoleId), cancellationToken);
        var roleCodes = roles
            .Select(role => role.RoleCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var isSuperAdmin = roleCodes.Contains(SuperAdminRoleCode, StringComparer.OrdinalIgnoreCase);

        if (isSuperAdmin)
        {
            var allPermissions = await _permissionRepository.GetListAsync(permission => permission.Status == EnableStatus.Enabled, cancellationToken);
            var permissionIds = allPermissions.Select(permission => permission.BasicId).ToHashSet();
            var superAdminPermissionCodes = allPermissions
                .Select(permission => permission.PermissionCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();
            superAdminPermissionCodes.Insert(0, "*");
            return new AuthorizationSnapshot(roleCodes, superAdminPermissionCodes, permissionIds);
        }

        var rolePermissions = await _rolePermissionRepository.GetValidByRoleIdsAsync(roles.Select(role => role.BasicId), now, cancellationToken);
        var roleGrantIds = rolePermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Grant)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        var roleDenyIds = rolePermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Deny)
            .Select(permission => permission.PermissionId)
            .ToHashSet();

        roleGrantIds.ExceptWith(roleDenyIds);

        var userPermissions = await _userPermissionRepository.GetValidByUserIdAsync(userId, now, cancellationToken);
        var userGrantIds = userPermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Grant)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        var userDenyIds = userPermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Deny)
            .Select(permission => permission.PermissionId)
            .ToHashSet();

        var finalPermissionIds = roleGrantIds;
        finalPermissionIds.UnionWith(userGrantIds);
        finalPermissionIds.ExceptWith(userDenyIds);

        var permissions = await _permissionRepository.GetByIdsAsync(finalPermissionIds, cancellationToken);
        var permissionCodes = permissions
            .Where(permission => permission.Status == EnableStatus.Enabled)
            .Select(permission => permission.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthorizationSnapshot(roleCodes, permissionCodes, finalPermissionIds);
    }
}
