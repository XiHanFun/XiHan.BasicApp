// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using XiHan.BasicApp.CodeGeneration.Application.Pages;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 权限码常量、按钮映射、页面登记表与六个种子器的结构约束测试。
/// </summary>
/// <remarks>
/// 这一整条链上的错误全部是"启动时静默跳过"：操作字典没先播，权限就派生不出来；
/// 权限没先播，菜单建立时解析不到 <c>code_gen:read</c> 就被跳过；父目录排在子项之后，
/// 子项的 ParentId 解析不到就变成顶级菜单。结果都是"某个菜单莫名其妙没了"，没有任何报错。
/// 本文件把链上的顺序、编码与归属固化成断言，并在失败消息里列出具体违规项。
/// </remarks>
public sealed class CodeGenPermissionAndSeederStructureTests
{
    /// <summary>
    /// 种子器执行链（与 <c>AddCodeGenerationDataSeeders</c> 的登记顺序一一对应）。
    /// </summary>
    private static readonly (string TypeName, int Order)[] SeederChain =
    [
        ("SysOperationSeeder", 100),
        ("SysResourceSeeder", 101),
        ("SysPermissionSeeder", 102),
        ("CodeGenerationMenuSeeder", 103),
        ("SysRolePermissionSeeder", 104),
        ("SysCodeGenTemplateSeeder", 105)
    ];

