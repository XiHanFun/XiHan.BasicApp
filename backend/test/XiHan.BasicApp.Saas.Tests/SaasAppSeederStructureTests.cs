// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.Demo;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// Saas 模块种子器结构约束测试。
/// </summary>
/// <remarks>
/// 种子器的两条硬约定都属于"违约不报错、只是数据悄悄少了一半"的类型：
/// <list type="number">
/// <item><b>平台租户域</b>——权限/菜单/字典/配置这类平台级记录必须落在 <c>TenantId = 0</c>。
/// TenantId 由写入拦截器按当时的租户上下文注入，而启动播种时上下文未必是平台租户，
/// 因此播种代码必须显式 <c>ICurrentTenant.Change(null)</c>。不切的后果是行照常写入、
/// 落在别的租户下，凡是按 <c>TenantId = 0</c> 查找的消费方（如菜单种子解析权限）统统查不到，
/// 表现为「干净库重建后菜单莫名少了几个」。</item>
/// <item><b>幂等</b>——种子每次启动都跑，必须先查后写，否则重启一次多一份数据。</item>
/// </list>
/// 两条都无法靠特性表达，故用 IL 调用图直接验证方法体里到底有没有这两下。
/// <para>
/// 兄弟模块（AI / CodeGeneration / Workflow）的平台域约束由
/// <c>XiHan.BasicApp.Api.Tests.PlatformSeederScopeTests</c> 覆盖，那条测试明确排除了本模块，
/// 因为本模块的种子是在方法体内内联切上下文而非继承 <see cref="PlatformDataSeederBase"/>。
/// </para>
/// </remarks>
public sealed class SaasAppSeederStructureTests
{
    /// <summary>
    /// 种子器必须能被发现，否则后续结构断言全是空跑。
    /// </summary>
    [Fact]
    public void Seeders_ShouldBeDiscoverable()
    {
        Assert.True(SeederTypes().Count >= 12, $"只发现了 {SeederTypes().Count} 个 Saas 种子器，扫描条件可能失效了。");
    }

