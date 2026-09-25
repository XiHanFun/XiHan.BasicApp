// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// Saas 模块种子器结构约束测试
/// </summary>
/// <remarks>
/// 种子的约定都属于「违约不报错、只是数据悄悄少了一半」的类型，这里逐条钉住：
/// <list type="number">
/// <item><b>平台上下文</b>：全部种子继承 <see cref="PlatformDataSeederBase"/>，整段在平台上下文里播、只播平台库；
/// 写业务租户数据的演示种子在代码里逐个切入目标租户。</item>
/// <item><b>阶段顺序</b>：超级管理员 → 操作字典 → 权限目录 → 菜单 → 套餐 → 其余平台数据 → 演示数据，后一阶段只依赖前面写好的数据。</item>
/// <item><b>先查后写</b>：种子每次启动都跑，不先查就是「重启一次多一份数据」。</item>
/// <item><b>演示开关</b>：演示种子只在配置开启时写，缺省不写，写错直接报错。</item>
/// </list>
/// </remarks>
public sealed class SaasAppSeederStructureTests
{
    /// <summary>
    /// 种子器必须能被发现，否则后续结构断言全是空跑。
    /// </summary>
    [Fact]
    public void Seeders_ShouldBeDiscoverable()
    {
        Assert.True(SeederTypes().Count >= 13, $"只发现了 {SeederTypes().Count} 个 Saas 种子器，扫描条件可能失效了。");
    }

