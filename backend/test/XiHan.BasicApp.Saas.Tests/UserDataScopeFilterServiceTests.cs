// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 数据范围过滤：平台不施加；租户里成员覆盖优先（取代全部角色），没有覆盖才按启用角色取并集，
/// 角色的部门明细只对自定义档位的角色生效。
/// </summary>
public sealed class UserDataScopeFilterServiceTests
{
    private const long TenantId = 7;
    private const long CurrentUserId = 1;
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 平台没有部门与成员关系：不施加数据范围
    /// </summary>
    [Fact]
    public async Task Platform_IsUnrestricted()
    {
        var fixture = new Fixture(contextTenantId: null);
        fixture.GivenRoles((10, DataPermissionScope.SelfOnly));

        var filter = await fixture.Service.ResolveAccessibleUsersAsync(Now);

        Assert.True(filter.Unrestricted);
    }

    /// <summary>
    /// 覆盖为全部：即便角色只看本人，也不限制
    /// </summary>
    [Fact]
    public async Task Override_All_ReplacesRoles()
    {
        var fixture = new Fixture();
        fixture.GivenRoles((10, DataPermissionScope.SelfOnly));
        fixture.GivenOverride(DataPermissionScope.All);

        var filter = await fixture.Service.ResolveAccessibleUsersAsync(Now);

        Assert.True(filter.Unrestricted);
    }

    /// <summary>
    /// 覆盖为仅本人：即便角色是全部，也只看本人
    /// </summary>
    [Fact]
    public async Task Override_SelfOnly_ReplacesRoles()
    {
        var fixture = new Fixture();
        fixture.GivenRoles((10, DataPermissionScope.All));
        fixture.GivenOverride(DataPermissionScope.SelfOnly);

        var filter = await fixture.Service.ResolveAccessibleUsersAsync(Now);

        Assert.False(filter.Unrestricted);
        Assert.Equal([CurrentUserId], filter.UserIds);
    }

    /// <summary>
    /// 覆盖为自定义：只看成员自己的部门明细，角色的部门不并入
    /// </summary>
    [Fact]
    public async Task Override_Custom_UsesMemberDepartmentsOnly()
    {
        var fixture = new Fixture();
        fixture.GivenRoles((10, DataPermissionScope.Custom));
        fixture.GivenRoleRows((10, 300));
        fixture.GivenOverride(DataPermissionScope.Custom);
        fixture.GivenMemberRows(200);
        fixture.GivenDepartmentMembers(200, 21, 22);
        fixture.GivenDepartmentMembers(300, 31);

        var filter = await fixture.Service.ResolveAccessibleUsersAsync(Now);

        Assert.Equal([CurrentUserId, 21L, 22L], filter.UserIds.Order().ToArray());
    }

