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
using XiHan.Framework.MultiTenancy.Abstractions;

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

    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    private readonly IUserPermissionRepository _userPermissionRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IPermissionDelegationRepository _permissionDelegationRepository;

    private readonly ITenantRepository _tenantRepository;

    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;

    private readonly ICurrentTenant _currentTenant;

    private readonly IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> _snapshotCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationSnapshotQueryService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleHierarchyRepository roleHierarchyRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IPermissionDelegationRepository permissionDelegationRepository,
        ITenantRepository tenantRepository,
        ITenantEditionPermissionRepository tenantEditionPermissionRepository,
        ICurrentTenant currentTenant,
        IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> snapshotCache)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _permissionDelegationRepository = permissionDelegationRepository;
        _tenantRepository = tenantRepository;
        _tenantEditionPermissionRepository = tenantEditionPermissionRepository;
        _currentTenant = currentTenant;
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

        // 分布式缓存：按 用户 × 租户上下文 缓存授权快照（多租户成员在不同租户角色不同，切换租户不串味）。
        // 失效由授权写路径触发——角色/角色权限/用户角色/用户权限/权限委托/权限定义启停删等变更时，
        // 对应 AppService 调 InvalidateAuthorizationAsync（按用户模式整体失效全部租户维度，considerUow 事务提交后生效）。
        var cacheKey = SaasCacheKeys.AuthorizationSnapshot(_currentTenant.Id, userId);
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

        var snapshot = item is null
            ? await BuildSnapshotAsync(userId, now, cancellationToken)
            : new AuthorizationSnapshot(item.Roles, item.Permissions, [.. item.PermissionIds]);

        // 套餐(Edition)运行时门控：在 per-user 缓存之外按当前租户上下文叠加，避免切换租户后缓存串味
        return await ApplyEditionGatingAsync(snapshot, cancellationToken);
    }

    /// <summary>
    /// 按当前租户版本(Edition)的权限白名单收窄有效权限。
    /// </summary>
    /// <remarks>
    /// 例外（不门控）：超管通配 *、平台运维态（无租户）、租户未绑定版本、版本未配置白名单（避免误锁）。
    /// 门控在缓存外叠加：原始快照仍按 userId 缓存，版本变更（升降级）即时生效，无需失效用户快照缓存。
    /// 性能：命中门控时按当前实现每次拉取版本白名单；后续可对"版本→白名单"做独立缓存优化（见方案 REQ-5.3）。
    /// </remarks>
    private async Task<AuthorizationSnapshot> ApplyEditionGatingAsync(AuthorizationSnapshot snapshot, CancellationToken cancellationToken)
    {
        // 超管通配 * 不受门控
        if (snapshot.Permissions.Contains("*"))
        {
            return snapshot;
        }

        // 平台运维态（无租户上下文）不门控
        var tenantId = _currentTenant.Id;
        if (tenantId is not > 0)
        {
            return snapshot;
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant?.EditionId is not > 0)
        {
            return snapshot;
        }

        var whitelist = await _tenantEditionPermissionRepository.GetByEditionIdAsync(tenant.EditionId.Value, cancellationToken);
        var allowedIds = whitelist
            .Where(item => item.Status == ValidityStatus.Valid)
            .Select(item => item.PermissionId)
            .ToHashSet();

        // 版本未配置白名单：视为未启用门控，保持原快照（避免把租户锁死）
        if (allowedIds.Count == 0)
        {
            return snapshot;
        }

        var gatedIds = new HashSet<long>(snapshot.PermissionIds);
        gatedIds.IntersectWith(allowedIds);

        // 全部已在白名单内，无需变更
        if (gatedIds.Count == snapshot.PermissionIds.Count)
        {
            return snapshot;
        }

        var gatedPermissions = await _permissionRepository.GetByIdsAsync(gatedIds, cancellationToken);
        var gatedCodes = gatedPermissions
            .Where(permission => permission.Status == EnableStatus.Enabled)
            .Select(permission => permission.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return snapshot with { Permissions = gatedCodes, PermissionIds = gatedIds };
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
        // 绑定行显式按租户上下文过滤：授权绑定（用户角色/直授/委托）按 (当前租户 OR 全局) 生效。
        // 租户上下文时与全局租户过滤器等价；平台态（无租户上下文，过滤器关闭）则仅全局绑定生效，
        // 防止多租户成员在平台态聚合出跨租户权限（渗漏）。
        var bindingScopeTenantId = _currentTenant.Id ?? 0;

        var userRoles = (await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
            .Where(item => item.TenantId == bindingScopeTenantId || item.TenantId == 0)
            .ToList();
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

        // 角色权限（含角色继承展开：后代继承祖先 Grant，Deny 覆盖）
        var roleGrantIds = await ResolveRoleGrantIdsAsync(roles.Select(role => role.BasicId), now, cancellationToken);

        var userPermissions = (await _userPermissionRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
            .Where(item => item.TenantId == bindingScopeTenantId || item.TenantId == 0)
            .ToList();
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
        // 委托绑定同样按 (当前租户 OR 全局) 生效，见 BuildSnapshotAsync 中绑定行过滤说明
        var bindingScopeTenantId = _currentTenant.Id ?? 0;
        var delegations = (await _permissionDelegationRepository.GetActiveByDelegateeIdAsync(userId, now, cancellationToken))
            .Where(item => item.TenantId == bindingScopeTenantId || item.TenantId == 0)
            .ToList();
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
            grantIds.UnionWith(await ResolveRoleGrantIdsAsync(roleIds, now, cancellationToken));
        }

        return grantIds;
    }

    /// <summary>
    /// 解析给定角色（含其继承链上的祖先角色）当前有效的 Grant 权限 ID 集合（Deny 覆盖）。
    /// </summary>
    /// <remarks>
    /// 角色继承语义：后代自动获得祖先的 Grant 权限，Deny 覆盖；继承链上仅启用角色参与。
    /// 无继承关系时展开结果即为角色自身，等价于不展开的原行为。
    /// </remarks>
    private async Task<HashSet<long>> ResolveRoleGrantIdsAsync(IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var directRoleIds = roleIds.Where(id => id > 0).Distinct().ToList();
        if (directRoleIds.Count == 0)
        {
            return [];
        }

        // 角色自身始终参与；再沿闭包表叠加其祖先角色（Depth>0），后代继承祖先权限。
        // 不依赖闭包表的自身行(Depth=0)：未配置继承关系时祖先集为空，展开结果即为角色自身（等价于原行为）。
        var expandedRoleIds = new HashSet<long>(directRoleIds);
        var ancestorIds = await _roleHierarchyRepository.GetAncestorIdsAsync(directRoleIds, includeSelf: false, cancellationToken);
        expandedRoleIds.UnionWith(ancestorIds);
        // 继承链上仅启用角色参与（停用角色不贡献权限）
        var enabledRoles = await _roleRepository.GetEnabledByIdsAsync(expandedRoleIds, cancellationToken);
        if (enabledRoles.Count == 0)
        {
            return [];
        }

        var rolePermissions = await _rolePermissionRepository.GetValidByRoleIdsAsync(enabledRoles.Select(role => role.BasicId), now, cancellationToken);
        var grantIds = rolePermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Grant)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        var denyIds = rolePermissions
            .Where(permission => permission.PermissionAction == PermissionAction.Deny)
            .Select(permission => permission.PermissionId)
            .ToHashSet();
        grantIds.ExceptWith(denyIds);
        return grantIds;
    }
}
