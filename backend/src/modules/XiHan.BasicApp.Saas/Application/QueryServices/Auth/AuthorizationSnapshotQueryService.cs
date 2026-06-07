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

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;

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

    private readonly IPermissionDelegationRepository _permissionDelegationRepository;

    private readonly IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> _snapshotCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationSnapshotQueryService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IPermissionDelegationRepository permissionDelegationRepository,
        IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> snapshotCache)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _permissionDelegationRepository = permissionDelegationRepository;
        _snapshotCache = snapshotCache;
    }

    /// <inheritdoc />
    public async Task<AuthorizationSnapshot> BuildAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 分布式缓存：按用户缓存授权快照。失效由授权写路径触发——角色/角色权限/用户角色/用户权限/
        // 权限委托/权限定义启停删等变更时，对应 AppService 调 InvalidateAuthorizationAsync（considerUow，事务提交后失效）。
        var cacheKey = SaasCacheKeys.AuthorizationSnapshot(userId);
        var item = await _snapshotCache.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                var built = await BuildSnapshotAsync(userId, now, cancellationToken);
                return new SaasAuthorizationSnapshotCacheItem
                {
                    UserId = userId,
                    Roles = built.Roles,
                    Permissions = built.Permissions,
                    PermissionIds = [.. built.PermissionIds],
                    CachedAt = DateTimeOffset.UtcNow
                };
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        return item is null
            ? await BuildSnapshotAsync(userId, now, cancellationToken)
            : new AuthorizationSnapshot(item.Roles, item.Permissions, [.. item.PermissionIds]);
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>
    /// 实时构建用户授权快照（缓存未命中时执行）。
    /// </summary>
    private async Task<AuthorizationSnapshot> BuildSnapshotAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
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

        // 叠加当前有效的权限委托（被委托人 = 当前用户）：
        // - 直接委托权限（PermissionId）→ 直接并入
        // - 委托角色（RoleId）→ 展开为该角色当前有效的 Grant 权限（扣除该角色 Deny）
        // 用户显式 Deny 仍然优先（最后再扣除一次）。
        var delegatedGrantIds = await ResolveDelegatedPermissionIdsAsync(userId, now, cancellationToken);
        if (delegatedGrantIds.Count > 0)
        {
            finalPermissionIds.UnionWith(delegatedGrantIds);
            finalPermissionIds.ExceptWith(userDenyIds);
        }

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

    /// <summary>
    /// 解析当前用户作为被委托人、当前有效的委托所赋予的权限 ID 集合。
    /// </summary>
    private async Task<HashSet<long>> ResolveDelegatedPermissionIdsAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var delegations = await _permissionDelegationRepository.GetActiveByDelegateeIdAsync(userId, now, cancellationToken);
        if (delegations.Count == 0)
        {
            return [];
        }

        var grantIds = new HashSet<long>();
        var roleIds = new HashSet<long>();
        foreach (var delegation in delegations)
        {
            if (delegation.PermissionId is > 0)
            {
                grantIds.Add(delegation.PermissionId.Value);
            }

            if (delegation.RoleId is > 0)
            {
                roleIds.Add(delegation.RoleId.Value);
            }
        }

        if (roleIds.Count > 0)
        {
            var rolePermissions = await _rolePermissionRepository.GetValidByRoleIdsAsync(roleIds, now, cancellationToken);
            var roleGrantIds = rolePermissions
                .Where(permission => permission.PermissionAction == PermissionAction.Grant)
                .Select(permission => permission.PermissionId)
                .ToHashSet();
            var roleDenyIds = rolePermissions
                .Where(permission => permission.PermissionAction == PermissionAction.Deny)
                .Select(permission => permission.PermissionId)
                .ToHashSet();
            roleGrantIds.ExceptWith(roleDenyIds);
            grantIds.UnionWith(roleGrantIds);
        }

        return grantIds;
    }
}
