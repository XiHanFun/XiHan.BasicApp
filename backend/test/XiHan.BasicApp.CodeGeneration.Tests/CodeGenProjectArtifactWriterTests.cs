// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 生成到项目的写入器测试。
/// </summary>
/// <remarks>
/// 这是本模块唯一会往磁盘写文件的组件。位置不由表配置随意指定：默认关闭 → 后端只写进源码根下与命名空间同名的项目、
/// 前端只写进含 package.json 的前端工程 → 每个产物拼接后都必须落在所属根内，且全部定好位置才动磁盘。
/// 任何一道闸松掉，代码生成就成了任意文件写入原语。
/// 此外手动文件（<see cref="ArtifactWriteMode.WriteOnce"/>）已存在时必须跳过，保护开发者写在里面的自定义代码。
/// 每个用例在 <c>Path.GetTempPath()</c> 下搭一份仓库骨架（宿主内容根为 backend/src/main/WebHost），测试结束递归清理。
/// </remarks>
public sealed class CodeGenProjectArtifactWriterTests : IDisposable
{
    private const string Project = "XiHan.BasicApp.Demo";

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
    /// 搭一份仓库骨架：后端 business 分组下有 Demo 项目，前端工程带 package.json。
    /// </summary>
    private RepositoryLayout NewRepository()
    {
        var root = CodeGenerationTestHelper.CreateTempDirectory();
        _tempDirectories.Add(root);
        var contentRoot = Directory.CreateDirectory(Path.Combine(root, "backend", "src", "main", "WebHost")).FullName;
        var project = Directory.CreateDirectory(Path.Combine(root, "backend", "src", "business", Project)).FullName;
        File.WriteAllText(Path.Combine(project, Project + ".csproj"), "<Project />");
        var frontend = Directory.CreateDirectory(Path.Combine(root, "frontend")).FullName;
        File.WriteAllText(Path.Combine(frontend, "package.json"), "{}");
        return new RepositoryLayout(root, contentRoot, project, frontend);
    }

    /// <summary>
    /// 构造写入器（缺省按开发环境的相对配置）。
    /// </summary>
    private static ProjectArtifactWriter CreateWriter(
        RepositoryLayout layout,
        bool enabled = true,
        string? backendRootPath = "../..",
        string? frontendRootPath = "../../../../frontend")
    {
        var options = Options.Create(new CodeGenerationOptions
        {
            EnableGenerateToProject = enabled,
            BackendRootPath = backendRootPath,
            FrontendRootPath = frontendRootPath
        });
        return new ProjectArtifactWriter(options, Mock.Of<IHostEnvironment>(environment => environment.ContentRootPath == layout.ContentRoot));
    }

    /// <summary>
    /// 构造一个产物。
    /// </summary>
    private static GeneratedArtifact Artifact(
        string relativePath,
        ArtifactSide? side = ArtifactSide.Backend,
        string? content = "content",
        ArtifactWriteMode writeMode = ArtifactWriteMode.AlwaysOverwrite,
        string templateCode = "tpl")
    {
        return new GeneratedArtifact(relativePath, Path.GetFileName(relativePath), content!, templateCode, writeMode, side);
    }

    /// <summary>
    /// 一对实体产物：自动文件（backend.entity）与手动文件（backend.entity.manual）。
    /// </summary>
    private static GeneratedArtifact[] EntityPair() =>
    [
        Artifact("Domain/Entities/Note.Generated.cs", content: "generated", templateCode: "backend.entity"),
        Artifact("Domain/Entities/Note.cs", content: "manual", writeMode: ArtifactWriteMode.WriteOnce, templateCode: "backend.entity.manual")
    ];

