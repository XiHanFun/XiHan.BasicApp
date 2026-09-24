// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 角色与用户数据范围批量变更：授予与撤销一次提交，只对自定义范围的对象开放，撤销只置失效、历史行就地复用。
/// </summary>
public sealed class DataScopeBatchUpdateTests
{
    private const long RoleId = 5;

    private const long UserId = 1;

    #region 角色数据范围

    /// <summary>
    /// 从未授予过的部门新增一行有效范围，含下级按授予项写入。
    /// </summary>
    [Fact]
    public async Task RoleBatchUpdate_NewDepartment_ShouldAddValidScope()
    {
        var fixture = new RoleFixture();

        var result = await fixture.GrantAsync(10, includeChildren: true);

        var added = Assert.Single(fixture.Added);
        Assert.Equal(RoleId, added.RoleId);
        Assert.True(added.IncludeChildren);
        Assert.Equal(ValidityStatus.Valid, added.Status);
        Assert.Equal([10L], result.GrantedDepartmentIds);
    }

    /// <summary>
    /// 已过期的历史行再授予：复用那一行并清掉时间窗，否则保存成功却不生效。
    /// </summary>
    [Fact]
    public async Task RoleBatchUpdate_ExpiredScope_ShouldReactivateAndClearTimeWindow()
    {
        var fixture = new RoleFixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10);
        row.EffectiveTime = DateTimeOffset.UtcNow.AddDays(-30);
        row.ExpirationTime = DateTimeOffset.UtcNow.AddDays(-1);

        var result = await fixture.GrantAsync(10, includeChildren: false);

