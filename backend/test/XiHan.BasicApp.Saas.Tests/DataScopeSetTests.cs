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
/// 数据范围设置：档位与自定义部门一次落地。部门明细与目标比出差量——新部门授予、历史行就地复用、
/// 目标之外的撤销（只置失效）；只有自定义档位带部门。角色的档位在角色上，成员的覆盖在成员关系上。
/// </summary>
public sealed class DataScopeSetTests
{
    private const long TenantId = 7;

    private const long RoleId = 5;

    private const long UserId = 1;

    #region 角色

    /// <summary>
    /// 切到自定义并选一个新部门：新增一行有效范围，档位随之改为自定义。
    /// </summary>
    [Fact]
    public async Task Role_CustomWithNewDepartment_AddsRowAndChangesScope()
    {
        var fixture = new RoleFixture(DataPermissionScope.SelfOnly);

        var result = await fixture.SetAsync(DataPermissionScope.Custom, (10, true));

        var added = Assert.Single(fixture.Added);
        Assert.Equal(RoleId, added.RoleId);
        Assert.True(added.IncludeChildren);
        Assert.Equal(ValidityStatus.Valid, added.Status);
        Assert.Equal([10L], result.GrantedDepartmentIds);
        Assert.True(result.ScopeChanged);
        Assert.Equal(DataPermissionScope.Custom, fixture.Role.DataScope);
        fixture.RoleRepository.Verify(repo => repo.UpdateAsync(fixture.Role, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 已过期的历史行再选中：复用那一行并清掉时间窗，否则保存成功却不生效。
    /// </summary>
    [Fact]
    public async Task Role_ExpiredRow_IsReactivatedWithTimeWindowCleared()
    {
        var fixture = new RoleFixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10);
        row.EffectiveTime = DateTimeOffset.UtcNow.AddDays(-30);
        row.ExpirationTime = DateTimeOffset.UtcNow.AddDays(-1);

        var result = await fixture.SetAsync(DataPermissionScope.Custom, (10, false));

        Assert.Empty(fixture.Added);
        Assert.Same(row, Assert.Single(fixture.Updated));
        Assert.Null(row.EffectiveTime);
        Assert.Null(row.ExpirationTime);
        Assert.Equal([10L], result.GrantedDepartmentIds);
    }

    /// <summary>
    /// 与现状一致：不写库，档位也不动。
    /// </summary>
    [Fact]
    public async Task Role_SameState_WritesNothing()
    {
        var fixture = new RoleFixture();
        fixture.AddRow(basicId: 100, departmentId: 10, includeChildren: true);

        var result = await fixture.SetAsync(DataPermissionScope.Custom, (10, true));

        Assert.False(result.ScopeChanged);
        Assert.Empty(result.GrantedDepartmentIds);
        Assert.Empty(result.RevokedDepartmentIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 已选的部门改含下级：就地更新那一行。
    /// </summary>
    [Fact]
    public async Task Role_ChangeIncludeChildren_UpdatesInPlace()
    {
        var fixture = new RoleFixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10, includeChildren: false);

        _ = await fixture.SetAsync(DataPermissionScope.Custom, (10, true));

        Assert.True(row.IncludeChildren);
        Assert.Same(row, Assert.Single(fixture.Updated));
    }

    /// <summary>
    /// 目标之外仍有效的部门撤销（只置失效）；别的角色的行不受影响。
    /// </summary>
    [Fact]
    public async Task Role_DepartmentsLeftOut_AreRevoked()
    {
        var fixture = new RoleFixture();
        var dropped = fixture.AddRow(basicId: 100, departmentId: 10);
        var kept = fixture.AddRow(basicId: 101, departmentId: 20);
        var others = fixture.AddRow(basicId: 200, departmentId: 10, roleId: 6);

        var result = await fixture.SetAsync(DataPermissionScope.Custom, (20, false));

        Assert.Equal(ValidityStatus.Invalid, dropped.Status);
        Assert.Equal(ValidityStatus.Valid, kept.Status);
        Assert.Equal(ValidityStatus.Valid, others.Status);
        Assert.Equal([10L], result.RevokedDepartmentIds);
    }

    /// <summary>
    /// 从自定义切到其它档位：部门明细全部撤销，档位改写。
    /// </summary>
    [Fact]
    public async Task Role_SwitchAwayFromCustom_RevokesAllDepartments()
    {
        var fixture = new RoleFixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10);

        var result = await fixture.SetAsync(DataPermissionScope.DepartmentAndChildren);

        Assert.Equal(ValidityStatus.Invalid, row.Status);
        Assert.True(result.ScopeChanged);
        Assert.Equal(DataPermissionScope.DepartmentAndChildren, fixture.Role.DataScope);
    }

    /// <summary>
    /// 自定义不带部门、非自定义带部门：都拒绝，且不写库。
    /// </summary>
    [Fact]
    public async Task Role_DepartmentsMismatchScope_AreRejected()
    {
        var fixture = new RoleFixture();

        var noDepartments = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.Custom));
        var strayDepartments = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.All, (10, false)));

        Assert.Contains("至少选择一个部门", noDepartments.Message, StringComparison.Ordinal);
        Assert.Contains("只有自定义", strayDepartments.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 全局角色在平台只能设档位：部门是租户自己的数据，不能自定义。
    /// </summary>
    [Fact]
    public async Task Role_GlobalRoleInPlatform_CannotBeCustom()
    {
        var fixture = new RoleFixture(DataPermissionScope.SelfOnly, roleTenantId: 0, contextTenantId: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.Custom, (10, false)));
        var result = await fixture.SetAsync(DataPermissionScope.DepartmentAndChildren);

        Assert.Contains("全局角色", exception.Message, StringComparison.Ordinal);
        Assert.True(result.ScopeChanged);
        Assert.Equal(DataPermissionScope.DepartmentAndChildren, fixture.Role.DataScope);
    }

    /// <summary>
    /// 全局角色在租户里只读。
    /// </summary>
    [Fact]
    public async Task Role_GlobalRoleInTenant_IsReadOnly()
    {
        var fixture = new RoleFixture(DataPermissionScope.SelfOnly, roleTenantId: 0);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.All));

        fixture.RoleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region 成员

    /// <summary>
    /// 成员设为自定义：写部门明细，覆盖挂在本租户的成员关系上。
    /// </summary>
    [Fact]
    public async Task Member_Custom_WritesRowsAndOverrideOnMembership()
    {
        var fixture = new MemberFixture();

        var result = await fixture.SetAsync(DataPermissionScope.Custom, (10, true));

        var added = Assert.Single(fixture.Added);
        Assert.Equal(UserId, added.UserId);
        Assert.True(added.IncludeChildren);
        Assert.True(result.ScopeChanged);
        Assert.Equal(DataPermissionScope.Custom, fixture.Membership.DataScopeOverride);
        fixture.TenantUserRepository.Verify(repo => repo.UpdateAsync(fixture.Membership, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 撤销过的部门再选中：复用那一行。
    /// </summary>
    [Fact]
    public async Task Member_RevokedRow_IsReactivated()
    {
        var fixture = new MemberFixture(DataPermissionScope.Custom);
        var row = fixture.AddRow(basicId: 100, departmentId: 10, status: ValidityStatus.Invalid);

        _ = await fixture.SetAsync(DataPermissionScope.Custom, (10, false));

        Assert.Empty(fixture.Added);
        Assert.Same(row, Assert.Single(fixture.Updated));
        Assert.Equal(ValidityStatus.Valid, row.Status);
    }

    /// <summary>
    /// 改回跟随角色：清掉覆盖，部门明细全部撤销；别的成员的行不受影响。
    /// </summary>
    [Fact]
    public async Task Member_FollowRoles_ClearsOverrideAndRevokesRows()
    {
        var fixture = new MemberFixture(DataPermissionScope.Custom);
        var own = fixture.AddRow(basicId: 100, departmentId: 10);
        var others = fixture.AddRow(basicId: 200, departmentId: 10, userId: 2);

        var result = await fixture.SetAsync(null);

        Assert.Null(fixture.Membership.DataScopeOverride);
        Assert.Equal(ValidityStatus.Invalid, own.Status);
        Assert.Equal(ValidityStatus.Valid, others.Status);
        Assert.Equal([10L], result.RevokedDepartmentIds);
    }

    /// <summary>
    /// 与现状一致：不写库，成员关系也不动。
    /// </summary>
    [Fact]
    public async Task Member_SameState_WritesNothing()
    {
        var fixture = new MemberFixture(DataPermissionScope.All);

        var result = await fixture.SetAsync(DataPermissionScope.All);

        Assert.False(result.ScopeChanged);
        fixture.VerifyNothingWritten();
        fixture.TenantUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 平台没有成员关系：成员数据范围是租户侧设置。
    /// </summary>
    [Fact]
    public async Task Member_InPlatform_IsRejected()
    {
        var fixture = new MemberFixture(contextTenantId: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.All));

        Assert.Contains("租户侧", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 支持成员（平台人员入驻）同样由所在租户维护。
    /// </summary>
    [Fact]
    public async Task Member_SupportMember_IsMaintainedByTenant()
    {
        var fixture = new MemberFixture(memberType: TenantMemberType.PlatformAdmin);

        var result = await fixture.SetAsync(DataPermissionScope.SelfOnly);

        Assert.True(result.ScopeChanged);
        Assert.Equal(DataPermissionScope.SelfOnly, fixture.Membership.DataScopeOverride);
    }

    /// <summary>
    /// 租户所有者看得到本租户全部数据：不设覆盖，免得被下级管理员收窄。
    /// </summary>
    [Fact]
    public async Task Member_Owner_IsRejected()
    {
        var fixture = new MemberFixture(memberType: TenantMemberType.Owner);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.SelfOnly));

        Assert.Null(fixture.Membership.DataScopeOverride);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 未接受邀请的成员不能维护数据范围。
    /// </summary>
    [Fact]
    public async Task Member_PendingInvite_IsRejected()
    {
        var fixture = new MemberFixture(inviteStatus: TenantMemberInviteStatus.Pending);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.SetAsync(DataPermissionScope.All));

        Assert.Contains("未接受邀请", exception.Message, StringComparison.Ordinal);
    }

    #endregion

    private static SysDepartment CreateDepartment(long id)
    {
        var department = new SysDepartment
        {
            TenantId = TenantId,
            DepartmentCode = $"D-{id}",
            DepartmentName = $"部门{id}",
            Status = EnableStatus.Enabled
        };
        SaasTestHelper.SetBasicId(department, id);
        return department;
    }

    private static Mock<IDepartmentRepository> CreateDepartmentRepository()
    {
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => CreateDepartment(id));
        return departmentRepository;
    }

    /// <summary>
    /// 角色数据范围夹具：仓储按内存行集合回放查询，写入逐次记录。
    /// </summary>
    private sealed class RoleFixture
    {
        private readonly List<SysRoleDataScope> _rows = [];

        public RoleFixture(
            DataPermissionScope dataScope = DataPermissionScope.Custom,
            long roleTenantId = TenantId,
            long? contextTenantId = TenantId)
        {
            Role = new SysRole
            {
                TenantId = roleTenantId,
                RoleCode = "ROLE",
                RoleName = "角色",
                RoleType = RoleType.Custom,
                DataScope = dataScope,
                Status = EnableStatus.Enabled
            };
            SaasTestHelper.SetBasicId(Role, RoleId);

            RoleRepository
                .Setup(repo => repo.GetByIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Role);
            RoleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysRole role, CancellationToken _) => role);
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
                RoleRepository.Object,
                new Mock<IUserRoleRepository>().Object,
                new Mock<IRolePermissionRepository>().Object,
                new Mock<IRoleHierarchyRepository>().Object,
                DataScopeRepository.Object,
                new Mock<IPermissionRepository>().Object,
                CreateDepartmentRepository().Object,
                new TestCurrentTenant(contextTenantId));
        }

        public RoleDomainService Service { get; }

        public SysRole Role { get; }

        public Mock<IRoleRepository> RoleRepository { get; } = new();

        public Mock<IRoleDataScopeRepository> DataScopeRepository { get; } = new();

        public List<SysRoleDataScope> Updated { get; } = [];

        public List<SysRoleDataScope> Added { get; } = [];

        public Task<DataScopeSetResult> SetAsync(DataPermissionScope scope, params (long DepartmentId, bool IncludeChildren)[] departments) =>
            Service.SetRoleDataScopeAsync(new RoleDataScopeSetCommand(
                RoleId,
                scope,
                [.. departments.Select(item => new DataScopeDepartmentItem(item.DepartmentId, item.IncludeChildren))]));

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
            RoleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    /// <summary>
    /// 成员数据范围夹具：仓储按内存行集合回放查询，写入逐次记录。
    /// </summary>
    private sealed class MemberFixture
    {
        private readonly List<SysUserDataScope> _rows = [];

        public MemberFixture(
            DataPermissionScope? dataScopeOverride = null,
            long? contextTenantId = TenantId,
            TenantMemberType memberType = TenantMemberType.Member,
            TenantMemberInviteStatus inviteStatus = TenantMemberInviteStatus.Accepted)
        {
            Membership = new SysTenantUser
            {
                TenantId = TenantId,
                UserId = UserId,
                MemberType = memberType,
                InviteStatus = inviteStatus,
                Status = ValidityStatus.Valid,
                DataScopeOverride = dataScopeOverride
            };
            TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Membership);
            TenantUserRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysTenantUser member, CancellationToken _) => member);

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
                new Mock<IUserRepository>().Object,
                new Mock<IUserSecurityRepository>().Object,
                TenantUserRepository.Object,
                new Mock<IPasswordHasher>().Object,
                new Mock<IAuthenticationService>().Object,
                new Mock<IUserRoleRepository>().Object,
                new Mock<IRoleRepository>().Object,
                new Mock<IUserPermissionRepository>().Object,
                new Mock<IPermissionRepository>().Object,
                DataScopeRepository.Object,
                CreateDepartmentRepository().Object,
                new Mock<IUserDepartmentRepository>().Object,
                new Mock<IUserSessionRepository>().Object,
                new TestCurrentTenant(contextTenantId),
                new Mock<IPasswordHistoryDomainService>().Object,
                new Mock<IConstraintRuleEnforcementDomainService>().Object,
                new Mock<ITenantQuotaDomainService>().Object,
                NullLogger<UserDomainService>.Instance);
        }

        public UserDomainService Service { get; }

        public SysTenantUser Membership { get; }

        public Mock<ITenantUserRepository> TenantUserRepository { get; } = new();

        public Mock<IUserDataScopeRepository> DataScopeRepository { get; } = new();

        public List<SysUserDataScope> Updated { get; } = [];

        public List<SysUserDataScope> Added { get; } = [];

        public Task<DataScopeSetResult> SetAsync(DataPermissionScope? scope, params (long DepartmentId, bool IncludeChildren)[] departments) =>
            Service.SetUserDataScopeAsync(new UserDataScopeSetCommand(
                UserId,
                scope,
                [.. departments.Select(item => new DataScopeDepartmentItem(item.DepartmentId, item.IncludeChildren))]));

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
