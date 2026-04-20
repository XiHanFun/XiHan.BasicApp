#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleResolutionDomainService
// Guid:e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 角色解析领域服务实现
/// </summary>
public class RoleResolutionDomainService : IRoleResolutionDomainService, IScopedDependency
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleResolutionDomainService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRoleHierarchyRepository roleHierarchyRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUserRoleCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        var roles = await GetEffectiveRolesAsync(userId, tenantId, cancellationToken);
        return roles.Select(r => r.RoleCode).Distinct().ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetEffectiveRoleIdsAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        var directRoles = await _userRepository.GetUserRolesAsync(userId, tenantId, cancellationToken);
        var directRoleIds = directRoles.Select(r => r.RoleId).Distinct().ToArray();

        if (directRoleIds.Length == 0)
            return [];

        var inheritedRoleIds = await _roleHierarchyRepository.GetInheritedRoleIdsAsync(directRoleIds, cancellationToken);
        return inheritedRoleIds.Distinct().ToArray();
    }

    private async Task<IReadOnlyList<SysRole>> GetEffectiveRolesAsync(long userId, long? tenantId, CancellationToken cancellationToken)
    {
        var effectiveRoleIds = await GetEffectiveRoleIdsAsync(userId, tenantId, cancellationToken);
        if (effectiveRoleIds.Count == 0)
            return [];

        var roles = await _roleRepository.GetByIdsAsync(effectiveRoleIds, cancellationToken);
        return roles.Where(r => r.Status == YesOrNo.Yes).ToArray();
    }
}
