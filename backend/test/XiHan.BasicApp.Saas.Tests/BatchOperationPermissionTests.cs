// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Moq;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 批量变更接口按实际操作鉴权测试。
/// </summary>
/// <remarks>
/// 回归锚点：批量接口曾在方法上叠挂授予与撤销两个 <c>[PermissionAuthorize]</c>，叠挂即「全部都要」；
/// 入口按钮只挂授予权限，于是看得到「角色成员」「分配权限」，只加人也一保存就被拒。
/// 现在特性只门控入口，撤销、启停等操作在本次请求里真的出现时才校验各自的权限。
/// </remarks>
public sealed class BatchOperationPermissionTests
{
    private const long CurrentUserId = 7;

    private readonly Mock<IOperationPermissionGuard> _guard = new();

    /// <summary>
    /// 每个批量接口只挂一个入口权限：再叠一个就又回到「全部都要」。
    /// </summary>
    [Theory]
    [InlineData(typeof(UserRoleAppService), nameof(UserRoleAppService.BatchUpdateUserRolesAsync), SaasPermissionCodes.UserRole.Grant)]
    [InlineData(typeof(UserRoleAppService), nameof(UserRoleAppService.BatchUpdateRoleMembersAsync), SaasPermissionCodes.UserRole.Grant)]
    [InlineData(typeof(RoleAppService), nameof(RoleAppService.BatchUpdateRolePermissionsAsync), SaasPermissionCodes.RolePermission.Grant)]
    [InlineData(typeof(RoleAppService), nameof(RoleAppService.BatchUpdateRoleParentsAsync), SaasPermissionCodes.RoleHierarchy.Read)]
    [InlineData(typeof(UserPermissionAppService), nameof(UserPermissionAppService.BatchUpdateUserPermissionsAsync), SaasPermissionCodes.UserPermission.Grant)]
    [InlineData(typeof(UserDepartmentAppService), nameof(UserDepartmentAppService.BatchUpdateUserDepartmentsAsync), SaasPermissionCodes.UserDepartment.Grant)]
    [InlineData(typeof(TenantEditionAppService), nameof(TenantEditionAppService.BatchUpdateTenantEditionPermissionsAsync), SaasPermissionCodes.TenantEditionPermission.Read)]
    public void BatchEndpoints_ShouldGateEntryWithSinglePermission(Type serviceType, string methodName, string expectedCode)
    {
        var codes = serviceType.GetMethod(methodName)!
            .GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.PermissionCode)
            .ToList();

