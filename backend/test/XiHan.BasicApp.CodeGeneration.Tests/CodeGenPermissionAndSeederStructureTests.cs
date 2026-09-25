// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.CodeGeneration.Application.Pages;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 权限码常量、按钮映射、页面登记表与三个种子器（权限目录、菜单、内置模板）的结构约束测试。
/// </summary>
/// <remarks>
/// 操作字典由 SaaS 统一播，权限目录按「资源 × 操作」声明，菜单按权限码绑定可见性：
/// 阶段错了或码对不上，种子会直接报错拖垮启动；父目录排在子项之后，子项的父菜单解析不到同样报错。
/// 本文件把阶段、编码与归属固化成断言，在失败消息里列出具体违规项。
/// </remarks>
public sealed class CodeGenPermissionAndSeederStructureTests
{
    /// <summary>
    /// 种子器与各自所在的阶段（与 <c>AddCodeGenerationDataSeeders</c> 的登记一一对应）。
    /// </summary>
    private static readonly (Type Type, int Order)[] Seeders =
    [
        (typeof(CodeGenPermissionCatalogSeeder), SeedOrders.PermissionCatalog + 10),
        (typeof(CodeGenerationMenuSeeder), SeedOrders.Menus + 10),
        (typeof(SysCodeGenTemplateSeeder), SeedOrders.PlatformData + 10)
    ];

    /// <summary>
    /// 不经构造函数取种子器实例，只为读取 Order / Name 这两个纯计算属性。
    /// </summary>
    private static IDataSeeder SeederInstance(Type type)
        => (IDataSeeder)RuntimeHelpers.GetUninitializedObject(type);

    /// <summary>
    /// 构造权限目录种子（只读它的声明）。
    /// </summary>
    private static CodeGenPermissionCatalogSeeder Catalog()
        => new(Mock.Of<ISqlSugarClientResolver>(), NullLogger<CodeGenPermissionCatalogSeeder>.Instance, Mock.Of<IServiceProvider>());

    /// <summary>
    /// 权限码常量类的模块与资源编码必须一致，权限码的资源段由它派生。
    /// </summary>
    [Fact]
    public void PermissionCodes_ModuleAndResourceShouldBeTheSame()
    {
        Assert.Equal("code_gen", CodeGenPermissionCodes.Module, StringComparer.Ordinal);
        Assert.Equal("code_gen", CodeGenPermissionCodes.Resource, StringComparer.Ordinal);
    }

