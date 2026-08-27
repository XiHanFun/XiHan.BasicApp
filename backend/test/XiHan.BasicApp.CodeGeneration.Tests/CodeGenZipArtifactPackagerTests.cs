// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 产物 Zip 打包器测试。
/// </summary>
/// <remarks>
/// 打包器的唯一职责是"让解压的人不会误覆盖自己写的代码"：
/// 自动文件进 <c>_generated/</c>（可整体覆盖），手动文件进 <c>_manual/</c>（仅在目标不存在时拷贝），
/// 并在包根附 README 说明区别。两个目录一旦混在一起，用户整体覆盖就会冲掉自定义代码，
/// 而这类丢失是无声的。全部断言在内存流上完成，不落盘。
/// </remarks>
public sealed class CodeGenZipArtifactPackagerTests
{
    private readonly ZipArtifactPackager _packager = new();

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
        return new GeneratedArtifact(relativePath, Path.GetFileName(relativePath), content, "tpl", writeMode);
    }

    /// <summary>
    /// 打开压缩包并读出全部条目路径。
    /// </summary>
    /// <param name="package">压缩包字节流</param>
    private static IReadOnlyList<string> EntryNames(byte[] package)
    {
        using var stream = new MemoryStream(package);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        return [.. archive.Entries.Select(entry => entry.FullName)];
    }

    /// <summary>
    /// 读取指定条目的文本内容。
    /// </summary>
    /// <param name="package">压缩包字节流</param>
    /// <param name="entryName">条目名</param>
    private static string EntryText(byte[] package, string entryName)
    {
        using var stream = new MemoryStream(package);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"压缩包中不存在条目 {entryName}");
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 产物清单为空引用必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task PackAsync_NullArtifactsShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _packager.PackAsync(null!));
    }

    /// <summary>
    /// 没有任何产物时仍必须产出带 README 的合法压缩包。
    /// </summary>
    [Fact]
    public async Task PackAsync_EmptyArtifactsShouldStillContainReadme()
    {
        var package = await _packager.PackAsync([]);

        Assert.Equal(["README.txt"], EntryNames(package));
    }

    /// <summary>
    /// 自动文件与手动文件必须按写入策略分到两个互不重叠的目录。
    /// </summary>
    [Fact]
    public async Task PackAsync_ShouldSplitArtifactsByWriteMode()
    {
        var package = await _packager.PackAsync(
        [
            Artifact("Domain/Entities/SysProduct.Generated.cs"),
            Artifact("Domain/Entities/SysProduct.cs", writeMode: ArtifactWriteMode.WriteOnce)
        ]);

        var entries = EntryNames(package);

        Assert.Contains("_generated/Domain/Entities/SysProduct.Generated.cs", entries);
        Assert.Contains("_manual/Domain/Entities/SysProduct.cs", entries);
    }

    /// <summary>
    /// 反斜杠路径必须统一成正斜杠，前导分隔符必须去掉，避免解压出畸形目录。
    /// </summary>
    /// <param name="relativePath">原始相对路径</param>
    /// <param name="expected">期望的包内条目名</param>
    [Theory]
    [InlineData("Domain\\Entities\\A.cs", "_generated/Domain/Entities/A.cs")]
    [InlineData("/Domain/A.cs", "_generated/Domain/A.cs")]
    [InlineData("//Domain/A.cs", "_generated/Domain/A.cs")]
    [InlineData("A.cs", "_generated/A.cs")]
    public async Task PackAsync_ShouldNormalizeEntryPath(string relativePath, string expected)
    {
        var package = await _packager.PackAsync([Artifact(relativePath)]);

        Assert.Contains(expected, EntryNames(package));
    }

    /// <summary>
    /// 相对路径为空白时回落到 Unnamed，不得产出空条目名让解压失败。
    /// </summary>
    /// <param name="relativePath">空白路径</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PackAsync_BlankRelativePathShouldFallBackToUnnamed(string relativePath)
    {
        var package = await _packager.PackAsync(
        [
            new GeneratedArtifact(relativePath, "x", "content", "tpl")
        ]);

        Assert.Contains("_generated/Unnamed", EntryNames(package));
    }

    /// <summary>
    /// 文件内容必须按 UTF-8 原样写入（含中文与换行）。
    /// </summary>
    [Fact]
    public async Task PackAsync_ShouldPreserveUtf8Content()
    {
        const string Content = "// 中文注释\nclass A { }\n";

        var package = await _packager.PackAsync([Artifact("A.cs", Content)]);

        Assert.Equal(Content, EntryText(package, "_generated/A.cs"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 内容为 null 的产物按空文件写入，不得抛空引用。
    /// </summary>
    [Fact]
    public async Task PackAsync_NullContentShouldBecomeEmptyEntry()
    {
        var package = await _packager.PackAsync([new GeneratedArtifact("A.cs", "A.cs", null!, "tpl")]);

        Assert.Equal(string.Empty, EntryText(package, "_generated/A.cs"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 包根 README 必须给出两类文件的数量，并逐条列出手动文件清单。
    /// </summary>
    [Fact]
    public async Task PackAsync_ReadmeShouldReportCountsAndListManualFiles()
    {
        var package = await _packager.PackAsync(
        [
            Artifact("A.Generated.cs"),
            Artifact("B.Generated.cs"),
            Artifact("C.cs", writeMode: ArtifactWriteMode.WriteOnce)
        ]);

        var readme = EntryText(package, "README.txt");

        Assert.Contains("_generated/  自动文件，共 2 个", readme, StringComparison.Ordinal);
        Assert.Contains("_manual/  手动文件，共 1 个", readme, StringComparison.Ordinal);
        Assert.Contains("手动文件清单：", readme, StringComparison.Ordinal);
        Assert.Contains("C.cs", readme, StringComparison.Ordinal);
    }

    /// <summary>
    /// 没有手动文件时 README 不得出现手动文件清单小节。
    /// </summary>
    [Fact]
    public async Task PackAsync_ReadmeShouldOmitManualListWhenNoManualArtifacts()
    {
        var package = await _packager.PackAsync([Artifact("A.Generated.cs")]);

        var readme = EntryText(package, "README.txt");

        Assert.DoesNotContain("手动文件清单：", readme, StringComparison.Ordinal);
    }

    /// <summary>
    /// 令牌已取消时必须在写入第一个条目前抛出。
    /// </summary>
    [Fact]
    public async Task PackAsync_CancelledTokenShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _packager.PackAsync([Artifact("A.cs")], cts.Token));
    }

    /// <summary>
    /// 只枚举一次的惰性序列也必须能正常打包（实现内部会先物化）。
    /// </summary>
    /// <remarks>
    /// README 的统计需要再遍历一遍产物；若不物化，惰性序列被消费两次会导致统计为空或重复取数。
    /// </remarks>
    [Fact]
    public async Task PackAsync_LazySequenceShouldBeMaterializedBeforeReadme()
    {
        var enumerationCount = 0;

        IEnumerable<GeneratedArtifact> Lazy()
        {
            enumerationCount++;
            yield return Artifact("A.cs", writeMode: ArtifactWriteMode.WriteOnce);
        }

        var package = await _packager.PackAsync(Lazy());

        Assert.Equal(1, enumerationCount);
        Assert.Contains("_manual/A.cs", EntryNames(package));
        Assert.Contains("_manual/  手动文件，共 1 个", EntryText(package, "README.txt"), StringComparison.Ordinal);
    }

    /// <summary>
    /// 打包结果必须是可被 <see cref="ZipArchive"/> 正常读回的完整字节流。
    /// </summary>
    [Fact]
    public async Task PackAsync_ShouldReturnCompleteReadableArchive()
    {
        var package = await _packager.PackAsync([Artifact("A.cs")]);

        Assert.NotEmpty(package);
        Assert.Equal(2, EntryNames(package).Count);
    }
}