    /// <summary>
    /// 种子器要么 sealed，要么是刻意留给同族种子继承的（目前只有租户版本种子被重算种子继承）。
    /// </summary>
    [Fact]
    public void Seeders_ShouldBeSealedUnlessDeliberatelyInherited()
    {
        var inherited = SeederTypes()
            .Select(type => type.BaseType)
            .Where(baseType => baseType is not null)
            .Select(baseType => baseType!)
            .ToHashSet();

        var offenders = SeederTypes()
            .Where(type => !type.IsSealed && !inherited.Contains(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下种子器既非 sealed 也没有派生类型：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 种子名称必须统一带 <c>[SaaS]</c> 前缀，播种日志里一眼能分辨是哪个模块写的。
    /// </summary>
    [Fact]
    public void SeederNames_ShouldCarryModulePrefix()
    {
        var offenders = SeederTypes()
            .Select(type => (type.Name, SeedName: ReadName(type)))
            .Where(item => item.SeedName is null || !item.SeedName.StartsWith("[SaaS]", StringComparison.Ordinal))
            .Select(item => $"{item.Name}={item.SeedName ?? "读取失败"}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下种子器的名称未带 [SaaS] 前缀：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 种子名称必须唯一，否则播种日志无法区分是哪一个在跑。
    /// </summary>
    [Fact]
    public void SeederNames_ShouldBeUnique()
    {
        var duplicated = SeederTypes()
            .Select(type => (type.Name, SeedName: ReadName(type)))
            .Where(item => item.SeedName is not null)
            .GroupBy(item => item.SeedName!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ← {string.Join(", ", group.Select(item => item.Name))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"种子名称重复：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 种子优先级必须两两不同：同序号之间的执行顺序不确定，而种子之间是有依赖的
    /// （身份 → 权限 → 版本 → 参数 → 字典 → 存储 → 菜单 → …）。
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
    /// 身份种子必须最先跑：权限、角色权限、菜单都要挂到它建出来的平台管理员/内置角色上。
    /// </summary>
    [Fact]
    public void IdentitySeeder_ShouldRunBeforeEveryOtherSaasSeeder()
    {
        var identityOrder = ReadOrder(typeof(SaasIdentitySeeder));
        var others = SeederTypes()
            .Where(type => type != typeof(SaasIdentitySeeder))
            .Select(type => (type.Name, Order: ReadOrder(type)))
            .Where(item => item.Order <= identityOrder)
            .Select(item => $"{item.Name}(Order={item.Order})")
            .ToList();

        Assert.True(others.Count == 0, $"以下种子排在身份种子(Order={identityOrder})之前或同序：{string.Join(", ", others)}");
    }

    /// <summary>
    /// 菜单种子必须排在权限种子之后：菜单按权限码解析 PermissionId，权限还没落库就会整条跳过。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldRunAfterPermissionSeeder()
    {
        Assert.True(
            ReadOrder(typeof(SaasMenuSeeder)) > ReadOrder(typeof(SaasPermissionSeeder)),
            $"菜单种子(Order={ReadOrder(typeof(SaasMenuSeeder))})必须晚于权限种子(Order={ReadOrder(typeof(SaasPermissionSeeder))})，否则菜单解析不到权限会静默跳过。");
    }

    /// <summary>
    /// 版本白名单重算种子必须排在所有模块权限种子之后，才能把外部模块权限一并纳入企业版白名单。
    /// </summary>
    [Fact]
    public void EditionReconcileSeeder_ShouldRunLast()
    {
        var reconcileOrder = ReadOrder(typeof(SaasTenantEditionReconcileSeeder));
        var later = SeederTypes()
            .Where(type => type != typeof(SaasTenantEditionReconcileSeeder))
            .Select(type => (type.Name, Order: ReadOrder(type)))
            .Where(item => item.Order >= reconcileOrder)
            .Select(item => $"{item.Name}(Order={item.Order})")
            .ToList();

        Assert.True(later.Count == 0, $"以下种子排在版本白名单重算种子之后：{string.Join(", ", later)}");
        Assert.True(reconcileOrder >= 900, $"重算种子的 Order 必须留在模块权限种子段(100/200/300/500)之后，实际 {reconcileOrder}。");
    }

    /// <summary>
    /// 重算种子必须与租户版本种子共用同一套幂等逻辑（继承而非复制），否则两处会逐渐分叉。
    /// </summary>
    [Fact]
    public void EditionReconcileSeeder_ShouldReuseEditionSeederLogic()
    {
        Assert.True(
            typeof(SaasTenantEditionReconcileSeeder).IsSubclassOf(typeof(SaasTenantEditionSeeder)),
            "重算种子必须继承租户版本种子，复用同一套幂等逻辑。");
    }

    /// <summary>
    /// 每个种子器都必须在平台租户域内播种：整段切到平台上下文，或播种代码里显式切换。
    /// </summary>
    /// <remarks>
    /// 演示种子除外——它们刻意按业务租户维度播种，切的是目标租户而非平台。
    /// </remarks>
    [Fact]
    public void Seeders_ShouldSeedWithinPlatformTenantScope()
    {
        var offenders = SeederTypes()
            .Where(type => !typeof(SaasDemoSeederBase).IsAssignableFrom(type))
            .Where(type => !typeof(PlatformDataSeederBase).IsAssignableFrom(type))
            .Where(type => !ReachesTenantScopeSwitch(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"以下种子既未继承平台域基类也未显式切换租户上下文，写出的行会落到启动时的租户下：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 演示种子也必须显式切换租户上下文（切到目标业务租户或平台），不能听天由命地用启动时上下文。
    /// </summary>
    [Fact]
    public void DemoSeeders_ShouldSwitchTenantScopeExplicitly()
    {
        var offenders = SeederTypes()
            .Where(type => typeof(SaasDemoSeederBase).IsAssignableFrom(type))
            .Where(type => !ReachesTenantScopeSwitch(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下演示种子未显式切换租户上下文：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 每个种子器都必须先查后写：种子每次启动都跑，不先查就是"重启一次多一份数据"。
    /// </summary>
    /// <remarks>
    /// 判据是播种代码里必须出现读取动作（<c>HasDataAsync</c> 或 SqlSugar 的 <c>Queryable</c>）。
    /// </remarks>
    [Fact]
    public void Seeders_ShouldReadBeforeTheyWrite()
    {
        var offenders = SeederTypes()
            .Where(type => !ReachesExistenceCheck(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"以下种子器的播种代码里找不到任何存在性查询，无法保证幂等：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 演示种子必须挂在可整体排除的 Demo 分组下，并读取开关配置键，保证"改配置 + 重启"即可关掉。
    /// </summary>
    [Fact]
    public void DemoSeederBase_ShouldDeclareExcludableGroupAndSwitch()
    {
        var attribute = typeof(SaasDemoSeederBase).GetCustomAttributes<DataSeedingAttribute>(inherit: false).FirstOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(SaasDemoSeederBase.DemoSeedingGroup, attribute!.Group, StringComparer.Ordinal);
        Assert.Equal("Demo", SaasDemoSeederBase.DemoSeedingGroup, StringComparer.Ordinal);
        Assert.Equal("Saas:Seed:EnableDemoData", SaasDemoSeederBase.EnableDemoDataConfigKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 每个演示种子都必须在播种入口检查开关，否则配置关掉了它照样往库里写演示数据。
    /// </summary>
    [Fact]
    public void DemoSeeders_ShouldHonourTheDemoDataSwitch()
    {
        var guard = typeof(SaasDemoSeederBase).GetMethod(
            "TrySkipWhenDemoDisabled",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(guard);

        var offenders = SeederTypes()
            .Where(type => typeof(SaasDemoSeederBase).IsAssignableFrom(type))
            .Where(type => !SeedEntryPoints(type).Any(method =>
                SaasAppIlCallGraph.Reaches(method, type, callee => callee == guard)))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下演示种子未检查演示数据开关：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 非演示种子不得挂上 Demo 分组，否则会被"排除演示数据"的配置一并关掉。
    /// </summary>
    [Fact]
    public void SystemSeeders_ShouldNotBeInTheDemoGroup()
    {
        var offenders = SeederTypes()
            .Where(type => !typeof(SaasDemoSeederBase).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttributes<DataSeedingAttribute>(inherit: true)
                .Any(attribute => string.Equals(attribute.Group, SaasDemoSeederBase.DemoSeedingGroup, StringComparison.Ordinal)))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下系统种子被划进了 Demo 分组，会随演示数据一起被排除：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 菜单种子的数据源必须直接取自页面登记表，不能另抄一份。
    /// </summary>
    /// <remarks>
    /// 登记表是菜单的单一事实源；一旦种子改用自己维护的副本，两边就会各自演化，
    /// 表现为"页面登记了但菜单里没有"。
    /// </remarks>
    [Fact]
    public void MenuSeeder_ShouldDeriveFromPageRegistryBase()
    {
        Assert.True(
            typeof(PageRegistryMenuSeederBase).IsAssignableFrom(typeof(SaasMenuSeeder)),
            "菜单种子必须继承 PageRegistryMenuSeederBase，直接以页面登记表为数据源。");
    }

    /// <summary>
    /// 枚举 Saas 模块的全部具体种子器。
    /// </summary>
    /// <returns>种子器类型集合。</returns>
    private static List<Type> SeederTypes()
    {
        return typeof(SaasIdentitySeeder).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(typeof(IDataSeeder).IsAssignableFrom)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 读取种子器声明的名称（走反射拿属性的 getter，不构造实例）。
    /// </summary>
    /// <param name="type">种子器类型。</param>
    /// <returns>名称。</returns>
    private static string? ReadName(Type type)
    {
        return ReadConstantFromGetter<string>(type, "Name");
    }

    /// <summary>
    /// 读取种子器声明的优先级。
    /// </summary>
    /// <param name="type">种子器类型。</param>
    /// <returns>优先级。</returns>
    private static int ReadOrder(Type type)
    {
        return ReadConstantFromGetter<int>(type, "Order");
    }

    /// <summary>
    /// 通过未初始化实例读取只返回常量的属性值（种子器的 Name/Order 都是表达式体常量）。
    /// </summary>
    /// <typeparam name="TValue">属性类型。</typeparam>
    /// <param name="type">种子器类型。</param>
    /// <param name="propertyName">属性名。</param>
    /// <returns>属性值。</returns>
    private static TValue ReadConstantFromGetter<TValue>(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{type.Name} 未声明 {propertyName} 属性。");

        var instance = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        return (TValue)property.GetValue(instance)!;
    }

    /// <summary>
    /// 枚举种子器的播种入口（自身及基类声明的 SeedAsync / SeedInternalAsync）。
    /// </summary>
    /// <param name="type">种子器类型。</param>
    /// <returns>入口方法集合。</returns>
    private static List<MethodInfo> SeedEntryPoints(Type type)
    {
        var methods = new List<MethodInfo>();
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            methods.AddRange(current
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => method.Name is "SeedAsync" or "SeedInternalAsync")
                .Where(method => !method.IsAbstract));
        }

        return methods;
    }

    /// <summary>
    /// 判断种子器的播种代码里是否显式切换过租户上下文。
    /// </summary>
    /// <param name="type">种子器类型。</param>
    /// <returns>是否切换。</returns>
    private static bool ReachesTenantScopeSwitch(Type type)
    {
        return SeedEntryPoints(type).Any(method => SaasAppIlCallGraph.Reaches(
            method,
            type,
            callee => callee.DeclaringType == typeof(ICurrentTenant)
                      && string.Equals(callee.Name, nameof(ICurrentTenant.Change), StringComparison.Ordinal)));
    }

    /// <summary>
    /// 判断种子器的播种代码里是否出现过存在性查询（先查后写的必要条件）。
    /// </summary>
    /// <param name="type">种子器类型。</param>
    /// <returns>是否出现。</returns>
    private static bool ReachesExistenceCheck(Type type)
    {
        return SeedEntryPoints(type).Any(method => SaasAppIlCallGraph.Reaches(
            method,
            type,
            callee => string.Equals(callee.Name, "HasDataAsync", StringComparison.Ordinal)
                      || string.Equals(callee.Name, "Queryable", StringComparison.Ordinal)));
    }
}
