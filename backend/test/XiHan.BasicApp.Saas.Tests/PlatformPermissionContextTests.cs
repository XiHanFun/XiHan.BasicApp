// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 作用侧不含当前上下文的权限码（随快照下发）先于通配判定拒绝：带通配权限也放不出来。
/// </summary>
public sealed class PlatformPermissionContextTests
{
    private const long UserId = 1;

    /// <summary>
    /// 通配权限不放行上下文拒绝码
    /// </summary>
    [Fact]
    public async Task Wildcard_DeniesContextDeniedCode()
    {
        var checker = CreateChecker(TenantDenied, "*");

        Assert.False(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.Tenant.Create));
        Assert.False(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.Tenant.Read));
    }

    /// <summary>
    /// 通配权限照常放行不在拒绝码里的权限
    /// </summary>
    [Fact]
    public async Task Wildcard_GrantsCodeOutsideContextDenied()
    {
        var checker = CreateChecker(TenantDenied, "*");

        Assert.True(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.User.Read));
    }

    /// <summary>
    /// 不在拒绝码里的权限按快照判定
    /// </summary>
    [Fact]
    public async Task CodeOutsideContextDenied_FollowsSnapshot()
    {
        var checker = CreateChecker([], SaasPermissionCodes.Tenant.Create);

        Assert.True(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.Tenant.Create));
    }

    /// <summary>
    /// 任一即可：剔除拒绝码后仍按其余码判定
    /// </summary>
    [Fact]
    public async Task IsAnyGranted_IgnoresContextDeniedCode()
    {
        var checker = CreateChecker(TenantDenied, SaasPermissionCodes.Tenant.Create, SaasPermissionCodes.User.Read);

        Assert.True(await checker.IsAnyGrantedAsync(UserId.ToString(), [SaasPermissionCodes.Tenant.Create, SaasPermissionCodes.User.Read]));
        Assert.False(await checker.IsAnyGrantedAsync(UserId.ToString(), [SaasPermissionCodes.Tenant.Create]));
    }

    /// <summary>
    /// 全部通过：含一个拒绝码即不通过
    /// </summary>
    [Fact]
    public async Task IsAllGranted_FailsOnContextDeniedCode()
    {
        var checker = CreateChecker(TenantDenied, "*");

        Assert.False(await checker.IsAllGrantedAsync(UserId.ToString(), [SaasPermissionCodes.User.Read, SaasPermissionCodes.Tenant.Create]));
        Assert.True(await checker.IsAllGrantedAsync(UserId.ToString(), [SaasPermissionCodes.User.Read]));
    }

    /// <summary>
    /// 下发给前端的权限清单不含拒绝码
    /// </summary>
    [Fact]
    public async Task GrantedPermissions_ExcludesContextDeniedCodes()
    {
        var checker = CreateChecker(TenantDenied, SaasPermissionCodes.Tenant.Read, SaasPermissionCodes.User.Read);

        var granted = await checker.GetGrantedPermissionsAsync(UserId.ToString());

        Assert.Equal([SaasPermissionCodes.User.Read], granted);
    }

    /// <summary>
    /// 租户目录、租户导出、版本导出、支持人员入驻、跨租户模仿都是平台侧，租户里不生效也不可授
    /// </summary>
    [Theory]
    [InlineData(SaasPermissionCodes.Tenant.Read)]
    [InlineData(SaasPermissionCodes.Tenant.Export)]
    [InlineData(SaasPermissionCodes.Tenant.SupportMember)]
    [InlineData(SaasPermissionCodes.TenantEdition.Export)]
    [InlineData(SaasPermissionCodes.Impersonation.CrossTenant)]
    public void TenantCatalogCodes_ArePlatformSide(string code)
    {
        var side = SaasPermissionDefinitions.All.Single(definition => definition.PermissionCode == code).Side;

        Assert.Equal(PermissionSide.Platform, side);
        Assert.False(side.IsTenantEffective());
        Assert.False(side.IsEffectiveIn(isPlatformContext: false));
        Assert.True(side.IsEffectiveIn(isPlatformContext: true));
    }

    private static readonly string[] TenantDenied = [SaasPermissionCodes.Tenant.Create, SaasPermissionCodes.Tenant.Read];

    private static SaasPermissionChecker CreateChecker(string[] contextDenied, params string[] permissions)
    {
        var snapshots = new Mock<IAuthorizationSnapshotQueryService>();
        snapshots
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot([], [.. permissions], [], new HashSet<string>(contextDenied, StringComparer.OrdinalIgnoreCase)));

        var sessions = new Mock<IUserSessionRepository>();
        sessions
            .Setup(repository => repository.GetByUserSessionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession?)null);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.UserId).Returns(UserId);

        return new SaasPermissionChecker(snapshots.Object, sessions.Object, currentUser.Object);
    }
}
