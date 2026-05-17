#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantProvisionDomainService
// Guid:b2c3d4e5-6f78-9012-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

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

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantProvisionDomainService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserRoleRepository userRoleRepository,
        ITenantUserRepository tenantUserRepository,
        ITenantEditionRepository tenantEditionRepository)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _userRoleRepository = userRoleRepository;
        _tenantUserRepository = tenantUserRepository;
        _tenantEditionRepository = tenantEditionRepository;
    }
    /// <inheritdoc />
    public async Task<SysUser> InitializeTenantAdminAsync(SysTenant tenant, string adminUserName, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        cancellationToken.ThrowIfCancellationRequested();

        // 创建管理员用户
        var adminUser = new SysUser
        {
            TenantId = tenant.BasicId,
            UserName = adminUserName.Trim(),
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
}
