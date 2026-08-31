// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Xml.Linq;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.Chat;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Printing;
using XiHan.BasicApp.Saas;
using XiHan.BasicApp.Workflow;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 宿主模块依赖装配对账测试。
/// </summary>
/// <remarks>
/// 这是本项目最重要的回归锚点：<c>[DependsOn]</c> 漏登记一个业务模块，
/// 该模块的服务注册、动态 API、种子器、实体建表会全部不执行，而且启动期一声不吭——是静默失效。
/// 因此这里守住「csproj 引用了 src/modules 下的模块工程，就必须登记到 [DependsOn]」这一向对账，漏登即红。
/// </remarks>
public sealed class WebHostModuleDependencyTests
{
    /// <summary>
    /// 六个核心业务模块必须全部出现在依赖登记里，一个都不能少。
    /// </summary>
    /// <remarks>
    /// 依赖集合刻意走框架真实 API <see cref="XiHanModuleHelper.FindDependedModuleTypes"/> 解析，
    /// 而不是裸读特性，保证测试口径与运行期模块加载器完全一致。
    /// 这里只查「核心模块一个都不能少」，不锁总数：src/business 下的示例业务模块可整体删除，
    /// 增删它不该把这条回归锚点弄红。
    /// </remarks>
    [Fact]
    public void DependsOn_ShouldCoverAllCoreBusinessModules()
    {
        Type[] expected =
        [
            typeof(XiHanBasicAppSaasModule),
            typeof(XiHanBasicAppCodeGenerationModule),
            typeof(XiHanBasicAppAIModule),
            typeof(XiHanBasicAppWorkflowModule),
            typeof(XiHanBasicAppPrintingModule),
            typeof(XiHanBasicAppChatModule)
        ];

        var actual = XiHanModuleHelper.FindDependedModuleTypes(typeof(XiHanBasicAppWebHostModule));

        Assert.All(expected, moduleType => Assert.Contains(moduleType, actual));
    }

    /// <summary>
    /// 依赖登记里不得出现重复类型：框架会静默去重，重复本身说明有人误加。
    /// </summary>
    [Fact]
    public void DependsOn_ShouldNotContainDuplicatedModuleTypes()
    {
        var actual = XiHanModuleHelper.FindDependedModuleTypes(typeof(XiHanBasicAppWebHostModule));

        Assert.Equal(actual.Count, actual.Distinct().Count());
    }

    /// <summary>
    /// 每个被依赖类型都必须是可实例化的曦寒模块，否则框架模块加载器会在启动期做类型检查时抛异常。
    /// </summary>
    [Fact]
    public void DependsOn_EveryDependedTypeShouldBeConcreteXiHanModule()
    {
        var actual = XiHanModuleHelper.FindDependedModuleTypes(typeof(XiHanBasicAppWebHostModule));

        Assert.All(actual, moduleType =>
        {
            Assert.True(
                XiHanModuleHelper.IsXiHanModule(moduleType),
                $"{moduleType.FullName} 不是合法的曦寒模块类型，框架加载器会在启动期抛异常。");
            Assert.True(moduleType.IsSubclassOf(typeof(XiHanModule)), $"{moduleType.FullName} 未继承 XiHanModule。");
        });
    }

    /// <summary>
    /// 宿主模块类自身必须 public、非抽象、直接继承 XiHanModule，否则应用根本起不来。
    /// </summary>
    [Fact]
    public void WebHostModule_ShouldBePublicConcreteAndDeriveFromXiHanModule()
    {
        var moduleType = typeof(XiHanBasicAppWebHostModule);

        Assert.True(moduleType.IsPublic, "宿主模块必须是 public，框架需要外部实例化它作为根模块。");
        Assert.False(moduleType.IsAbstract, "宿主模块不得为抽象类。");
        Assert.Equal(typeof(XiHanModule), moduleType.BaseType);
    }

    /// <summary>
    /// 正向对账：csproj 里引用了 src/modules 下的模块工程，就必须在 DependsOn 里登记。
    /// </summary>
    /// <remarks>
    /// 这条守的正是「新增模块时必须在此登记」——新加了工程引用却忘了写 DependsOn，
    /// 编译能过、启动不报错，但该模块整块能力不会生效。
    /// </remarks>
    [Fact]
    public void DependsOn_ShouldRegisterEveryReferencedModuleProject()
    {
        var referenced = ReadReferencedModuleAssemblyNames();
        var registered = ReadDependedModuleAssemblyNames();

        var missing = referenced.Except(registered, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        Assert.True(
            missing.Count == 0,
            $"这些模块工程被 csproj 引用了却没在 [DependsOn] 里登记，装配会静默失效：{string.Join("、", missing)}");
    }

    /// <summary>
    /// 读取 csproj 中 src/modules 下的工程引用，转成程序集名集合。
    /// </summary>
    /// <returns>被直接引用的模块程序集名集合。</returns>
    private static HashSet<string> ReadReferencedModuleAssemblyNames()
    {
        var csprojPath = Path.Combine(
            WebHostTestHelper.ResolveWebHostProjectRoot(), "XiHan.BasicApp.WebHost.csproj");
        Assert.True(File.Exists(csprojPath), $"未找到被测工程文件：{csprojPath}");

        // csproj 无 xml 命名空间，XName 直接用元素名
        var document = XDocument.Load(csprojPath);

        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Replace('\\', '/'))
            .Where(include => include.Contains("/modules/", StringComparison.Ordinal))
            .Select(include => Path.GetFileNameWithoutExtension(include))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// 读取 DependsOn 登记的模块类型，转成程序集名集合。
    /// </summary>
    /// <returns>已登记的模块程序集名集合。</returns>
    private static HashSet<string> ReadDependedModuleAssemblyNames()
    {
        return XiHanModuleHelper.FindDependedModuleTypes(typeof(XiHanBasicAppWebHostModule))
            .Select(moduleType => moduleType.Assembly.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
