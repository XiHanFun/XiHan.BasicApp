// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.BasicApp.Printing.Infrastructure.Seeders.System;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using PrintingPageRegistry = XiHan.BasicApp.Printing.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模块权限码、页面登记表与种子器执行次序的契约测试。
/// </summary>
/// <remarks>
/// 三个种子器之间有硬顺序：权限（500）→ 菜单（501）→ 角色授权（502）。
/// 菜单建立时就要绑定 <c>print-template:read</c>，权限还没播种就会被静默跳过——
/// 表现是"升级完成、菜单却没出来"，而且没有任何报错。角色授权同理，必须能拿到权限主键。
/// 顺序值一旦被改动或与其它模块段位撞车，问题只会在全新环境的首次初始化时暴露，
/// 所以把顺序、种子名与权限定义清单在这里钉死。
/// </remarks>
public sealed class PrintingExtraSeedContractTests
{
    /// <summary>
    /// 三个种子器的顺序必须是"权限先于菜单、菜单先于角色授权"，且整体落在 500+ 独立段。
    /// </summary>
    [Fact]
    public void Seeders_OrderShouldKeepPermissionBeforeMenuBeforeRoleGrant()
    {
        var permission = OrderOf(typeof(PrintingPermissionSeeder));
        var menu = OrderOf(typeof(PrintingMenuSeeder));
        var rolePermission = OrderOf(typeof(PrintingRolePermissionSeeder));

        Assert.Equal(500, permission);
        Assert.Equal(501, menu);
        Assert.Equal(502, rolePermission);
        Assert.True(
            permission < menu && menu < rolePermission,
            $"种子顺序被打乱（权限 {permission}、菜单 {menu}、角色授权 {rolePermission}），菜单会因权限缺失被静默跳过。");
    }

