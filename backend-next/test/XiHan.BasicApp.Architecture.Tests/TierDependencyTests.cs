// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 方案 D2：包间只能向下依赖、同层不引用；跨层协作经 Contracts 的接口与消息主题。
/// </summary>
public sealed class TierDependencyTests
{
    [Fact]
    public void EverySourceProject_IsAKnownPackage()
    {
        var unknown = Packages.Projects.Keys.Where(static name => !Packages.Ranks.ContainsKey(name)).ToList();
        Assert.True(unknown.Count == 0, "Unknown packages (add them to the catalog in Model/Packages.cs with their tier): " + string.Join(", ", unknown));
    }

    [Fact]
    public void EverySourceProject_IsReferencedByArchitectureTests()
    {
        var loaded = Packages.Assemblies.Select(static a => Packages.ShortName(a.GetName().Name!)).ToHashSet(StringComparer.Ordinal);
        var missing = Packages.Projects.Keys.Where(name => !loaded.Contains(name)).ToList();
        Assert.True(missing.Count == 0, "Packages not loaded by the architecture tests (add a ProjectReference and the assembly to Model/Packages.cs): " + string.Join(", ", missing));
    }

    [Fact]
    public void Packages_OnlyDependDownwards_AndNeverSideways()
    {
        var violations = new List<string>();
        foreach (var (package, path) in Packages.Projects)
        {
            var rank = Packages.Ranks[package];
            foreach (var referenced in Packages.PackageReferencesOf(path))
            {
                if (!Packages.Ranks.TryGetValue(referenced, out var referencedRank))
                {
                    violations.Add($"{package} -> {referenced}: unknown package");
                }
                else if (referencedRank >= rank)
                {
                    violations.Add($"{package} (rank {rank}) -> {referenced} (rank {referencedRank}): dependencies must point to a strictly lower rank");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Host_IsTheOnlyProjectUsingTheWebSdk()
    {
        foreach (var (package, path) in Packages.Projects)
        {
            var isWeb = File.ReadAllText(path).Contains("Microsoft.NET.Sdk.Web", StringComparison.Ordinal);
            Assert.True(isWeb == string.Equals(package, "Host", StringComparison.Ordinal), $"{package}: only the Host uses Microsoft.NET.Sdk.Web; capability packages are class libraries");
        }
    }
}