    /// <summary>
    /// 每个权限码都必须是 {资源}:{操作} 两段式，且资源段等于资源常量。
    /// </summary>
    [Fact]
    public void PermissionCodes_EveryCodeShouldFollowTwoSegmentConvention()
    {
        var offenders = PermissionCodeConstants()
            .Where(item => !item.Value.StartsWith(CodeGenPermissionCodes.Resource + ":", StringComparison.Ordinal)
                || item.Value.Count(character => character == ':') != 1
                || item.Value.EndsWith(':'))
            .Select(item => $"{item.Name}={item.Value}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下权限码不符合 {{资源}}:{{操作}} 两段式：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 权限码不得重复：两个常量指向同一个码会让授权配置里出现语义重叠的条目。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldBeUnique()
    {
        var duplicates = PermissionCodeConstants()
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("/", group.Select(item => item.Name))}）")
            .ToList();

        Assert.True(duplicates.Count == 0, $"以下权限码被重复定义：{string.Join("、", duplicates)}");
    }

    /// <summary>
    /// 权限码用到的操作必须都在平台操作字典内，否则权限目录种子直接报错。
    /// </summary>
    [Fact]
    public void PermissionCodes_ActionsShouldExistInSeededOperationDictionary()
    {
        var seededOperations = OperationSeeds.All.Select(operation => operation.Code).ToHashSet(StringComparer.Ordinal);

        var offenders = PermissionCodeConstants()
            .Select(item => item.Value[(item.Value.IndexOf(':', StringComparison.Ordinal) + 1)..])
            .Where(action => !seededOperations.Contains(action))
            .ToList();

        Assert.True(offenders.Count == 0, $"以下操作码不在操作字典内：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 权限目录恰好声明常量表里的全部权限码，全部是平台侧、挂在代码生成资源上。
    /// </summary>
    [Fact]
    public void PermissionCatalog_ShouldDeclareExactlyTheConstantCodesOnPlatformSide()
    {
        var catalog = Catalog();
        var resource = Assert.Single(catalog.Resources);

        Assert.Equal(CodeGenPermissionCodes.Resource, resource.Code);
        Assert.Equal(CodeGenPermissionCodes.Module, catalog.ModuleCode);
        Assert.Equal(
            PermissionCodeConstants().Select(item => item.Value).Order(StringComparer.Ordinal),
            catalog.Permissions.Select(permission => permission.Code).Order(StringComparer.Ordinal));
        Assert.All(catalog.Permissions, permission =>
        {
            Assert.Equal(PermissionSide.Platform, permission.Side);
            Assert.Same(resource, permission.Resource);
        });
    }

    /// <summary>
    /// 标准按钮键必须唯一：键是前端 action key，重复会让两个按钮抢同一个坑位。
    /// </summary>
    [Fact]
    public void ButtonPermissionMappings_ButtonKeysShouldBeUnique()
    {
        var duplicates = ButtonPermissionMappings.Buttons
            .GroupBy(button => button.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, $"以下按钮键重复：{string.Join("、", duplicates)}");
    }

    /// <summary>
    /// 每个按钮的操作码都必须在去重后的标准操作集内，权限码才派生得出来。
    /// </summary>
    [Fact]
    public void ButtonPermissionMappings_EveryButtonActionShouldExistInActions()
    {
        var offenders = ButtonPermissionMappings.Buttons
            .Where(button => !ButtonPermissionMappings.Actions.Contains(button.Action, StringComparer.Ordinal))
            .Select(button => $"{button.Key}→{button.Action}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下按钮的操作码不在标准操作集内：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 标准操作集就是按钮操作码按出现顺序去重的结果，两处不得各写一份。
    /// </summary>
    [Fact]
    public void ButtonPermissionMappings_ActionsShouldBeDistinctButtonActionsInOrder()
    {
        var derived = ButtonPermissionMappings.Buttons
            .Select(button => button.Action)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(derived, ButtonPermissionMappings.Actions);
    }

    /// <summary>
    /// 查询与详情共享读取操作：它们没有独立按钮权限，走列表页的读取权限。
    /// </summary>
    [Fact]
    public void ButtonPermissionMappings_QueryAndDetailShouldShareReadAction()
    {
        var query = ButtonPermissionMappings.Buttons.Single(button => button.Key == "query");
        var detail = ButtonPermissionMappings.Buttons.Single(button => button.Key == "detail");

        Assert.Equal("read", query.Action, StringComparer.Ordinal);
        Assert.Equal("read", detail.Action, StringComparer.Ordinal);
    }

    /// <summary>
    /// 标准按钮集必须覆盖七个基础动作，生成的页面按钮才配得齐。
    /// </summary>
    [Fact]
    public void ButtonPermissionMappings_ShouldCoverAllStandardButtons()
    {
        Assert.Equal(
            ["query", "detail", "create", "update", "delete", "export", "import"],
            ButtonPermissionMappings.Buttons.Select(button => button.Key));
    }

    /// <summary>
    /// 按钮标题不得为空，菜单种子会把它写成按钮节点的显示名。
    /// </summary>
    [Fact]
    public void ButtonPermissionMappings_EveryButtonShouldHaveTitle()
    {
        Assert.All(ButtonPermissionMappings.Buttons, button => Assert.False(string.IsNullOrWhiteSpace(button.Title)));
    }

    /// <summary>
    /// 页面码必须唯一：菜单种子按 MenuCode 幂等落库，重复码会互相覆盖。
    /// </summary>
    [Fact]
    public void PageRegistry_PageCodesShouldBeUnique()
    {
        var duplicates = PageRegistry.All
            .GroupBy(page => page.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, $"以下页面码重复登记：{string.Join("、", duplicates)}");
    }

    /// <summary>
    /// 父目录必须排在子项之前，种子按顺序解析 ParentId。
    /// </summary>
    /// <remarks>顺序反了，子项的父级解析不到，菜单会悄悄跑到顶层去。</remarks>
    [Fact]
    public void PageRegistry_ParentShouldBeDeclaredBeforeChild()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var offenders = new List<string>();
        foreach (var page in PageRegistry.All)
        {
            if (page.ParentCode is not null && !seen.Contains(page.ParentCode))
            {
                offenders.Add($"{page.Code}→{page.ParentCode}");
            }

            seen.Add(page.Code);
        }

        Assert.True(offenders.Count == 0, $"以下页面的父目录未排在其之前：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 开发工具目录是跨模块共用的父级，必须作为首项登记。
    /// </summary>
    [Fact]
    public void PageRegistry_DevelopDirectoryShouldBeDeclaredFirst()
    {
        Assert.Equal(PageRegistry.DevelopDirectoryCode, PageRegistry.All[0].Code, StringComparer.Ordinal);
        Assert.Equal(MenuType.Directory, PageRegistry.All[0].MenuType);
        Assert.Same(PageRegistry.DevelopDirectory, PageRegistry.All[0]);
    }

    /// <summary>
    /// 菜单项必须绑定权限码与组件路径；目录项不绑权限、也没有组件。
    /// </summary>
    [Fact]
    public void PageRegistry_MenuAndDirectoryShouldFollowTheirOwnShape()
    {
        var offenders = new List<string>();
        foreach (var page in PageRegistry.All)
        {
            switch (page.MenuType)
            {
                case MenuType.Menu when string.IsNullOrWhiteSpace(page.PermissionCode):
                    offenders.Add($"{page.Code}（菜单未绑定权限码，可见性无从判定）");
                    break;

                case MenuType.Menu when string.IsNullOrWhiteSpace(page.Component):
                    offenders.Add($"{page.Code}（菜单未指定组件路径）");
                    break;

                case MenuType.Directory when page.PermissionCode is not null:
                    offenders.Add($"{page.Code}（目录不应绑定权限码）");
                    break;

                default:
                    break;
            }
        }

        Assert.True(offenders.Count == 0, $"页面登记形态不符：{string.Join("；", offenders)}");
    }

    /// <summary>
    /// 代码生成页面必须绑定本模块的读取权限码，与权限种子/菜单种子同一事实源。
    /// </summary>
    [Fact]
    public void PageRegistry_CodeGenPageShouldBindReadPermission()
    {
        var page = PageRegistry.All.Single(item => item.Code == CodeGenPermissionCodes.Resource);

        Assert.Equal(CodeGenPermissionCodes.Read, page.PermissionCode, StringComparer.Ordinal);
        Assert.Equal(PageRegistry.DevelopDirectoryCode, page.ParentCode, StringComparer.Ordinal);
        Assert.Equal(MenuType.Menu, page.MenuType);
    }

    /// <summary>
    /// 文案键统一为 <c>menu.{页面码中的 . 与 - 换成 _}</c>，前端 menu.ts 按此维护双语文案。
    /// </summary>
    [Fact]
    public void PageRegistry_I18nKeyShouldFollowNamingConvention()
    {
        var offenders = PageRegistry.All
            .Where(page => !string.Equals(
                page.I18nKey,
                "menu." + page.Code.Replace('.', '_').Replace('-', '_'),
                StringComparison.Ordinal))
            .Select(page => $"{page.Code}→{page.I18nKey}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下页面的 I18nKey 不符合约定：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 页面路径与路由名不得为空，否则前端注册不出可访问的路由。
    /// </summary>
    [Fact]
    public void PageRegistry_PathAndRouteNameShouldNotBeBlank()
    {
        Assert.All(PageRegistry.All, page =>
        {
            Assert.False(string.IsNullOrWhiteSpace(page.Path), $"{page.Code} 的 Path 为空。");
            Assert.False(string.IsNullOrWhiteSpace(page.RouteName), $"{page.Code} 的 RouteName 为空。");
            Assert.StartsWith("/", page.Path, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// 按钮登记：码按「页面码.动作」命名且不重复，挂在本模块的菜单页下，权限码取自 <see cref="CodeGenPermissionCodes"/>——
    /// 菜单种子按权限码解析按钮，解析不到就跳过，前端对应的动作便永远不显示。
    /// </summary>
    [Fact]
    public void PageRegistry_ButtonsShouldHangUnderModulePagesWithDeclaredPermissions()
    {
        var pageCodes = PageRegistry.All
            .Where(page => page.MenuType == MenuType.Menu)
            .Select(page => page.Code)
            .ToHashSet(StringComparer.Ordinal);
        var declaredPermissionCodes = typeof(CodeGenPermissionCodes).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(CodeGenPermissionCodes).Namespace && type.IsAbstract && type.IsSealed)
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Where(code => code.Contains(':', StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(PageRegistry.Buttons);
        Assert.Equal(
            PageRegistry.Buttons.Count,
            PageRegistry.Buttons.Select(button => button.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(PageRegistry.Buttons, button =>
        {
            Assert.Contains(button.ParentCode, pageCodes);
            Assert.StartsWith($"{button.ParentCode}.", button.Code, StringComparison.Ordinal);
            Assert.Contains(button.PermissionCode, declaredPermissionCodes);
        });
    }

    /// <summary>
    /// 种子器各在自己的阶段、用代码生成的号段（+10）：权限目录 → 菜单 → 内置模板。
    /// </summary>
    [Fact]
    public void Seeders_ShouldRunInTheirPhases()
    {
        Assert.All(Seeders, item => Assert.Equal(item.Order, SeederInstance(item.Type).Order));
    }

    /// <summary>
    /// 模块内的种子器正好是这三个：操作字典由 SaaS 统一播，角色授权由运营或演示数据负责。
    /// </summary>
    [Fact]
    public void Seeders_RosterShouldMatchRegisteredChain()
    {
        var discovered = CodeGenerationTestHelper.ModuleAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IDataSeeder)))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal);

        Assert.Equal(Seeders.Select(item => item.Type.Name).Order(StringComparer.Ordinal), discovered);
    }

    /// <summary>
    /// 种子名统一带模块前缀，全部在平台上下文里播。
    /// </summary>
    [Fact]
    public void Seeders_ShouldCarryModulePrefixAndSeedWithinPlatformScope()
    {
        Assert.All(Seeders, item =>
        {
            var name = SeederInstance(item.Type).Name;
            Assert.StartsWith("[CodeGeneration]", name, StringComparison.Ordinal);
            Assert.True(name.Length > "[CodeGeneration]".Length, $"{item.Type.Name} 的种子名只有前缀，没有实际描述。");
            Assert.True(item.Type.IsAssignableTo(typeof(PlatformDataSeederBase)), $"{item.Type.Name} 未继承 PlatformDataSeederBase。");
        });
    }

    /// <summary>
    /// 菜单种子必须由页面登记表驱动，且喂进去的就是本模块 PageRegistry 的那两份清单。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldBeDrivenByModulePageRegistry()
    {
        var seederType = typeof(CodeGenerationMenuSeeder);
        Assert.True(seederType.IsAssignableTo(typeof(PageRegistryMenuSeederBase)));

        var instance = RuntimeHelpers.GetUninitializedObject(seederType);
        var pages = seederType.GetProperty("Pages", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);
        var buttons = seederType.GetProperty("Buttons", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);

        Assert.Same(PageRegistry.All, pages);
        Assert.Same(PageRegistry.Buttons, buttons);
    }

    /// <summary>
    /// 取权限码常量类上的全部 public const string 字段。
    /// </summary>
    private static IReadOnlyList<(string Name, string Value)> PermissionCodeConstants()
    {
        return
        [
            .. typeof(CodeGenPermissionCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                .Where(field => !string.Equals(field.Name, nameof(CodeGenPermissionCodes.Module), StringComparison.Ordinal)
                    && !string.Equals(field.Name, nameof(CodeGenPermissionCodes.Resource), StringComparison.Ordinal))
                .Select(field => (field.Name, (string)field.GetRawConstantValue()!))
        ];
    }
}
