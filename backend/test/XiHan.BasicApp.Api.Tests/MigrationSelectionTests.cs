// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Infrastructure.Migrations;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 升级脚本选取规则测试：同一版本无论启动多少次都只执行一次，基线与程序版本各自划定边界。
/// </summary>
public sealed class MigrationSelectionTests
{
    private const string CurrentVersion = "3.9.0";

    /// <summary>
    /// 连续启动五次，每个脚本只执行一次。
    /// </summary>
    /// <remarks>
    /// 直接回归旧实现的缺陷：状态记在 version.txt 且「无事可做」分支写回未执行标记，
    /// 导致与当前版本同号的脚本每隔一次启动就重跑一次。
    /// </remarks>
    [Fact]
    public void SelectPending_ShouldRunEachScriptExactlyOnce_AcrossRepeatedStartups()
    {
        var scripts = BuildScripts("3.8.0", "3.9.0");
        var applied = new HashSet<string>(StringComparer.Ordinal);
        var executionsPerStartup = new List<int>();

        for (var startup = 0; startup < 5; startup++)
        {
            var pending = SqlScriptMigrationRunner.SelectPending(scripts, applied, "3.7.0", CurrentVersion);
            executionsPerStartup.Add(pending.Count);

            // 模拟执行成功后写入台账
            foreach (var script in pending)
            {
                _ = applied.Add(script.Version);
            }
        }

        Assert.Equal(2, executionsPerStartup[0]);
        Assert.Equal([0, 0, 0, 0], executionsPerStartup.Skip(1));
        Assert.Equal(2, executionsPerStartup.Sum());
    }

    /// <summary>
    /// 台账中已成功执行的版本不再选中。
    /// </summary>
    [Fact]
    public void SelectPending_ShouldSkipAppliedVersions()
    {
        var scripts = BuildScripts("3.8.0", "3.9.0");
        var applied = new HashSet<string>(["3.8.0"], StringComparer.Ordinal);

        var pending = SqlScriptMigrationRunner.SelectPending(scripts, applied, null, CurrentVersion);

        Assert.Equal(["3.9.0"], pending.Select(script => script.Version));
    }

    /// <summary>
    /// 不高于基线的脚本一律跳过：全新部署的表结构已是当前实体形态，早于基线的脚本没有意义。
    /// </summary>
    [Fact]
    public void SelectPending_ShouldSkipScriptsAtOrBelowBaseline()
    {
        var scripts = BuildScripts("3.7.0", "3.8.0", "3.9.0");

        var pending = SqlScriptMigrationRunner.SelectPending(scripts, new HashSet<string>(StringComparer.Ordinal), "3.8.0", CurrentVersion);

        Assert.Equal(["3.9.0"], pending.Select(script => script.Version));
    }

    /// <summary>
    /// 高于当前程序版本的脚本不执行：二进制还没升到那一版。
    /// </summary>
    [Fact]
    public void SelectPending_ShouldSkipScriptsAboveCurrentProgramVersion()
    {
        var scripts = BuildScripts("3.9.0", "3.10.0", "4.0.0");

        var pending = SqlScriptMigrationRunner.SelectPending(scripts, new HashSet<string>(StringComparer.Ordinal), null, CurrentVersion);

        Assert.Equal(["3.9.0"], pending.Select(script => script.Version));
    }

    /// <summary>
    /// 按语义化版本升序执行，而非文件名字典序。
    /// </summary>
    [Fact]
    public void SelectPending_ShouldOrderBySemanticVersion()
    {
        var scripts = BuildScripts("3.10.0", "3.9.0", "3.2.0");

        var pending = SqlScriptMigrationRunner.SelectPending(scripts, new HashSet<string>(StringComparer.Ordinal), null, "3.10.0");

        Assert.Equal(["3.2.0", "3.9.0", "3.10.0"], pending.Select(script => script.Version));
    }

    /// <summary>
    /// 版本比较按数值而非字符串：3.10.0 高于 3.9.0。
    /// </summary>
    [Fact]
    public void CompareVersion_ShouldCompareNumericallyPerSegment()
    {
        Assert.True(SqlScriptMigrationRunner.CompareVersion("3.10.0", "3.9.0") > 0);
        Assert.True(SqlScriptMigrationRunner.CompareVersion("3.9.0", "3.9.0") == 0);
        Assert.True(SqlScriptMigrationRunner.CompareVersion("3.9", "3.9.1") < 0);
        Assert.True(SqlScriptMigrationRunner.CompareVersion("bad", "0.0.1") < 0);
    }

    private static List<MigrationScript> BuildScripts(params string[] versions)
    {
        return [.. versions.Select(version => new MigrationScript(version, $"{version}.sql", $"/tmp/{version}.sql"))];
    }
}
