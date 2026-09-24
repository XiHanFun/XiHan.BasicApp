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
/// 用户部门归属批量变更：分配与撤销一次提交，主部门始终唯一，撤销只置失效、历史行就地复用。
/// </summary>
public sealed class UserDepartmentBatchUpdateTests
{
    private const long UserId = 1;

    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    #region 主部门

    /// <summary>
    /// 用户第一个部门自动成为主部门。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_FirstDepartment_ShouldBecomeMain()
    {
        var fixture = new Fixture();

        var result = await fixture.AssignAsync(new UserDepartmentBatchAssignItem(10, IsMain: false));

        var added = Assert.Single(fixture.Added);
        Assert.True(added.IsMain);
        Assert.Equal(ValidityStatus.Valid, added.Status);
        Assert.Equal([10L], result.AssignedDepartmentIds);
    }

    /// <summary>
    /// 已有主部门时新进的普通部门不抢主部门，原主部门不被改写。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AdditionalDepartment_ShouldKeepExistingMain()
    {
        var fixture = new Fixture();
        var main = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);

        _ = await fixture.AssignAsync(new UserDepartmentBatchAssignItem(20, IsMain: false));

        Assert.False(Assert.Single(fixture.Added).IsMain);
        Assert.True(main.IsMain);
        Assert.Empty(fixture.Updated);
    }

    /// <summary>
    /// 新进部门指定为主部门时取代原主部门。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_NewMainDepartment_ShouldReplaceExistingMain()
    {
        var fixture = new Fixture();
        var oldMain = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);

        _ = await fixture.AssignAsync(new UserDepartmentBatchAssignItem(20, IsMain: true));

        Assert.True(Assert.Single(fixture.Added).IsMain);
        Assert.False(oldMain.IsMain);
        Assert.Same(oldMain, Assert.Single(fixture.Updated));
    }

    /// <summary>
    /// 把已有的部门改设为主部门：只调整主部门标记，不算新分配（不触发进群事件）。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_ExistingDepartmentAsMain_ShouldOnlySwitchMainFlag()
    {
        var fixture = new Fixture();
        var oldMain = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);
        var other = fixture.AddRow(basicId: 101, departmentId: 20);

        var result = await fixture.AssignAsync(new UserDepartmentBatchAssignItem(20, IsMain: true));

        Assert.False(oldMain.IsMain);
        Assert.True(other.IsMain);
        Assert.Empty(fixture.Added);
        Assert.Equal(2, fixture.Updated.Count);
        Assert.Empty(result.AssignedDepartmentIds);
    }

    /// <summary>
    /// 撤掉主部门后由留下的有效归属中最早创建的接任。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokeMain_ShouldPromoteEarliestRemaining()
    {
        var fixture = new Fixture();
        var main = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true, createdOffsetDays: 0);
        var later = fixture.AddRow(basicId: 101, departmentId: 20, createdOffsetDays: 2);
        var earlier = fixture.AddRow(basicId: 102, departmentId: 30, createdOffsetDays: 1);

        var result = await fixture.Service.BatchUpdateUserDepartmentsAsync(new UserDepartmentBatchUpdateCommand(UserId, [], [100]));

        Assert.Equal(ValidityStatus.Invalid, main.Status);
        Assert.False(main.IsMain);
        Assert.True(earlier.IsMain);
        Assert.False(later.IsMain);
        Assert.Equal([10L], result.RevokedDepartmentIds);
    }

    /// <summary>
    /// 同一次提交里撤掉唯一的主部门、再进一个新部门：新部门接任主部门。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokeMainAndAssignNew_ShouldMakeNewOneMain()
    {
        var fixture = new Fixture();
        var main = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);

        _ = await fixture.Service.BatchUpdateUserDepartmentsAsync(new UserDepartmentBatchUpdateCommand(
            UserId,
            [new UserDepartmentBatchAssignItem(20, IsMain: false)],
            [100]));

        Assert.False(main.IsMain);
        Assert.True(Assert.Single(fixture.Added).IsMain);
    }

    /// <summary>
    /// 一次提交指定多个主部门时整批拒绝。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_MultipleMainAssigns_ShouldReject()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.AssignAsync(
            new UserDepartmentBatchAssignItem(10, IsMain: true),
            new UserDepartmentBatchAssignItem(20, IsMain: true)));

        Assert.Contains("主部门", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    #endregion

    #region 分配与撤销

    /// <summary>
    /// 撤销过的部门再分配：唯一索引按 租户×用户×部门，必须复用那一行，并按本次分配项重写岗位等字段。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokedDepartment_ShouldReactivateExistingRow()
    {
        var fixture = new Fixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10, status: ValidityStatus.Invalid);
        row.PositionId = 3;
        row.JobNumber = "OLD";

        var result = await fixture.AssignAsync(new UserDepartmentBatchAssignItem(10, IsMain: false, PositionId: 5, JobNumber: " A-01 "));

        Assert.Empty(fixture.Added);
        Assert.Same(row, Assert.Single(fixture.Updated));
        Assert.Equal(ValidityStatus.Valid, row.Status);
        Assert.Equal(5, row.PositionId);
        Assert.Equal("A-01", row.JobNumber);
        Assert.True(row.IsMain);
        Assert.Equal([10L], result.AssignedDepartmentIds);
    }

    /// <summary>
    /// 已有效的部门再次下发：不写库、不算分配，岗位等字段不被覆盖。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_AlreadyValidDepartment_ShouldBeNoChange()
    {
        var fixture = new Fixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);
        row.PositionId = 3;

        var result = await fixture.AssignAsync(new UserDepartmentBatchAssignItem(10, IsMain: false, PositionId: 5));

        Assert.Equal(3, row.PositionId);
        Assert.Empty(result.AssignedDepartmentIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 撤销只置为失效（不删行），且只认本用户名下的有效记录：别人的记录主键混进来不受影响。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_Revoke_ShouldInvalidateOnlyOwnValidRows()
    {
        var fixture = new Fixture();
        var own = fixture.AddRow(basicId: 100, departmentId: 10);
        var others = fixture.AddRow(basicId: 200, departmentId: 10, userId: 2);

        var result = await fixture.Service.BatchUpdateUserDepartmentsAsync(new UserDepartmentBatchUpdateCommand(UserId, [], [100, 200]));

        Assert.Equal(ValidityStatus.Invalid, own.Status);
        Assert.Equal(ValidityStatus.Valid, others.Status);
        Assert.Equal([10L], result.RevokedDepartmentIds);
    }

    /// <summary>
    /// 同一部门既撤又分配时以分配为准：不撤销、不写库。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokeAndAssignSameDepartment_ShouldKeepAssigned()
    {
        var fixture = new Fixture();
        var row = fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);

        var result = await fixture.Service.BatchUpdateUserDepartmentsAsync(new UserDepartmentBatchUpdateCommand(
            UserId,
            [new UserDepartmentBatchAssignItem(10, IsMain: false)],
            [100]));

        Assert.Equal(ValidityStatus.Valid, row.Status);
        Assert.Empty(result.RevokedDepartmentIds);
        fixture.VerifyNothingWritten();
    }

    /// <summary>
    /// 只撤不分配时不要求当前租户成员：已离开租户的用户也能清理其部门归属。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_RevokeOnly_ShouldNotRequireTenantMember()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, departmentId: 10, isMain: true);

        _ = await fixture.Service.BatchUpdateUserDepartmentsAsync(new UserDepartmentBatchUpdateCommand(UserId, [], [100]));

        fixture.TenantUserRepository.Verify(
            repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 任一部门停用时整批拒绝，撤销也不落库。
    /// </summary>
    [Fact]
    public async Task BatchUpdate_DisabledDepartment_ShouldRejectWholeBatch()
    {
        var fixture = new Fixture();
        fixture.AddRow(basicId: 100, departmentId: 10);
        fixture.AddDepartment(30, EnableStatus.Disabled);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BatchUpdateUserDepartmentsAsync(
            new UserDepartmentBatchUpdateCommand(UserId, [new UserDepartmentBatchAssignItem(30, IsMain: false)], [100])));

        Assert.Contains("停用部门", exception.Message, StringComparison.Ordinal);
        fixture.VerifyNothingWritten();
    }

    #endregion

    /// <summary>
    /// 批量变更的测试夹具：仓储按内存行集合回放查询，写入逐次记录。
    /// </summary>
    private sealed class Fixture
    {
        private readonly List<SysUserDepartment> _rows = [];

        private readonly List<SysDepartment> _departments = [];

        public Fixture()
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

            AddDepartment(10);
            AddDepartment(20);
            DepartmentRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _departments.FirstOrDefault(department => department.BasicId == id));

            UserDepartmentRepository
                .Setup(repo => repo.GetListAsync(
                    It.IsAny<Expression<Func<SysUserDepartment, bool>>>(),
                    It.IsAny<Expression<Func<SysUserDepartment, object>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysUserDepartment, bool>> predicate, Expression<Func<SysUserDepartment, object>> orderBy, CancellationToken _) =>
                    (IReadOnlyList<SysUserDepartment>)_rows.Where(predicate.Compile()).OrderBy(orderBy.Compile()).ToList());
            UserDepartmentRepository
                .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserDepartment>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserDepartment> entities, CancellationToken _) =>
                {
                    var list = entities.ToList();
                    Updated.AddRange(list);
                    return list;
                });
            UserDepartmentRepository
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysUserDepartment>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysUserDepartment> entities, CancellationToken _) =>
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
                new Mock<IUserDataScopeRepository>().Object,
                DepartmentRepository.Object,
                UserDepartmentRepository.Object,
                new Mock<IUserSessionRepository>().Object,
                new TestCurrentTenant(7),
                new Mock<IPasswordHistoryDomainService>().Object,
                new Mock<IConstraintRuleEnforcementDomainService>().Object,
                new Mock<ITenantQuotaDomainService>().Object,
                NullLogger<UserDomainService>.Instance);
        }

        public UserDomainService Service { get; }

        public Mock<ITenantUserRepository> TenantUserRepository { get; } = new();

        public Mock<IDepartmentRepository> DepartmentRepository { get; } = new();

        public Mock<IUserDepartmentRepository> UserDepartmentRepository { get; } = new();

        public List<SysUserDepartment> Updated { get; } = [];

        public List<SysUserDepartment> Added { get; } = [];

        public Task<UserDepartmentBatchUpdateResult> AssignAsync(params UserDepartmentBatchAssignItem[] assigns) =>
            Service.BatchUpdateUserDepartmentsAsync(new UserDepartmentBatchUpdateCommand(UserId, assigns, []));

        public void AddDepartment(long id, EnableStatus status = EnableStatus.Enabled)
        {
            var department = new SysDepartment
            {
                TenantId = 7,
                DepartmentCode = $"D-{id}",
                DepartmentName = $"部门{id}",
                Status = status
            };
            SaasTestHelper.SetBasicId(department, id);
            _departments.Add(department);
        }

        public SysUserDepartment AddRow(
            long basicId,
            long departmentId,
            bool isMain = false,
            ValidityStatus status = ValidityStatus.Valid,
            int createdOffsetDays = 0,
            long userId = UserId)
        {
            var row = new SysUserDepartment
            {
                UserId = userId,
                DepartmentId = departmentId,
                IsMain = isMain,
                Status = status,
                CreatedTime = BaseTime.AddDays(createdOffsetDays)
            };
            SaasTestHelper.SetBasicId(row, basicId);
            _rows.Add(row);
            return row;
        }

        public void VerifyNothingWritten()
        {
            UserDepartmentRepository.Verify(
                repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysUserDepartment>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            UserDepartmentRepository.Verify(
                repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysUserDepartment>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
