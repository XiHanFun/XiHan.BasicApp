// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 受控落盘写入器测试。
/// </summary>
/// <remarks>
/// 这是本模块唯一会往磁盘写文件的组件，安全策略是 fail-closed 的三道闸：
/// 默认禁用 → 必须配白名单根目录 → 目标路径与每个产物拼接后都必须落在根目录内。
/// 任何一道闸松掉，代码生成就成了任意文件写入原语。
/// 此外手动文件（<see cref="ArtifactWriteMode.WriteOnce"/>）已存在时必须跳过，
/// 保护开发者写在里面的自定义代码。
/// 用例只在 <c>Path.GetTempPath()</c> 下建独立目录，测试结束递归清理。
/// </remarks>
public sealed class CodeGenFileSystemArtifactWriterTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    /// <summary>
    /// 清理本用例创建的全部临时目录。
    /// </summary>
    public void Dispose()
    {
        foreach (var directory in _tempDirectories)
        {
            CodeGenerationTestHelper.DeleteDirectorySafely(directory);
        }
    }

    /// <summary>
    /// 新建一个受本用例托管的临时目录。
    /// </summary>
    private string NewTempDirectory()
    {
        var path = CodeGenerationTestHelper.CreateTempDirectory();
        _tempDirectories.Add(path);
        return path;
    }

    /// <summary>
    /// 构造写入器。
    /// </summary>
    /// <param name="enabled">是否启用落盘</param>
    /// <param name="allowedRoots">白名单根目录</param>
    private static FileSystemArtifactWriter CreateWriter(bool enabled, params string[] allowedRoots)
    {
        return new FileSystemArtifactWriter(Options.Create(new CodeGenerationOptions
        {
            EnableCustomPathDisk = enabled,
            AllowedRootPaths = allowedRoots
        }));
    }

    /// <summary>
    /// 构造一个产物。
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <param name="content">内容</param>
    /// <param name="writeMode">写入策略</param>
    private static GeneratedArtifact Artifact(
        string relativePath,
        string content = "content",
        ArtifactWriteMode writeMode = ArtifactWriteMode.AlwaysOverwrite)
    {
        return new GeneratedArtifact(relativePath, "x", content, "tpl", writeMode);
    }

    /// <summary>
    /// 未显式开启落盘时一律拒绝。
    /// </summary>
    [Fact]
    public async Task WriteAsync_DisabledShouldFailClosed()
    {
        var root = NewTempDirectory();
        var writer = CreateWriter(enabled: false, root);

        var result = await writer.WriteAsync([Artifact("A.cs")], root);

        Assert.False(result.Success);
        Assert.Contains("自定义路径落盘未启用", result.Message!, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(root));
    }

    /// <summary>
    /// 已开启但没配白名单同样拒绝——空白名单等于"哪儿都不许写"。
    /// </summary>
    [Fact]
    public async Task WriteAsync_EmptyAllowedRootsShouldFailClosed()
    {
        var root = NewTempDirectory();
        var writer = CreateWriter(enabled: true);

        var result = await writer.WriteAsync([Artifact("A.cs")], root);

        Assert.False(result.Success);
        Assert.Contains("未配置落盘白名单根目录", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 表配置没填生成路径时拒绝，并明确指向 GenPath。
    /// </summary>
    /// <param name="targetRoot">空白目标路径</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WriteAsync_BlankTargetRootShouldFail(string? targetRoot)
    {
        var root = NewTempDirectory();
        var writer = CreateWriter(enabled: true, root);

        var result = await writer.WriteAsync([Artifact("A.cs")], targetRoot);

        Assert.False(result.Success);
        Assert.Contains("GenPath", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 目标路径不在白名单内必须拒绝，且不得写出任何文件。
    /// </summary>
    [Fact]
    public async Task WriteAsync_TargetOutsideWhitelistShouldFail()
    {
        var allowed = NewTempDirectory();
        var outside = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([Artifact("A.cs")], outside);

        Assert.False(result.Success);
        Assert.Contains("不在白名单内", result.Message!, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(outside));
    }

    /// <summary>
    /// 白名单根目录本身与其子目录都属于允许范围。
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhitelistRootItselfAndSubDirectoryShouldBeAllowed()
    {
        var allowed = NewTempDirectory();
        var child = Path.Combine(allowed, "sub", "deeper");
        var writer = CreateWriter(enabled: true, allowed);

        var atRoot = await writer.WriteAsync([Artifact("A.cs")], allowed);
        var atChild = await writer.WriteAsync([Artifact("B.cs")], child);

        Assert.True(atRoot.Success);
        Assert.True(atChild.Success);
        Assert.True(File.Exists(Path.Combine(allowed, "A.cs")));
        Assert.True(File.Exists(Path.Combine(child, "B.cs")));
    }

    /// <summary>
    /// 与白名单同前缀但并非其子目录的路径必须被拒（防 <c>C:\gen</c> 放行 <c>C:\gen-evil</c>）。
    /// </summary>
    [Fact]
    public async Task WriteAsync_SiblingWithSamePrefixShouldNotBeTreatedAsInside()
    {
        var allowed = NewTempDirectory();
        var sibling = allowed + "-evil";
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([Artifact("A.cs")], sibling);

        Assert.False(result.Success);
        Assert.Contains("不在白名单内", result.Message!, StringComparison.Ordinal);
        Assert.False(Directory.Exists(sibling));
    }

    /// <summary>
    /// 产物相对路径为绝对路径或带盘符时必须整体拒绝。
    /// </summary>
    /// <param name="relativePath">非法相对路径</param>
    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/evil.cs")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WriteAsync_RootedOrBlankArtifactPathShouldFail(string relativePath)
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([Artifact(relativePath)], allowed);

        Assert.False(result.Success);
        Assert.Contains("产物相对路径非法", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 产物相对路径经 <c>..</c> 逃逸出目标根时必须被二次校验拦下。
    /// </summary>
    [Fact]
    public async Task WriteAsync_TraversalArtifactPathShouldFail()
    {
        var allowed = NewTempDirectory();
        var target = Path.Combine(allowed, "project");
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([Artifact("../../escaped.cs")], target);

        Assert.False(result.Success);
        Assert.Contains("产物路径越界", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 自动文件按相对路径建目录并写入，重复写入直接覆盖。
    /// </summary>
    [Fact]
    public async Task WriteAsync_AlwaysOverwriteArtifactShouldBeRewritten()
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);
        var target = Path.Combine(allowed, "Domain", "Entities", "A.Generated.cs");

        await writer.WriteAsync([Artifact("Domain/Entities/A.Generated.cs", "v1")], allowed);
        var second = await writer.WriteAsync([Artifact("Domain/Entities/A.Generated.cs", "v2")], allowed);

        Assert.True(second.Success);
        Assert.Equal(1, second.WrittenCount);
        Assert.Equal(0, second.SkippedCount);
        Assert.Equal("v2", await File.ReadAllTextAsync(target), StringComparer.Ordinal);
    }

    /// <summary>
    /// 手动文件已存在时必须跳过并记入 SkippedPaths，绝不覆盖开发者写的代码。
    /// </summary>
    [Fact]
    public async Task WriteAsync_ExistingWriteOnceArtifactShouldBeSkipped()
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);
        var artifact = Artifact("Domain/Entities/A.cs", "generated", ArtifactWriteMode.WriteOnce);

        var first = await writer.WriteAsync([artifact], allowed);
        await File.WriteAllTextAsync(Path.Combine(allowed, "Domain", "Entities", "A.cs"), "我手写的业务代码");
        var second = await writer.WriteAsync([artifact], allowed);

        Assert.Equal(1, first.WrittenCount);
        Assert.Equal(0, second.WrittenCount);
        Assert.Equal(1, second.SkippedCount);
        Assert.Contains("Domain/Entities/A.cs", second.SkippedPaths);
        Assert.Equal(
            "我手写的业务代码",
            await File.ReadAllTextAsync(Path.Combine(allowed, "Domain", "Entities", "A.cs")),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 反斜杠相对路径也要能正确落到子目录。
    /// </summary>
    [Fact]
    public async Task WriteAsync_BackslashRelativePathShouldStillLandInSubDirectory()
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([Artifact("Domain\\Entities\\A.cs")], allowed);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(allowed, "Domain", "Entities", "A.cs")));
    }

    /// <summary>
    /// 内容为 null 的产物写成空文件，不得抛空引用。
    /// </summary>
    [Fact]
    public async Task WriteAsync_NullContentShouldProduceEmptyFile()
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([new GeneratedArtifact("A.cs", "A.cs", null!, "tpl")], allowed);

        Assert.True(result.Success);
        Assert.Equal(string.Empty, await File.ReadAllTextAsync(Path.Combine(allowed, "A.cs")), StringComparer.Ordinal);
    }

    /// <summary>
    /// 产物清单为空引用必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task WriteAsync_NullArtifactsShouldThrow()
    {
        var writer = CreateWriter(enabled: true, NewTempDirectory());

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteAsync(null!, "any"));
    }

    /// <summary>
    /// 空产物清单是合法输入，返回写入 0 个的成功结果。
    /// </summary>
    [Fact]
    public async Task WriteAsync_EmptyArtifactsShouldSucceedWithZeroWritten()
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);

        var result = await writer.WriteAsync([], allowed);

        Assert.True(result.Success);
        Assert.Equal(0, result.WrittenCount);
        Assert.Empty(result.SkippedPaths);
    }

    /// <summary>
    /// 白名单里的空白项必须被跳过，不得让空串把整个文件系统放行。
    /// </summary>
    [Fact]
    public async Task WriteAsync_BlankWhitelistEntryShouldNotAllowEverything()
    {
        var allowed = NewTempDirectory();
        var outside = NewTempDirectory();
        var writer = CreateWriter(enabled: true, "   ", allowed);

        var result = await writer.WriteAsync([Artifact("A.cs")], outside);

        Assert.False(result.Success);
        Assert.Contains("不在白名单内", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 令牌已取消时必须在写第一个文件前抛出。
    /// </summary>
    [Fact]
    public async Task WriteAsync_CancelledTokenShouldThrowBeforeWriting()
    {
        var allowed = NewTempDirectory();
        var writer = CreateWriter(enabled: true, allowed);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => writer.WriteAsync([Artifact("A.cs")], allowed, cts.Token));

        Assert.Empty(Directory.GetFiles(allowed));
    }

    /// <summary>
    /// 失败结果统一是"零写入、零跳过、空清单"，调用方不必再判空。
    /// </summary>
    [Fact]
    public void Fail_ShouldCarryZeroCountsAndEmptyPaths()
    {
        var result = GeneratedArtifactWriteResult.Fail("坏了");

        Assert.False(result.Success);
        Assert.Equal("坏了", result.Message, StringComparer.Ordinal);
        Assert.Equal(0, result.WrittenCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.SkippedPaths);
    }
}
