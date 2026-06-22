#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SuperAdminProtector
// Guid:3e9b2c47-5d18-4a6f-b0e2-9c71f4a8d530
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/22 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 超级管理员保护守卫实现。
/// </summary>
public sealed class SuperAdminProtector : ISuperAdminProtector
{
    /// <summary>
    /// 超级管理员角色编码（与种子/授权快照约定一致）。
    /// </summary>
    private const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 禁止操作统一提示。
    /// </summary>
    private const string ForbiddenMessage = "无权操作超级管理员数据。";

    private readonly ICurrentUser _currentUser;

    private readonly IRoleRepository _roleRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SuperAdminProtector(
        ICurrentUser currentUser,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository)
    {
        _currentUser = currentUser;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    /// <inheritdoc />
    public bool IsCurrentUserSuperAdmin()
    {
        return _currentUser.IsInRole(SuperAdminRoleCode);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<long>> GetProtectedRoleIdsAsync(CancellationToken cancellationToken = default)
    {
        // 写路径低频，直接查不缓存。RoleCode==super_admin 的角色（System 角色，TenantId=0）。
        var roles = await _roleRepository.GetListAsync(
            role => role.RoleCode == SuperAdminRoleCode,
            cancellationToken);

        return roles.Select(role => role.BasicId).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<long>> GetProtectedUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var roleIds = await GetProtectedRoleIdsAsync(cancellationToken);
        if (roleIds.Count == 0)
        {
            return [];
        }

        // 持有受保护角色、且授权有效（Status=Valid）的用户。
        var userRoles = await _userRoleRepository.GetListAsync(
            userRole => roleIds.Contains(userRole.RoleId) && userRole.Status == ValidityStatus.Valid,
            cancellationToken);

        return userRoles.Select(userRole => userRole.UserId).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsProtectedRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var roleIds = await GetProtectedRoleIdsAsync(cancellationToken);
        return roleIds.Contains(roleId);
    }

    /// <inheritdoc />
    public async Task<bool> IsProtectedUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var userIds = await GetProtectedUserIdsAsync(cancellationToken);
        return userIds.Contains(userId);
    }

    /// <inheritdoc />
    public async Task EnsureCanWriteRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        if (IsCurrentUserSuperAdmin())
        {
            return;
        }

        if (await IsProtectedRoleAsync(roleId, cancellationToken))
        {
            throw new UserFriendlyException(ForbiddenMessage);
        }
    }

    /// <inheritdoc />
    public async Task EnsureCanWriteUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (IsCurrentUserSuperAdmin())
        {
            return;
        }

        if (await IsProtectedUserAsync(userId, cancellationToken))
        {
            throw new UserFriendlyException(ForbiddenMessage);
        }
    }

    /// <inheritdoc />
    public async Task EnsureCanAssignRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        if (IsCurrentUserSuperAdmin())
        {
            return;
        }

        // 禁止非超管授予/撤销 super_admin 角色。
        if (await IsProtectedRoleAsync(roleId, cancellationToken))
        {
            throw new UserFriendlyException(ForbiddenMessage);
        }
    }
}