    /// <summary>
    /// 三个种子器的名称必须带 [Printing] 前缀，初始化日志才能区分是哪个模块的种子在跑。
    /// </summary>
    /// <param name="seederType">种子器类型。</param>
    [Theory]
    [InlineData(typeof(PrintingPermissionSeeder))]
    [InlineData(typeof(PrintingMenuSeeder))]
    [InlineData(typeof(PrintingRolePermissionSeeder))]
    public void Seeders_NameShouldCarryModulePrefix(Type seederType)
    {
        var name = (string)RequireProperty(seederType, "Name").GetValue(UninitializedInstance(seederType))!;

        Assert.StartsWith("[Printing]", name, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限种子播在平台租户域内，因此必须继承平台种子基类。
    /// </summary>
    [Fact]
    public void PermissionSeeder_ShouldSeedInPlatformTenantDomain()
    {
        Assert.True(
            typeof(PrintingPermissionSeeder).IsAssignableTo(typeof(PlatformDataSeederBase)),
            "打印权限是 TenantId=0 的全局权限，权限种子必须继承 PlatformDataSeederBase。");
    }

    /// <summary>
    /// 菜单种子必须复用页面登记表基类，页面与按钮都从本模块的 PageRegistry 取，杜绝两处各写一份。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldSourcePagesFromModuleRegistry()
    {
        var instance = UninitializedInstance(typeof(PrintingMenuSeeder));

        Assert.True(
            typeof(PrintingMenuSeeder).IsAssignableTo(typeof(PageRegistryMenuSeederBase)),
            "打印菜单种子未继承 PageRegistryMenuSeederBase，页面登记表就不再是单一事实源。");
        Assert.Same(PrintingPageRegistry.All, RequireProperty(typeof(PrintingMenuSeeder), "Pages").GetValue(instance));
        Assert.Same(PrintingPageRegistry.Buttons, RequireProperty(typeof(PrintingMenuSeeder), "Buttons").GetValue(instance));
        Assert.Equal("Printing", RequireProperty(typeof(PrintingMenuSeeder), "ModuleName").GetValue(instance));
    }

    /// <summary>
    /// 权限种子的定义清单必须与权限码常量完全一致，少一条就等于该权限永远无法被授予。
    /// </summary>
    [Fact]
    public void PermissionSeeder_DefinitionsShouldCoverExactlyAllPermissionCodes()
    {
        var codes = SeedDefinitions().Select(definition => (string)definition[0]!).ToList();
        var missing = PrintingPermissionCodes.All.Where(code => !codes.Contains(code, StringComparer.Ordinal)).ToList();
        var extra = codes.Where(code => !PrintingPermissionCodes.All.Contains(code, StringComparer.Ordinal)).ToList();

        Assert.True(missing.Count == 0, $"权限种子缺少权限码：{string.Join("、", missing)}");
        Assert.True(extra.Count == 0, $"权限种子存在常量里没有的权限码：{string.Join("、", extra)}");
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 权限种子的排序值必须互不相同且落在打印模块自己的 2800 段，避免与其它模块权限混排。
    /// </summary>
    [Fact]
    public void PermissionSeeder_SortValuesShouldBeUniqueAndInModuleRange()
    {
        var sorts = SeedDefinitions().Select(definition => (int)definition[4]!).ToList();

        Assert.Equal(sorts.Count, sorts.Distinct().Count());
        Assert.All(sorts, sort => Assert.InRange(sort, 2800, 2899));
    }

    /// <summary>
    /// 只读权限不需要审计，其余六个写/使用类权限必须开启审计留痕。
    /// </summary>
    [Fact]
    public void PermissionSeeder_AuditFlagShouldBeOffOnlyForRead()
    {
        var auditByCode = SeedDefinitions().ToDictionary(
            definition => (string)definition[0]!,
            definition => (bool)definition[3]!,
            StringComparer.Ordinal);

        Assert.False(auditByCode[PrintingPermissionCodes.Read]);
        var unaudited = auditByCode
            .Where(pair => pair.Key != PrintingPermissionCodes.Read && !pair.Value)
            .Select(pair => pair.Key)
            .ToList();
        Assert.True(unaudited.Count == 0, $"以下写/使用类权限未开启审计：{string.Join("、", unaudited)}");
    }

    /// <summary>
    /// 权限码必须唯一、统一带模块前缀、且只用小写字母与短横线，权限目录与前端按钮码才能稳定匹配。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldBeUniqueAndFollowNamingConvention()
    {
        Assert.Equal(PrintingPermissionCodes.Module, PrintingPermissionCodes.Resource);
        Assert.Equal(
            PrintingPermissionCodes.All.Count,
            PrintingPermissionCodes.All.Distinct(StringComparer.Ordinal).Count());

        var offenders = PrintingPermissionCodes.All
            .Where(code => !code.StartsWith(PrintingPermissionCodes.Module + ":", StringComparison.Ordinal)
                || !code.All(character => char.IsAsciiLetterLower(character) || character is '-' or ':'))
            .ToList();

        Assert.True(offenders.Count == 0, $"不符合「模块前缀 + 小写短横线」命名的权限码：{string.Join("、", offenders)}");
    }

    /// <summary>
    /// 可授租户清单必须是全量清单去掉平台专属项，两侧同时改动才不会出现"授不出去的权限"。
    /// </summary>
    [Fact]
    public void TenantGrantable_ShouldBeAllMinusPlatformOnly()
    {
        Assert.Equal(
            PrintingPermissionCodes.All.Where(code => code != PrintingPermissionCodes.GlobalManage),
            PrintingPermissionCodes.TenantGrantable);
        Assert.All(
            PrintingPermissionCodes.TenantGrantable,
            code => Assert.Contains(code, PrintingPermissionCodes.All));
    }

    /// <summary>
    /// 页面登记表只登记一个打印模板页，且挂在 Saas 持有的 setting 目录下（本模块不自建目录）。
    /// </summary>
    [Fact]
    public void PageRegistry_ShouldRegisterSingleMenuUnderSettingDirectory()
    {
        var page = Assert.Single(PrintingPageRegistry.All);

        Assert.Equal("setting.print-template", page.Code);
        Assert.Equal("setting", page.ParentCode);
        Assert.Equal(MenuType.Menu, page.MenuType);
        Assert.Equal(PrintingPermissionCodes.Read, page.PermissionCode);
        Assert.Equal("/setting/print-template", page.Path);
        Assert.Equal("setting/print-template/index", page.Component);
    }

    /// <summary>
    /// 国际化键必须遵循 menu.{页面码把点和短横线换成下划线} 的约定，前端 menu.ts 才能对上文案。
    /// </summary>
    [Fact]
    public void PageRegistry_I18nKeyShouldFollowNamingConvention()
    {
        var page = Assert.Single(PrintingPageRegistry.All);
        var expected = "menu." + page.Code.Replace('.', '_').Replace('-', '_');

        Assert.Equal(expected, page.I18nKey);
    }

    /// <summary>
    /// 页面按钮必须全部挂在本模块页面下，编码以页面码开头、排序互不相同。
    /// </summary>
    [Fact]
    public void PageRegistry_ButtonsShouldBelongToOwningPage()
    {
        var page = Assert.Single(PrintingPageRegistry.All);
        var buttons = PrintingPageRegistry.Buttons;

        Assert.All(buttons, button => Assert.Equal(page.Code, button.ParentCode));
        Assert.All(buttons, button => Assert.StartsWith(page.Code + ".", button.Code, StringComparison.Ordinal));
        Assert.Equal(buttons.Count, buttons.Select(button => button.Sort).Distinct().Count());
        Assert.Equal(buttons.Count, buttons.Select(button => button.Code).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 页面权限码与按钮权限码合起来必须恰好覆盖全部七个权限码，权限目录里不会出现"无处可授"的孤儿权限。
    /// </summary>
    [Fact]
    public void PageRegistry_PageAndButtonsShouldCoverEveryPermissionCode()
    {
        var covered = PrintingPageRegistry.Buttons
            .Select(button => button.PermissionCode)
            .Concat(PrintingPageRegistry.All.Select(page => page.PermissionCode).OfType<string>())
            .ToHashSet(StringComparer.Ordinal);
        var uncovered = PrintingPermissionCodes.All.Where(code => !covered.Contains(code)).ToList();
        var unknown = covered.Where(code => !PrintingPermissionCodes.All.Contains(code, StringComparer.Ordinal)).ToList();

        Assert.True(uncovered.Count == 0, $"没有任何页面或按钮承载的权限码：{string.Join("、", uncovered)}");
        Assert.True(unknown.Count == 0, $"页面登记表引用了常量里不存在的权限码：{string.Join("、", unknown)}");
    }

    /// <summary>
    /// 读取权限种子里那份私有定义清单，元素按 (Code, Name, Description, Audit, Sort) 展开。
    /// </summary>
    private static List<object?[]> SeedDefinitions()
    {
        var field = typeof(PrintingPermissionSeeder)
            .GetField("Definitions", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PrintingPermissionSeeder 未找到 Definitions 定义清单字段。");
        var values = (System.Collections.IEnumerable)field.GetValue(null)!;

        var definitions = new List<object?[]>();
        foreach (var value in values)
        {
            var tuple = (ITuple)value;
            definitions.Add([.. Enumerable.Range(0, tuple.Length).Select(index => tuple[index])]);
        }

        return definitions;
    }

    /// <summary>
    /// 取种子器的 Order 值。
    /// </summary>
    private static int OrderOf(Type seederType)
    {
        return (int)RequireProperty(seederType, "Order").GetValue(UninitializedInstance(seederType))!;
    }

    /// <summary>
    /// 创建不执行构造函数的实例，用于只读取覆写的元数据属性而不触碰任何外部依赖。
    /// </summary>
    private static object UninitializedInstance(Type seederType)
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(seederType);
    }

    /// <summary>
    /// 取得必须存在的属性（含受保护成员），缺失时给出可定位的失败消息。
    /// </summary>
    private static PropertyInfo RequireProperty(Type seederType, string propertyName)
    {
        return seederType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{seederType.Name} 上未找到属性 {propertyName}。");
    }
}