    /// <summary>
    /// 种子器类型名（供 <c>[Theory]</c> 逐个校验）。
    /// </summary>
    public static TheoryData<string> SeederTypeNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var (typeName, _) in SeederChain)
            {
                data.Add(typeName);
            }

            return data;
        }
    }

    /// <summary>
    /// 按类型名取本模块的种子器类型（种子器所在命名空间以 <c>System</c> 结尾，走反射避免 using 歧义）。
    /// </summary>
    /// <param name="typeName">类型名</param>
    private static Type SeederType(string typeName)
        => CodeGenerationTestHelper.ModuleAssembly.GetType(
            "XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders.System." + typeName,
            throwOnError: true)!;

    /// <summary>
    /// 不经构造函数取种子器实例，只为读取 Order / Name 这两个纯计算属性。
    /// </summary>
    /// <remarks>种子器的构造依赖 SqlSugar 客户端解析器与服务提供者，测试里既不该也不需要造出来。</remarks>
    /// <param name="typeName">类型名</param>
    private static IDataSeeder SeederInstance(string typeName)
        => (IDataSeeder)RuntimeHelpers.GetUninitializedObject(SeederType(typeName));

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
    /// 权限码用到的操作必须都在平台操作字典种子播下的操作集合内，否则种子会跳过该权限。
    /// </summary>
    [Fact]
    public void PermissionCodes_ActionsShouldExistInSeededOperationDictionary()
    {
        string[] seededOperations = ["read", "create", "update", "delete", "export", "import", "execute"];

        var offenders = PermissionCodeConstants()
            .Select(item => item.Value[(item.Value.IndexOf(':', StringComparison.Ordinal) + 1)..])
            .Where(action => !seededOperations.Contains(action, StringComparer.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0, $"以下操作码不在操作字典种子内：{string.Join("、", offenders)}");
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
    /// 本模块暂无独立按钮级权限，按钮登记表必须保持为空。
    /// </summary>
    /// <remarks>
    /// 一旦这里加了条目，就必须同步在 <see cref="CodeGenPermissionCodes"/> 与权限种子里补上对应权限码，
    /// 否则菜单种子会因解析不到权限而静默跳过这些按钮。本用例是那次改动的提醒闸。
    /// </remarks>
    [Fact]
    public void PageRegistry_ButtonsShouldBeEmpty()
    {
        Assert.Empty(PageRegistry.Buttons);
    }

    /// <summary>
    /// 六个种子器的执行序必须与登记顺序一致：操作字典 → 资源 → 权限 → 菜单 → 角色授权 → 模板。
    /// </summary>
    /// <param name="typeName">种子器类型名</param>
    [Theory]
    [MemberData(nameof(SeederTypeNames))]
    public void Seeder_OrderShouldMatchDocumentedChain(string typeName)
    {
        var expected = SeederChain.Single(item => string.Equals(item.TypeName, typeName, StringComparison.Ordinal)).Order;

        Assert.Equal(expected, SeederInstance(typeName).Order);
    }

    /// <summary>
    /// 种子器的执行序不得重复，重复会让链内顺序变成不确定。
    /// </summary>
    [Fact]
    public void Seeder_OrdersShouldBeUnique()
    {
        var duplicates = SeederChain
            .Select(item => SeederInstance(item.TypeName).Order)
            .GroupBy(order => order)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(duplicates.Count == 0, $"以下执行序被多个种子器占用：{string.Join("、", duplicates)}");
    }

    /// <summary>
    /// 代码生成的种子统一落在 100+ 独立段，与 Saas 的种子段互不交叠。
    /// </summary>
    /// <param name="typeName">种子器类型名</param>
    [Theory]
    [MemberData(nameof(SeederTypeNames))]
    public void Seeder_OrderShouldStayInModuleReservedRange(string typeName)
    {
        var order = SeederInstance(typeName).Order;

        Assert.InRange(order, 100, 199);
    }

    /// <summary>
    /// 种子名统一带模块前缀，启动日志里才分得清是谁在播。
    /// </summary>
    /// <param name="typeName">种子器类型名</param>
    [Theory]
    [MemberData(nameof(SeederTypeNames))]
    public void Seeder_NameShouldCarryModulePrefix(string typeName)
    {
        var name = SeederInstance(typeName).Name;

        Assert.StartsWith("[CodeGeneration]", name, StringComparison.Ordinal);
        Assert.True(name.Length > "[CodeGeneration]".Length, $"{typeName} 的种子名只有前缀，没有实际描述。");
    }

    /// <summary>
    /// 平台级数据（操作字典/资源/权限/角色授权）必须由平台种子基类播下，整个过程在 TenantId = 0 上下文内。
    /// </summary>
    /// <remarks>
    /// 不切平台上下文的后果是静默的：行照常写入但落到了别的租户下，
    /// 按 TenantId = 0 查找的消费方（如菜单种子解析权限）会查不到并跳过。
    /// </remarks>
    /// <param name="typeName">种子器类型名</param>
    [Theory]
    [InlineData("SysOperationSeeder")]
    [InlineData("SysResourceSeeder")]
    [InlineData("SysPermissionSeeder")]
    [InlineData("SysRolePermissionSeeder")]
    public void PlatformSeeder_ShouldInheritPlatformDataSeederBase(string typeName)
    {
        Assert.True(
            SeederType(typeName).IsAssignableTo(typeof(XiHan.BasicApp.Saas.Infrastructure.Seeders.System.PlatformDataSeederBase)),
            $"{typeName} 未继承 PlatformDataSeederBase，平台数据可能被写到当前租户下。");
    }

    /// <summary>
    /// 菜单种子必须由页面登记表驱动，且喂进去的就是本模块 PageRegistry 的那两份清单。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldBeDrivenByModulePageRegistry()
    {
        var seederType = SeederType("CodeGenerationMenuSeeder");
        Assert.True(
            seederType.IsAssignableTo(typeof(XiHan.BasicApp.Saas.Infrastructure.Seeders.System.PageRegistryMenuSeederBase)),
            "CodeGenerationMenuSeeder 未继承 PageRegistryMenuSeederBase，页面登记表就不是单一事实源了。");

        var instance = RuntimeHelpers.GetUninitializedObject(seederType);
        var pages = seederType.GetProperty("Pages", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);
        var buttons = seederType.GetProperty("Buttons", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);

        Assert.Same(PageRegistry.All, pages);
        Assert.Same(PageRegistry.Buttons, buttons);
    }

    /// <summary>
    /// 六个种子器都必须实现框架的种子接口，才会被 <c>AddDataSeeder</c> 收集执行。
    /// </summary>
    /// <param name="typeName">种子器类型名</param>
    [Theory]
    [MemberData(nameof(SeederTypeNames))]
    public void Seeder_ShouldImplementDataSeederContract(string typeName)
    {
        var seederType = SeederType(typeName);

        Assert.True(seederType.IsAssignableTo(typeof(IDataSeeder)), $"{typeName} 未实现 IDataSeeder。");
        Assert.True(seederType.IsAssignableTo(typeof(DataSeederBase)), $"{typeName} 未继承 DataSeederBase。");
        Assert.False(seederType.IsAbstract, $"{typeName} 是抽象类，无法被注册执行。");
    }

    /// <summary>
    /// 权限种子必须排在菜单种子之前：菜单建立时要解析 <c>code_gen:read</c> 绑定可见性。
    /// </summary>
    [Fact]
    public void Seeder_PermissionShouldRunBeforeMenu()
    {
        Assert.True(SeederInstance("SysPermissionSeeder").Order < SeederInstance("CodeGenerationMenuSeeder").Order);
        Assert.True(SeederInstance("SysOperationSeeder").Order < SeederInstance("SysPermissionSeeder").Order);
        Assert.True(SeederInstance("SysResourceSeeder").Order < SeederInstance("SysPermissionSeeder").Order);
        Assert.True(SeederInstance("CodeGenerationMenuSeeder").Order < SeederInstance("SysRolePermissionSeeder").Order);
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
