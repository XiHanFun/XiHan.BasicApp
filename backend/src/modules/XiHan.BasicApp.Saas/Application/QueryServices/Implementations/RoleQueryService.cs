#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleQueryService
// Guid:c9f5a3b2-4d7e-5f6a-0b2c-3d4e5f6a7b82
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Caching.Attributes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 角色查询服务
/// </summary>
public class RoleQueryService : IRoleQueryService, ITransientDependency
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleQueryService(IRoleRepository roleRepository, IRoleHierarchyRepository roleHierarchyRepository)
    {
        _roleRepository = roleRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.RoleById, ExpireSeconds = 300)]
    public async Task<RoleDto?> GetByIdAsync(long id)
    {
        var entity = await _roleRepository.GetByIdAsync(id);
        return entity is null ? null : SaasReadModelMapper.MapRole(entity);
    }

    /// <inheritdoc />
    public async Task<RoleDto?> GetByCodeAsync(string roleCode, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var entity = await _roleRepository.GetByRoleCodeAsync(roleCode, tenantId);
        return entity is null ? null : SaasReadModelMapper.MapRole(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RolePermissionRelationDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        var relations = await _roleRepository.GetRolePermissionsAsync(roleId, tenantId);
        return relations.Select(relation => new RolePermissionRelationDto
        {
            TenantId = relation.TenantId,
            RoleId = relation.RoleId,
            PermissionId = relation.PermissionId,
            Status = relation.Status
        }).ToArray();
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, long? tenantId = null)
    {
        return _roleRepository.GetCustomDataScopeDepartmentIdsAsync(roleId, tenantId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<long>> GetRoleParentRoleIdsAsync(long roleId, long? tenantId = null)
    {
        return _roleHierarchyRepository.GetDirectParentRoleIdsAsync(roleId, tenantId);
    }
}
