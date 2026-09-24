// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户数据范围过滤服务实现
/// </summary>
/// <remarks>
/// 数据范围是租户侧概念：部门、成员关系都属于租户，平台不施加数据范围（平台的可见性由权限码与作用侧决定）。
/// 租户里先看成员关系上的覆盖：有覆盖就只按覆盖（自定义时取成员自己的部门明细）；没有覆盖才按角色——
/// 启用角色的档位取并集，自定义档位的角色再并入它的部门明细。
/// </remarks>
public sealed class UserDataScopeFilterService : IUserDataScopeFilterService
{
    private readonly ICurrentUser _currentUser;

    private readonly ICurrentTenant _currentTenant;

    private readonly IDataScopeDecisionDomainService _dataScopeDecision;

    private readonly IDepartmentHierarchyDomainService _departmentHierarchy;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    private readonly IRoleRepository _roleRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserDataScopeRepository _userDataScopeRepository;

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDataScopeFilterService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRoleDataScopeRepository roleDataScopeRepository,
        IUserDataScopeRepository userDataScopeRepository,
        IUserDepartmentRepository userDepartmentRepository,
        ITenantUserRepository tenantUserRepository,
        IDepartmentHierarchyDomainService departmentHierarchy,
        IDataScopeDecisionDomainService dataScopeDecision,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _userDataScopeRepository = userDataScopeRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _tenantUserRepository = tenantUserRepository;
        _departmentHierarchy = departmentHierarchy;
        _dataScopeDecision = dataScopeDecision;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 解析当前用户可见的用户主键集合。
    /// </summary>
    public async Task<UserDataScopeFilter> ResolveAccessibleUsersAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 无登录上下文（系统/内部调用）不施加数据范围
        if (_currentUser.UserId is not { } currentUserId || currentUserId <= 0)
        {
            return UserDataScopeFilter.Unlimited;
        }

        if (_currentTenant.IsPlatformOperation())
        {
            return UserDataScopeFilter.Unlimited;
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(currentUserId, cancellationToken);
        var grants = membership?.DataScopeOverride is { } overrideScope
            ? await ResolveMemberGrantsAsync(membership, overrideScope, cancellationToken)
            : await ResolveRoleGrantsAsync(currentUserId, now, cancellationToken);

        var userDepartments = await _userDepartmentRepository.GetValidByUserIdAsync(currentUserId, cancellationToken);
        var userDepartmentIds = userDepartments.Select(item => item.DepartmentId).ToList();

        var decision = _dataScopeDecision.Decide(grants, userDepartmentIds, now);
        if (decision.AllowsAllData)
        {
            return UserDataScopeFilter.Unlimited;
        }

        // 汇总可见部门：直接部门 + 含子部门（按层级展开后代）
        var departmentIds = new HashSet<long>(decision.DepartmentIds);
        foreach (var departmentId in decision.DepartmentAndChildrenIds)
        {
            departmentIds.Add(departmentId);
            departmentIds.UnionWith(await _departmentHierarchy.GetDescendantIdsAsync(departmentId, cancellationToken));
        }

        // 本人始终可见；再并入可见部门下的成员
        var accessibleUserIds = new HashSet<long> { currentUserId };
        if (departmentIds.Count > 0)
        {
            var memberIds = await _userDepartmentRepository.GetUserIdsByDepartmentIdsAsync(departmentIds, cancellationToken);
            accessibleUserIds.UnionWith(memberIds);
        }

        return new UserDataScopeFilter(false, accessibleUserIds);
    }

    /// <summary>
    /// 成员覆盖：只按覆盖档位，自定义时取成员在本租户的部门明细
    /// </summary>
    private async Task<List<DataScopeGrantSnapshot>> ResolveMemberGrantsAsync(
        SysTenantUser membership,
        DataPermissionScope scope,
        CancellationToken cancellationToken)
    {
        if (scope != DataPermissionScope.Custom)
        {
            return [new DataScopeGrantSnapshot(membership.BasicId, AuthorizationGrantSource.User, scope, [], IncludeChildren: false, EffectivePeriod.Always)];
        }

        var rows = await _userDataScopeRepository.GetValidByUserIdAsync(membership.UserId, cancellationToken);
        return [.. rows.Select(row => new DataScopeGrantSnapshot(
            row.BasicId,
            AuthorizationGrantSource.User,
            DataPermissionScope.Custom,
            [row.DepartmentId],
            row.IncludeChildren,
            EffectivePeriod.Always))];
    }

    /// <summary>
    /// 按角色：启用角色的档位取并集，自定义档位的角色并入它的部门明细
    /// </summary>
    private async Task<List<DataScopeGrantSnapshot>> ResolveRoleGrantsAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken);
        var roleIds = userRoles.Select(item => item.RoleId).Distinct().ToList();
        if (roleIds.Count == 0)
        {
            return [];
        }

        var roles = await _roleRepository.GetEnabledByIdsAsync(roleIds, cancellationToken);
        var grants = roles
            .Where(role => role.DataScope != DataPermissionScope.Custom)
            .Select(role => new DataScopeGrantSnapshot(
                role.BasicId,
                AuthorizationGrantSource.Role,
                role.DataScope,
                [],
                IncludeChildren: false,
                EffectivePeriod.Always))
            .ToList();

        var customRoleIds = roles
            .Where(role => role.DataScope == DataPermissionScope.Custom)
            .Select(role => role.BasicId)
            .ToList();
        if (customRoleIds.Count > 0)
        {
            var rows = await _roleDataScopeRepository.GetValidByRoleIdsAsync(customRoleIds, now, cancellationToken);
            grants.AddRange(rows.Select(row => new DataScopeGrantSnapshot(
                row.BasicId,
                AuthorizationGrantSource.Role,
                DataPermissionScope.Custom,
                [row.DepartmentId],
                row.IncludeChildren,
                new EffectivePeriod(row.EffectiveTime, row.ExpirationTime),
                row.Status == ValidityStatus.Valid)));
        }

        return grants;
    }
}