    /// <summary>
    /// 项目里已有同名的手写类（只有手动文件、没有自动文件）时整体拒绝：跳过它再写出自动文件会重复定义而编译不过。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_ForeignManualFileShouldFailWithoutWritingAnything()
    {
        var layout = NewRepository();
        var handWritten = Path.Combine(layout.Project, "Domain", "Entities", "Note.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(handWritten)!);
        await File.WriteAllTextAsync(handWritten, "public class Note { }");

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs"), .. EntityPair()], Project);

        Assert.False(result.Success);
        Assert.Contains("Domain/Entities/Note.cs", result.Message!, StringComparison.Ordinal);
        Assert.Contains("不是代码生成器产出的", result.Message!, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(layout.Project, "Domain", "Entities", "Note.Generated.cs")));
        Assert.False(File.Exists(Path.Combine(layout.Project, "A.cs")));
        Assert.Equal("public class Note { }", await File.ReadAllTextAsync(handWritten));
    }

    /// <summary>
    /// 重新生成：手动文件与自动文件都在，照常覆盖自动文件、跳过手动文件。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_RegenerationWithBothFilesShouldOverwriteGeneratedAndKeepManual()
    {
        var layout = NewRepository();
        var writer = CreateWriter(layout);
        await writer.WriteToProjectAsync(EntityPair(), Project);
        var manual = Path.Combine(layout.Project, "Domain", "Entities", "Note.cs");
        await File.WriteAllTextAsync(manual, "edited");

        var result = await writer.WriteToProjectAsync(EntityPair(), Project);

        Assert.True(result.Success, result.Message);
        Assert.Equal(["Domain/Entities/Note.cs"], result.SkippedPaths);
        Assert.Equal("edited", await File.ReadAllTextAsync(manual));
    }

