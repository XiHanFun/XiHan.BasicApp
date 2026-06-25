#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeFilterService
// Guid:2a3b4c5d-6e7f-4081-9b2c-d3e4f5a6b712
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户数据范围过滤服务实现
/// </summary>
public sealed class UserDataScopeFilterService : IUserDataScopeFilterService
{
    private const string SuperAdminRoleCode = "super_admin";

    private readonly ICurrentUser _currentUser;

    private readonly IDataScopeDecisionDomainService _dataScopeDecision;

    private readonly IDepartmentHierarchyDomainService _departmentHierarchy;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    private readonly IRoleRepository _roleRepository;

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
        IDepartmentHierarchyDomainService departmentHierarchy,
        IDataScopeDecisionDomainService dataScopeDecision,
        ICurrentUser currentUser)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _userDataScopeRepository = userDataScopeRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _departmentHierarchy = departmentHierarchy;
        _dataScopeDecision = dataScopeDecision;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<UserDataScopeFilter> ResolveAccessibleUsersAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 无登录上下文（系统/内部调用）不施加数据范围
        if (_currentUser.UserId is not { } currentUserId || currentUserId <= 0)
        {
            return UserDataScopeFilter.Unlimited;
        }

        var userRoles = await _userRoleRepository.GetValidByUserIdAsync(currentUserId, now, cancellationToken);
        var roleIds = userRoles.Select(item => item.RoleId).Distinct().ToList();

        var roles = roleIds.Count == 0
            ? []
            : await _roleRepository.GetEnabledByIdsAsync(roleIds, cancellationToken);
        if (roles.Any(role => string.Equals(role.RoleCode, SuperAdminRoleCode, StringComparison.OrdinalIgnoreCase)))
        {
            return UserDataScopeFilter.Unlimited;
        }

        // 角色级数据范围：每个角色贡献其 DataScope 语义；Custom 部门明细来自 RoleDataScope 行
        var grants = new List<DataScopeGrantSnapshot>();
        foreach (var role in roles)
        {
            grants.Add(new DataScopeGrantSnapshot(
                role.BasicId,
                AuthorizationGrantSource.Role,
                role.DataScope,
                [],
                IncludeChildren: false,
                EffectivePeriod.Always));
        }

        if (roleIds.Count > 0)
        {
            var roleDataScopeRows = await _roleDataScopeRepository.GetValidByRoleIdsAsync(roleIds, now, cancellationToken);
            foreach (var row in roleDataScopeRows)
            {
                grants.Add(new DataScopeGrantSnapshot(
                    row.BasicId,
                    AuthorizationGrantSource.Role,
                    DataPermissionScope.Custom,
                    [row.DepartmentId],
                    row.IncludeChildren,
                    new EffectivePeriod(row.EffectiveTime, row.ExpirationTime),
                    row.Status == ValidityStatus.Valid));
            }
        }

        // 用户级数据范围（直授自定义部门）
        var userDataScopeRows = await _userDataScopeRepository.GetValidByUserIdAsync(currentUserId, cancellationToken);
        foreach (var row in userDataScopeRows)
        {
            grants.Add(new DataScopeGrantSnapshot(
                row.BasicId,
                AuthorizationGrantSource.User,
                DataPermissionScope.Custom,
                [row.DepartmentId],
                row.IncludeChildren,
                EffectivePeriod.Always));
        }

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
}
