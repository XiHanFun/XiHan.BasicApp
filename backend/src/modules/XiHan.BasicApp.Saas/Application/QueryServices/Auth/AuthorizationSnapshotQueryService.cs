// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照查询服务实现
/// </summary>
/// <remarks>
/// 生效权限 = 当前上下文的授权绑定 ∩ 作用侧允许当前上下文的权限 ∩ 套餐白名单（仅业务租户）：
/// <list type="bullet">
///   <item>授权绑定（用户角色 / 直授 / 委托）只在所属上下文生效：平台的绑定（含超管）不带进任何租户，租户的绑定也不带进平台；</item>
///   <item>作用侧来自权限目录（<c>SysPermission.Side</c>），不在当前上下文生效的权限码随快照下发，鉴权与菜单据此先行拒绝；</item>
///   <item>超管的通配 * 只在平台成立，业务租户里一律经套餐门控；</item>
///   <item>租户所有者（持有本租户的 tenant_owner 系统角色）拿到租户生效的全部权限，再经套餐门控收窄到白名单——
///   不靠授权行，套餐升降、新增权限码都即时反映。</item>
/// </list>
/// </remarks>
public sealed class AuthorizationSnapshotQueryService
    : IAuthorizationSnapshotQueryService
{
    private readonly IUserRoleRepository _userRoleRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IRoleHierarchyDomainService _roleHierarchyDomainService;

    private readonly IUserPermissionRepository _userPermissionRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IPermissionDelegationRepository _permissionDelegationRepository;

    private readonly IPermissionConditionRepository _permissionConditionRepository;

    private readonly ITenantRepository _tenantRepository;

    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;

    private readonly ICurrentTenant _currentTenant;

    private readonly IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> _snapshotCache;

    private readonly IDistributedCache<SaasEditionGateCacheItem, string> _editionGateCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationSnapshotQueryService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleHierarchyDomainService roleHierarchyDomainService,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IPermissionDelegationRepository permissionDelegationRepository,
        IPermissionConditionRepository permissionConditionRepository,
        ITenantRepository tenantRepository,
        ITenantEditionPermissionRepository tenantEditionPermissionRepository,
        ICurrentTenant currentTenant,
        IDistributedCache<SaasAuthorizationSnapshotCacheItem, string> snapshotCache,
        IDistributedCache<SaasEditionGateCacheItem, string> editionGateCache)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleHierarchyDomainService = roleHierarchyDomainService;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _permissionDelegationRepository = permissionDelegationRepository;
        _permissionConditionRepository = permissionConditionRepository;
        _tenantRepository = tenantRepository;
        _tenantEditionPermissionRepository = tenantEditionPermissionRepository;
        _currentTenant = currentTenant;
        _snapshotCache = snapshotCache;
        _editionGateCache = editionGateCache;
    }

    /// <summary>
    /// 构建用户授权快照
    /// </summary>
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
                    ContextDeniedCodes = [.. built.ContextDeniedCodes],
                    Conditions = built.Conditions,
                    CachedAt = DateTimeOffset.UtcNow
                };
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        var snapshot = item is null
            ? await BuildSnapshotAsync(userId, now, cancellationToken)
            : new AuthorizationSnapshot(
                item.Roles,
                item.Permissions,
                [.. item.PermissionIds],
                new HashSet<string>(item.ContextDeniedCodes, StringComparer.OrdinalIgnoreCase))
            {
                Conditions = item.Conditions?.Normalize() ?? new AuthorizationConditions()
            };

        // 套餐(Edition)运行时门控：在 per-user 缓存之外按当前租户上下文叠加，避免切换租户后缓存串味
        return await ApplyEditionGatingAsync(snapshot, cancellationToken);
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>
    /// 按当前租户版本(Edition)的权限白名单收窄有效权限。
    /// </summary>
    /// <remarks>
    /// 业务租户上下文一律门控：租户未绑定版本、或版本白名单为空时，生效权限为空（不放行）；平台上下文不门控。
    /// 门控在用户快照缓存之外叠加：白名单走独立的版本门控缓存（10 分钟 TTL，版本权限/租户换版写路径调
    /// InvalidateEditionGateAsync 失效），缓存不可用时直接查库，鉴权热路径不再每请求查 2 次库。
    /// </remarks>
    private async Task<AuthorizationSnapshot> ApplyEditionGatingAsync(AuthorizationSnapshot snapshot, CancellationToken cancellationToken)
    {
        // 平台上下文（0 号租户）不门控
        var tenantId = _currentTenant.Id;
        if (tenantId is not > 0)
        {
            return snapshot;
        }

        var gate = await _editionGateCache.GetOrAddAsync(
            SaasCacheKeys.EditionGate(tenantId.Value),
            () => LoadEditionGateAsync(tenantId.Value, cancellationToken),
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken)
            ?? await LoadEditionGateAsync(tenantId.Value, cancellationToken);

        var allowedIds = gate.PermissionIds.ToHashSet();

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

        return snapshot with { Permissions = gatedCodes, PermissionIds = gatedIds, Conditions = snapshot.Conditions.Retain(gatedCodes) };
    }

    /// <summary>
    /// 从库里读取租户的套餐白名单（未绑定版本即空白名单）
    /// </summary>
    private async Task<SaasEditionGateCacheItem> LoadEditionGateAsync(long tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant?.EditionId is not > 0)
        {
            return new SaasEditionGateCacheItem { EditionId = null, CachedAt = DateTimeOffset.UtcNow };
        }

        var whitelist = await _tenantEditionPermissionRepository.GetByEditionIdAsync(tenant.EditionId.Value, cancellationToken);
        return new SaasEditionGateCacheItem
        {
            EditionId = tenant.EditionId,
            PermissionIds = [.. whitelist
                .Where(item => item.Status == ValidityStatus.Valid)
                .Select(item => item.PermissionId)
                .Distinct()],
            CachedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 实时构建用户授权快照（缓存未命中时执行）。
    /// </summary>
    private async Task<AuthorizationSnapshot> BuildSnapshotAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // 授权绑定（用户角色 / 直授 / 委托）只在所属上下文生效：严格按当前作用域取（平台就是 0 号租户）。
        // 平台的绑定（含超管）不带进任何租户，租户的绑定也不带进平台。
        var bindingScopeTenantId = _currentTenant.Id ?? 0;
        var isPlatformContext = bindingScopeTenantId == 0;

        // 作用侧：权限目录里在当前上下文生效与不生效的两部分
        var catalog = await _permissionRepository.GetListAsync(permission => permission.Status == EnableStatus.Enabled, cancellationToken);
        var effectiveCatalog = catalog.Where(permission => permission.Side.IsEffectiveIn(isPlatformContext)).ToList();
        var contextDeniedCodes = catalog
            .Where(permission => !permission.Side.IsEffectiveIn(isPlatformContext))
            .Select(permission => permission.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var userRoles = (await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
            .Where(item => item.TenantId == bindingScopeTenantId)
            .ToList();
        var roles = await _roleRepository.GetEnabledByIdsAsync(userRoles.Select(item => item.RoleId), cancellationToken);
        var roleCodes = roles
            .Select(role => role.RoleCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        // 超管只在平台成立：持有平台的 super_admin 绑定，拿到平台生效的全部权限与通配 *
        var isSuperAdmin = isPlatformContext && roleCodes.Contains(SaasRoleCodes.SuperAdmin, StringComparer.OrdinalIgnoreCase);
        // 租户所有者只在所属租户成立：持有本租户的 tenant_owner 系统角色，拿到租户生效的全部权限（随后经套餐门控收窄）
        var isTenantOwner = !isPlatformContext && roles.Any(IsTenantOwnerRole);

        if (isSuperAdmin || isTenantOwner)
        {
            var permissionIds = effectiveCatalog.Select(permission => permission.BasicId).ToHashSet();
            var allPermissionCodes = effectiveCatalog
                .Select(permission => permission.PermissionCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (isSuperAdmin)
            {
                allPermissionCodes.Insert(0, "*");
            }

            return new AuthorizationSnapshot(roleCodes, allPermissionCodes, permissionIds, contextDeniedCodes);
        }

        // 角色权限：每个角色按自己的有效继承链结算，角色级 Deny 只作用于本链
        var heldChains = await LoadRoleChainsAsync(roles.Select(role => role.BasicId), "role", now, cancellationToken);

        var userPermissions = (await _userPermissionRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
            .Where(item => item.TenantId == bindingScopeTenantId)
            .ToList();

        // 叠加当前有效的权限委托（被委托人 = 当前用户）：
        // - 直接委托权限（PermissionId）→ 无条件路径
        // - 委托角色（RoleId）→ 按该角色的有效继承链结算（与持有角色同一口径）
        var (delegatedPermissionIds, delegatedChains) = await ResolveDelegationsAsync(userId, now, cancellationToken);

        // 授权行上启用的条件：写在授予行上是「满足才授予」，写在禁止行上是「满足才禁止」
        var rolePermissionRows = heldChains.Rows.Concat(delegatedChains.Rows).ToList();
        var conditions = await _permissionConditionRepository.GetValidByAuthorizationIdsAsync(
            rolePermissionRows.Select(row => row.BasicId),
            userPermissions.Select(row => row.BasicId),
            cancellationToken);
        var resolution = AuthorizationConditionResolver.Resolve(new AuthorizationGrantSources(
            [.. heldChains.Chains, .. delegatedChains.Chains],
            rolePermissionRows,
            userPermissions,
            delegatedPermissionIds,
            conditions));

        // 只留作用侧允许当前上下文、且启用的权限
        var effectivePermissions = effectiveCatalog.Where(permission => resolution.GrantedPermissionIds.Contains(permission.BasicId)).ToList();
        var effectiveIds = effectivePermissions.Select(permission => permission.BasicId).ToHashSet();
        var permissionCodes = effectivePermissions
            .Select(permission => permission.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthorizationSnapshot(roleCodes, permissionCodes, effectiveIds, contextDeniedCodes)
        {
            Conditions = BuildConditions(resolution, catalog)
        };
    }

    /// <summary>
    /// 合并结果按权限码落进快照：条件集补上所绑权限码，只留生效权限的条件
    /// </summary>
    private static AuthorizationConditions BuildConditions(AuthorizationGrantResolution resolution, IReadOnlyList<SysPermission> catalog)
    {
        var codes = catalog
            .Where(permission => !string.IsNullOrWhiteSpace(permission.PermissionCode))
            .ToDictionary(permission => permission.BasicId, permission => permission.PermissionCode);
        var conditions = new AuthorizationConditions();
        foreach (var (permissionId, paths) in resolution.ConditionalGrants)
        {
            if (codes.TryGetValue(permissionId, out var code))
            {
                conditions.Grants[code] = paths;
            }
        }

        foreach (var (permissionId, keys) in resolution.ConditionalDenies)
        {
            if (codes.TryGetValue(permissionId, out var code))
            {
                conditions.Denies[code] = keys;
            }
        }

        foreach (var (key, (permissionId, set)) in resolution.Sets)
        {
            if (codes.TryGetValue(permissionId, out var code))
            {
                set.PermissionCode = code;
                conditions.Sets[key] = set;
            }
        }

        return conditions.Retain(conditions.Grants.Keys.Concat(conditions.Denies.Keys));
    }

    /// <summary>
    /// 租户所有者角色：租户自己的系统角色、编码为 tenant_owner（系统角色租户里不能新建，同码的自定义角色不算）
    /// </summary>
    private static bool IsTenantOwnerRole(SysRole role)
    {
        return role.RoleType == RoleType.System
            && !role.IsGlobal
            && string.Equals(role.RoleCode, SaasRoleCodes.TenantOwner, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 当前用户作为被委托人、当前有效的委托：直接委托的权限主键，以及委托角色的继承链
    /// </summary>
    private async Task<(HashSet<long> PermissionIds, RoleChainSet Chains)> ResolveDelegationsAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // 委托绑定同样只在所属上下文生效，见 BuildSnapshotAsync 中绑定行过滤说明
        var bindingScopeTenantId = _currentTenant.Id ?? 0;
        var delegations = (await _permissionDelegationRepository.GetActiveByDelegateeIdAsync(userId, now, cancellationToken))
            .Where(item => item.TenantId == bindingScopeTenantId)
            .ToList();
        if (delegations.Count == 0)
        {
            return ([], RoleChainSet.Empty);
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

        var chains = roleIds.Count > 0
            ? await LoadRoleChainsAsync(roleIds, "delegation-role", now, cancellationToken)
            : RoleChainSet.Empty;
        return (grantIds, chains);
    }

    /// <summary>
    /// 给定角色的有效继承链与链上当前有效的授权行
    /// </summary>
    /// <remarks>
    /// 角色继承语义：每个角色单独结算自己的有效继承链（自身 + 经启用角色可达的上级），
    /// 链上 Grant 并集减去链上 Deny 并集；多个角色之间取并集，一个角色的 Deny 不影响另一个独立角色。
    /// 自身停用的角色不贡献权限；停用的上级不贡献，也切断经由它的继承。
    /// </remarks>
    private async Task<RoleChainSet> LoadRoleChainsAsync(IEnumerable<long> roleIds, string sourcePrefix, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var enabledRoles = await _roleRepository.GetEnabledByIdsAsync(roleIds.Where(id => id > 0).Distinct(), cancellationToken);
        if (enabledRoles.Count == 0)
        {
            return RoleChainSet.Empty;
        }

        var roleIdList = enabledRoles.Select(role => role.BasicId).ToList();
        var effectiveAncestors = await _roleHierarchyDomainService.GetEffectiveAncestorsAsync(roleIdList, cancellationToken);
        var chains = roleIdList
            .Select(roleId => new AuthorizationRoleChain($"{sourcePrefix}:{roleId}", effectiveAncestors[roleId].Keys.Append(roleId).ToHashSet()))
            .ToList();

        var rows = await _rolePermissionRepository.GetValidByRoleIdsAsync(chains.SelectMany(chain => chain.RoleIds).Distinct(), now, cancellationToken);
        return new RoleChainSet(chains, rows);
    }

    /// <summary>
    /// 角色链与链上授权行
    /// </summary>
    private sealed record RoleChainSet(IReadOnlyList<AuthorizationRoleChain> Chains, IReadOnlyList<SysRolePermission> Rows)
    {
        public static RoleChainSet Empty { get; } = new([], []);
    }
}
