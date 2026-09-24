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
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 用户角色批量变更：授予与撤销一次提交，SSD 按最终角色集执法，撤销只置失效、历史行就地复用。
/// 以角色为中心维护成员、单条复活授权，与按用户批量同一口径（成员可授权、角色可分配、职责分离、成员上限）。
/// </summary>
public sealed class UserRoleBatchUpdateTests
{
    private const long UserId = 1;

    #region SSD 执法

    /// <summary>
    /// 拒绝类违规必须阻断授予且不写入用户角色。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_WhenSsdViolationDenies_ShouldBlockWithoutPersist()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        fixture.SetupViolation(new ConstraintViolation(1, "SSD-01", "出纳与会计互斥", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.Deny));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.GrantAsync(20));

        Assert.Contains("SSD-01", exception.Message, StringComparison.Ordinal);
        Assert.Contains("出纳与会计互斥", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 需审批类违规按失败关闭处理（当前无自动审批路由，先阻断以防越权）。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_WhenSsdViolationRequiresApproval_ShouldBlock()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        fixture.SetupViolation(new ConstraintViolation(1, "SSD-02", "审批约束", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.RequireApproval));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.GrantAsync(20));

        Assert.Contains("SSD-02", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 警告类违规放行并正常落库。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_WhenSsdViolationWarns_ShouldAllowGrant()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        fixture.SetupViolation(new ConstraintViolation(1, "SSD-03", "警告约束", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.Warning));

        var result = await fixture.GrantAsync(20);

        Assert.Equal([20L], result.GrantedRoleIds);
        Assert.Equal([20L], fixture.Added.Select(userRole => userRole.RoleId));
    }

    /// <summary>
    /// 执法输入是「现有有效角色 − 本次撤销 + 本次授予」这一最终角色集，约束类型固定为 SSD：
    /// 同一次提交里撤掉互斥的那个角色，就不该再被它挡住。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_ShouldEvaluateFinalRoleSetAsSsd()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        fixture.AddRow(basicId: 101, roleId: 11);