    /// <summary>
    /// 后端产物写进与命名空间同名的项目，前端产物写进前端工程，并报出两个写入位置。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_ShouldWriteBackendIntoProjectAndFrontendIntoFrontendRoot()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync(
            [Artifact("Domain/Entities/Note.Generated.cs"), Artifact("src/views/demo/note/index.vue", ArtifactSide.Frontend)],
            Project);

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, result.WrittenCount);
        Assert.True(File.Exists(Path.Combine(layout.Project, "Domain", "Entities", "Note.Generated.cs")));
        Assert.True(File.Exists(Path.Combine(layout.Frontend, "src", "views", "demo", "note", "index.vue")));
        Assert.Equal([layout.Project, layout.Frontend], result.TargetRoots);
    }

    /// <summary>
    /// 只有后端产物时不碰前端配置：没配前端工程目录也照常写。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_BackendOnlyShouldNotRequireFrontendRoot()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout, frontendRootPath: null).WriteToProjectAsync([Artifact("A.cs")], Project);

        Assert.True(result.Success, result.Message);
        Assert.Equal([layout.Project], result.TargetRoots);
    }

    /// <summary>
    /// 绝对路径的根目录配置同样可用。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_AbsoluteRootsShouldWork()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout, backendRootPath: Path.Combine(layout.Root, "backend", "src"), frontendRootPath: layout.Frontend)
            .WriteToProjectAsync([Artifact("A.cs"), Artifact("src/a.ts", ArtifactSide.Frontend)], Project);

        Assert.True(result.Success, result.Message);
        Assert.True(File.Exists(Path.Combine(layout.Project, "A.cs")));
        Assert.True(File.Exists(Path.Combine(layout.Frontend, "src", "a.ts")));
    }

    /// <summary>
    /// 未开启生成到项目时一律拒绝。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_DisabledShouldFailClosed()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout, enabled: false).WriteToProjectAsync([Artifact("A.cs")], Project);

        Assert.False(result.Success);
        Assert.Contains("未开启生成到项目", result.Message!, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(layout.Project, "A.cs")));
    }

    /// <summary>
    /// 表配置没填命名空间时拒绝：后端项目靠它定位。
    /// </summary>
    /// <param name="backendProject">空白命名空间</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WriteToProjectAsync_BlankNamespaceShouldFail(string? backendProject)
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs")], backendProject);

        Assert.False(result.Success);
        Assert.Contains("没填命名空间", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 命名空间里带路径字符或不是标识符时拒绝，杜绝借命名空间拼出别的目录。
    /// </summary>
    /// <param name="backendProject">非法项目名</param>
    [Theory]
    [InlineData("../XiHan.BasicApp.Demo")]
    [InlineData("business/XiHan.BasicApp.Demo")]
    [InlineData("XiHan..Demo")]
    [InlineData("C:\\Demo")]
    public async Task WriteToProjectAsync_InvalidProjectNameShouldFail(string backendProject)
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs")], backendProject);

        Assert.False(result.Success);
        Assert.Contains("不是合法的项目名", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 源码根下没有同名项目（csproj）时拒绝，不替用户新建项目。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_MissingProjectShouldFail()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs")], "XiHan.BasicApp.Missing");

        Assert.False(result.Success);
        Assert.Contains("找不到后端项目 XiHan.BasicApp.Missing", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 只有目录、没有同名 csproj 的不算项目。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_DirectoryWithoutCsprojShouldNotCountAsProject()
    {
        var layout = NewRepository();
        Directory.CreateDirectory(Path.Combine(layout.Root, "backend", "src", "modules", "XiHan.BasicApp.Empty"));

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs")], "XiHan.BasicApp.Empty");

        Assert.False(result.Success);
        Assert.Contains("找不到后端项目", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 不同分组下有同名项目时拒绝，不猜写哪个。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_AmbiguousProjectShouldFail()
    {
        var layout = NewRepository();
        var duplicate = Directory.CreateDirectory(Path.Combine(layout.Root, "backend", "src", "modules", Project)).FullName;
        File.WriteAllText(Path.Combine(duplicate, Project + ".csproj"), "<Project />");

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs")], Project);

        Assert.False(result.Success);
        Assert.Contains("不唯一", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 后端源码根没配或不存在时拒绝，并指明配置键。
    /// </summary>
    /// <param name="backendRootPath">后端源码根配置</param>
    /// <param name="expected">错误信息片段</param>
    [Theory]
    [InlineData(null, "未配置后端源码根目录")]
    [InlineData("../../nowhere", "后端源码根目录不存在")]
    public async Task WriteToProjectAsync_BadBackendRootShouldFail(string? backendRootPath, string expected)
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout, backendRootPath: backendRootPath).WriteToProjectAsync([Artifact("A.cs")], Project);

        Assert.False(result.Success);
        Assert.Contains(expected, result.Message!, StringComparison.Ordinal);
        Assert.Contains("CodeGeneration:BackendRootPath", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 前端工程目录没有 package.json 时拒绝，且后端产物也不写（全部定好位置才动磁盘）。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_FrontendRootWithoutPackageJsonShouldFailWithoutWritingAnything()
    {
        var layout = NewRepository();
        File.Delete(Path.Combine(layout.Frontend, "package.json"));

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs"), Artifact("src/a.ts", ArtifactSide.Frontend)], Project);

        Assert.False(result.Success);
        Assert.Contains("没有 package.json", result.Message!, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(layout.Project, "A.cs")));
    }

    /// <summary>
    /// 归属不明的产物（模板分组既不是后端也不是前端）不能生成到项目。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_ArtifactWithoutSideShouldFail()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs", side: null)], Project);

        Assert.False(result.Success);
        Assert.Contains("既不是后端也不是前端", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 产物相对路径为绝对路径、带盘符或空白时整体拒绝。
    /// </summary>
    /// <param name="relativePath">非法相对路径</param>
    [Theory]
    [InlineData("C:/evil.cs")]
    [InlineData("/evil.cs")]
    [InlineData("sub/a:b.cs")]
    [InlineData("   ")]
    public async Task WriteToProjectAsync_RootedOrBlankArtifactPathShouldFail(string relativePath)
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact(relativePath)], Project);

        Assert.False(result.Success);
        Assert.Contains("产物相对路径非法", result.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 产物经 <c>..</c> 逃逸出所属根时拦下，排在它前面的合法产物也不写。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_TraversalShouldFailWithoutWritingEarlierArtifacts()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("A.cs"), Artifact("../Other/evil.cs")], Project);

        Assert.False(result.Success);
        Assert.Contains("产物路径越界", result.Message!, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(layout.Project, "A.cs")));
        Assert.False(File.Exists(Path.Combine(layout.Root, "backend", "src", "business", "Other", "evil.cs")));
    }

    /// <summary>
    /// 前端产物同样不能逃出前端工程（同前缀的兄弟目录也不算在内）。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_FrontendTraversalIntoSiblingShouldFail()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("../frontend-evil/a.ts", ArtifactSide.Frontend)], Project);

        Assert.False(result.Success);
        Assert.Contains("产物路径越界", result.Message!, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(layout.Root, "frontend-evil")));
    }

    /// <summary>
    /// 自动文件重复写入直接覆盖。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_AlwaysOverwriteArtifactShouldBeRewritten()
    {
        var layout = NewRepository();
        var writer = CreateWriter(layout);

        await writer.WriteToProjectAsync([Artifact("Domain/A.Generated.cs", content: "v1")], Project);
        var result = await writer.WriteToProjectAsync([Artifact("Domain/A.Generated.cs", content: "v2")], Project);

        Assert.True(result.Success, result.Message);
        Assert.Equal("v2", await File.ReadAllTextAsync(Path.Combine(layout.Project, "Domain", "A.Generated.cs")));
    }

    /// <summary>
    /// 手动文件已存在时跳过并记入 SkippedPaths，绝不覆盖开发者写的代码。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_ExistingWriteOnceArtifactShouldBeSkipped()
    {
        var layout = NewRepository();
        var target = Path.Combine(layout.Project, "Domain", "A.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllTextAsync(target, "hand-written");

        var result = await CreateWriter(layout).WriteToProjectAsync(
            [Artifact("Domain/A.cs", content: "generated", writeMode: ArtifactWriteMode.WriteOnce), Artifact("Domain/B.cs", writeMode: ArtifactWriteMode.WriteOnce)],
            Project);

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, result.WrittenCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(["Domain/A.cs"], result.SkippedPaths);
        Assert.Equal("hand-written", await File.ReadAllTextAsync(target));
        Assert.True(File.Exists(Path.Combine(layout.Project, "Domain", "B.cs")));
    }

    /// <summary>
    /// 反斜杠相对路径也要落到子目录；内容为 null 写成空文件。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_BackslashPathAndNullContentShouldBeHandled()
    {
        var layout = NewRepository();

        var result = await CreateWriter(layout).WriteToProjectAsync([Artifact("Domain\\Entities\\A.cs", content: null)], Project);

        Assert.True(result.Success, result.Message);
        var target = Path.Combine(layout.Project, "Domain", "Entities", "A.cs");
        Assert.True(File.Exists(target));
        Assert.Equal(string.Empty, await File.ReadAllTextAsync(target));
    }

    /// <summary>
    /// 产物清单为空引用直接拒绝；空清单写入 0 个、没有写入位置。
    /// </summary>
    [Fact]
    public async Task WriteToProjectAsync_NullOrEmptyArtifacts()
    {
        var layout = NewRepository();
        var writer = CreateWriter(layout);

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteToProjectAsync(null!, Project));
        var result = await writer.WriteToProjectAsync([], Project);

        Assert.True(result.Success);
        Assert.Equal(0, result.WrittenCount);
        Assert.Empty(result.TargetRoots);
    }

    /// <summary>
    /// 一份仓库骨架的各个目录。
    /// </summary>
    /// <param name="Root">仓库根</param>
    /// <param name="ContentRoot">宿主内容根</param>
    /// <param name="Project">后端 Demo 项目目录</param>
    /// <param name="Frontend">前端工程目录</param>
    private sealed record RepositoryLayout(string Root, string ContentRoot, string Project, string Frontend);
}
