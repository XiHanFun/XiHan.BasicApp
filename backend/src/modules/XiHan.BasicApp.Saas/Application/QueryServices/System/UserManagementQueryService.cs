#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserManagementQueryService
// Guid:473aee9f-393a-4918-8edb-af8b81bde9d6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
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
/// 账号管理页面查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "账号管理")]
public sealed class UserManagementQueryService
    : SaasApplicationService, IUserManagementQueryService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserManagementQueryService(
        IUserRepository userRepository,
        ITenantUserRepository tenantUserRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IDepartmentRepository departmentRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IUserDataScopeRepository userDataScopeRepository,
        IUserSecurityRepository userSecurityRepository,
        IUserSessionRepository userSessionRepository,
        IUserStatisticsRepository userStatisticsRepository,
        IExternalLoginRepository externalLoginRepository,
        IPasswordHistoryRepository passwordHistoryRepository)
    {
        _userRepository = userRepository;
        _tenantUserRepository = tenantUserRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _departmentRepository = departmentRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _userDataScopeRepository = userDataScopeRepository;
        _userSecurityRepository = userSecurityRepository;
        _userSessionRepository = userSessionRepository;
        _userStatisticsRepository = userStatisticsRepository;
        _externalLoginRepository = externalLoginRepository;
        _passwordHistoryRepository = passwordHistoryRepository;
    }

    private const int MaxSessionCount = 20;
    private const int MaxStatisticsCount = 8;
    private const int MaxPasswordHistoryCount = 10;
    private readonly IUserRepository _userRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserDepartmentRepository _userDepartmentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserDataScopeRepository _userDataScopeRepository;
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IUserStatisticsRepository _userStatisticsRepository;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.User.Read)]
    public async Task<UserManagementDetailDto?> GetUserManagementDetailAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);

        return new UserManagementDetailDto
        {
            User = UserApplicationMapper.ToDetailDto(user),
            TenantMembership = tenantMember is null ? null : TenantMemberApplicationMapper.ToListItemDto(tenantMember, now),
            Departments = await GetDepartmentsAsync(user.BasicId, cancellationToken),
            Roles = await GetRolesAsync(user.BasicId, tenantMember, now, cancellationToken),
            Permissions = await GetPermissionsAsync(user.BasicId, tenantMember, now, cancellationToken),
            DataScopes = await GetDataScopesAsync(user.BasicId, tenantMember, cancellationToken),
            Security = await GetSecurityAsync(user, now, cancellationToken),
            Sessions = await GetSessionsAsync(user, now, cancellationToken),
            Statistics = await GetStatisticsAsync(user, cancellationToken),
            ExternalLogins = await GetExternalLoginsAsync(user, cancellationToken),
            PasswordHistories = await GetPasswordHistoriesAsync(user, cancellationToken),
            GeneratedTime = now
        };
    }

    private async Task<List<UserDepartmentListItemDto>> GetDepartmentsAsync(long userId, CancellationToken cancellationToken)
    {
        var userDepartments = await _userDepartmentRepository.GetListAsync(
            item => item.UserId == userId,
            item => item.CreatedTime,
            cancellationToken);

        if (userDepartments.Count == 0)
        {
            return [];
        }

        var departmentMap = await BuildDepartmentMapAsync(userDepartments.Select(item => item.DepartmentId), cancellationToken);

        return [.. userDepartments
            .Select(item => UserDepartmentApplicationMapper.ToListItemDto(
                item,
                departmentMap.GetValueOrDefault(item.DepartmentId)))
            .OrderByDescending(item => item.IsMain)
            .ThenBy(item => item.DepartmentCode)
            .ThenBy(item => item.DepartmentId)];
    }

    private async Task<List<UserRoleListItemDto>> GetRolesAsync(
        long userId,
        SysTenantUser? tenantMember,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.GetListAsync(
            item => item.UserId == userId,
            item => item.CreatedTime,
            cancellationToken);

        if (userRoles.Count == 0)
        {
            return [];
        }

        var roleMap = await BuildRoleMapAsync(userRoles.Select(item => item.RoleId), cancellationToken);

        return [.. userRoles
            .Select(item => UserRoleApplicationMapper.ToListItemDto(
                item,
                roleMap.GetValueOrDefault(item.RoleId),
                tenantMember,
                now))
            .OrderBy(item => item.RoleCode)
            .ThenBy(item => item.RoleId)];
    }

    private async Task<List<UserPermissionListItemDto>> GetPermissionsAsync(
        long userId,
        SysTenantUser? tenantMember,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var userPermissions = await _userPermissionRepository.GetListAsync(
            item => item.UserId == userId,
            item => item.CreatedTime,
            cancellationToken);

        if (userPermissions.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(userPermissions.Select(item => item.PermissionId), cancellationToken);

        return [.. userPermissions
            .Select(item => UserPermissionApplicationMapper.ToListItemDto(
                item,
                permissionMap.GetValueOrDefault(item.PermissionId),
                tenantMember,
                now))
            .OrderBy(item => item.PermissionCode)
            .ThenBy(item => item.PermissionId)];
    }

    private async Task<List<UserDataScopeListItemDto>> GetDataScopesAsync(
        long userId,
        SysTenantUser? tenantMember,
        CancellationToken cancellationToken)
    {
        var dataScopes = await _userDataScopeRepository.GetListAsync(
            item => item.UserId == userId,
            item => item.CreatedTime,
            cancellationToken);

        if (dataScopes.Count == 0)
        {
            return [];
        }

        var departmentMap = await BuildDepartmentMapAsync(dataScopes.Select(item => item.DepartmentId), cancellationToken);

        return [.. dataScopes
            .Select(item => UserDataScopeApplicationMapper.ToListItemDto(
                item,
                departmentMap.GetValueOrDefault(item.DepartmentId),
                tenantMember))
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.DepartmentId)];
    }

    private async Task<UserSecurityDetailDto?> GetSecurityAsync(SysUser user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(
            item => item.UserId == user.BasicId,
            cancellationToken);

        return security is null
            ? null
            : UserSecurityApplicationMapper.ToDetailDto(security, user, now);
    }

    private async Task<List<UserSessionListItemDto>> GetSessionsAsync(SysUser user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sessions = await _userSessionRepository.GetListAsync(
            item => item.UserId == user.BasicId,
            item => item.LastActivityTime,
            cancellationToken);

        return [.. sessions
            .Select(item => UserSessionApplicationMapper.ToListItemDto(item, user, now))
            .OrderByDescending(item => item.IsOnline)
            .ThenByDescending(item => item.LastActivityTime)
            .ThenByDescending(item => item.LoginTime)
            .Take(MaxSessionCount)];
    }

    private async Task<List<UserStatisticsDetailDto>> GetStatisticsAsync(SysUser user, CancellationToken cancellationToken)
    {
        var statistics = await _userStatisticsRepository.GetListAsync(
            item => item.UserId == user.BasicId,
            item => item.StatisticsDate,
            cancellationToken);

        return [.. statistics
            .Select(item => UserStatisticsApplicationMapper.ToDetailDto(item, user))
            .OrderByDescending(item => item.StatisticsDate)
            .ThenBy(item => item.Period)
            .Take(MaxStatisticsCount)];
    }

    private async Task<List<ExternalLoginListItemDto>> GetExternalLoginsAsync(SysUser user, CancellationToken cancellationToken)
    {
        var externalLogins = await _externalLoginRepository.GetListAsync(
            item => item.UserId == user.BasicId,
            item => item.CreatedTime,
            cancellationToken);

        return [.. externalLogins
            .Select(item => ExternalLoginApplicationMapper.ToListItemDto(item, user))
            .OrderBy(item => item.Provider)
            .ThenByDescending(item => item.LastLoginTime)
            .ThenByDescending(item => item.CreatedTime)];
    }

    private async Task<List<PasswordHistoryListItemDto>> GetPasswordHistoriesAsync(SysUser user, CancellationToken cancellationToken)
    {
        var passwordHistories = await _passwordHistoryRepository.GetListAsync(
            item => item.UserId == user.BasicId,
            item => item.ChangedTime,
            cancellationToken);

        return [.. passwordHistories
            .Select(item => PasswordHistoryApplicationMapper.ToListItemDto(item, user))
            .OrderByDescending(item => item.ChangedTime)
            .ThenByDescending(item => item.CreatedTime)
            .Take(MaxPasswordHistoryCount)];
    }

    private async Task<IReadOnlyDictionary<long, SysDepartment>> BuildDepartmentMapAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken)
    {
        var ids = departmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysDepartment>();
        }

        var departments = await _departmentRepository.GetByIdsAsync(ids, cancellationToken);
        return departments.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(item => item.BasicId);
    }
}
