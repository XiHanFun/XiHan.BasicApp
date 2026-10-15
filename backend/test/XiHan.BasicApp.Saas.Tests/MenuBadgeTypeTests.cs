// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 菜单标签类型：取值就是前端组件库的语气词，旧的 default / primary / error 不再接受。
/// </summary>
public sealed class MenuBadgeTypeTests
{
    /// <summary>
    /// 六种语气都能存，前后空白去掉。
    /// </summary>
    [Theory]
    [InlineData("neutral")]
    [InlineData("brand")]
    [InlineData("info")]
    [InlineData("success")]
    [InlineData("warning")]
    [InlineData(" danger ")]
    public async Task Create_WithTone_ShouldStoreTrimmed(string badgeType)
    {
        var service = CreateService();

        var result = await service.CreateAsync(CreateCommand(badgeType));

        Assert.Equal(badgeType.Trim(), result.Menu.BadgeType);
    }

    /// <summary>
    /// 不配标签类型时存空。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_WithoutBadgeType_ShouldStoreNull(string? badgeType)
    {
        var service = CreateService();

        var result = await service.CreateAsync(CreateCommand(badgeType));

        Assert.Null(result.Menu.BadgeType);
    }

    /// <summary>
    /// 旧词与不认识的值一律拒绝，不在库里留第二套词。
    /// </summary>
    [Theory]
    [InlineData("default")]
    [InlineData("primary")]
    [InlineData("error")]
    [InlineData("Danger")]
    [InlineData("red")]
    public async Task Create_WithUnknownBadgeType_ShouldThrow(string badgeType)
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync(CreateCommand(badgeType)));

        Assert.Equal("badgeType", exception.ParamName);
    }

    private static MenuCreateCommand CreateCommand(string? badgeType)
    {
        return new MenuCreateCommand(
            ParentId: null,
            PermissionId: null,
            MenuName: "工作台",
            MenuCode: "workbench",
            MenuType: MenuType.Directory,
            Path: "/workbench",
            Component: null,
            RouteName: null,
            Redirect: null,
            Icon: null,
            Title: null,
            I18nKey: null,
            IsExternal: false,
            ExternalUrl: null,
            IsCache: false,
            IsVisible: true,
            IsAffix: false,
            Badge: "新",
            BadgeType: badgeType,
            BadgeDot: false,
            Metadata: null,
            Status: EnableStatus.Enabled,
            Sort: 0,
            Remark: null);
    }

    private static MenuDomainService CreateService()
    {
        var menuRepository = new Mock<IMenuRepository>();
        menuRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysMenu, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        menuRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysMenu>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysMenu menu, CancellationToken _) => menu);

        return new MenuDomainService(
            menuRepository.Object,
            new Mock<IPermissionRepository>().Object,
            new Mock<ICurrentTenant>().Object);
    }
}
