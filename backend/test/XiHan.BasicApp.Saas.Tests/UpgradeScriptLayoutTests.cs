// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using XiHan.Framework.Upgrade.Options;
using XiHan.Framework.Upgrade.Services;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 升级脚本目录布局测试。
/// </summary>
/// <remarks>
/// 守的是一件很容易再次发生的事：<c>UpdateScripts</c> 的目录布局必须与框架
/// <c>FileSystemUpgradeScriptProvider</c> 的扫描方式对得上。
/// <para>
/// 它扫的是 <c>UpdateScripts/&lt;版本&gt;/*.sql</c> 子目录（第一步就是 <c>Directory.GetDirectories</c>）。
/// 2026-08-27 之前脚本是平铺在根目录下的，于是 provider 一条都收不到——四个迁移脚本
/// 从写下起就没执行过，而且没有任何东西会报错，是静默失效。
/// </para>
/// <para>
/// 这条测试直接拿真实 provider 扫真实目录，布局一退化就红。
/// </para>
/// </remarks>
public sealed class UpgradeScriptLayoutTests
{
    /// <summary>
    /// 框架 provider 必须能扫到仓库里的全部升级脚本。
    /// </summary>
    [Fact]
    public async Task UpdateScripts_ShouldBeDiscoverableByFrameworkProvider()
    {
        var rootPath = ResolveUpdateScriptsRoot();
        Assert.True(Directory.Exists(rootPath), $"升级脚本目录不存在：{rootPath}");

        // 目录下实际有多少 .sql（含子目录），provider 就该扫出多少
        var actualSqlCount = Directory.GetFiles(rootPath, "*.sql", SearchOption.AllDirectories).Length;
        Assert.True(actualSqlCount > 0, "升级脚本目录下一个 .sql 都没有，测试失去意义");

        var provider = new FileSystemUpgradeScriptProvider(
            Options.Create(new XiHanUpgradeOptions { MigrationsRootPath = rootPath }));

        var scripts = await provider.GetScriptsAsync();

        Assert.Equal(actualSqlCount, scripts.Count);
    }

    /// <summary>
    /// 脚本必须落在以版本号命名的子目录里，而不是平铺在根目录下。
    /// </summary>
    [Fact]
    public void UpdateScripts_ShouldNotContainFlatSqlFiles()
    {
        var rootPath = ResolveUpdateScriptsRoot();

        var flatFiles = Directory.GetFiles(rootPath, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(
            flatFiles.Count == 0,
            $"这些脚本平铺在 UpdateScripts 根目录下，provider 只扫子目录、收不到它们：{string.Join(", ", flatFiles)}");
    }

    /// <summary>
    /// 定位仓库中的升级脚本目录
    /// </summary>
    /// <remarks>
    /// 以本测试源文件位置为锚点向上回溯，不依赖运行目录：
    /// <c>backend/test/XiHan.BasicApp.Saas.Tests/</c> → <c>backend/src/main/XiHan.BasicApp.WebHost/UpdateScripts</c>。
    /// </remarks>
    private static string ResolveUpdateScriptsRoot([CallerFilePath] string testFilePath = "")
    {
        var testDirectory = Path.GetDirectoryName(testFilePath)
            ?? throw new InvalidOperationException("无法解析测试源文件目录。");

        return Path.GetFullPath(Path.Combine(
            testDirectory, "..", "..", "src", "main", "XiHan.BasicApp.WebHost", "UpdateScripts"));
    }
}
