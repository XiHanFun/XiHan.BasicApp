// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 按钮码下发测试。
/// </summary>
/// <remarks>
/// 前端页面按钮的门控依据是这里下发的按钮码，前端不持有任何权限码。
/// 因此这条通道的每一种判定都必须落在测试里：
/// <list type="number">
/// <item>只下发当前用户有权限的按钮；</item>
/// <item>通配 <c>*</c> 下发全部按钮；</item>
/// <item>模仿态下按禁用清单剔除，与鉴权入口同口径；</item>
/// <item>按钮行缺权限绑定时判未授权（fail-closed），不能因为播种没到位就把按钮全放出去。</item>
/// </list>
/// </remarks>
public sealed class SaasAppGrantedButtonCodeTests
{
    private readonly Mock<IMenuRepository> _menuRepository = new();
    private readonly Mock<IPermissionRepository> _permissionRepository = new();
    private readonly Mock<IDistributedCache<SaasMenuRoutesCacheItem, string>> _menuRoutesCache = new();

    /// <summary>
    /// 只下发当前用户持有权限的按钮。
    /// </summary>
    [Fact]
    public async Task GetGrantedButtonCodesAsync_ShouldReturnOnlyPermittedButtons()
    {
        ArrangeCatalog();

        var codes = await CreateService().GetGrantedButtonCodesAsync(Snapshot([SaasPermissionCodes.User.Create], [1]));

        Assert.Equal(["identity.user.create"], codes);
    }

    /// <summary>
    /// 通配权限下发全部按钮。
    /// </summary>
    [Fact]
    public async Task GetGrantedButtonCodesAsync_WithWildcard_ShouldReturnEveryButton()
    {
        ArrangeCatalog();

        var codes = await CreateService().GetGrantedButtonCodesAsync(Snapshot(["*"], []));

        Assert.Equal(["identity.user.create", "identity.user.delete"], codes);
    }

    /// <summary>
    /// 模仿态下按禁用清单剔除，通配也顶不掉。
    /// </summary>
    [Fact]
    public async Task GetGrantedButtonCodesAsync_WhileImpersonating_ShouldDropDeniedButtons()
    {
        ArrangeCatalog();

        var codes = await CreateService().GetGrantedButtonCodesAsync(
            Snapshot(["*"], []),
            ImpersonationDefaults.DeniedPermissionCodes);

        // saas:user:delete 在模仿态禁用清单里
        Assert.Equal(["identity.user.create"], codes);
    }

    /// <summary>
    /// 按钮行没绑上权限时判未授权，而不是当作「无需权限」放行。
    /// </summary>
    [Fact]
    public async Task GetGrantedButtonCodesAsync_ButtonWithoutPermission_ShouldBeDenied()
    {
        ArrangeCatalog(unboundButtonCode: "identity.user.orphan");

        var codes = await CreateService().GetGrantedButtonCodesAsync(Snapshot(["*"], []));

        Assert.DoesNotContain("identity.user.orphan", codes);
    }

    /// <summary>
    /// 隐藏的按钮不下发。
    /// </summary>
    [Fact]
    public async Task GetGrantedButtonCodesAsync_InvisibleButton_ShouldBeSkipped()
    {
        ArrangeCatalog(hideDelete: true);

        var codes = await CreateService().GetGrantedButtonCodesAsync(Snapshot(["*"], []));

        Assert.Equal(["identity.user.create"], codes);
    }

