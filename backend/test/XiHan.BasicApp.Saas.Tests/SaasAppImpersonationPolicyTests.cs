// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 模仿登录准入判定测试。
/// </summary>
/// <remarks>
/// 准入判定是整套模仿登录唯一的授权闸门：它放行之后，签发链路会以被模仿者的完整身份签出一枚可用令牌，
/// 之后的鉴权、租户过滤、菜单裁剪全部按被模仿者走，没有第二道人为把关。
/// 因此本类以拒绝路径为主：越权、跨租户、平级、禁用账号、嵌套、自模仿逐条锁住。
/// </remarks>
public sealed class SaasAppImpersonationPolicyTests
{
    private const long OperatorUserId = 1001;
    private const long TargetUserId = 2002;
    private const long TenantId = 10;
    private const long OtherTenantId = 20;

    private readonly Mock<IAuthContextQueryService> _authContext = new();
    private readonly Mock<IAuthorizationSnapshotQueryService> _snapshots = new();
    private readonly Mock<ICurrentTenant> _currentTenant = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IPermissionChecker> _permissionChecker = new();
    private readonly Mock<IPermissionRepository> _permissionRepository = new();
    private readonly Mock<IRoleHierarchyRepository> _roleHierarchyRepository = new();
    private readonly Mock<IRolePermissionRepository> _rolePermissionRepository = new();
    private readonly Mock<ISuperAdminProtector> _superAdminProtector = new();
    private readonly Mock<ITenantUserRepository> _tenantUserRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    private readonly DateTimeOffset _now = new(2026, 8, 29, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 租户管理员模仿本租户普通成员应通过，并把模仿会话落在该租户上下文。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_TenantAdminImpersonatesMember_ShouldPass()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: false);
        ArrangeTenant();
        ArrangeMembership(OperatorUserId, TenantMemberType.Admin);
        ArrangeMembership(TargetUserId, TenantMemberType.Member);

