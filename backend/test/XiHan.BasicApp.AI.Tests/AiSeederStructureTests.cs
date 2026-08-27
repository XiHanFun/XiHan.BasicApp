// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using XiHan.BasicApp.AI.Infrastructure.Seeders.System;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using AiPageRegistry = XiHan.BasicApp.AI.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 模块种子器编排的结构约束测试（反射型，不连库）。
/// </summary>
/// <remarks>
/// 种子链是"操作字典 → 资源 → 权限(资源×操作) → 角色授权 → 菜单"的严格偏序：
/// 任何一环的 Order 被改到前面去，后一环就会在干净库上查不到前置数据而静默 <c>return</c>——
/// 不报错、不回滚，只是整条 AI 权限/菜单链在新环境里从未建立。
/// 另外 AI 段固定在 200+ 独立区间，与 Saas(10-37)、代码生成(100-105) 不交叠；一旦交叠会互相插队。
/// <para>
/// Order/Name 都是不依赖实例字段的常量属性，故这里用 <see cref="RuntimeHelpers.GetUninitializedObject(Type)"/>
/// 取值，避免为读两个常量去伪造数据库客户端与日志器。
/// </para>
/// </remarks>
public sealed class AiSeederStructureTests
{
    /// <summary>
    /// 种子链的严格偏序：数组内每一项的 Order 必须严格小于其后一项。
    /// </summary>
    private static readonly Type[][] OrderedSeedChains =
    [
        [typeof(SysOperationSeeder), typeof(SysResourceSeeder), typeof(SysPermissionSeeder), typeof(SysRolePermissionSeeder)],
        [typeof(SysOperationSeeder), typeof(KnowledgeResourceSeeder), typeof(KnowledgePermissionSeeder), typeof(KnowledgeRolePermissionSeeder)],
        [typeof(SysOperationSeeder), typeof(PromptResourceSeeder), typeof(PromptPermissionSeeder), typeof(PromptRolePermissionSeeder)],
        [typeof(SysOperationSeeder), typeof(AssistantResourceSeeder), typeof(AssistantPermissionSeeder), typeof(AssistantRolePermissionSeeder)]
    ];