        _ = await fixture.Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [20], [100]));

        Assert.Equal([11L, 20L], fixture.EvaluatedRoleIds?.Order());
        Assert.Equal(ConstraintType.SSD, fixture.EvaluatedType);
    }

    /// <summary>
    /// 只撤不授时角色集只会变小，不触发 SSD 评估。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokeOnly_ShouldNotEvaluateSsd()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);

        _ = await fixture.Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [], [100]));

        Assert.Null(fixture.EvaluatedRoleIds);
    }

    #endregion

    #region 授予与撤销

    /// <summary>
    /// 从未绑定过的角色新增一行有效绑定。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_NewRole_ShouldAddValidBinding()
    {
        var fixture = new Fixture();

        var result = await fixture.GrantAsync(20);

        var added = Assert.Single(fixture.Added);
        Assert.Equal(UserId, added.UserId);
        Assert.Equal(20, added.RoleId);
        Assert.Equal(ValidityStatus.Valid, added.Status);
        Assert.Equal([20L], result.GrantedRoleIds);
    }

    /// <summary>
    /// 撤销过的角色再授予：唯一索引按 租户×用户×角色，必须复用那一行而不是再插一行。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokedRole_ShouldReactivateExistingRow()
    {
        var fixture = new Fixture();
        var row = fixture.AddRow(basicId: 100, roleId: 20, status: ValidityStatus.Invalid);

        var result = await fixture.GrantAsync(20);

        Assert.Empty(fixture.Added);
        Assert.Same(row, Assert.Single(fixture.Updated));
        Assert.Equal(ValidityStatus.Valid, row.Status);
        Assert.Equal([20L], result.GrantedRoleIds);
    }

    /// <summary>
    /// 已过期的绑定再授予：只改状态的话时间窗仍把它挡在外面，保存成功却不生效。授予即从现在起生效。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_ExpiredRole_ShouldClearTimeWindow()
    {
        var fixture = new Fixture();
        var row = fixture.AddRow(basicId: 100, roleId: 20, expirationTime: DateTimeOffset.UtcNow.AddDays(-1));
        row.EffectiveTime = DateTimeOffset.UtcNow.AddDays(-30);

        var result = await fixture.GrantAsync(20);

        Assert.Null(row.EffectiveTime);
        Assert.Null(row.ExpirationTime);
        Assert.Equal(ValidityStatus.Valid, row.Status);
        Assert.Equal([20L], result.GrantedRoleIds);
    }

    /// <summary>
    /// 已经生效的绑定原样保留：不写库、不算变更，也就不会发一条多余的审计事件。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AlreadyEffectiveRole_ShouldBeNoChange()
    {
        var fixture = new Fixture();
        var expiration = DateTimeOffset.UtcNow.AddDays(30);
        var row = fixture.AddRow(basicId: 100, roleId: 20, expirationTime: expiration);

        var result = await fixture.GrantAsync(20);

        Assert.Empty(result.GrantedRoleIds);
        Assert.Equal(expiration, row.ExpirationTime);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 撤销只置为失效（不删行），且只认本用户名下的有效记录：别人的记录主键混进来不受影响。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_Revoke_ShouldInvalidateOnlyOwnValidRows()
    {
        var fixture = new Fixture();
        var own = fixture.AddRow(basicId: 100, roleId: 10);
        var alreadyInvalid = fixture.AddRow(basicId: 101, roleId: 11, status: ValidityStatus.Invalid);
        var others = fixture.AddRow(basicId: 200, roleId: 10, userId: 2);

        var result = await fixture.Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [], [100, 101, 200]));

        Assert.Equal(ValidityStatus.Invalid, own.Status);
        Assert.Equal(ValidityStatus.Invalid, alreadyInvalid.Status);
        Assert.Equal(ValidityStatus.Valid, others.Status);
        Assert.Same(own, Assert.Single(fixture.Updated));
        Assert.Equal([10L], result.RevokedRoleIds);
    }

    /// <summary>
    /// 同一角色既撤又授时以授予为准：不撤销，结果不因实体实例是否共享而变。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokeAndGrantSameRole_ShouldKeepGranted()
    {
        var fixture = new Fixture();
        var row = fixture.AddRow(basicId: 100, roleId: 10);

        var result = await fixture.Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [10], [100]));

        Assert.Equal(ValidityStatus.Valid, row.Status);
        Assert.Empty(result.RevokedRoleIds);
        Assert.Empty(result.GrantedRoleIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 任一角色不可分配（停用）时整批拒绝，撤销也不落库。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_DisabledRole_ShouldRejectWholeBatch()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        fixture.AddRole(30, EnableStatus.Disabled);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [20, 30], [100])));

        Assert.Contains("停用角色", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 角色不存在时整批拒绝。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_MissingRole_ShouldReject()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.GrantAsync(999));

        Assert.Contains("角色不存在", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 空提交直接返回，不查成员、不写库。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_Empty_ShouldReturnWithoutTouchingRepositories()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [0], [-1]));

        Assert.Empty(result.GrantedRoleIds);
        Assert.Empty(result.RevokedRoleIds);
        fixture.TenantUserRepository.Verify(
            repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.VerifyNothingWritten();
    }

    #endregion

    #region 以角色为中心

    /// <summary>
    /// 加入新成员新增有效绑定；移出只认本角色名下的有效记录；同一成员既移出又加入以加入为准。
    /// </summary>
    [Fact]
    public async Task RoleMembers_GrantAndRevoke_ShouldApplyToThisRoleOnly()
    {
        var fixture = new Fixture();
        var leaving = fixture.AddRow(basicId: 100, roleId: 10, userId: 2);
        var staying = fixture.AddRow(basicId: 101, roleId: 10, userId: 3);
        var otherRole = fixture.AddRow(basicId: 102, roleId: 11, userId: 2);

        var result = await fixture.Service.BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateCommand(10, [UserId, 3], [100, 101, 102]));

        var added = Assert.Single(fixture.Added);
        Assert.Equal((UserId, 10L), (added.UserId, added.RoleId));
        Assert.Equal(ValidityStatus.Invalid, leaving.Status);
        Assert.Equal(ValidityStatus.Valid, staying.Status);
        Assert.Equal(ValidityStatus.Valid, otherRole.Status);
        Assert.Equal([UserId], result.GrantedUserIds);
        Assert.Equal([2L], result.RevokedUserIds);
    }

    /// <summary>
    /// 加入的成员按「现有有效角色 + 本角色」评估职责分离，违规即整体阻断。
    /// </summary>
    [Fact]
    public async Task RoleMembers_SsdViolation_ShouldBlockWholeBatch()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        fixture.SetupViolation(new ConstraintViolation(1, "SSD-01", "出纳与会计互斥", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.Deny));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateCommand(20, [UserId], [])));

        Assert.Contains("SSD-01", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 不是本租户可授权成员的用户不能加入。
    /// </summary>
    [Fact]
    public async Task RoleMembers_NonMember_ShouldReject()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateCommand(10, [99], [])));

        Assert.Contains("租户成员不存在", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 成员上限：加入后超过上限整体拒绝；同批移出的名额可以让出。
    /// </summary>
    [Fact]
    public async Task RoleMembers_Capacity_ShouldCountLeavingSeats()
    {
        var fixture = new Fixture();
        fixture.AddRole(30, maxMembers: 2);
        fixture.AddRow(basicId: 100, roleId: 30, userId: 2);
        fixture.AddRow(basicId: 101, roleId: 30, userId: 3);

        var overflow = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateCommand(30, [UserId], [])));
        var swapped = await fixture.Service.BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateCommand(30, [UserId], [100]));

        Assert.Contains("最多 2 个成员", overflow.Message, StringComparison.Ordinal);
        Assert.Equal([UserId], swapped.GrantedUserIds);
        Assert.Equal([2L], swapped.RevokedUserIds);
    }

    #endregion

    #region 成员上限与单条复活

    /// <summary>
    /// 按用户批量授予同样受成员上限约束。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RoleAtCapacity_ShouldReject()
    {
        var fixture = new Fixture();
        fixture.AddRole(30, maxMembers: 1);
        fixture.AddRow(basicId: 100, roleId: 30, userId: 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.GrantAsync(30));

        Assert.Contains("最多 1 个成员", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 已过期的历史行不占名额；尚未生效的预约占名额。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_Capacity_ShouldIgnoreExpiredAndCountReserved()
    {
        var expired = new Fixture();
        expired.AddRole(30, maxMembers: 1);
        expired.AddRow(basicId: 100, roleId: 30, userId: 2, expirationTime: DateTimeOffset.UtcNow.AddDays(-1));
        _ = await expired.GrantAsync(30);
        Assert.Single(expired.Added);

        var reserved = new Fixture();
        reserved.AddRole(30, maxMembers: 1);
        var future = reserved.AddRow(basicId: 100, roleId: 30, userId: 2);
        future.EffectiveTime = DateTimeOffset.UtcNow.AddDays(1);
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => reserved.GrantAsync(30));
    }

    /// <summary>
    /// 单条状态改回有效：与批量授予同样过职责分离与成员上限。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_Revive_ShouldEnforceSsdAndCapacity()
    {
        var fixture = new Fixture();
        fixture.AddRole(30, maxMembers: 1);
        fixture.AddRow(basicId: 100, roleId: 30, userId: 2);
        var revoked = fixture.AddRow(basicId: 101, roleId: 30, status: ValidityStatus.Invalid);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateUserRoleStatusAsync(new UserRoleStatusChangeCommand(revoked.BasicId, ValidityStatus.Valid, null)));

        Assert.Contains("最多 1 个成员", exception.Message, StringComparison.Ordinal);
        fixture.UserRoleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 单条状态改回有效命中职责分离违规时阻断。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ReviveIntoSsdConflict_ShouldBlock()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, roleId: 10);
        var revoked = fixture.AddRow(basicId: 101, roleId: 20, status: ValidityStatus.Invalid);
        fixture.SetupViolation(new ConstraintViolation(1, "SSD-01", "出纳与会计互斥", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.Deny));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateUserRoleStatusAsync(new UserRoleStatusChangeCommand(revoked.BasicId, ValidityStatus.Valid, null)));

        Assert.Contains("SSD-01", exception.Message, StringComparison.Ordinal);
        fixture.UserRoleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    /// <summary>
    /// 批量变更的测试夹具：仓储按内存行集合回放查询，写入逐次记录。
    /// </summary>
    private sealed class Fixture
    {
        private readonly List<SysUserRole> _rows = [];

        private readonly List<SysRole> _roles = [];

        public Fixture()
        {
            TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(It.IsIn(UserId, 2L, 3L), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long userId, CancellationToken _) => new SysTenantUser
                {
                    UserId = userId,
                    MemberType = TenantMemberType.Member,
                    InviteStatus = TenantMemberInviteStatus.Accepted,
                    Status = ValidityStatus.Valid
                });

            AddRole(10);
            AddRole(11);
            AddRole(20);

            RoleRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysRole, bool>> predicate, CancellationToken _) =>
                    (IReadOnlyList<SysRole>)_roles.Where(predicate.Compile()).ToList());
            RoleRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _roles.Find(role => role.BasicId == id));
            UserRoleRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _rows.Find(row => row.BasicId == id));
            UserRoleRepository
                .Setup(repo => repo.CountOccupiedByRoleIdAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long roleId, DateTimeOffset now, CancellationToken _) => _rows.Count(row =>
                    row.RoleId == roleId
                    && row.Status == ValidityStatus.Valid
                    && (row.ExpirationTime is null || row.ExpirationTime > now)));

            UserRoleRepository
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysUserRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysUserRole, bool>> predicate, CancellationToken _) =>
                    (IReadOnlyList<SysUserRole>)_rows.Where(predicate.Compile()).ToList());
            UserRoleRepository
                .Setup(repo => repo.GetValidByUserIdAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long userId, DateTimeOffset now, CancellationToken _) =>
                    (IReadOnlyList<SysUserRole>)_rows
                        .Where(userRole => userRole.UserId == userId
                            && userRole.Status == ValidityStatus.Valid
                            && (userRole.EffectiveTime is null || userRole.EffectiveTime <= now)
                            && (userRole.ExpirationTime is null || userRole.ExpirationTime > now))
                        .ToList());
            UserRoleRepository
                .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserRole>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserRole> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Updated.AddRange(list);
                    return list;
                });
            UserRoleRepository
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysUserRole>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserRole> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Added.AddRange(list);
                    return list.ToArray();
                });

            Enforcement
                .Setup(service => service.EvaluateRoleAssignmentsAsync(
                    It.IsAny<IEnumerable<long>>(),
                    It.IsAny<ConstraintType>(),
                    It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<long> roleIds, ConstraintType type, CancellationToken _) =>
                {
                    EvaluatedRoleIds = roleIds.ToList();
                    EvaluatedType = type;
                })
                .ReturnsAsync(ConstraintEnforcementResult.Pass);

            var currentTenant = new Mock<ICurrentTenant>();
            currentTenant.SetupGet(tenant => tenant.Id).Returns((long?)7);

            Service = new UserDomainService(
                new Mock<IUserRepository>().Object,
                new Mock<IUserSecurityRepository>().Object,
                TenantUserRepository.Object,
                new Mock<IPasswordHasher>().Object,
                new Mock<IAuthenticationService>().Object,
                UserRoleRepository.Object,
                RoleRepository.Object,
                new Mock<IUserPermissionRepository>().Object,
                new Mock<IPermissionRepository>().Object,
                new Mock<IUserDataScopeRepository>().Object,
                new Mock<IDepartmentRepository>().Object,
                new Mock<IUserDepartmentRepository>().Object,
                new Mock<IUserSessionRepository>().Object,
                currentTenant.Object,
                new Mock<IPasswordHistoryDomainService>().Object,
                Enforcement.Object,
                new Mock<ITenantQuotaDomainService>().Object,
                NullLogger<UserDomainService>.Instance);
        }

        public UserDomainService Service { get; }

        public Mock<ITenantUserRepository> TenantUserRepository { get; } = new();

        public Mock<IRoleRepository> RoleRepository { get; } = new();

        public Mock<IUserRoleRepository> UserRoleRepository { get; } = new();

        public Mock<IConstraintRuleEnforcementDomainService> Enforcement { get; } = new();

        public List<SysUserRole> Updated { get; } = [];

        public List<SysUserRole> Added { get; } = [];

        public IReadOnlyList<long>? EvaluatedRoleIds { get; private set; }

        public ConstraintType? EvaluatedType { get; private set; }

        public Task<UserRoleBatchUpdateResult> GrantAsync(long roleId) =>
            Service.BatchUpdateUserRolesAsync(new UserRoleBatchUpdateCommand(UserId, [roleId], []));

        public void AddRole(long id, EnableStatus status = EnableStatus.Enabled, int maxMembers = 0)
        {
            var role = new SysRole
            {
                TenantId = 7,
                RoleCode = $"ROLE-{id}",
                RoleName = $"角色{id}",
                RoleType = RoleType.Custom,
                MaxMembers = maxMembers,
                Status = status
            };
            SaasTestHelper.SetBasicId(role, id);
            _roles.Add(role);
        }

        public SysUserRole AddRow(
            long basicId,
            long roleId,
            ValidityStatus status = ValidityStatus.Valid,
            DateTimeOffset? expirationTime = null,
            long userId = UserId)
        {
            var row = new SysUserRole
            {
                UserId = userId,
                RoleId = roleId,
                Status = status,
                ExpirationTime = expirationTime
            };
            SaasTestHelper.SetBasicId(row, basicId);
            _rows.Add(row);
            return row;
        }

        public void SetupViolation(ConstraintViolation violation)
        {
            Enforcement
                .Setup(service => service.EvaluateRoleAssignmentsAsync(
                    It.IsAny<IEnumerable<long>>(),
                    It.IsAny<ConstraintType>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ConstraintEnforcementResult([violation]));
        }

        public void VerifyNothingWritten()
        {
            UserRoleRepository.Verify(
                repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserRole>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            UserRoleRepository.Verify(
                repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysUserRole>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
