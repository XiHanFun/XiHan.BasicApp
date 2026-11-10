// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查框架的包版本与锁定提交
/// </summary>
/// <remarks>
/// <c>props/framework.props</c>：<c>XiHan.BasicApp.slnx</c> 按 <c>XiHanFrameworkPackageVersion</c> 还原 NuGet 包，必须是确切版本；
/// <c>XiHanFun.Local.slnx</c> 以源码引用同级 XiHan.Framework，检出的 HEAD 必须是 <c>XiHanFrameworkCommit</c>。升级框架时两处一起改
/// </remarks>
public sealed class FrameworkPinTests
{
    [Fact]
    public void PackageVersion_IsExact()
    {
        var version = Pin("XiHanFrameworkPackageVersion");
        Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$", version);
    }

    [Fact]
    public void FrameworkCheckout_IsAtThePinnedCommit()
    {
        var pinned = Pin("XiHanFrameworkCommit");
        Assert.Matches("^[0-9a-f]{7,40}$", pinned);
        Assert.SkipUnless(BuiltFromFrameworkSource(), "Built against the XiHan.Framework packages; the pinned commit applies to the source build (XiHanFun.Local.slnx).");

        // 与 framework.props 的缺省相对路径一致：backend/../../XiHan.Framework
        var framework = Path.GetFullPath(Path.Combine(Packages.Root, "..", "..", "XiHan.Framework"));
        Assert.True(Directory.Exists(Path.Combine(framework, "framework", "src")), $"Framework source is expected at {framework} for the source build.");

        var head = Git(framework, "rev-parse", "HEAD");
        Assert.True(head.StartsWith(pinned, StringComparison.Ordinal), $"XiHan.Framework HEAD is {head} but props/framework.props pins {pinned}; check out the pinned commit or update the pin deliberately.");
    }

    private static string Pin(string name)
    {
        var props = XDocument.Load(Path.Combine(Packages.Root, "props", "framework.props"));
        return props.Descendants(name).Single().Value.Trim();
    }

    private static bool BuiltFromFrameworkSource()
        => typeof(FrameworkPinTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(static attribute => attribute.Key == "XiHanFrameworkSource" && string.Equals(attribute.Value, "true", StringComparison.OrdinalIgnoreCase));

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
