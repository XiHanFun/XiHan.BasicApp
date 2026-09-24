// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 平台级种子必须在平台租户上下文内播种，且只播平台库。
/// </summary>
/// <remarks>
/// 起因：AI / CodeGeneration / Workflow 三个模块的权限链种子直接继承 DataSeederBase，
/// 未切平台租户，写出的操作/资源/权限行落在了启动时的租户上下文下而非 TenantId = 0。
/// 菜单种子按 TenantId = 0 解析权限，查不到即跳过，表现为干净库重建后少了 7 个菜单，
/// 且只有一条 WRN 日志，其余一切正常。
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
    /// AI / 代码生成 / 工作流的种子必须继承平台域基类（在平台租户上下文内播种）。
    /// </summary>
    [Fact]
    public void ModuleSeeders_ShouldSeedWithinPlatformTenantScope()
    {
        Assembly[] moduleAssemblies =
        [
            typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly,
            typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly,
            typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly
        ];

        var violations = moduleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDataSeeder).IsAssignableFrom(type))
            .Where(type => !typeof(PlatformDataSeederBase).IsAssignableFrom(type))
            .Where(type => !typeof(PageRegistryMenuSeederBase).IsAssignableFrom(type))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个种子不在平台租户上下文内播种，" +
            $"其写出的行会落到启动时的租户下，按 TenantId = 0 查找的消费方将静默查不到：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 所有模块的种子只播平台库：租户独立库初始化不跑它们。
    /// </summary>
    [Fact]
    public void AllModuleSeeders_ShouldTargetPlatformDatabaseOnly()
    {
        var violations = AllModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDataSeeder).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttribute<DataSeedingAttribute>(inherit: true)?.Target != DbInitializationTarget.Platform)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个种子没有声明只播平台库（[DataSeeding(Target = DbInitializationTarget.Platform)]），" +
            $"租户独立库初始化时会在租户上下文里再跑一遍：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}