    /// <summary>
    /// 没有覆盖：按启用角色；非自定义档位角色残留的部门明细不生效
    /// </summary>
    [Fact]
    public async Task NoOverride_RoleRowsApplyOnlyToCustomRoles()
    {
        var fixture = new Fixture();
        fixture.GivenRoles((10, DataPermissionScope.Custom), (11, DataPermissionScope.SelfOnly));
        fixture.GivenRoleRows((10, 300), (11, 400));
        fixture.GivenDepartmentMembers(300, 31);
        fixture.GivenDepartmentMembers(400, 41);

        var filter = await fixture.Service.ResolveAccessibleUsersAsync(Now);

        Assert.Equal([CurrentUserId, 31L], filter.UserIds.Order().ToArray());
        fixture.RoleDataScopes.Verify(
            repo => repo.GetValidByRoleIdsAsync(It.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new[] { 10L })), Now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 停用角色（不在启用角色里）的部门明细不生效
    /// </summary>
    [Fact]
    public async Task NoOverride_DisabledRoleRowsAreIgnored()
    {
        var fixture = new Fixture();
        fixture.GivenUserRoleIds(10, 12);
        fixture.GivenRoles((10, DataPermissionScope.SelfOnly));
        fixture.GivenRoleRows((12, 500));
        fixture.GivenDepartmentMembers(500, 51);

        var filter = await fixture.Service.ResolveAccessibleUsersAsync(Now);

        Assert.Equal([CurrentUserId], filter.UserIds);
        fixture.RoleDataScopes.Verify(
            repo => repo.GetValidByRoleIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 过滤依赖替身
    /// </summary>
    private sealed class Fixture
    {
        private readonly List<SysRole> _roles = [];
        private readonly List<long> _userRoleIds = [];
        private readonly List<SysRoleDataScope> _roleRows = [];
        private readonly List<SysUserDataScope> _memberRows = [];
        private readonly Dictionary<long, long[]> _departmentMembers = [];
        private readonly SysTenantUser _membership = new() { TenantId = TenantId, UserId = CurrentUserId, InviteStatus = TenantMemberInviteStatus.Accepted, Status = ValidityStatus.Valid };

        public Fixture(long? contextTenantId = TenantId)
        {
            var userRoles = new Mock<IUserRoleRepository>();
            _ = userRoles
                .Setup(repo => repo.GetValidByUserIdAsync(CurrentUserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => [.. _userRoleIds.Select(roleId => new SysUserRole { UserId = CurrentUserId, RoleId = roleId })]);
            var roles = new Mock<IRoleRepository>();
            _ = roles
                .Setup(repo => repo.GetEnabledByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) => [.. _roles.Where(role => ids.Contains(role.BasicId))]);
            _ = RoleDataScopes
                .Setup(repo => repo.GetValidByRoleIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<long> ids, DateTimeOffset _, CancellationToken _) => [.. _roleRows.Where(row => ids.Contains(row.RoleId))]);
            var memberRows = new Mock<IUserDataScopeRepository>();
            _ = memberRows
                .Setup(repo => repo.GetValidByUserIdAsync(CurrentUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _memberRows);
            var userDepartments = new Mock<IUserDepartmentRepository>();
            _ = userDepartments
                .Setup(repo => repo.GetValidByUserIdAsync(CurrentUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _ = userDepartments
                .Setup(repo => repo.GetUserIdsByDepartmentIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) => [.. ids.SelectMany(id => _departmentMembers.GetValueOrDefault(id) ?? [])]);
            var tenantUsers = new Mock<ITenantUserRepository>();
            _ = tenantUsers
                .Setup(repo => repo.GetMembershipAsync(CurrentUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_membership);
            var hierarchy = new Mock<IDepartmentHierarchyDomainService>();
            _ = hierarchy
                .Setup(service => service.GetDescendantIdsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            var currentUser = new Mock<ICurrentUser>();
            _ = currentUser.SetupGet(user => user.UserId).Returns(CurrentUserId);

            Service = new UserDataScopeFilterService(
                userRoles.Object,
                roles.Object,
                RoleDataScopes.Object,
                memberRows.Object,
                userDepartments.Object,
                tenantUsers.Object,
                hierarchy.Object,
                new DataScopeDecisionDomainService(),
                currentUser.Object,
                new TestCurrentTenant(contextTenantId));
        }

        public UserDataScopeFilterService Service { get; }

        public Mock<IRoleDataScopeRepository> RoleDataScopes { get; } = new();

        public void GivenUserRoleIds(params long[] roleIds) => _userRoleIds.AddRange(roleIds);

        public void GivenRoles(params (long RoleId, DataPermissionScope Scope)[] roles)
        {
            foreach (var (roleId, scope) in roles)
            {
                var role = new SysRole { TenantId = TenantId, RoleCode = $"R{roleId}", RoleName = $"角色{roleId}", DataScope = scope, Status = EnableStatus.Enabled };
                SaasTestHelper.SetBasicId(role, roleId);
                _roles.Add(role);
                if (!_userRoleIds.Contains(roleId))
                {
                    _userRoleIds.Add(roleId);
                }
            }
        }

        public void GivenRoleRows(params (long RoleId, long DepartmentId)[] rows)
        {
            foreach (var (roleId, departmentId) in rows)
            {
                _roleRows.Add(new SysRoleDataScope { RoleId = roleId, DepartmentId = departmentId, Status = ValidityStatus.Valid });
            }
        }

        public void GivenOverride(DataPermissionScope scope) => _membership.DataScopeOverride = scope;

        public void GivenMemberRows(params long[] departmentIds)
        {
            foreach (var departmentId in departmentIds)
            {
                _memberRows.Add(new SysUserDataScope { UserId = CurrentUserId, DepartmentId = departmentId, Status = ValidityStatus.Valid });
            }
        }

        public void GivenDepartmentMembers(long departmentId, params long[] userIds) => _departmentMembers[departmentId] = userIds;
    }
}
