// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户开通领域服务实现
/// </summary>
public sealed class TenantProvisionDomainService
    : ITenantProvisionDomainService
{
    /// <summary>
    /// 租户初始化 Owner 角色编码
    /// </summary>
    private const string TenantOwnerRoleCode = "tenant_owner";

    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly ITenantEditionRepository _tenantEditionRepository;

    private readonly ITenantRepository _tenantRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IUserPermissionRepository _userPermissionRepository;

    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantProvisionDomainService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserRoleRepository userRoleRepository,
        ITenantUserRepository tenantUserRepository,
        ITenantEditionRepository tenantEditionRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        ITenantEditionPermissionRepository tenantEditionPermissionRepository,
        ICurrentTenant currentTenant)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userRoleRepository = userRoleRepository;
        _tenantUserRepository = tenantUserRepository;
        _tenantEditionRepository = tenantEditionRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _tenantEditionPermissionRepository = tenantEditionPermissionRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<SysUser> ProvisionTenantAdminAsync(SysTenant tenant, string adminUserName, string adminEmail, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        cancellationToken.ThrowIfCancellationRequested();

        // 1) 确保版本：未指定则取默认版本并持久化——必须在进入租户上下文之前完成：
        //    SysTenant 行本身是 TenantId=0 的平台数据，租户上下文内写它会被写路径租户边界拒绝（语义上这本就是平台态操作）
        var editionId = tenant.EditionId ?? await AssignDefaultEditionAsync(tenant, cancellationToken);
        if (editionId.HasValue && tenant.EditionId != editionId)
        {
            tenant.EditionId = editionId;
            await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        }

        // 在新租户上下文内开通，确保审计/上下文一致（实体均显式置 TenantId）
        using var tenantScope = _currentTenant.Change(tenant.BasicId, tenant.TenantName);

        // 2) 创建管理员（用户/安全/成员）
        var adminUser = await InitializeTenantAdminAsync(tenant, adminUserName, adminEmail, passwordHash, cancellationToken);

        // 3) 创建 Owner 角色并按版本白名单授权
        var ownerRoleId = await CreateOwnerRoleWithEditionPermissionsAsync(tenant, editionId, cancellationToken);

        // 4) 绑定管理员到 Owner 角色
        await AssignAdminRoleAsync(tenant, adminUser.BasicId, ownerRoleId, cancellationToken);

        return adminUser;
    }

    /// <inheritdoc />
    public async Task<SysUser> InitializeTenantAdminAsync(SysTenant tenant, string adminUserName, string adminEmail, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        cancellationToken.ThrowIfCancellationRequested();

        // 邮箱是全平台唯一的登录身份标识
        var normalizedEmail = adminEmail.Trim();
        if (await _userRepository.ExistsEmailGloballyAsync(normalizedEmail, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("管理员邮箱已被其他账号使用。");
        }

        // 创建管理员用户
        var adminUser = new SysUser
        {
            TenantId = tenant.BasicId,
            UserName = adminUserName.Trim(),
            Email = normalizedEmail,
            Status = EnableStatus.Enabled,
            IsSystemAccount = true
        };
        adminUser = await _userRepository.AddAsync(adminUser, cancellationToken);

        // 创建用户安全信息（密码）
        var security = new SysUserSecurity
        {
            TenantId = tenant.BasicId,
            UserId = adminUser.BasicId,
            Password = passwordHash,
            LastPasswordChangeTime = DateTimeOffset.UtcNow
        };
        await _userSecurityRepository.AddAsync(security, cancellationToken);

        // 创建租户成员关系
        var tenantUser = new SysTenantUser
        {
            TenantId = tenant.BasicId,
            UserId = adminUser.BasicId,
            MemberType = TenantMemberType.Owner,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            RespondedTime = DateTimeOffset.UtcNow
        };
        await _tenantUserRepository.AddAsync(tenantUser, cancellationToken);

        return adminUser;
    }

    /// <inheritdoc />
    public async Task AssignAdminRoleAsync(SysTenant tenant, long adminUserId, long ownerRoleId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();

        var userRole = new SysUserRole
        {
            TenantId = tenant.BasicId,
            UserId = adminUserId,
            RoleId = ownerRoleId,
            Status = ValidityStatus.Valid
        };
        await _userRoleRepository.AddAsync(userRole, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long?> AssignDefaultEditionAsync(SysTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();

        var defaultEdition = await _tenantEditionRepository.GetDefaultEditionAsync(cancellationToken);
        if (defaultEdition is null)
        {
            return null;
        }

        tenant.EditionId = defaultEdition.BasicId;
        return defaultEdition.BasicId;
    }

    /// <inheritdoc />
    public async Task<int> ReconcileTenantAuthorizationWithEditionAsync(SysTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();

        if (tenant.EditionId is not > 0)
        {
            return 0;
        }

        var whitelist = await _tenantEditionPermissionRepository.GetByEditionIdAsync(tenant.EditionId.Value, cancellationToken);
        var allowedIds = whitelist
            .Where(item => item.Status == ValidityStatus.Valid)
            .Select(item => item.PermissionId)
            .ToHashSet();

        // 白名单为空视为门控未启用（与运行时鉴权门控语义一致），不做回收，避免误清
        if (allowedIds.Count == 0)
        {
            return 0;
        }

        // 仅处理该租户自有绑定行（TenantId=本租户）；全局行（TenantId=0）属平台运维资产，不在回收范围
        using var tenantScope = _currentTenant.Change(tenant.BasicId, tenant.TenantName);
        var tenantId = tenant.BasicId;
        var now = DateTimeOffset.UtcNow;

        var staleRolePermissions = (await _rolePermissionRepository.GetListAsync(
                item => item.TenantId == tenantId && item.Status == ValidityStatus.Valid,
                cancellationToken))
            .Where(item => !allowedIds.Contains(item.PermissionId))
            .ToList();
        foreach (var item in staleRolePermissions)
        {
            item.Status = ValidityStatus.Invalid;
            item.ExpirationTime = now;
            item.Remark = "套餐变更回收：超出当前版本权限白名单";
        }

        if (staleRolePermissions.Count > 0)
        {
            _ = await _rolePermissionRepository.UpdateRangeAsync(staleRolePermissions, cancellationToken);
        }

        var staleUserPermissions = (await _userPermissionRepository.GetListAsync(
                item => item.TenantId == tenantId && item.Status == ValidityStatus.Valid,
                cancellationToken))
            .Where(item => !allowedIds.Contains(item.PermissionId))
            .ToList();
        foreach (var item in staleUserPermissions)
        {
            item.Status = ValidityStatus.Invalid;
            item.ExpirationTime = now;
            item.Remark = "套餐变更回收：超出当前版本权限白名单";
        }

        if (staleUserPermissions.Count > 0)
        {
            _ = await _userPermissionRepository.UpdateRangeAsync(staleUserPermissions, cancellationToken);
        }

        return staleRolePermissions.Count + staleUserPermissions.Count;
    }

    /// <inheritdoc />
    public async Task<int> ReconcileEditionTenantsAuthorizationAsync(long editionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (editionId <= 0)
        {
            return 0;
        }

        var tenants = await _tenantRepository.GetListAsync(tenant => tenant.EditionId == editionId, cancellationToken);
        var total = 0;
        foreach (var tenant in tenants)
        {
            total += await ReconcileTenantAuthorizationWithEditionAsync(tenant, cancellationToken);
        }

        return total;
    }

    /// <summary>
    /// 创建租户 Owner 角色，并按其版本(Edition)允许的权限白名单批量授权
    /// </summary>
    private async Task<long> CreateOwnerRoleWithEditionPermissionsAsync(SysTenant tenant, long? editionId, CancellationToken cancellationToken)
    {
        var role = new SysRole
        {
            TenantId = tenant.BasicId,
            RoleCode = TenantOwnerRoleCode,
            RoleName = "租户所有者",
            RoleDescription = "租户初始化所有者角色，拥有租户版本范围内全部权限",
            RoleType = RoleType.Custom,
            DataScope = DataPermissionScope.All,
            MaxMembers = 0,
            Status = EnableStatus.Enabled,
            Sort = 1,
            Remark = "租户开通初始化角色"
        };
        role = await _roleRepository.AddAsync(role, cancellationToken);

        if (!editionId.HasValue)
        {
            return role.BasicId;
        }

        var whitelist = await _tenantEditionPermissionRepository.GetByEditionIdAsync(editionId.Value, cancellationToken);
        var grants = whitelist
            .Where(item => item.Status == ValidityStatus.Valid)
            .Select(item => item.PermissionId)
            .Distinct()
            .Select(permissionId => new SysRolePermission
            {
                TenantId = tenant.BasicId,
                RoleId = role.BasicId,
                PermissionId = permissionId,
                PermissionAction = PermissionAction.Grant,
                Status = ValidityStatus.Valid,
                GrantReason = "租户开通按版本白名单初始化",
                Remark = "租户开通初始化授权"
            })
            .ToList();

        if (grants.Count > 0)
        {
            _ = await _rolePermissionRepository.AddRangeAsync(grants, cancellationToken);
        }

        return role.BasicId;
    }
}