        Assert.Equal([expectedCode], codes);
    }

    /// <summary>
    /// 缺权限时按权限显示名提示，而不是只抛权限码。
    /// </summary>
    [Fact]
    public async Task Guard_WhenNotGranted_ShouldThrowWithPermissionName()
    {
        var checker = new Mock<IPermissionChecker>();
        checker
            .Setup(item => item.IsGrantedAsync(CurrentUserId.ToString(), SaasPermissionCodes.UserRole.Revoke, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateGuard(checker).EnsureGrantedAsync(SaasPermissionCodes.UserRole.Revoke));

        Assert.Contains("用户角色撤销", exception.Message);
    }

    /// <summary>
    /// 持有权限时放行。
    /// </summary>
    [Fact]
    public async Task Guard_WhenGranted_ShouldPass()
    {
        var checker = new Mock<IPermissionChecker>();
        checker
            .Setup(item => item.IsGrantedAsync(CurrentUserId.ToString(), SaasPermissionCodes.UserRole.Revoke, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateGuard(checker).EnsureGrantedAsync(SaasPermissionCodes.UserRole.Revoke);

        checker.VerifyAll();
    }

    /// <summary>
    /// 只加人的角色成员提交不需要撤销权限。
    /// </summary>
    [Fact]
    public async Task RoleMembers_GrantOnly_ShouldNotRequireRevoke()
    {
        var domain = new Mock<IUserDomainService>();
        domain
            .Setup(item => item.BatchUpdateRoleMembersAsync(It.IsAny<RoleMemberBatchUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleMemberBatchUpdateResult([1], []));

        await CreateUserRoleService(domain).BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateDto { RoleId = 10, GrantUserIds = [1] });

        _guard.Verify(item => item.EnsureGrantedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        domain.Verify(item => item.BatchUpdateRoleMembersAsync(It.IsAny<RoleMemberBatchUpdateCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 含移出项而缺撤销权限时，在写入前拒绝整个提交。
    /// </summary>
    [Fact]
    public async Task RoleMembers_WithRevokeButNoRevokePermission_ShouldRejectBeforeWriting()
    {
        _guard
            .Setup(item => item.EnsureGrantedAsync(SaasPermissionCodes.UserRole.Revoke, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserFriendlyException("缺少「用户角色撤销」权限。"));
        var domain = new Mock<IUserDomainService>();

        await Assert.ThrowsAsync<UserFriendlyException>(() => CreateUserRoleService(domain)
            .BatchUpdateRoleMembersAsync(new RoleMemberBatchUpdateDto { RoleId = 10, GrantUserIds = [1], RevokeUserRoleIds = [100] }));

        domain.Verify(item => item.BatchUpdateRoleMembersAsync(It.IsAny<RoleMemberBatchUpdateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 角色权限：只授予不校验撤销，含撤销才校验撤销。
    /// </summary>
    /// <param name="revokeIds">撤销的授权记录主键。</param>
    /// <param name="expectRevokeCheck">是否应校验撤销权限。</param>
    [Theory]
    [InlineData(new long[0], false)]
    [InlineData(new long[] { 0 }, false)]
    [InlineData(new long[] { 300 }, true)]
    public async Task RolePermissions_ShouldRequireRevokeOnlyWhenRevoking(long[] revokeIds, bool expectRevokeCheck)
    {
        var domain = new Mock<IRoleDomainService>();
        domain
            .Setup(item => item.BatchUpdateRolePermissionsAsync(It.IsAny<RolePermissionBatchUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RolePermissionBatchUpdateResult([], []));
        var service = new RoleAppService(
            domain.Object,
            Mock.Of<IRoleHierarchyDomainService>(),
            Mock.Of<ISaasCacheInvalidator>(),
            Mock.Of<IAuthorizationChangeNotifier>(),
            Mock.Of<IImpersonationPolicyService>(),
            Mock.Of<ISuperAdminProtector>(),
            Mock.Of<IRolePermissionRepository>(),
            _guard.Object,
            Mock.Of<IFieldSecurityService>());

        await service.BatchUpdateRolePermissionsAsync(new RolePermissionBatchUpdateDto
        {
            RoleId = 10,
            GrantPermissionIds = [200],
            RevokeRolePermissionIds = [.. revokeIds]
        });

        _guard.Verify(
            item => item.EnsureGrantedAsync(SaasPermissionCodes.RolePermission.Revoke, It.IsAny<CancellationToken>()),
            expectRevokeCheck ? Times.Once() : Times.Never());
    }

    /// <summary>
    /// 角色上级：新增校验新增权限并过模仿登录授出校验，解除校验删除权限；实际变化逐条留审计并失效导航。
    /// </summary>
    /// <param name="addIds">新增的上级。</param>
    /// <param name="removeIds">解除的上级。</param>
    [Theory]
    [InlineData(new long[] { 20 }, new long[0])]
    [InlineData(new long[0], new long[] { 30 })]
    [InlineData(new long[] { 20 }, new long[] { 30 })]
    public async Task RoleParents_ShouldCheckOnlyTheOperationsPresent(long[] addIds, long[] removeIds)
    {
        var hierarchy = new Mock<IRoleHierarchyDomainService>();
        hierarchy
            .Setup(item => item.UpdateParentsAsync(It.IsAny<RoleHierarchyBatchUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleHierarchyBatchUpdateCommand command, CancellationToken _) =>
                new RoleHierarchyBatchUpdateResult(command.AddParentRoleIds, command.RemoveParentRoleIds));
        var impersonation = new Mock<IImpersonationPolicyService>();
        var notifier = new Mock<IAuthorizationChangeNotifier>();
        var cache = new Mock<ISaasCacheInvalidator>();
        var service = new RoleAppService(
            Mock.Of<IRoleDomainService>(),
            hierarchy.Object,
            cache.Object,
            notifier.Object,
            impersonation.Object,
            Mock.Of<ISuperAdminProtector>(),
            Mock.Of<IRolePermissionRepository>(),
            _guard.Object,
            Mock.Of<IFieldSecurityService>());

        await service.BatchUpdateRoleParentsAsync(new RoleHierarchyBatchUpdateDto
        {
            RoleId = 10,
            AddParentRoleIds = [.. addIds],
            RemoveParentRoleIds = [.. removeIds]
        });

        _guard.Verify(
            item => item.EnsureGrantedAsync(SaasPermissionCodes.RoleHierarchy.Create, It.IsAny<CancellationToken>()),
            addIds.Length > 0 ? Times.Once() : Times.Never());
        _guard.Verify(
            item => item.EnsureGrantedAsync(SaasPermissionCodes.RoleHierarchy.Delete, It.IsAny<CancellationToken>()),
            removeIds.Length > 0 ? Times.Once() : Times.Never());
        impersonation.Verify(
            item => item.EnsureCanGrantRoleIdsAsync(It.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(addIds)), It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (var parentId in addIds)
        {
            notifier.Verify(item => item.NotifyAsync(PermissionChangeType.RoleAddParent, null, 10, null, null, parentId, It.IsAny<CancellationToken>()), Times.Once);
        }

        foreach (var parentId in removeIds)
        {
            notifier.Verify(item => item.NotifyAsync(PermissionChangeType.RoleRemoveParent, null, 10, null, null, parentId, It.IsAny<CancellationToken>()), Times.Once);
        }

        cache.Verify(item => item.InvalidateNavigationAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 版本权限：授予、撤销、启停各自只校验本次出现的那一项，与抽屉里按按钮码分别放开一致。
    /// </summary>
    [Fact]
    public async Task EditionPermissions_ShouldCheckOnlyTheOperationsPresent()
    {
        var domain = new Mock<ITenantEditionDomainService>();
        domain
            .Setup(item => item.BatchUpdateTenantEditionPermissionsAsync(It.IsAny<TenantEditionPermissionBatchUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantEditionPermissionBatchUpdateResult(0, 0, 1));
        var service = new TenantEditionAppService(
            domain.Object,
            Mock.Of<ITenantProvisionDomainService>(),
            Mock.Of<ISaasCacheInvalidator>(),
            _guard.Object,
            Mock.Of<IFieldSecurityService>());

        await service.BatchUpdateTenantEditionPermissionsAsync(new TenantEditionPermissionBatchUpdateDto
        {
            EditionId = 1,
            StatusChanges = [new TenantEditionPermissionStatusItemDto { BasicId = 50, Status = ValidityStatus.Valid }]
        });

        _guard.Verify(item => item.EnsureGrantedAsync(SaasPermissionCodes.TenantEditionPermission.Update, It.IsAny<CancellationToken>()), Times.Once);
        _guard.Verify(item => item.EnsureGrantedAsync(SaasPermissionCodes.TenantEditionPermission.Grant, It.IsAny<CancellationToken>()), Times.Never);
        _guard.Verify(item => item.EnsureGrantedAsync(SaasPermissionCodes.TenantEditionPermission.Revoke, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static OperationPermissionGuard CreateGuard(Mock<IPermissionChecker> checker)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(CurrentUserId);
        return new OperationPermissionGuard(currentUser.Object, checker.Object);
    }

    private UserRoleAppService CreateUserRoleService(Mock<IUserDomainService> domain)
    {
        return new UserRoleAppService(
            domain.Object,
            Mock.Of<ISaasCacheInvalidator>(),
            Mock.Of<IAuthorizationChangeNotifier>(),
            Mock.Of<IImpersonationPolicyService>(),
            Mock.Of<ISuperAdminProtector>(),
            Mock.Of<IUserRoleRepository>(),
            _guard.Object);
    }
}