    /// <summary>
    /// 一个按钮都没登记时返回空集合，不查权限表。
    /// </summary>
    [Fact]
    public async Task GetGrantedButtonCodesAsync_NoButtons_ShouldReturnEmpty()
    {
        _menuRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysMenu, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var codes = await CreateService().GetGrantedButtonCodesAsync(Snapshot(["*"], []));

        Assert.Empty(codes);
        _permissionRepository.Verify(
            repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysPermission, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 模仿态下菜单也按禁用清单剔除，通配顶不掉。
    /// </summary>
    /// <remarks>
    /// 菜单可见性判的是权限主键，而禁用清单是权限码：只把清单里的码从快照里删掉裁不掉菜单，
    /// 必须把清单本身传进来。
    /// </remarks>
    [Fact]
    public async Task GetRoutesAsync_WhileImpersonating_ShouldDropDeniedMenus()
    {
        ArrangeMenuCatalog();

        var routes = await CreateService().GetRoutesAsync(
            Snapshot(["*"], []),
            ImpersonationDefaults.DeniedPermissionCodes);

        // saas:user:delete 在模仿态禁用清单里
        Assert.Equal(["user-create"], routes.Select(route => route.Name).ToList());
    }

    /// <summary>
    /// 不传禁用清单时菜单照常全下发。
    /// </summary>
    [Fact]
    public async Task GetRoutesAsync_WithoutDeniedCodes_ShouldKeepEveryMenu()
    {
        ArrangeMenuCatalog();

        var routes = await CreateService().GetRoutesAsync(Snapshot(["*"], []));

        Assert.Equal(["user-create", "user-delete"], routes.Select(route => route.Name).ToList());
    }

    /// <summary>
    /// 不关联权限的菜单对所有人可见。
    /// </summary>
    [Fact]
    public async Task GetRoutesAsync_MenuWithoutPermission_ShouldStayVisible()
    {
        ArrangeMenuCatalog(unboundMenuName: "public-page");

        var routes = await CreateService().GetRoutesAsync(
            Snapshot([], []),
            ImpersonationDefaults.DeniedPermissionCodes);

        Assert.Equal(["public-page"], routes.Select(route => route.Name).ToList());
    }

    /// <summary>
    /// 禁用清单进缓存键：模仿态与常态不共用同一条菜单缓存。
    /// </summary>
    [Fact]
    public void MenuRoutesCacheKey_ShouldDifferByDeniedCodes()
    {
        var normal = SaasCacheKeys.MenuRoutes([1, 2], hasAllPermissions: false);
        var impersonating = SaasCacheKeys.MenuRoutes([1, 2], hasAllPermissions: false, ImpersonationDefaults.DeniedPermissionCodes);

        Assert.NotEqual(normal, impersonating);
    }

    /// <summary>
    /// 通配权限同样要按禁用清单分键。
    /// </summary>
    [Fact]
    public void MenuRoutesCacheKey_WithWildcard_ShouldDifferByDeniedCodes()
    {
        var normal = SaasCacheKeys.MenuRoutes([], hasAllPermissions: true);
        var impersonating = SaasCacheKeys.MenuRoutes([], hasAllPermissions: true, ImpersonationDefaults.DeniedPermissionCodes);

        Assert.NotEqual(normal, impersonating);
    }

    /// <summary>
    /// 布置两张菜单：新增页（saas:user:create）与删除页（saas:user:delete，在模仿态禁用清单里）。
    /// </summary>
    private void ArrangeMenuCatalog(string? unboundMenuName = null)
    {
        var menus = new List<SysMenu>
        {
            BuildMenu(201, "user-create", permissionId: 1),
            BuildMenu(202, "user-delete", permissionId: 2)
        };
        if (unboundMenuName is not null)
        {
            menus.Clear();
            menus.Add(BuildMenu(203, unboundMenuName, permissionId: null));
        }

        _menuRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysMenu, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(menus);

        var createPermission = new SysPermission { PermissionCode = SaasPermissionCodes.User.Create };
        SaasTestHelper.SetBasicId(createPermission, 1);
        var deletePermission = new SysPermission { PermissionCode = SaasPermissionCodes.User.Delete };
        SaasTestHelper.SetBasicId(deletePermission, 2);

        _permissionRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysPermission, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([createPermission, deletePermission]);
    }

    private static SysMenu BuildMenu(long id, string routeName, long? permissionId)
    {
        var menu = new SysMenu
        {
            MenuCode = routeName,
            MenuName = routeName,
            RouteName = routeName,
            Path = $"/{routeName}",
            MenuType = MenuType.Menu,
            Status = EnableStatus.Enabled,
            IsVisible = true,
            PermissionId = permissionId
        };
        SaasTestHelper.SetBasicId(menu, id);
        return menu;
    }

    private MenuRouteQueryService CreateService()
    {
        return new MenuRouteQueryService(
            _menuRepository.Object,
            _permissionRepository.Object,
            _menuRoutesCache.Object);
    }

    private static AuthorizationSnapshot Snapshot(List<string> permissions, HashSet<long> permissionIds)
    {
        return new AuthorizationSnapshot([], permissions, permissionIds);
    }

    /// <summary>
    /// 布置两个按钮：新增（saas:user:create）与删除（saas:user:delete，在模仿态禁用清单里）。
    /// </summary>
    private void ArrangeCatalog(bool hideDelete = false, string? unboundButtonCode = null)
    {
        var create = BuildButton(101, "identity.user.create", permissionId: 1);
        var delete = BuildButton(102, "identity.user.delete", permissionId: 2, isVisible: !hideDelete);
        var menus = new List<SysMenu> { create, delete };
        if (unboundButtonCode is not null)
        {
            menus.Add(BuildButton(103, unboundButtonCode, permissionId: null));
        }

        _menuRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysMenu, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(menus);

        var createPermission = new SysPermission { PermissionCode = SaasPermissionCodes.User.Create };
        SaasTestHelper.SetBasicId(createPermission, 1);
        var deletePermission = new SysPermission { PermissionCode = SaasPermissionCodes.User.Delete };
        SaasTestHelper.SetBasicId(deletePermission, 2);

        _permissionRepository
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysPermission, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([createPermission, deletePermission]);
    }

    private static SysMenu BuildButton(long id, string code, long? permissionId, bool isVisible = true)
    {
        var menu = new SysMenu
        {
            MenuCode = code,
            MenuName = code,
            MenuType = MenuType.Button,
            Status = EnableStatus.Enabled,
            IsVisible = isVisible,
            PermissionId = permissionId
        };
        SaasTestHelper.SetBasicId(menu, id);
        return menu;
    }
}
