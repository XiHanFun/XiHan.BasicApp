// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 平台级种子必须在平台租户上下文内播种，且只播平台库。
/// </summary>
/// <remarks>
/// 种子写出的操作、资源、权限、菜单等行必须落在 TenantId = 0：不切平台上下文，行就落在启动时的租户下，
/// 按 TenantId = 0 查找的一方（如菜单解析权限）查不到。全部种子因此统一继承 <see cref="PlatformDataSeederBase"/>。
/// 框架把所有模块的种子按 Order 统一排序，各模块在同一阶段里错开号段（<see cref="SeedOrders"/>）。
/// <para>
/// 本应用的种子写的都是固定在平台库的实体（目录、角色与授权、配置字典、模板等）：租户独立库初始化只建表，
/// 不播这些种子——在租户上下文里跑一遍只会把平台数据戳上租户号写进平台库。
/// </para>
/// </remarks>
public sealed class PlatformSeederScopeTests
{
    /// <summary>
    /// 全部业务模块程序集
    /// </summary>
    private static readonly Assembly[] AllModuleAssemblies =
    [
        typeof(BasicApp.Saas.XiHanBasicAppSaasModule).Assembly,
        typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly,
        typeof(BasicApp.Chat.XiHanBasicAppChatModule).Assembly,
        typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly,
        typeof(BasicApp.Printing.XiHanBasicAppPrintingModule).Assembly,
        typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly
    ];

    /// <summary>
    /// 模块号段：同一阶段内各模块错开的偏移
    /// </summary>
    private static readonly Dictionary<Assembly, int> ModuleOffsets = new()
    {
        [typeof(BasicApp.Saas.XiHanBasicAppSaasModule).Assembly] = 0,
        [typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly] = 10,
        [typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly] = 20,
        [typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly] = 30,
        [typeof(BasicApp.Chat.XiHanBasicAppChatModule).Assembly] = 40,
        [typeof(BasicApp.Printing.XiHanBasicAppPrintingModule).Assembly] = 50,
    };

    /// <summary>
    /// 全部模块的种子都继承平台种子基类（在平台上下文内播种）。
    /// </summary>
    [Fact]
    public void ModuleSeeders_ShouldSeedWithinPlatformTenantScope()
    {
        var violations = SeederTypes()
            .Where(type => !typeof(PlatformDataSeederBase).IsAssignableFrom(type))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个种子不在平台租户上下文内播种，" +
            $"其写出的行会落到启动时的租户下，按 TenantId = 0 查找的消费方将静默查不到：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 全部模块的种子 Order 两两不同：框架统一排序，同序号的执行先后不确定。
    /// </summary>
    [Fact]
    public void ModuleSeeders_OrdersShouldBeGloballyUnique()
    {
        var duplicated = SeederTypes()
            .GroupBy(OrderOf)
            .Where(group => group.Count() > 1)
            .Select(group => $"Order={group.Key} ← {string.Join(", ", group.Select(type => type.Name))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"种子优先级冲突：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 基础种子落在所属模块的号段里（阶段内的偏移 = 模块偏移 ~ 模块偏移 + 9），一眼能看出是哪个模块在哪个阶段；演示种子另成一段。
    /// </summary>
    [Fact]
    public void ModuleSeeders_ShouldStayInTheirModuleBand()
    {
        var violations = SeederTypes()
            .Where(type => OrderOf(type) < SeedOrders.Demo)
            .Where(type => OrderOf(type) % 100 / 10 * 10 != ModuleOffsets[type.Assembly])
            .Select(type => $"{type.Name}(Order={OrderOf(type)})")
            .ToList();

        Assert.True(violations.Count == 0, $"下列种子不在所属模块的号段里：{string.Join("、", violations)}");
    }

    /// <summary>
    /// 所有模块的种子只播平台库：租户独立库初始化不跑它们。
    /// </summary>
    [Fact]
    public void AllModuleSeeders_ShouldTargetPlatformDatabaseOnly()
    {
        var violations = SeederTypes()
            .Where(type => type.GetCustomAttribute<DataSeedingAttribute>(inherit: true)?.Target != DbInitializationTarget.Platform)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个种子没有声明只播平台库（[DataSeeding(Target = DbInitializationTarget.Platform)]），" +
            $"租户独立库初始化时会在租户上下文里再跑一遍：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static List<Type> SeederTypes()
    {
        return [.. AllModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDataSeeder).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)];
    }

    private static int OrderOf(Type type)
    {
        return ((IDataSeeder)RuntimeHelpers.GetUninitializedObject(type)).Order;
    }
}
