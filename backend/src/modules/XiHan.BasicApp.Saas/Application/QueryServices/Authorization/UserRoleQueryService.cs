#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRoleQueryService
// Guid:6a37e3a1-c7c1-4a45-8e84-d1d74f51647d
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
/// 用户角色查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户角色")]
public sealed class UserRoleQueryService(
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserRoleQueryService
{
    /// <summary>
    /// 用户角色仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回当前有效角色授权</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Read)]
    public async Task<IReadOnlyList<UserRoleListItemDto>> GetUserRolesAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        var now = DateTimeOffset.UtcNow;
        var userRoles = onlyValid
            ? await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken)
            : await _userRoleRepository.GetListAsync(
                userRole => userRole.UserId == userId,
                userRole => userRole.CreatedTime,
                cancellationToken);

        if (userRoles.Count == 0)
        {
            return [];
        }

        var roleMap = await BuildRoleMapAsync(userRoles.Select(userRole => userRole.RoleId), cancellationToken);

        return [.. userRoles
            .Select(userRole => UserRoleApplicationMapper.ToListItemDto(
                userRole,
                roleMap.GetValueOrDefault(userRole.RoleId),
                tenantMember,
                now))
            .OrderBy(item => item.RoleCode)
            .ThenBy(item => item.RoleId)];
    }

    /// <summary>
    /// 获取用户角色详情
    /// </summary>
    /// <param name="id">用户角色绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Read)]
    public async Task<UserRoleDetailDto?> GetUserRoleDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户角色绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userRole = await _userRoleRepository.GetByIdAsync(id, cancellationToken);
        if (userRole is null)
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userRole.UserId, cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(userRole, role, tenantMember, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建角色映射
    /// </summary>
    /// <param name="roleIds">角色主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色映射</returns>
    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(roleId => roleId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(role => role.BasicId);
    }
}
