// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.CodeGeneration.Application.QueryServices;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 表配置查询服务测试。
/// </summary>
public sealed class CodeGenTableQueryServiceTests
{
    private readonly Mock<IMenuRepository> _menuRepository = new();

    private CodeGenTableQueryService CreateService(params SysMenu[] menus)
    {
        _menuRepository
            .Setup(repository => repository.GetListAsync(It.IsAny<Expression<Func<SysMenu, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysMenu, bool>> predicate, CancellationToken _) => (IReadOnlyList<SysMenu>)[.. menus.Where(predicate.Compile())]);
        return new CodeGenTableQueryService(
            Mock.Of<ICodeGenTableRepository>(),
            Mock.Of<ICodeGenTableColumnRepository>(),
            Mock.Of<IFieldSecurityService>(),
            _menuRepository.Object);
    }

    private static SysMenu Menu(long id, string code, MenuType menuType, int sort, long? parentId = null, long tenantId = 0, string? name = null)
    {
        return CodeGenerationTestHelper.WithId(
            new SysMenu { MenuCode = code, MenuName = name ?? code, MenuType = menuType, Sort = sort, ParentId = parentId, TenantId = tenantId },
            id);
    }

    /// <summary>
    /// 父菜单候选是平台菜单树（目录与菜单，按排序、带上级），按钮与租户菜单不进；
    /// 只有带菜单码的目录可选，菜单与没有菜单码的目录只表明位置。
    /// </summary>
    [Fact]
    public async Task GetParentMenuOptionsAsync_ShouldReturnPlatformTreeWithOnlyCodedDirectoriesSelectable()
    {
        var service = CreateService(
            Menu(1, "develop", MenuType.Directory, 800, name: "开发中心"),
            Menu(2, "develop.code-gen", MenuType.Menu, 20, parentId: 1, name: "代码生成"),
            Menu(3, "develop.code-gen.create", MenuType.Button, 1, parentId: 2),
            Menu(4, "", MenuType.Directory, 900, name: "手建目录"),
            Menu(5, "tenant-only", MenuType.Directory, 5, tenantId: 7),
            Menu(6, "workbench", MenuType.Directory, 10, name: "工作台"));

        var options = await service.GetParentMenuOptionsAsync();

        Assert.Equal([6L, 2L, 1L, 4L], options.Select(option => option.Value));
        Assert.Equal([true, false, true, false], options.Select(option => option.Selectable));
        Assert.Equal(1L, options.Single(option => option.Value == 2).ParentValue);
        Assert.Equal("开发中心", options.Single(option => option.Value == 1).Label);
    }
}