    /// <summary>
    /// 模块内全部种子器（供 [Theory] 逐个检查）。
    /// </summary>
    public static TheoryData<Type> AllSeederTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in SeederTypes())
            {
                data.Add(type);
            }

            return data;
        }
    }

    /// <summary>
    /// 种子器清单必须与预期完全一致：新增一个种子器却不给它安排 Order 区间时，在这里先变红。
    /// </summary>
    [Fact]
    public void ModuleAssembly_Seeders_ShouldMatchExpectedRoster()
    {
        var expected = new[]
        {
            nameof(AiMenuSeeder),
            nameof(AssistantPermissionSeeder),
            nameof(AssistantResourceSeeder),
            nameof(AssistantRolePermissionSeeder),
            nameof(KnowledgePermissionSeeder),
            nameof(KnowledgeResourceSeeder),
            nameof(KnowledgeRolePermissionSeeder),
            nameof(PromptPermissionSeeder),
            nameof(PromptResourceSeeder),
            nameof(PromptRolePermissionSeeder),
            nameof(SysOperationSeeder),
            nameof(SysPermissionSeeder),
            nameof(SysResourceSeeder),
            nameof(SysRolePermissionSeeder)
        }.OrderBy(name => name, StringComparer.Ordinal).ToList();
        var actual = SeederTypes().Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, actual, StringComparer.Ordinal);
    }

    /// <summary>
    /// 每个种子器都必须在平台租户域内播种：非菜单类走 <see cref="PlatformDataSeederBase"/>（整段切到平台上下文），
    /// 菜单类走 <see cref="PageRegistryMenuSeederBase"/>（内部自行切平台域）。
    /// 挂在租户域上会把平台级权限/菜单误播到某个租户里。
    /// </summary>
    /// <param name="seederType">被检查的种子器类型。</param>
    [Theory]
    [MemberData(nameof(AllSeederTypes))]
    public void Seeder_ShouldSeedWithinPlatformTenantScope(Type seederType)
    {
        var isPlatformScoped = seederType.IsAssignableTo(typeof(PlatformDataSeederBase))
            || seederType.IsAssignableTo(typeof(PageRegistryMenuSeederBase));

        Assert.True(isPlatformScoped, $"{seederType.Name} 未继承平台域种子基类，会把平台数据播到当前租户里。");
    }

    /// <summary>
    /// 种子器名称必须带 <c>[Ai]</c> 前缀且互不相同：多模块种子共用一份运行日志，缺前缀就分不清是谁在播。
    /// </summary>
    [Fact]
    public void Seeder_Names_ShouldBeAiPrefixedAndUnique()
    {
        var names = SeederTypes().Select(type => (type.Name, Value: ReadName(type))).ToList();
        var badPrefix = names
            .Where(item => !item.Value.StartsWith("[Ai]", StringComparison.Ordinal))
            .Select(item => $"{item.Name}={item.Value}")
            .ToList();
        var duplicated = names
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(badPrefix.Count == 0, $"下列种子器名称缺少 [Ai] 前缀：{string.Join("；", badPrefix)}。");
        Assert.True(duplicated.Count == 0, $"种子器名称重复：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// 每个种子器的名称都不得为空白，否则运行日志里只剩一串空格。
    /// </summary>
    /// <param name="seederType">被检查的种子器类型。</param>
    [Theory]
    [MemberData(nameof(AllSeederTypes))]
    public void Seeder_Name_ShouldNotBeBlank(Type seederType)
    {
        Assert.False(string.IsNullOrWhiteSpace(ReadName(seederType)), $"{seederType.Name} 的 Name 为空白。");
    }

    /// <summary>
    /// 种子 Order 必须两两不同：同序号的两个种子器执行先后不确定，前置数据可能还没落库。
    /// </summary>
    [Fact]
    public void Seeder_Orders_ShouldBeUnique()
    {
        var duplicated = SeederTypes()
            .Select(type => (type.Name, Order: ReadOrder(type)))
            .GroupBy(item => item.Order)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("、", group.Select(item => item.Name))}）")
            .ToList();

        Assert.True(duplicated.Count == 0, $"种子 Order 重复：{string.Join("；", duplicated)}。");
    }

    /// <summary>
    /// AI 段的 Order 必须全部落在 200~217 的独立区间内，越界会与 Saas / 代码生成的种子链互相插队。
    /// </summary>
    /// <param name="seederType">被检查的种子器类型。</param>
    [Theory]
    [MemberData(nameof(AllSeederTypes))]
    public void Seeder_Order_ShouldStayInsideAiReservedRange(Type seederType)
    {
        var order = ReadOrder(seederType);

        Assert.True(order is >= 200 and <= 217, $"{seederType.Name} 的 Order={order} 越出 AI 预留区间 200~217。");
    }

    /// <summary>
    /// 操作字典必须是整段的第一个：<c>SysPermissionSeeder</c> 按「资源 × 操作」派生权限，
    /// 操作表为空时它会直接跳过，整条 AI 权限/菜单/授权链在干净库上静默失效。
    /// </summary>
    [Fact]
    public void OperationSeeder_ShouldRunFirstInTheModule()
    {
        var minimum = SeederTypes().Min(type => ReadOrder(type));

        Assert.Equal(200, ReadOrder(typeof(SysOperationSeeder)));
        Assert.Equal(minimum, ReadOrder(typeof(SysOperationSeeder)));
    }

    /// <summary>
    /// 菜单种子必须是整段的最后一个：菜单建立时即绑定权限可见性，权限还没落库就绑不上。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldRunLastInTheModule()
    {
        var maximum = SeederTypes().Max(type => ReadOrder(type));

        Assert.Equal(217, ReadOrder(typeof(AiMenuSeeder)));
        Assert.Equal(maximum, ReadOrder(typeof(AiMenuSeeder)));
    }

    /// <summary>
    /// 四条种子链的内部偏序必须严格成立：操作字典 → 资源 → 权限 → 角色授权。
    /// </summary>
    [Fact]
    public void SeedChains_ShouldRespectResourceThenPermissionThenGrantOrder()
    {
        var violations = new List<string>();
        foreach (var chain in OrderedSeedChains)
        {
            for (var index = 1; index < chain.Length; index++)
            {
                var previous = ReadOrder(chain[index - 1]);
                var current = ReadOrder(chain[index]);
                if (previous >= current)
                {
                    violations.Add($"{chain[index - 1].Name}({previous}) 必须早于 {chain[index].Name}({current})");
                }
            }
        }

        Assert.True(violations.Count == 0, $"种子链偏序被破坏：{string.Join("；", violations)}。");
    }

    /// <summary>
    /// 四段资源/权限/授权必须整段不交错：知识库(205-208) → 提示词库(209-212) → 助手(213-216)，
    /// 交错后前一段还没建完就开始建后一段，跨段依赖会读到半成品。
    /// </summary>
    [Fact]
    public void SeedSegments_ShouldNotInterleave()
    {
        Assert.Equal(201, ReadOrder(typeof(SysResourceSeeder)));
        Assert.Equal(202, ReadOrder(typeof(SysPermissionSeeder)));
        Assert.Equal(204, ReadOrder(typeof(SysRolePermissionSeeder)));
        Assert.Equal(205, ReadOrder(typeof(KnowledgeResourceSeeder)));
        Assert.Equal(206, ReadOrder(typeof(KnowledgePermissionSeeder)));
        Assert.Equal(208, ReadOrder(typeof(KnowledgeRolePermissionSeeder)));
        Assert.Equal(209, ReadOrder(typeof(PromptResourceSeeder)));
        Assert.Equal(210, ReadOrder(typeof(PromptPermissionSeeder)));
        Assert.Equal(212, ReadOrder(typeof(PromptRolePermissionSeeder)));
        Assert.Equal(213, ReadOrder(typeof(AssistantResourceSeeder)));
        Assert.Equal(214, ReadOrder(typeof(AssistantPermissionSeeder)));
        Assert.Equal(216, ReadOrder(typeof(AssistantRolePermissionSeeder)));
    }

    /// <summary>
    /// 菜单种子必须直接复用页面登记表这份单一事实源，另抄一份页面定义会与登记表悄悄漂移。
    /// </summary>
    [Fact]
    public void MenuSeeder_ShouldReusePageRegistryAsSingleSourceOfTruth()
    {
        var seeder = RuntimeHelpers.GetUninitializedObject(typeof(AiMenuSeeder));
        var pages = (IReadOnlyList<PageDescriptor>)ReadNonPublicProperty(seeder, "Pages")!;
        var buttons = (IReadOnlyList<ButtonDescriptor>)ReadNonPublicProperty(seeder, "Buttons")!;
        var moduleName = (string)ReadNonPublicProperty(seeder, "ModuleName")!;

        Assert.Same(AiPageRegistry.All, pages);
        Assert.Same(AiPageRegistry.Buttons, buttons);
        Assert.Equal("AI", moduleName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 种子器都不得声明为 abstract 或泛型：数据种子注册按具体类型登记，抽象/泛型类型无法被解析出来执行。
    /// </summary>
    /// <param name="seederType">被检查的种子器类型。</param>
    [Theory]
    [MemberData(nameof(AllSeederTypes))]
    public void Seeder_ShouldBeConcreteNonGenericType(Type seederType)
    {
        Assert.False(seederType.IsAbstract, $"{seederType.Name} 是抽象类，无法被注册执行。");
        Assert.False(seederType.IsGenericTypeDefinition, $"{seederType.Name} 是泛型定义，无法被注册执行。");
    }

    /// <summary>
    /// 每个种子器都必须重写 <c>SeedInternalAsync</c>（或由菜单基类统一提供实现），否则注册了也什么都不播。
    /// </summary>
    /// <param name="seederType">被检查的种子器类型。</param>
    [Theory]
    [MemberData(nameof(AllSeederTypes))]
    public void Seeder_ShouldProvideSeedImplementation(Type seederType)
    {
        var declaresOwnImplementation = seederType.GetMethod(
            "SeedInternalAsync",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is not null;
        var inheritsMenuImplementation = seederType.IsAssignableTo(typeof(PageRegistryMenuSeederBase));

        Assert.True(
            declaresOwnImplementation || inheritsMenuImplementation,
            $"{seederType.Name} 既未重写 SeedInternalAsync，也不是页面登记表驱动的菜单种子。");
    }

    /// <summary>
    /// 取模块内全部种子器类型。
    /// </summary>
    /// <returns>种子器类型清单。</returns>
    private static IReadOnlyList<Type> SeederTypes()
    {
        return [.. typeof(SysOperationSeeder).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsNested: false })
            .Where(type => string.Equals(type.Namespace, "XiHan.BasicApp.AI.Infrastructure.Seeders.System", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Seeder", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 读种子器的执行序号（常量属性，无需构造实例）。
    /// </summary>
    /// <param name="seederType">种子器类型。</param>
    /// <returns>执行序号。</returns>
    private static int ReadOrder(Type seederType)
    {
        return (int)ReadPublicProperty(seederType, "Order")!;
    }

    /// <summary>
    /// 读种子器的展示名称（常量属性，无需构造实例）。
    /// </summary>
    /// <param name="seederType">种子器类型。</param>
    /// <returns>展示名称。</returns>
    private static string ReadName(Type seederType)
    {
        return (string)ReadPublicProperty(seederType, "Name")!;
    }

    /// <summary>
    /// 在未初始化实例上读公共属性（仅适用于返回常量、不触碰字段的属性）。
    /// </summary>
    /// <param name="seederType">种子器类型。</param>
    /// <param name="propertyName">属性名。</param>
    /// <returns>属性值。</returns>
    private static object? ReadPublicProperty(Type seederType, string propertyName)
    {
        var instance = RuntimeHelpers.GetUninitializedObject(seederType);
        var property = seederType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{seederType.Name} 未声明 {propertyName} 属性。");
        return property.GetValue(instance);
    }

    /// <summary>
    /// 在未初始化实例上读受保护属性（仅适用于返回常量或静态引用、不触碰字段的属性）。
    /// </summary>
    /// <param name="instance">未初始化实例。</param>
    /// <param name="propertyName">属性名。</param>
    /// <returns>属性值。</returns>
    private static object? ReadNonPublicProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{instance.GetType().Name} 未声明 {propertyName} 属性。");
        return property.GetValue(instance);
    }
}
