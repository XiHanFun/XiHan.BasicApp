// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 授权快照：生效权限 = 当前上下文的绑定 ∩ 作用侧 ∩ 套餐白名单（仅业务租户）。
/// </summary>
public sealed class AuthorizationSnapshotSideTests
{
    private const long UserId = 1001;
    private const long TenantId = 7;
    private const long EditionId = 70;

    private const long SuperAdminRoleId = 1;
    private const long TenantAdminRoleId = 2;
    private const long PlatformOpsRoleId = 3;
    private const long TenantOwnerRoleId = 4;
    private const long ImpostorOwnerRoleId = 5;

    private const long TenantCreateId = 101;
    private const long DepartmentReadId = 102;
    private const long UserReadId = 103;
    private const long NotificationReadId = 104;

    private readonly TestCurrentTenant _currentTenant = new();
    private readonly List<SysUserRole> _userRoles = [];
    private readonly List<SysRolePermission> _rolePermissions = [];
    private readonly List<SysTenantEditionPermission> _editionPermissions = [];
    private readonly SysTenant _tenant = new() { TenantCode = "acme", TenantName = "Acme" };
    private bool _editionGateCacheUnavailable;

    private readonly List<SysPermission> _catalog =
    [
        Permission(TenantCreateId, "saas:tenant:create", PermissionSide.Platform),
        Permission(DepartmentReadId, "saas:department:read", PermissionSide.Tenant),
        Permission(UserReadId, "saas:user:read", PermissionSide.Both),
        Permission(NotificationReadId, "saas:notification:read", PermissionSide.Both)
    ];

    private readonly List<SysRole> _roles =
    [
        Role(SuperAdminRoleId, "super_admin", 0),
        Role(TenantAdminRoleId, "tenant_admin", TenantId),
        Role(PlatformOpsRoleId, "platform_ops", 0),
        Role(TenantOwnerRoleId, SaasRoleCodes.TenantOwner, TenantId, RoleType.System),
        Role(ImpostorOwnerRoleId, SaasRoleCodes.TenantOwner, TenantId, RoleType.Custom)
    ];

    /// <summary>
    /// 构造测试上下文
    /// </summary>
    public AuthorizationSnapshotSideTests()
    {
        SaasTestHelper.SetBasicId(_tenant, TenantId);
    }

