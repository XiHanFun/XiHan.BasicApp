#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionQueryService
// Guid:5d6e7f80-9102-1234-def0-123456789a02
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
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限查询服务
/// </summary>
public class PermissionQueryService : IPermissionQueryService, ITransientDependency
{
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionQueryService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.PermissionById, ExpireSeconds = 300)]
    public async Task<PermissionDto?> GetByIdAsync(long id)
    {
        var entity = await _permissionRepository.GetByIdAsync(id);
        return entity is null ? null : SaasReadModelMapper.MapPermission(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        var permissions = await _permissionRepository.GetRolePermissionsAsync(roleId, tenantId);
        return permissions.Select(SaasReadModelMapper.MapPermission).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionDto>> GetUserPermissionsAsync(long userId, long? tenantId = null)
    {
        var permissions = await _permissionRepository.GetUserPermissionsAsync(userId, tenantId);
        return permissions.Select(SaasReadModelMapper.MapPermission).ToArray();
    }
}
