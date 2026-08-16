// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// 用户角色授予路径的 SSD 执法接入测试：约束违规必须阻断授予且不落库。
/// </summary>
public sealed class UserRoleGrantSoDEnforcementTests
{
    /// <summary>
    /// 拒绝类违规必须阻断授予且不写入用户角色。
    /// </summary>
    [Fact]
    public async Task CreateUserRole_WhenSsdViolationDenies_ShouldBlockGrantWithoutPersist()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMemberAndRole();
        fixture.SetupExistingRoles(userId: 1, [10]);
        fixture.Enforcement
            .Setup(service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConstraintEnforcementResult(
            [
                new ConstraintViolation(1, "SSD-01", "出纳与会计互斥", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.Deny)
            ]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateUserRoleAsync(new UserRoleGrantCommand(1, 20, null, null, "测试", null)));

        Assert.Contains("SSD-01", exception.Message, StringComparison.Ordinal);
        Assert.Contains("出纳与会计互斥", exception.Message, StringComparison.Ordinal);
        fixture.UserRoleRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 需审批类违规按失败关闭处理（当前无自动审批路由，先阻断以防越权）。
    /// </summary>
    [Fact]
    public async Task CreateUserRole_WhenSsdViolationRequiresApproval_ShouldBlock()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMemberAndRole();
        fixture.SetupExistingRoles(userId: 1, [10]);
        fixture.Enforcement
            .Setup(service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConstraintEnforcementResult(
            [
                new ConstraintViolation(1, "SSD-02", "审批约束", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.RequireApproval)
            ]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateUserRoleAsync(new UserRoleGrantCommand(1, 20, null, null, "测试", null)));

        Assert.Contains("SSD-02", exception.Message, StringComparison.Ordinal);
        fixture.UserRoleRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 警告类违规放行并正常落库。
    /// </summary>
    [Fact]
    public async Task CreateUserRole_WhenSsdViolationWarns_ShouldAllowGrant()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMemberAndRole();
        fixture.SetupExistingRoles(userId: 1, [10]);
        fixture.Enforcement
            .Setup(service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConstraintEnforcementResult(
            [
                new ConstraintViolation(1, "SSD-03", "警告约束", ConstraintType.SSD, 0, [10L, 20L], ViolationAction.Warning)
            ]));

        var result = await fixture.Service.CreateUserRoleAsync(new UserRoleGrantCommand(1, 20, null, null, "测试", null));

        Assert.NotNull(result.UserRole);
        fixture.UserRoleRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 无违规时正常落库。
    /// </summary>
    [Fact]
    public async Task CreateUserRole_WithoutViolation_ShouldPersist()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMemberAndRole();
        fixture.SetupExistingRoles(userId: 1, [10]);
        fixture.Enforcement
            .Setup(service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConstraintEnforcementResult.Pass);

        var result = await fixture.Service.CreateUserRoleAsync(new UserRoleGrantCommand(1, 20, null, null, "测试", null));

        Assert.Equal(20, result.UserRole.RoleId);
        fixture.UserRoleRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 执法输入必须为「现有有效角色 + 拟授予角色」，约束类型固定为 SSD。
    /// </summary>
    [Fact]
    public async Task CreateUserRole_ShouldEvaluateExistingRolesPlusNewRoleAsSsd()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMemberAndRole();
        fixture.SetupExistingRoles(userId: 1, [10]);
        IEnumerable<long>? capturedRoleIds = null;
        ConstraintType? capturedType = null;
        fixture.Enforcement
            .Setup(service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<long> roleIds, ConstraintType type, CancellationToken _) =>
            {
                capturedRoleIds = roleIds.ToList();
                capturedType = type;
            })
            .ReturnsAsync(ConstraintEnforcementResult.Pass);

        _ = await fixture.Service.CreateUserRoleAsync(new UserRoleGrantCommand(1, 20, null, null, "测试", null));

        Assert.Equal([10L, 20L], capturedRoleIds);
        Assert.Equal(ConstraintType.SSD, capturedType);
    }

    /// <summary>
    /// 无既有角色时仍应以拟授予角色单独评估。
    /// </summary>
    [Fact]
    public async Task CreateUserRole_WithoutExistingRoles_ShouldStillEvaluate()
    {
        var fixture = CreateFixture();
        fixture.SetupValidMemberAndRole();
        fixture.SetupExistingRoles(userId: 1, []);
        fixture.Enforcement
            .Setup(service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConstraintEnforcementResult.Pass);

        _ = await fixture.Service.CreateUserRoleAsync(new UserRoleGrantCommand(1, 20, null, null, "测试", null));

        fixture.Enforcement.Verify(
            service => service.EvaluateRoleAssignmentsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<ConstraintType>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 创建用户角色授予路径的测试夹具（模拟全部依赖）。
    /// </summary>
    private static GrantFixture CreateFixture()
    {
        var tenantUserRepository = new Mock<ITenantUserRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var userRoleRepository = new Mock<IUserRoleRepository>();
        userRoleRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysUserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRoleRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserRole entity, CancellationToken _) => entity);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns((long?)7);

        var enforcement = new Mock<IConstraintRuleEnforcementDomainService>();

        var service = new UserDomainService(
            new Mock<IUserRepository>().Object,
            new Mock<IUserSecurityRepository>().Object,
            tenantUserRepository.Object,
            new Mock<IPasswordHasher>().Object,
            new Mock<IAuthenticationService>().Object,
            userRoleRepository.Object,
            roleRepository.Object,
            new Mock<IUserPermissionRepository>().Object,
            new Mock<IPermissionRepository>().Object,
            new Mock<IUserDataScopeRepository>().Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IUserDepartmentRepository>().Object,
            new Mock<IUserSessionRepository>().Object,
            currentTenant.Object,
            new Mock<IPasswordHistoryDomainService>().Object,
            enforcement.Object,
            NullLogger<UserDomainService>.Instance);
        return new GrantFixture(service, userRoleRepository, tenantUserRepository, roleRepository, enforcement);
    }

    /// <summary>
    /// 用户角色授予测试依赖集合。
    /// </summary>
    private sealed record GrantFixture(
        UserDomainService Service,
        Mock<IUserRoleRepository> UserRoleRepository,
        Mock<ITenantUserRepository> TenantUserRepository,
        Mock<IRoleRepository> RoleRepository,
        Mock<IConstraintRuleEnforcementDomainService> Enforcement)
    {
        /// <summary>
        /// 预设有效租户成员与可分配角色。
        /// </summary>
        public void SetupValidMemberAndRole()
        {
            TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SysTenantUser
                {
                    UserId = 1,
                    MemberType = TenantMemberType.Member,
                    InviteStatus = TenantMemberInviteStatus.Accepted,
                    Status = ValidityStatus.Valid
                });
            RoleRepository
                .Setup(repo => repo.GetByIdAsync(20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SysRole
                {
                    TenantId = 7,
                    RoleCode = "ROLE-20",
                    RoleName = "角色20",
                    RoleType = RoleType.Custom,
                    Status = EnableStatus.Enabled
                });
        }

        /// <summary>
        /// 预设用户现有有效角色。
        /// </summary>
        public void SetupExistingRoles(long userId, IReadOnlyList<long> roleIds)
        {
            var userRoles = roleIds
                .Select(roleId => new SysUserRole { UserId = userId, RoleId = roleId })
                .ToArray();
            UserRoleRepository
                .Setup(repo => repo.GetValidByUserIdAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userRoles);
        }
    }
}
