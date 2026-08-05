// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 平台级种子必须在平台租户上下文内播种。
/// </summary>
/// <remarks>
/// 起因：AI / CodeGeneration / Workflow 三个模块的权限链种子直接继承 DataSeederBase，
/// 未切平台租户，写出的操作/资源/权限行落在了启动时的租户上下文下而非 TenantId = 0。
/// 菜单种子按 TenantId = 0 解析权限，查不到即跳过，表现为干净库重建后少了 7 个菜单，
/// 且只有一条 WRN 日志，其余一切正常。
/// </remarks>
public sealed class PlatformSeederScopeTests
{
    /// <summary>
    /// 明确按租户维度播种、无需平台域的种子。列入即声明「这份数据属于当前租户」。
    /// </summary>
    private static readonly IReadOnlySet<string> TenantScopedSeeders = new HashSet<string>(StringComparer.Ordinal)
    {
        // 代码生成模板：随租户走还是随平台走尚未裁定，维持原行为，待确认后再归位
        "SysCodeGenTemplateSeeder"
    };

    /// <summary>
    /// 各业务模块的种子必须继承平台域基类，或显式列入按租户播种的白名单。
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
            .Where(type => !TenantScopedSeeders.Contains(type.Name))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个种子既不在平台租户上下文内播种，也不在按租户播种的白名单内，" +
            $"其写出的行会落到启动时的租户下，按 TenantId = 0 查找的消费方将静默查不到：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 白名单不得残留失效条目：种子已删除或已改为平台域后必须同步移除。
    /// </summary>
    [Fact]
    public void TenantScopedAllowList_ShouldNotContainStaleEntries()
    {
        Assembly[] moduleAssemblies =
        [
            typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly,
            typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly,
            typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly
        ];

        var live = moduleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDataSeeder).IsAssignableFrom(type))
            .Where(type => !typeof(PlatformDataSeederBase).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var stale = TenantScopedSeeders.Where(name => !live.Contains(name)).ToList();

        Assert.True(stale.Count == 0,
            $"下列白名单条目已失效，请移除：{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }
}