    /// <summary>
    /// 平台超管：通配 * 加平台生效的权限，租户侧的码作为上下文拒绝码下发
    /// </summary>
    [Fact]
    public async Task PlatformSuperAdmin_GetsWildcardAndPlatformEffectiveCodes()
    {
        BindRole(SuperAdminRoleId, bindingTenantId: 0);

        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["*", "saas:notification:read", "saas:tenant:create", "saas:user:read"], snapshot.Permissions);
        Assert.Equal(["saas:department:read"], snapshot.ContextDeniedCodes);
    }

    /// <summary>
    /// 平台的超管绑定不带进租户：切进租户后既没有角色也没有权限
    /// </summary>
    [Fact]
    public async Task PlatformSuperAdminBinding_DoesNotCarryIntoTenant()
    {
        BindRole(SuperAdminRoleId, bindingTenantId: 0);
        BindEdition(DepartmentReadId, UserReadId, NotificationReadId);

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Empty(snapshot.Roles);
        Assert.Empty(snapshot.Permissions);
        Assert.Contains("saas:tenant:create", snapshot.ContextDeniedCodes);
    }

    /// <summary>
    /// 租户的绑定也不带进平台
    /// </summary>
    [Fact]
    public async Task TenantBinding_DoesNotCarryIntoPlatform()
    {
        BindRole(TenantAdminRoleId, bindingTenantId: TenantId);
        GrantRole(TenantAdminRoleId, UserReadId);

        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Empty(snapshot.Roles);
        Assert.Empty(snapshot.Permissions);
    }

    /// <summary>
    /// 租户里只留租户能生效的权限：角色上挂着的平台侧权限不生效，且作为上下文拒绝码下发
    /// </summary>
    [Fact]
    public async Task TenantContext_DropsPlatformSideGrant()
    {
        BindRole(TenantAdminRoleId, bindingTenantId: TenantId);
        GrantRole(TenantAdminRoleId, TenantCreateId, DepartmentReadId, UserReadId);
        BindEdition(DepartmentReadId, UserReadId, NotificationReadId);

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["saas:department:read", "saas:user:read"], snapshot.Permissions);
        Assert.Equal(["saas:tenant:create"], snapshot.ContextDeniedCodes);
    }

    /// <summary>
    /// 平台里租户侧权限不生效：平台角色挂的租户侧码被剔除
    /// </summary>
    [Fact]
    public async Task PlatformContext_DropsTenantSideGrant()
    {
        BindRole(PlatformOpsRoleId, bindingTenantId: 0);
        GrantRole(PlatformOpsRoleId, TenantCreateId, DepartmentReadId);

        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["saas:tenant:create"], snapshot.Permissions);
    }

    /// <summary>
    /// 套餐门控：租户里只留白名单内的权限
    /// </summary>
    [Fact]
    public async Task TenantContext_IntersectsWithEditionWhitelist()
    {
        BindRole(TenantAdminRoleId, bindingTenantId: TenantId);
        GrantRole(TenantAdminRoleId, DepartmentReadId, UserReadId, NotificationReadId);
        BindEdition(UserReadId);

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["saas:user:read"], snapshot.Permissions);
        Assert.Equal([UserReadId], snapshot.PermissionIds);
    }

    /// <summary>
    /// 租户所有者：持有本租户的所有者系统角色，不靠授权行就拿到租户生效的全部权限，再由套餐收窄；
    /// 平台侧的码仍作为上下文拒绝码下发，也没有通配 *
    /// </summary>
    [Fact]
    public async Task TenantOwner_GetsWholeEditionWithoutGrants()
    {
        BindRole(TenantOwnerRoleId, bindingTenantId: TenantId);
        BindEdition(DepartmentReadId, UserReadId);

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["saas:department:read", "saas:user:read"], snapshot.Permissions);
        Assert.DoesNotContain("*", snapshot.Permissions);
        Assert.Contains("saas:tenant:create", snapshot.ContextDeniedCodes);
    }

    /// <summary>
    /// 所有者角色只在所属租户成立：带到平台什么也不给
    /// </summary>
    [Fact]
    public async Task TenantOwnerBinding_DoesNotCarryIntoPlatform()
    {
        BindRole(TenantOwnerRoleId, bindingTenantId: TenantId);

        using var scope = _currentTenant.Change(0);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Empty(snapshot.Permissions);
    }

    /// <summary>
    /// 同码的自定义角色不是所有者角色：只按它自己的授权行算
    /// </summary>
    [Fact]
    public async Task CustomRoleWithOwnerCode_GetsOnlyItsGrants()
    {
        BindRole(ImpostorOwnerRoleId, bindingTenantId: TenantId);
        GrantRole(ImpostorOwnerRoleId, UserReadId);
        BindEdition(DepartmentReadId, UserReadId);

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["saas:user:read"], snapshot.Permissions);
    }

    /// <summary>
    /// 套餐门控失败即拒绝：租户没绑套餐时一个权限也不生效
    /// </summary>
    [Fact]
    public async Task TenantWithoutEdition_GetsNoPermission()
    {
        BindRole(TenantAdminRoleId, bindingTenantId: TenantId);
        GrantRole(TenantAdminRoleId, DepartmentReadId, UserReadId);

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Empty(snapshot.Permissions);
        Assert.Empty(snapshot.PermissionIds);
    }

    /// <summary>
    /// 套餐门控失败即拒绝：套餐白名单为空时一个权限也不生效
    /// </summary>
    [Fact]
    public async Task TenantWithEmptyEditionWhitelist_GetsNoPermission()
    {
        BindRole(TenantAdminRoleId, bindingTenantId: TenantId);
        GrantRole(TenantAdminRoleId, DepartmentReadId, UserReadId);
        BindEdition();

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Empty(snapshot.Permissions);
    }

    /// <summary>
    /// 门控缓存不可用时回库读取白名单，不因缓存故障放行
    /// </summary>
    [Fact]
    public async Task EditionGateCacheUnavailable_LoadsWhitelistFromRepository()
    {
        BindRole(TenantAdminRoleId, bindingTenantId: TenantId);
        GrantRole(TenantAdminRoleId, DepartmentReadId, UserReadId);
        BindEdition(DepartmentReadId);
        _editionGateCacheUnavailable = true;

        using var scope = _currentTenant.Change(TenantId);
        var snapshot = await CreateService().BuildAsync(UserId, DateTimeOffset.UtcNow);

        Assert.Equal(["saas:department:read"], snapshot.Permissions);
    }

    private void BindRole(long roleId, long bindingTenantId)
    {
        _userRoles.Add(new SysUserRole { UserId = UserId, RoleId = roleId, TenantId = bindingTenantId, Status = ValidityStatus.Valid });
    }

    private void GrantRole(long roleId, params long[] permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            _rolePermissions.Add(new SysRolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionAction = PermissionAction.Grant,
                Status = ValidityStatus.Valid
            });
        }
    }

    private void BindEdition(params long[] permissionIds)
    {
        _tenant.EditionId = EditionId;
        _editionPermissions.AddRange(permissionIds.Select(permissionId => new SysTenantEditionPermission
        {
            EditionId = EditionId,
            PermissionId = permissionId,
            Status = ValidityStatus.Valid
        }));
    }

    private AuthorizationSnapshotQueryService CreateService()
    {
        var userRoleRepository = new Mock<IUserRoleRepository>();
        userRoleRepository
            .Setup(repository => repository.GetValidByUserIdAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _userRoles);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository
            .Setup(repository => repository.GetEnabledByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) => [.. _roles.Where(role => ids.Contains(role.BasicId))]);

        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        rolePermissionRepository
            .Setup(repository => repository.GetValidByRoleIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> ids, DateTimeOffset _, CancellationToken _) => [.. _rolePermissions.Where(binding => ids.Contains(binding.RoleId))]);

        var roleHierarchyRepository = new Mock<IRoleHierarchyRepository>();
        roleHierarchyRepository
            .Setup(repository => repository.GetAncestorIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var userPermissionRepository = new Mock<IUserPermissionRepository>();
        userPermissionRepository
            .Setup(repository => repository.GetValidByUserIdAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var permissionRepository = new Mock<IPermissionRepository>();
        permissionRepository
            .Setup(repository => repository.GetListAsync(It.IsAny<Expression<Func<SysPermission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysPermission, bool>> predicate, CancellationToken _) => [.. _catalog.Where(predicate.Compile())]);
        permissionRepository
            .Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) => [.. _catalog.Where(permission => ids.Contains(permission.BasicId))]);

        var delegationRepository = new Mock<IPermissionDelegationRepository>();
        delegationRepository
            .Setup(repository => repository.GetActiveByDelegateeIdAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository
            .Setup(repository => repository.GetByIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _tenant);

        var editionPermissionRepository = new Mock<ITenantEditionPermissionRepository>();
        editionPermissionRepository
            .Setup(repository => repository.GetByEditionIdAsync(EditionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _editionPermissions);

        // 快照缓存直通工厂：同时覆盖缓存项与快照之间的往返
        var snapshotCache = new Mock<IDistributedCache<SaasAuthorizationSnapshotCacheItem, string>>();
        snapshotCache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasAuthorizationSnapshotCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<SaasAuthorizationSnapshotCacheItem>> factory, Func<DistributedCacheEntryOptions>? _, bool? _, bool _, CancellationToken _)
                => InvokeAsync(factory));

        // 门控缓存：可用时直通工厂，不可用时（hideErrors 吞掉异常）返回 null
        var editionGateCache = new Mock<IDistributedCache<SaasEditionGateCacheItem, string>>();
        editionGateCache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasEditionGateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<SaasEditionGateCacheItem>> factory, Func<DistributedCacheEntryOptions>? _, bool? _, bool _, CancellationToken _)
                => _editionGateCacheUnavailable ? Task.FromResult<SaasEditionGateCacheItem?>(null) : InvokeAsync(factory));

        return new AuthorizationSnapshotQueryService(
            userRoleRepository.Object,
            roleRepository.Object,
            rolePermissionRepository.Object,
            roleHierarchyRepository.Object,
            userPermissionRepository.Object,
            permissionRepository.Object,
            delegationRepository.Object,
            tenantRepository.Object,
            editionPermissionRepository.Object,
            _currentTenant,
            snapshotCache.Object,
            editionGateCache.Object);
    }

    private static async Task<TItem?> InvokeAsync<TItem>(Func<Task<TItem>> factory) where TItem : class
    {
        return await factory();
    }

    private static SysPermission Permission(long id, string code, PermissionSide side)
    {
        var permission = new SysPermission
        {
            PermissionCode = code,
            PermissionName = code,
            Side = side,
            Status = EnableStatus.Enabled
        };
        SaasTestHelper.SetBasicId(permission, id);
        return permission;
    }

    private static SysRole Role(long id, string code, long tenantId, RoleType roleType = RoleType.Custom)
    {
        var role = new SysRole
        {
            RoleCode = code,
            RoleName = code,
            RoleType = roleType,
            TenantId = tenantId,
            Status = EnableStatus.Enabled
        };
        SaasTestHelper.SetBasicId(role, id);
        return role;
    }
}
