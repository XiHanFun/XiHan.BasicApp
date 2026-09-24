// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户开通领域服务实现
/// </summary>
public sealed class TenantProvisionDomainService
    : ITenantProvisionDomainService
{
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

    /// <summary>
    /// 开通租户管理员：管理员账号、所有者成员关系、所有者角色及其绑定
    /// </summary>
    /// <remarks>
    /// 所有者角色是系统角色，不写授权行：授权快照让持有者拿到租户生效的全部权限，再经套餐门控收窄，
    /// 所以套餐升降、新增权限码都即时反映，无需回头同步。账号、成员关系、角色与绑定都是该租户的数据，切入该租户写。
    /// </remarks>
    /// <param name="tenant">已创建的租户实体</param>
    /// <param name="adminUserName">管理员用户名</param>
    /// <param name="adminEmail">管理员邮箱（登录身份标识，全平台唯一）</param>
    /// <param name="passwordHash">管理员密码哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的管理员用户</returns>
    public async Task<SysUser> ProvisionTenantAdminAsync(SysTenant tenant, string adminUserName, string adminEmail, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureProvisionable(tenant);

        // 邮箱是全平台唯一的登录身份标识
        var normalizedEmail = adminEmail.Trim();
        if (await _userRepository.ExistsEmailGloballyAsync(normalizedEmail, cancellationToken: cancellationToken))
        {
            throw new UserFriendlyException("管理员邮箱已被其他账号使用。");
        }

        // 用户名在目标租户内唯一，租户范围显式传入（连带平台账号一起比对，避免与平台账号重名）
        var normalizedUserName = adminUserName.Trim();
        if (await _userRepository.ExistsUserNameInTenantAsync(tenant.BasicId, normalizedUserName, cancellationToken: cancellationToken))
        {
            throw new UserFriendlyException("管理员用户名已被使用。");
        }

        using var tenantScope = EnterTenantScope(tenant);

        var adminUser = await _userRepository.AddAsync(new SysUser
        {
            UserName = normalizedUserName,
            Email = normalizedEmail,
            Status = EnableStatus.Enabled,
            IsSystemAccount = true
        }, cancellationToken);

        await _userSecurityRepository.AddAsync(new SysUserSecurity
        {
            UserId = adminUser.BasicId,
            Password = passwordHash,
            LastPasswordChangeTime = DateTimeOffset.UtcNow,
            // 初始密码由平台设置
            PasswordChangeRequired = true
        }, cancellationToken);

        await _tenantUserRepository.AddAsync(new SysTenantUser
        {
            UserId = adminUser.BasicId,
            MemberType = TenantMemberType.Owner,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            RespondedTime = DateTimeOffset.UtcNow
        }, cancellationToken);

        var ownerRole = await _roleRepository.AddAsync(SysRole.CreateTenantOwnerRole(), cancellationToken);
        await _userRoleRepository.AddAsync(new SysUserRole
        {
            UserId = adminUser.BasicId,
            RoleId = ownerRole.BasicId,
            Status = ValidityStatus.Valid,
            GrantReason = "租户开通"
        }, cancellationToken);

        return adminUser;
    }

    /// <summary>
    /// 为租户分配默认版本
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分配的默认版本ID（null 表示无默认版本）</returns>
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

    /// <summary>
    /// 套餐变更（含降级）后回收越界授权：将该租户超出其当前版本权限白名单的
    /// 角色权限/用户直授权限行置为失效（保留行以供审计追溯）
    /// </summary>
    /// <remarks>
    /// 与运行时门控语义一致：版本未绑定或白名单为空（门控未启用）时不做任何回收，避免误清。
    /// 运行时门控已保证越界权限不生效，本方法负责数据层面的存量清理（REQ-5.3）。
    /// </remarks>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回收（置失效）的授权行数</returns>
    public async Task<int> ReconcileTenantAuthorizationWithEditionAsync(SysTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();

        if (tenant.EditionId is not > 0)
        {
            return 0;
        }

        // 版本白名单是平台数据，在平台作用域读
        HashSet<long> allowedIds;
        using (_currentTenant.Change(null))
        {
            var whitelist = await _tenantEditionPermissionRepository.GetByEditionIdAsync(tenant.EditionId.Value, cancellationToken);
            allowedIds = whitelist
                .Where(item => item.Status == ValidityStatus.Valid)
                .Select(item => item.PermissionId)
                .ToHashSet();
        }

        // 白名单为空视为门控未启用（与运行时鉴权门控语义一致），不做回收，避免误清
        if (allowedIds.Count == 0)
        {
            return 0;
        }

        // 授权绑定是该租户的数据，切入该租户读写；只处理该租户自有绑定行，
        // 读共享可见的全局行（TenantId=0）属平台资产，不在回收范围
        using var tenantScope = EnterTenantScope(tenant);
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

    /// <summary>
    /// 版本权限白名单收窄（撤销/停用映射）后，对绑定该版本的所有租户回收越界授权
    /// </summary>
    /// <param name="editionId">版本ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回收（置失效）的授权行数合计</returns>
    public async Task<int> ReconcileEditionTenantsAuthorizationAsync(long editionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (editionId <= 0)
        {
            return 0;
        }

        // 租户注册表是平台数据，在平台作用域读；逐个租户的回收各自切入该租户
        IReadOnlyList<SysTenant> tenants;
        using (_currentTenant.Change(null))
        {
            tenants = await _tenantRepository.GetListAsync(tenant => tenant.EditionId == editionId, cancellationToken);
        }

        var total = 0;
        foreach (var tenant in tenants)
        {
            total += await ReconcileTenantAuthorizationWithEditionAsync(tenant, cancellationToken);
        }

        return total;
    }

    /// <summary>
    /// 取待初始化管理员的租户
    /// </summary>
    /// <remarks>
    /// 任何隔离模式都是先建租户、再初始化管理员；库隔离租户在两步之间初始化独立库——开通过程中写成员、授权时
    /// 会连带写日志等租户库数据，库得先在。租户可开通（见 <see cref="EnsureProvisionable"/>）且还没有所有者时才能初始化。
    /// </remarks>
    public async Task<SysTenant> GetTenantAwaitingAdminAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId), "租户主键必须大于 0。");
        }

        SysTenant tenant;
        using (_currentTenant.Change(null))
        {
            tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
                ?? throw new UserFriendlyException("租户不存在。");
        }

        EnsureProvisionable(tenant);

        using (EnterTenantScope(tenant))
        {
            if (await _tenantUserRepository.AnyAsync(member => member.MemberType == TenantMemberType.Owner, cancellationToken))
            {
                throw new UserFriendlyException("该租户已开通管理员。");
            }
        }

        return tenant;
    }

    /// <summary>
    /// 切入目标租户作用域：该租户的数据只在该作用域内写
    /// </summary>
    private IDisposable EnterTenantScope(SysTenant tenant)
    {
        return _currentTenant.Change(tenant.BasicId, tenant.TenantName);
    }

    /// <summary>
    /// 校验租户可开通
    /// </summary>
    /// <remarks>
    /// 账号、成员关系、角色与授权固定在平台库；开通时连带写的租户数据（日志等）在租户自己的库里，
    /// 所以库隔离租户要等独立库配置完成。Schema 隔离尚未实装，一律拒绝。
    /// </remarks>
    private static void EnsureProvisionable(SysTenant tenant)
    {
        switch (tenant.IsolationMode)
        {
            case TenantIsolationMode.Field:
                return;

            case TenantIsolationMode.Database when tenant.ConfigStatus == TenantConfigStatus.Configured:
                return;

            case TenantIsolationMode.Database:
                throw new UserFriendlyException("库隔离租户要先初始化数据库，再初始化管理员。");

            default:
                throw new UserFriendlyException("暂不支持 Schema 隔离，请选择字段隔离或库隔离。");
        }
    }
}
