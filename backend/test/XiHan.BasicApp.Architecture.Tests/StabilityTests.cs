// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Architecture.Tests.Model;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查包清单与操作的稳定级别
/// </summary>
/// <remarks>
/// Tier 0–2 的包是 Stable：6.x 内操作 ID、路由、请求与响应字段、权限码与错误码不破坏，要去掉先标 Deprecated、下个大版本再删
/// Tier 3 可选模块是 Preview，小版本里可以破坏。新操作可以在 Stable 包里先以 Preview 发布，但操作不能比所在的包更稳定
/// </remarks>
public sealed class StabilityTests
{
    /// <summary>
    /// 可选模块的起始秩
    /// </summary>
    private const int OptionalModuleRank = 30;

    [Fact]
    public void Manifests_AreStableUpToTier2_AndPreviewForOptionalModules()
    {
        var mismatches = Manifests()
            .Select(static m => (m.Package, m.Stability, Expected: Packages.Ranks[m.Package] < OptionalModuleRank ? Stability.Stable : Stability.Preview))
            .Where(static m => m.Stability != m.Expected)
            .Select(static m => $"{m.Package}: Stability = {m.Stability}, expected {m.Expected}")
            .ToList();

        Assert.True(mismatches.Count == 0, "Package manifests with an unexpected stability:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    [Fact]
    public void Operations_AreNoMoreStableThanTheirPackage()
    {
        var packages = Manifests().ToDictionary(static m => m.Package, static m => m.Stability, StringComparer.Ordinal);
        var mismatches = OperationContractTests.Operations()
            .Where(o => o.Attribute.Stability == Stability.Stable && packages[o.Package] != Stability.Stable)
            .Select(o => $"{o.Attribute.Id}: Stable in the {packages[o.Package]} package {o.Package}")
            .ToList();

        Assert.True(mismatches.Count == 0, "Operations more stable than their package:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    private static IEnumerable<(string Package, Stability Stability)> Manifests()
        => Packages.Assemblies
            .SelectMany(static a => a.GetTypes())
            .Where(static t => typeof(IFeature).IsAssignableFrom(t) && t is { IsAbstract: false, IsClass: true })
            .Select(static t => (Package: Packages.ShortName(t.Assembly.GetName().Name!), Feature: (IFeature)Activator.CreateInstance(t)!))
            .Where(static f => Packages.Ranks[f.Package] < Packages.Ranks["Host"])
            .Select(static f => (f.Package, f.Feature.Manifest.Stability));
}
