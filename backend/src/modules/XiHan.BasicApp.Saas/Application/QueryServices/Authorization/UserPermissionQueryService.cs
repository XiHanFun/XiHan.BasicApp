#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionQueryService
// Guid:b3c10a62-fdf8-4550-b3ab-59ba8841ac29
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
/// 用户直授权限查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户直授权限")]
public sealed class UserPermissionQueryService(
    IUserPermissionRepository userPermissionRepository,
    IPermissionRepository permissionRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserPermissionQueryService
{
    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取用户直授权限列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回当前有效直授权限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Read)]
    public async Task<IReadOnlyList<UserPermissionListItemDto>> GetUserPermissionsAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        var now = DateTimeOffset.UtcNow;
        var userPermissions = onlyValid
            ? await _userPermissionRepository.GetValidByUserIdAsync(userId, now, cancellationToken)
            : await _userPermissionRepository.GetListAsync(
                userPermission => userPermission.UserId == userId,
                userPermission => userPermission.CreatedTime,
                cancellationToken);

        if (userPermissions.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(userPermissions.Select(userPermission => userPermission.PermissionId), cancellationToken);

        return [.. userPermissions
            .Select(userPermission => UserPermissionApplicationMapper.ToListItemDto(
                userPermission,
                permissionMap.GetValueOrDefault(userPermission.PermissionId),
                tenantMember,
                now))
            .OrderBy(item => item.PermissionCode)
            .ThenBy(item => item.PermissionId)];
    }

    /// <summary>
    /// 获取用户直授权限详情
    /// </summary>
    /// <param name="id">用户直授权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Read)]
    public async Task<UserPermissionDetailDto?> GetUserPermissionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户直授权限绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userPermission = await _userPermissionRepository.GetByIdAsync(id, cancellationToken);
        if (userPermission is null)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(userPermission, permission, tenantMember, DateTimeOffset.UtcNow);
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
