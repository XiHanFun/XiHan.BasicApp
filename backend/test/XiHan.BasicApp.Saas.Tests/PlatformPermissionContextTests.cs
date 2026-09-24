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
/// 平台专属权限码只在平台上下文生效：持有者带通配权限进入租户也调不动平台接口。
/// </summary>
public sealed class PlatformPermissionContextTests
{
    private const long UserId = 1;

    /// <summary>
    /// 租户上下文里通配权限不放行平台专属码
    /// </summary>
    [Fact]
    public async Task TenantContext_Wildcard_DeniesPlatformOnlyCode()
    {
        var checker = CreateChecker(tenantId: 7, "*");

        Assert.False(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.Tenant.Create));
        Assert.False(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.Tenant.Read));
    }

    /// <summary>
    /// 租户上下文里通配权限照常放行两侧都生效的码
    /// </summary>
    [Fact]
    public async Task TenantContext_Wildcard_GrantsSharedCode()
    {
        var checker = CreateChecker(tenantId: 7, "*");

        Assert.True(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.User.Read));
    }

    /// <summary>
    /// 平台上下文里平台专属码按快照判定
    /// </summary>
    [Fact]
    public async Task PlatformContext_GrantsPlatformOnlyCode()
    {
        var checker = CreateChecker(tenantId: null, SaasPermissionCodes.Tenant.Create);

        Assert.True(await checker.IsGrantedAsync(UserId.ToString(), SaasPermissionCodes.Tenant.Create));
    }

    /// <summary>
    /// 任一即可：剔除平台专属码后仍按其余码判定
    /// </summary>
    [Fact]
    public async Task TenantContext_IsAnyGranted_IgnoresPlatformOnlyCode()
    {
        var checker = CreateChecker(tenantId: 7, SaasPermissionCodes.Tenant.Create, SaasPermissionCodes.User.Read);

        Assert.True(await checker.IsAnyGrantedAsync(UserId.ToString(), [SaasPermissionCodes.Tenant.Create, SaasPermissionCodes.User.Read]));
        Assert.False(await checker.IsAnyGrantedAsync(UserId.ToString(), [SaasPermissionCodes.Tenant.Create]));
    }

    /// <summary>
    /// 下发给前端的权限清单在租户上下文里不含平台专属码
    /// </summary>
    [Fact]
    public async Task TenantContext_GrantedPermissions_ExcludesPlatformOnlyCodes()
    {
        var checker = CreateChecker(tenantId: 7, SaasPermissionCodes.Tenant.Read, SaasPermissionCodes.User.Read);

        var granted = await checker.GetGrantedPermissionsAsync(UserId.ToString());

        Assert.Equal([SaasPermissionCodes.User.Read], granted);
    }

    /// <summary>
    /// 租户目录、租户导出、版本导出、支持人员入驻都属平台专属，不可授予租户
    /// </summary>
    [Theory]
    [InlineData(SaasPermissionCodes.Tenant.Read)]
    [InlineData(SaasPermissionCodes.Tenant.Export)]
    [InlineData(SaasPermissionCodes.Tenant.SupportMember)]
    [InlineData(SaasPermissionCodes.TenantEdition.Export)]
    public void TenantCatalogCodes_ArePlatformOnly(string code)
    {
        Assert.Contains(code, SaasPlatformPermissions.PlatformOnlyCodes);
        Assert.False(SaasPlatformPermissions.IsTenantGrantable(code));
        Assert.False(SaasPlatformPermissions.IsEffectiveIn(code, isPlatformContext: false));
    }

    private static SaasPermissionChecker CreateChecker(long? tenantId, params string[] permissions)
    {
        var snapshots = new Mock<IAuthorizationSnapshotQueryService>();
        snapshots
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot([], [.. permissions], []));

        var sessions = new Mock<IUserSessionRepository>();
        sessions
            .Setup(repository => repository.GetByUserSessionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession?)null);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.UserId).Returns(UserId);

        return new SaasPermissionChecker(snapshots.Object, sessions.Object, currentUser.Object, new TestCurrentTenant(tenantId));
    }
}
