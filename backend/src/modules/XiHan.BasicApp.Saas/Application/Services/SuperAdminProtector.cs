// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 当前用户是否为超级管理员（持有 <c>super_admin</c> 角色）。
    /// </summary>
    public bool IsCurrentUserSuperAdmin()
    {
        return _currentUser.IsInRole(SuperAdminRoleCode);
    }

    /// <summary>
    /// 获取受保护角色 id 集合（RoleCode == <c>super_admin</c> 的角色）。
    /// </summary>
    public async Task<IReadOnlyCollection<long>> GetProtectedRoleIdsAsync(CancellationToken cancellationToken = default)
    {
        // 写路径低频，直接查不缓存。RoleCode==super_admin 的角色（System 角色，TenantId=0）。
        var roles = await _roleRepository.GetListAsync(
            role => role.RoleCode == SuperAdminRoleCode,
            cancellationToken);

        return roles.Select(role => role.BasicId).Distinct().ToList();
    }

    /// <summary>
    /// 获取受保护用户 id 集合（持有受保护角色、且授权有效的用户）。
    /// </summary>
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

    /// <summary>
    /// 指定角色是否受保护（是否为 <c>super_admin</c> 角色）。
    /// </summary>
    public async Task<bool> IsProtectedRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var roleIds = await GetProtectedRoleIdsAsync(cancellationToken);
        return roleIds.Contains(roleId);
    }

    /// <summary>
    /// 指定用户是否受保护（是否为超管用户）。
    /// </summary>
    public async Task<bool> IsProtectedUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var userIds = await GetProtectedUserIdsAsync(cancellationToken);
        return userIds.Contains(userId);
    }

    /// <summary>
    /// 校验当前用户可对指定角色执行写操作；非超管且该角色受保护时抛出禁止异常。
    /// </summary>
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

    /// <summary>
    /// 校验当前用户可对指定用户执行写操作；非超管且该用户受保护时抛出禁止异常。
    /// </summary>
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

    /// <summary>
    /// 校验当前用户可对指定角色执行授予/撤销操作；非超管且该角色为 <c>super_admin</c> 时抛出禁止异常。
    /// </summary>
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
