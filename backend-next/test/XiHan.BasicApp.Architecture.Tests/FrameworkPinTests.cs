// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Xml.Linq;
using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 方案 D11：框架以 ProjectReference 引用并锁定提交。<c>props/framework.props</c> 里的 <c>XiHanFrameworkCommit</c> 必须是同级 XiHan.Framework 检出的 HEAD；升级框架时同步改这一处。
/// </summary>
public sealed class FrameworkPinTests
{
    [Fact]
    public void FrameworkCheckout_IsAtThePinnedCommit()
    {
        var props = XDocument.Load(Path.Combine(Packages.Root, "props", "framework.props"));
        var pinned = props.Descendants("XiHanFrameworkCommit").Single().Value.Trim();
        Assert.Matches("^[0-9a-f]{7,40}$", pinned);

        // 与 framework.props 的缺省相对路径一致：backend-next/../../XiHan.Framework
        var framework = Path.GetFullPath(Path.Combine(Packages.Root, "..", "..", "XiHan.Framework"));
        Assert.True(Directory.Exists(Path.Combine(framework, "next", "src")), $"Framework source is expected at {framework} (ProjectReference mode); package mode is not used yet.");

        var head = Git(framework, "rev-parse", "HEAD");
        Assert.True(head.StartsWith(pinned, StringComparison.Ordinal), $"XiHan.Framework HEAD is {head} but props/framework.props pins {pinned}; check out the pinned commit or update the pin deliberately.");
    }

    private static string Git(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = directory };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {process.StandardError.ReadToEnd()}");
        return output;
    }
}
