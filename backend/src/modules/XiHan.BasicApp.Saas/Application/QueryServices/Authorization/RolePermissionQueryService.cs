#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionQueryService
// Guid:f2a5ff15-7f8e-4c90-a3e8-6bbad64e83bc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 角色权限查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色权限")]
public sealed class RolePermissionQueryService(
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository,
    IPermissionRepository permissionRepository)
    : SaasApplicationService, IRolePermissionQueryService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 获取角色权限列表
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="onlyValid">是否仅返回有效授权</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Read)]
    public async Task<IReadOnlyList<RolePermissionListItemDto>> GetRolePermissionsAsync(long roleId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _ = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        var rolePermissions = onlyValid
            ? await _rolePermissionRepository.GetValidByRoleIdsAsync([roleId], DateTimeOffset.UtcNow, cancellationToken)
            : await _rolePermissionRepository.GetListAsync(
                rolePermission => rolePermission.RoleId == roleId,
                rolePermission => rolePermission.CreatedTime,
                cancellationToken);

        if (rolePermissions.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(rolePermissions.Select(rolePermission => rolePermission.PermissionId), cancellationToken);

        return [.. rolePermissions
            .Select(rolePermission => RolePermissionApplicationMapper.ToListItemDto(
                rolePermission,
                permissionMap.GetValueOrDefault(rolePermission.PermissionId)))
            .OrderBy(item => item.PermissionCode)
            .ThenBy(item => item.PermissionId)];
    }

    /// <summary>
    /// 获取角色权限详情
    /// </summary>
    /// <param name="id">角色权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Read)]
    public async Task<RolePermissionDetailDto?> GetRolePermissionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色权限绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var rolePermission = await _rolePermissionRepository.GetByIdAsync(id, cancellationToken);
        if (rolePermission is null)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(rolePermission, permission);
    }

    /// <summary>
    /// 构建权限定义映射
    /// </summary>
    /// <param name="permissionIds">权限主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限定义映射</returns>
    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(permissionId => permissionId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(permission => permission.BasicId);
    }
}