        var plan = await CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now);

        Assert.Equal(TargetUserId, plan.Target.BasicId);
        Assert.Equal(TenantId, plan.TargetTenantId);
        Assert.False(plan.IsCrossTenant);
        Assert.False(plan.OperatorIsSuperAdmin);
    }

    /// <summary>
    /// 超级管理员在平台运维态可模仿任意非超管用户，落点按目标自身唯一租户解析。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_SuperAdminFromPlatform_ShouldResolveTargetOwnTenant()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: true);
        ArrangeTenant();
        ArrangeMembership(TargetUserId, TenantMemberType.Owner);
        _tenantUserRepository
            .Setup(repository => repository.GetActiveByUserIdAsync(TargetUserId, _now, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildMembership(TargetUserId, TenantMemberType.Owner)]);
        _permissionChecker
            .Setup(checker => checker.IsGrantedAsync(
                OperatorUserId.ToString(),
                SaasPermissionCodes.Impersonation.CrossTenant,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var plan = await CreateService().AuthorizeStartAsync(
            OperatorUserId, operatorTenantId: null, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now);

        Assert.Equal(TenantId, plan.TargetTenantId);
        Assert.True(plan.IsCrossTenant);
        Assert.True(plan.OperatorIsSuperAdmin);
    }

    /// <summary>
    /// 超管可以模仿租户所有者与管理员——「不得平级或向上模仿」只约束非超管。
    /// </summary>
    /// <param name="memberType">目标成员类型。</param>
    [Theory]
    [InlineData(TenantMemberType.Owner)]
    [InlineData(TenantMemberType.Admin)]
    public async Task AuthorizeStartAsync_SuperAdminImpersonatesTenantAdmin_ShouldPass(TenantMemberType memberType)
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: true);
        ArrangeTenant();
        ArrangeMembership(TargetUserId, memberType);

        var plan = await CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now);

        Assert.Equal(TenantId, plan.TargetTenantId);
    }

    /// <summary>
    /// 目标是超级管理员时一律拒绝，超管发起也不例外。
    /// </summary>
    /// <param name="operatorIsSuperAdmin">发起人是否超管。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthorizeStartAsync_TargetIsSuperAdmin_ShouldThrow(bool operatorIsSuperAdmin)
    {
        ArrangeTarget();
        ArrangeSnapshot(operatorIsSuperAdmin);
        ArrangeTenant();
        _superAdminProtector
            .Setup(protector => protector.IsProtectedUserAsync(TargetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("超级管理员", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已处于模仿态时不得再次发起，避免模仿链把真实操作者冲掉。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_AlreadyImpersonating_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: true, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("模仿状态", exception.Message, StringComparison.Ordinal);
        _userRepository.Verify(
            repository => repository.GetByIdIgnoreTenantAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 不得模仿自己。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_SelfTarget_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, OperatorUserId, requestedTenantId: null, _now));

        Assert.Contains("自己", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 目标账号被禁用时拒绝。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_TargetDisabled_ShouldThrow()
    {
        ArrangeTarget(status: EnableStatus.Disabled);
        ArrangeSnapshot(isSuperAdmin: true);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("禁用", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 跨租户模仿缺少跨租户权限码时拒绝。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_CrossTenantWithoutPermission_ShouldThrow()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: false);
        ArrangeTenant(OtherTenantId);
        _permissionChecker
            .Setup(checker => checker.IsGrantedAsync(
                It.IsAny<string>(),
                SaasPermissionCodes.Impersonation.CrossTenant,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, OtherTenantId, _now));

        Assert.Contains("跨租户", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非超管在平台运维态下没有成员关系可作判据，一律拒绝。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_NonSuperAdminOnPlatform_ShouldThrow()
    {
        ArrangeTarget(tenantId: 0);
        ArrangeSnapshot(isSuperAdmin: false);
        _permissionChecker
            .Setup(checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, operatorTenantId: null, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("平台运维态", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 目标不是该租户的有效成员时拒绝。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_TargetNotTenantMember_ShouldThrow()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: true);
        ArrangeTenant();
        _tenantUserRepository
            .Setup(repository => repository.GetMembershipAsync(TenantId, TargetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantUser?)null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("有效成员", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 成员关系已过期时视同无效。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_TargetMembershipExpired_ShouldThrow()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: true);
        ArrangeTenant();
        var expired = BuildMembership(TargetUserId, TenantMemberType.Member);
        expired.ExpirationTime = _now.AddDays(-1);
        _tenantUserRepository
            .Setup(repository => repository.GetMembershipAsync(TenantId, TargetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));
    }

    /// <summary>
    /// 非超管不得模仿同级或更高权限的成员。
    /// </summary>
    /// <param name="memberType">目标成员类型。</param>
    [Theory]
    [InlineData(TenantMemberType.Owner)]
    [InlineData(TenantMemberType.Admin)]
    [InlineData(TenantMemberType.PlatformAdmin)]
    public async Task AuthorizeStartAsync_NonSuperAdminTargetsAdministrator_ShouldThrow(TenantMemberType memberType)
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: false);
        ArrangeTenant();
        ArrangeMembership(OperatorUserId, TenantMemberType.Admin);
        ArrangeMembership(TargetUserId, memberType);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("同级或更高权限", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 发起人只是普通成员时不得发起模仿，即便持有权限码。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_OperatorIsPlainMember_ShouldThrow()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: false);
        ArrangeTenant();
        ArrangeMembership(OperatorUserId, TenantMemberType.Member);
        ArrangeMembership(TargetUserId, TenantMemberType.Member);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now));

        Assert.Contains("所有者或管理员", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 超管判定读实时快照而不是令牌里的角色声明。
    /// </summary>
    [Fact]
    public async Task AuthorizeStartAsync_SuperAdminDecision_ShouldReadLiveSnapshot()
    {
        ArrangeTarget();
        ArrangeSnapshot(isSuperAdmin: true);
        ArrangeTenant();
        ArrangeMembership(TargetUserId, TenantMemberType.Owner);
        _currentUser.Setup(user => user.IsInRole(It.IsAny<string>())).Returns(false);

        _ = await CreateService().AuthorizeStartAsync(
            OperatorUserId, TenantId, operatorIsImpersonating: false, TargetUserId, requestedTenantId: null, _now);

        _snapshots.Verify(service => service.BuildAsync(OperatorUserId, _now, It.IsAny<CancellationToken>()), Times.Once);
        _currentUser.Verify(user => user.IsInRole(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// 非超管不得授出平台专属权限码。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantPermissionIdsAsync_PlatformOnlyCode_ShouldThrow()
    {
        ArrangePermissionLookup(1, SaasPermissionCodes.Impersonation.CrossTenant);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateService().EnsureCanGrantPermissionIdsAsync([1]));

        Assert.Contains("平台专属", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非管理类成员不得授出模仿登录权限。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantPermissionIdsAsync_PlainMemberGrantsImpersonation_ShouldThrow()
    {
        ArrangePermissionLookup(2, SaasPermissionCodes.Impersonation.Start);
        _currentUser.Setup(user => user.UserId).Returns(OperatorUserId);
        _currentTenant.Setup(tenant => tenant.Id).Returns(TenantId);
        ArrangeMembership(OperatorUserId, TenantMemberType.Member);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateService().EnsureCanGrantPermissionIdsAsync([2]));

        Assert.Contains("模仿登录权限", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户管理员可以授出模仿登录权限。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantPermissionIdsAsync_TenantAdminGrantsImpersonation_ShouldPass()
    {
        ArrangePermissionLookup(2, SaasPermissionCodes.Impersonation.Start);
        _currentUser.Setup(user => user.UserId).Returns(OperatorUserId);
        _currentTenant.Setup(tenant => tenant.Id).Returns(TenantId);
        ArrangeMembership(OperatorUserId, TenantMemberType.Admin);

        await CreateService().EnsureCanGrantPermissionIdsAsync([2]);
    }

    /// <summary>
    /// 超级管理员授权不受上述限制。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantPermissionIdsAsync_SuperAdmin_ShouldPass()
    {
        _superAdminProtector.Setup(protector => protector.IsCurrentUserSuperAdmin()).Returns(true);

        await CreateService().EnsureCanGrantPermissionIdsAsync([1, 2, 3]);

        _permissionRepository.Verify(
            repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysPermission, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 普通业务权限码不受模仿授权门槛影响。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantPermissionIdsAsync_OrdinaryCode_ShouldPass()
    {
        ArrangePermissionLookup(3, SaasPermissionCodes.User.Read);

        await CreateService().EnsureCanGrantPermissionIdsAsync([3]);

        _tenantUserRepository.Verify(
            repository => repository.GetMembershipAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 角色里含模仿登录权限时，非管理类成员不得把这个角色授出去。
    /// </summary>
    /// <remarks>
    /// 角色是与直授等价的授权通道；这条断言堵住"绕开直授准入、改用角色发模仿权限"的路。
    /// </remarks>
    [Fact]
    public async Task EnsureCanGrantRoleIdsAsync_RoleCarryingImpersonation_ShouldThrow()
    {
        ArrangeRoleExpansion(roleId: 9, permissionId: 2);
        ArrangePermissionLookup(2, SaasPermissionCodes.Impersonation.Start);
        _currentUser.Setup(user => user.UserId).Returns(OperatorUserId);
        _currentTenant.Setup(tenant => tenant.Id).Returns(TenantId);
        ArrangeMembership(OperatorUserId, TenantMemberType.Member);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateService().EnsureCanGrantRoleIdsAsync([9]));

        Assert.Contains("模仿登录权限", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 继承链上的祖先角色带来的模仿权限同样被拦住。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantRoleIdsAsync_AncestorRoleCarryingImpersonation_ShouldThrow()
    {
        _roleHierarchyRepository
            .Setup(repository => repository.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([9, 10]);
        _rolePermissionRepository
            .Setup(repository => repository.GetValidByRoleIdsAsync(
                It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysRolePermission { RoleId = 10, PermissionId = 2 }]);
        ArrangePermissionLookup(2, SaasPermissionCodes.Impersonation.Start);
        _currentUser.Setup(user => user.UserId).Returns(OperatorUserId);
        _currentTenant.Setup(tenant => tenant.Id).Returns(TenantId);
        ArrangeMembership(OperatorUserId, TenantMemberType.Member);

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateService().EnsureCanGrantRoleIdsAsync([9]));
    }

    /// <summary>
    /// 租户管理员可以授出含模仿登录权限的角色。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantRoleIdsAsync_TenantAdmin_ShouldPass()
    {
        ArrangeRoleExpansion(roleId: 9, permissionId: 2);
        ArrangePermissionLookup(2, SaasPermissionCodes.Impersonation.Start);
        _currentUser.Setup(user => user.UserId).Returns(OperatorUserId);
        _currentTenant.Setup(tenant => tenant.Id).Returns(TenantId);
        ArrangeMembership(OperatorUserId, TenantMemberType.Admin);

        await CreateService().EnsureCanGrantRoleIdsAsync([9]);
    }

    /// <summary>
    /// 不含模仿登录权限的普通角色照常可授。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantRoleIdsAsync_OrdinaryRole_ShouldPass()
    {
        ArrangeRoleExpansion(roleId: 9, permissionId: 3);
        ArrangePermissionLookup(3, SaasPermissionCodes.User.Read);

        await CreateService().EnsureCanGrantRoleIdsAsync([9]);

        _tenantUserRepository.Verify(
            repository => repository.GetMembershipAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 超级管理员授出角色不查角色权限。
    /// </summary>
    [Fact]
    public async Task EnsureCanGrantRoleIdsAsync_SuperAdmin_ShouldPass()
    {
        _superAdminProtector.Setup(protector => protector.IsCurrentUserSuperAdmin()).Returns(true);

        await CreateService().EnsureCanGrantRoleIdsAsync([9]);

        _rolePermissionRepository.Verify(
            repository => repository.GetValidByRoleIdsAsync(
                It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 把角色展开成「角色自身 + 祖先角色」的权限集合。
    /// </summary>
    private void ArrangeRoleExpansion(long roleId, long permissionId)
    {
        _roleHierarchyRepository
            .Setup(repository => repository.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([roleId]);
        _rolePermissionRepository
            .Setup(repository => repository.GetValidByRoleIdsAsync(
                It.IsAny<IEnumerable<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysRolePermission { RoleId = roleId, PermissionId = permissionId }]);
    }

    private ImpersonationPolicyService CreateService()
    {
        return new ImpersonationPolicyService(
            _authContext.Object,
            _snapshots.Object,
            _currentTenant.Object,
            _currentUser.Object,
            _permissionChecker.Object,
            _permissionRepository.Object,
            _roleHierarchyRepository.Object,
            _rolePermissionRepository.Object,
            _superAdminProtector.Object,
            _tenantUserRepository.Object,
            _userRepository.Object);
    }

    private void ArrangeTarget(EnableStatus status = EnableStatus.Enabled, long tenantId = TenantId)
    {
        var target = new SysUser { UserName = "target", Status = status, TenantId = tenantId };
        SaasTestHelper.SetBasicId(target, TargetUserId);
        _userRepository
            .Setup(repository => repository.GetByIdIgnoreTenantAsync(TargetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
    }

    private void ArrangeSnapshot(bool isSuperAdmin)
    {
        var roles = isSuperAdmin ? new List<string> { "super_admin" } : ["tenant_admin"];
        _snapshots
            .Setup(service => service.BuildAsync(OperatorUserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot(roles, [], []));
    }

    private void ArrangeTenant(long tenantId = TenantId)
    {
        _authContext
            .Setup(service => service.GetLoginTenantOrThrowAsync(tenantId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginTenantContext(tenantId, "演示租户"));
        ArrangeMembership(TargetUserId, TenantMemberType.Member, tenantId);
    }

    private void ArrangeMembership(long userId, TenantMemberType memberType, long tenantId = TenantId)
    {
        _tenantUserRepository
            .Setup(repository => repository.GetMembershipAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildMembership(userId, memberType, tenantId));
    }

    private void ArrangePermissionLookup(long permissionId, string permissionCode)
    {
        var permission = new SysPermission { PermissionCode = permissionCode };
        SaasTestHelper.SetBasicId(permission, permissionId);
        _permissionRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysPermission, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([permission]);
    }

    private static SysTenantUser BuildMembership(long userId, TenantMemberType memberType, long tenantId = TenantId)
    {
        return new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            MemberType = memberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid
        };
    }
}
