#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAppService
// Guid:467e4fc7-d206-418c-bf77-5b2e1cd252ec
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 权限应用服务
/// </summary>
public class PermissionAppService : ApplicationServiceBase, IPermissionAppService
{
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionRepository"></param>
    public PermissionAppService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 根据权限ID获取权限
    /// </summary>
    /// <param name="permissionId"></param>
    /// <returns></returns>
    public async Task<PermissionDto?> GetByIdAsync(long permissionId)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId);
        return permission?.Adapt<PermissionDto>();
    }

    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null)
    {
        var permissions = await _permissionRepository.GetRolePermissionsAsync(roleId, tenantId);
        return permissions.Select(static permission => permission.Adapt<PermissionDto>()).ToArray();
    }

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<PermissionDto>> GetUserPermissionsAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var permissions = await _permissionRepository.GetUserPermissionsAsync(query.UserId, query.TenantId);
        return permissions.Select(static permission => permission.Adapt<PermissionDto>()).ToArray();
    }
}