    /// <summary>
    /// 具体种子器一律 sealed：共用逻辑放在抽象基类里，不靠继承具体种子复用。
    /// </summary>
    [Fact]
    public void Seeders_ShouldBeSealed()
    {
        var offenders = SeederTypes().Where(type => !type.IsSealed).Select(type => type.Name).ToList();

        Assert.True(offenders.Count == 0, $"以下种子器不是 sealed：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 种子名称必须统一带 <c>[SaaS]</c> 前缀且唯一，播种日志里一眼能分辨是谁写的。
    /// </summary>
    [Fact]
    public void SeederNames_ShouldCarryModulePrefixAndBeUnique()
    {
        var names = SeederTypes().Select(type => (type.Name, SeedName: ReadName(type))).ToList();

        var unprefixed = names.Where(item => !item.SeedName.StartsWith("[SaaS]", StringComparison.Ordinal)).Select(item => $"{item.Name}={item.SeedName}").ToList();
        Assert.True(unprefixed.Count == 0, $"以下种子器的名称未带 [SaaS] 前缀：{string.Join(", ", unprefixed)}");

        var duplicated = names.GroupBy(item => item.SeedName, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.True(duplicated.Count == 0, $"种子名称重复：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 种子优先级两两不同：同序号之间的执行顺序不确定。
    /// </summary>
    [Fact]
    public void SeederOrders_ShouldBeUnique()
    {
        var duplicated = SeederTypes()
            .Select(type => (type.Name, Order: ReadOrder(type)))
            .GroupBy(item => item.Order)
            .Where(group => group.Count() > 1)
            .Select(group => $"Order={group.Key} ← {string.Join(", ", group.Select(item => item.Name))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"种子优先级冲突：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 全部种子在平台上下文里播、只播平台库。
    /// </summary>
    [Fact]
    public void Seeders_ShouldSeedWithinPlatformScope()
    {
        var offenders = SeederTypes().Where(type => !typeof(PlatformDataSeederBase).IsAssignableFrom(type)).Select(type => type.Name).ToList();
        Assert.True(offenders.Count == 0, $"以下种子没有继承平台种子基类：{string.Join(", ", offenders)}");

        var attribute = typeof(PlatformDataSeederBase).GetCustomAttribute<DataSeedingAttribute>(inherit: false);
        Assert.NotNull(attribute);
        Assert.Equal(DbInitializationTarget.Platform, attribute!.Target);
    }

    /// <summary>
    /// 各种子落在自己的阶段：阶段由依赖决定，排错了不会报错，只会「菜单少了」「企业版缺权限」。
    /// </summary>
    [Fact]
    public void Seeders_ShouldRunInTheirPhase()
    {
        Assert.Equal(SeedOrders.PlatformIdentity, ReadOrder(typeof(SaasSuperAdminSeeder)));
        Assert.Equal(SeedOrders.Operations, ReadOrder(typeof(SaasOperationSeeder)));
        Assert.Equal(SeedOrders.PermissionCatalog, ReadOrder(typeof(SaasPermissionCatalogSeeder)));
        Assert.Equal(SeedOrders.Menus, ReadOrder(typeof(SaasMenuSeeder)));
        Assert.Equal(SeedOrders.Editions, ReadOrder(typeof(SaasEditionSeeder)));

        foreach (var type in new[] { typeof(SaasSettingSeeder), typeof(SaasStorageSeeder), typeof(SaasMessageTemplateSeeder), typeof(SaasOAuthAppSeeder), typeof(SaasTaskSeeder) })
        {
            Assert.InRange(ReadOrder(type), SeedOrders.PlatformData, SeedOrders.Demo - 1);
        }
    }

    /// <summary>
    /// 阶段本身按依赖递增：超管 → 操作 → 权限目录 → 菜单 → 套餐 → 平台数据 → 演示。
    /// </summary>
    [Fact]
    public void SeedPhases_ShouldFollowDependencies()
    {
        int[] phases = [SeedOrders.PlatformIdentity, SeedOrders.Operations, SeedOrders.PermissionCatalog, SeedOrders.Menus, SeedOrders.Editions, SeedOrders.PlatformData, SeedOrders.Demo];

        Assert.Equal(phases.Order(), phases);
        Assert.All(phases.Zip(phases.Skip(1)), pair => Assert.True(pair.Second - pair.First >= 100, "每个阶段要留出各模块错开的号段。"));
    }

    /// <summary>
    /// 演示种子都在演示阶段、继承演示基类；非演示种子都在演示阶段之前。
    /// </summary>
    [Fact]
    public void DemoSeeders_ShouldBeSeparatedFromBaseline()
    {
        foreach (var type in SeederTypes())
        {
            var isDemo = typeof(DemoDataSeederBase).IsAssignableFrom(type);
            var order = ReadOrder(type);
            Assert.True(isDemo == order >= SeedOrders.Demo, $"{type.Name}(Order={order}) 是否演示种子与所在阶段不一致。");
        }
    }

    /// <summary>
    /// 每个种子器都必须先查后写。
    /// </summary>
    [Fact]
    public void Seeders_ShouldReadBeforeTheyWrite()
    {
        var offenders = SeederTypes()
            .Where(type => !ReachesExistenceCheck(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下种子器的播种代码里找不到任何存在性查询，无法保证幂等：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 写业务租户数据的演示种子必须显式切入目标租户，不能用启动时的上下文。
    /// </summary>
    [Theory]
    [InlineData(typeof(SaasDemoSeeder))]
    [InlineData(typeof(SaasDemoNotificationSeeder))]
    public void TenantDemoSeeders_ShouldSwitchIntoTargetTenant(Type type)
    {
        Assert.True(ReachesTenantScopeSwitch(type), $"{type.Name} 写业务租户数据却没有切换租户上下文。");
    }

    /// <summary>
    /// 菜单种子的数据源必须直接取自页面登记表，不能另抄一份。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldDeriveFromPageRegistryBase()
    {
        Assert.True(typeof(PageRegistryMenuSeederBase).IsAssignableFrom(typeof(SaasMenuSeeder)));
    }

    /// <summary>
    /// 演示开关：缺省与 false 不写，true 才写。
    /// </summary>
    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    public async Task DemoSwitch_ShouldSeedOnlyWhenEnabled(string? value, bool expected)
    {
        var seeder = new ProbeDemoSeeder(BuildServices(value));

        await seeder.SeedAsync();

        Assert.Equal(expected, seeder.Seeded);
        Assert.Equal("Saas:Seed:EnableDemoData", DemoDataSeederBase.EnableDemoDataKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 演示开关写错（不是布尔值）直接报错，不按关闭处理。
    /// </summary>
    [Fact]
    public async Task DemoSwitch_Malformed_ShouldThrow()
    {
        var seeder = new ProbeDemoSeeder(BuildServices("yes"));

        _ = await Assert.ThrowsAsync<InvalidOperationException>(seeder.SeedAsync);
        Assert.False(seeder.Seeded);
    }

    private static ServiceProvider BuildServices(string? demoSwitch)
    {
        var settings = new Dictionary<string, string?>();
        if (demoSwitch is not null)
        {
            settings[DemoDataSeederBase.EnableDemoDataKey] = demoSwitch;
        }

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .AddSingleton<ICurrentTenant>(new TestCurrentTenant())
            .BuildServiceProvider();
    }

    /// <summary>
    /// 枚举 Saas 模块的全部具体种子器。
    /// </summary>
    private static List<Type> SeederTypes()
    {
        return typeof(SaasSuperAdminSeeder).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(typeof(IDataSeeder).IsAssignableFrom)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static string ReadName(Type type)
    {
        return ReadConstantFromGetter<string>(type, "Name");
    }

    private static int ReadOrder(Type type)
    {
        return ReadConstantFromGetter<int>(type, "Order");
    }

    /// <summary>
    /// 通过未初始化实例读取只返回常量的属性值（种子器的 Name/Order 都是表达式体常量）。
    /// </summary>
    private static TValue ReadConstantFromGetter<TValue>(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{type.Name} 未声明 {propertyName} 属性。");

        var instance = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        return (TValue)property.GetValue(instance)!;
    }

    /// <summary>
    /// 枚举种子器的播种入口（自身及基类声明的 SeedAsync / SeedInternalAsync / SeedDemoAsync）。
    /// </summary>
    /// <remarks>每个入口在它声明的类型内展开调用：基类里的私有步骤也算在内。</remarks>
    private static List<MethodInfo> SeedEntryPoints(Type type)
    {
        var methods = new List<MethodInfo>();
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            methods.AddRange(current
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => method.Name is "SeedAsync" or "SeedInternalAsync" or "SeedDemoAsync")
                .Where(method => !method.IsAbstract));
        }

        return methods;
    }

    private static bool ReachesTenantScopeSwitch(Type type)
    {
        return SeedEntryPoints(type).Any(method => SaasAppIlCallGraph.Reaches(
            method,
            method.DeclaringType!,
            callee => callee.DeclaringType == typeof(ICurrentTenant)
                      && string.Equals(callee.Name, nameof(ICurrentTenant.Change), StringComparison.Ordinal)));
    }

    private static bool ReachesExistenceCheck(Type type)
    {
        return SeedEntryPoints(type).Any(method => SaasAppIlCallGraph.Reaches(
            method,
            method.DeclaringType!,
            callee => string.Equals(callee.Name, "HasDataAsync", StringComparison.Ordinal)
                      || string.Equals(callee.Name, "Queryable", StringComparison.Ordinal)));
    }

    /// <summary>
    /// 探针演示种子：只记下有没有走到写入
    /// </summary>
    private sealed class ProbeDemoSeeder(IServiceProvider serviceProvider)
        : DemoDataSeederBase(Mock.Of<ISqlSugarClientResolver>(), NullLogger.Instance, serviceProvider)
    {
        public bool Seeded { get; private set; }

        public override int Order => SeedOrders.Demo + 99;

        public override string Name => "[SaaS]演示开关探针";

        protected override Task SeedDemoAsync()
        {
            Seeded = true;
            return Task.CompletedTask;
        }
    }
}