        Assert.Empty(fixture.Added);
        Assert.Same(row, Assert.Single(fixture.Updated));
        Assert.Null(row.EffectiveTime);
        Assert.Null(row.ExpirationTime);
        Assert.Equal([10L], result.GrantedDepartmentIds);
    }

    /// <summary>
    /// 已生效且含下级一致：不写库、不算变更。
    /// </summary>
    [Fact]
    public async Task RoleBatchUpdate_SameEffectiveScope_ShouldBeNoChange()
    {
        var fixture = new RoleFixture();
        fixture.AddRow(basicId: 100, departmentId: 10, includeChildren: true);

        var result = await fixture.GrantAsync(10, includeChildren: true);

        Assert.Empty(result.GrantedDepartmentIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 已授予的部门再次下发即改含下级。
    /// </summary>
    [Fact]
    public async Task RoleBatchUpdate_ChangeIncludeChildren_ShouldUpdateInPlace()
    {
        var fixture = new RoleFixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10, includeChildren: false);

        _ = await fixture.GrantAsync(10, includeChildren: true);

        Assert.True(row.IncludeChildren);
        Assert.Same(row, Assert.Single(fixture.Updated));
    }

    /// <summary>
    /// 撤销只认本角色名下的有效记录；同一部门既撤又授时以授予为准。
    /// </summary>
    [Fact]
    public async Task RoleBatchUpdate_Revoke_ShouldSkipForeignRowsAndLoseToGrant()
    {
        var fixture = new RoleFixture();
        var own = fixture.AddRow(basicId: 100, departmentId: 10);
        var kept = fixture.AddRow(basicId: 101, departmentId: 20);
        var others = fixture.AddRow(basicId: 200, departmentId: 10, roleId: 6);

        var result = await fixture.Service.BatchUpdateRoleDataScopesAsync(new RoleDataScopeBatchUpdateCommand(
            RoleId,
            [new RoleDataScopeBatchGrantItem(20, IncludeChildren: false)],
            [100, 101, 200]));

        Assert.Equal(ValidityStatus.Invalid, own.Status);
        Assert.Equal(ValidityStatus.Valid, kept.Status);
        Assert.Equal(ValidityStatus.Valid, others.Status);
        Assert.Equal([10L], result.RevokedDepartmentIds);
    }

    /// <summary>
    /// 非自定义数据权限范围的角色不能维护部门范围。
    /// </summary>
    [Fact]
    public async Task RoleBatchUpdate_NonCustomRole_ShouldReject()
    {
        var fixture = new RoleFixture(DataPermissionScope.DepartmentOnly);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.GrantAsync(10, includeChildren: false));

        Assert.Contains("自定义", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    #endregion

    #region 用户数据范围

    /// <summary>
    /// 从未授予过的部门新增一行有效范围。
    /// </summary>
    [Fact]
    public async Task UserBatchUpdate_NewDepartment_ShouldAddValidScope()
    {
        var fixture = new UserFixture();

        var result = await fixture.GrantAsync(10, includeChildren: true);

        var added = Assert.Single(fixture.Added);
        Assert.Equal(UserId, added.UserId);
        Assert.True(added.IncludeChildren);
        Assert.Equal([10L], result.GrantedDepartmentIds);
    }

    /// <summary>
    /// 撤销过的部门再授予：复用那一行。
    /// </summary>
    [Fact]
    public async Task UserBatchUpdate_RevokedScope_ShouldReactivateExistingRow()
    {
        var fixture = new UserFixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10, status: ValidityStatus.Invalid);

        _ = await fixture.GrantAsync(10, includeChildren: false);

        Assert.Empty(fixture.Added);
        Assert.Same(row, Assert.Single(fixture.Updated));
        Assert.Equal(ValidityStatus.Valid, row.Status);
    }

    /// <summary>
    /// 撤销只置为失效，只认本用户名下的有效记录。
    /// </summary>
    [Fact]
    public async Task UserBatchUpdate_Revoke_ShouldInvalidateOnlyOwnValidRows()
    {
        var fixture = new UserFixture();
        var own = fixture.AddRow(basicId: 100, departmentId: 10);
        var others = fixture.AddRow(basicId: 200, departmentId: 10, userId: 2);

        var result = await fixture.Service.BatchUpdateUserDataScopesAsync(new UserDataScopeBatchUpdateCommand(UserId, [], [100, 200]));

        Assert.Equal(ValidityStatus.Invalid, own.Status);
        Assert.Equal(ValidityStatus.Valid, others.Status);
        Assert.Equal([10L], result.RevokedDepartmentIds);
    }

    /// <summary>
    /// 数据权限范围未覆盖为自定义的用户不能维护部门范围。
    /// </summary>
    [Fact]
    public async Task UserBatchUpdate_NonCustomUser_ShouldReject()
    {
        var fixture = new UserFixture(dataScopeOverride: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.GrantAsync(10, includeChildren: false));

        Assert.Contains("自定义", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 空提交直接返回，不查成员、不写库。
    /// </summary>
    [Fact]
    public async Task UserBatchUpdate_Empty_ShouldReturnWithoutTouchingRepositories()
    {
        var fixture = new UserFixture();

        var result = await fixture.Service.BatchUpdateUserDataScopesAsync(new UserDataScopeBatchUpdateCommand(UserId, [new UserDataScopeBatchGrantItem(0, false)], [-1]));

        Assert.Empty(result.GrantedDepartmentIds);
        fixture.TenantUserRepository.Verify(
            repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.VerifyNothingWritten();
    }

    #endregion

    private static SysDepartment CreateDepartment(long id)
    {
        var department = new SysDepartment
        {
            TenantId = 7,
            DepartmentCode = $"D-{id}",
            DepartmentName = $"部门{id}",
            Status = EnableStatus.Enabled
        };
        SaasTestHelper.SetBasicId(department, id);
        return department;
    }

    /// <summary>
    /// 角色数据范围夹具：仓储按内存行集合回放查询，写入逐次记录。
    /// </summary>
    private sealed class RoleFixture
    {
        private readonly List<SysRoleDataScope> _rows = [];

        public RoleFixture(DataPermissionScope dataScope = DataPermissionScope.Custom)
        {
            var role = new SysRole
            {
                TenantId = 7,
                RoleCode = "ROLE",
                RoleName = "角色",
                RoleType = RoleType.Custom,
                DataScope = dataScope,
                Status = EnableStatus.Enabled
            };
            SaasTestHelper.SetBasicId(role, RoleId);

            var roleRepository = new Mock<IRoleRepository>();
            roleRepository
                .Setup(repo => repo.GetByIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(role);
            var departmentRepository = new Mock<IDepartmentRepository>();
            departmentRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => CreateDepartment(id));

            DataScopeRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysRoleDataScope, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysRoleDataScope, bool>> predicate, CancellationToken _) =>
                    (IReadOnlyList<SysRoleDataScope>)_rows.Where(predicate.Compile()).ToList());
            DataScopeRepository
                .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysRoleDataScope>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysRoleDataScope> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Updated.AddRange(list);
                    return list;
                });
            DataScopeRepository
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRoleDataScope>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysRoleDataScope> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Added.AddRange(list);
                    return list.ToArray();
                });

            Service = new RoleDomainService(
                roleRepository.Object,
                new Mock<IUserRoleRepository>().Object,
                new Mock<IRolePermissionRepository>().Object,
                new Mock<IRoleHierarchyRepository>().Object,
                DataScopeRepository.Object,
                new Mock<IPermissionRepository>().Object,
                departmentRepository.Object,
                new TestCurrentTenant(7));
        }

        public RoleDomainService Service { get; }

        public Mock<IRoleDataScopeRepository> DataScopeRepository { get; } = new();

        public List<SysRoleDataScope> Updated { get; } = [];

        public List<SysRoleDataScope> Added { get; } = [];

        public Task<RoleDataScopeBatchUpdateResult> GrantAsync(long departmentId, bool includeChildren) =>
            Service.BatchUpdateRoleDataScopesAsync(new RoleDataScopeBatchUpdateCommand(
                RoleId,
                [new RoleDataScopeBatchGrantItem(departmentId, includeChildren)],
                []));

        public SysRoleDataScope AddRow(
            long basicId,
            long departmentId,
            bool includeChildren = false,
            ValidityStatus status = ValidityStatus.Valid,
            long roleId = RoleId)
        {
            var row = new SysRoleDataScope
            {
                RoleId = roleId,
                DepartmentId = departmentId,
                IncludeChildren = includeChildren,
                Status = status
            };
            SaasTestHelper.SetBasicId(row, basicId);
            _rows.Add(row);
            return row;
        }

        public void VerifyNothingWritten()
        {
            DataScopeRepository.Verify(
                repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysRoleDataScope>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            DataScopeRepository.Verify(
                repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRoleDataScope>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    /// <summary>
    /// 用户数据范围夹具：仓储按内存行集合回放查询，写入逐次记录。
    /// </summary>
    private sealed class UserFixture
    {
        private readonly List<SysUserDataScope> _rows = [];

        public UserFixture(DataPermissionScope? dataScopeOverride = DataPermissionScope.Custom)
        {
            TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SysTenantUser
                {
                    UserId = UserId,
                    MemberType = TenantMemberType.Member,
                    InviteStatus = TenantMemberInviteStatus.Accepted,
                    Status = ValidityStatus.Valid
                });

            var user = new SysUser
            {
                TenantId = 7,
                UserName = "user",
                Status = EnableStatus.Enabled,
                DataScopeOverride = dataScopeOverride
            };
            SaasTestHelper.SetBasicId(user, UserId);
            var userRepository = new Mock<IUserRepository>();
            userRepository
                .Setup(repo => repo.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            var departmentRepository = new Mock<IDepartmentRepository>();
            departmentRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => CreateDepartment(id));

            DataScopeRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysUserDataScope, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysUserDataScope, bool>> predicate, CancellationToken _) =>
                    (IReadOnlyList<SysUserDataScope>)_rows.Where(predicate.Compile()).ToList());
            DataScopeRepository
                .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserDataScope>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserDataScope> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Updated.AddRange(list);
                    return list;
                });
            DataScopeRepository
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysUserDataScope>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserDataScope> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Added.AddRange(list);
                    return list.ToArray();
                });

            Service = new UserDomainService(
                userRepository.Object,
                new Mock<IUserSecurityRepository>().Object,
                TenantUserRepository.Object,
                new Mock<IPasswordHasher>().Object,
                new Mock<IAuthenticationService>().Object,
                new Mock<IUserRoleRepository>().Object,
                new Mock<IRoleRepository>().Object,
                new Mock<IUserPermissionRepository>().Object,
                new Mock<IPermissionRepository>().Object,
                DataScopeRepository.Object,
                departmentRepository.Object,
                new Mock<IUserDepartmentRepository>().Object,
                new Mock<IUserSessionRepository>().Object,
                new TestCurrentTenant(7),
                new Mock<IPasswordHistoryDomainService>().Object,
                new Mock<IConstraintRuleEnforcementDomainService>().Object,
                new Mock<ITenantQuotaDomainService>().Object,
                NullLogger<UserDomainService>.Instance);
        }

        public UserDomainService Service { get; }

        public Mock<ITenantUserRepository> TenantUserRepository { get; } = new();

        public Mock<IUserDataScopeRepository> DataScopeRepository { get; } = new();

        public List<SysUserDataScope> Updated { get; } = [];

        public List<SysUserDataScope> Added { get; } = [];

        public Task<UserDataScopeBatchUpdateResult> GrantAsync(long departmentId, bool includeChildren) =>
            Service.BatchUpdateUserDataScopesAsync(new UserDataScopeBatchUpdateCommand(
                UserId,
                [new UserDataScopeBatchGrantItem(departmentId, includeChildren)],
                []));

        public SysUserDataScope AddRow(
            long basicId,
            long departmentId,
            ValidityStatus status = ValidityStatus.Valid,
            long userId = UserId)
        {
            var row = new SysUserDataScope
            {
                UserId = userId,
                DepartmentId = departmentId,
                Status = status
            };
            SaasTestHelper.SetBasicId(row, basicId);
            _rows.Add(row);
            return row;
        }

        public void VerifyNothingWritten()
        {
            DataScopeRepository.Verify(
                repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserDataScope>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            DataScopeRepository.Verify(
                repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysUserDataScope>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
