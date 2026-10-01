// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 生成到项目的写入器
/// </summary>
/// <remarks>
/// 写入位置按宿主内容根推导，不接受表配置给的任意路径：后端产物写进后端源码根下与命名空间同名的项目
/// （<c>&lt;分组&gt;/&lt;命名空间&gt;/&lt;命名空间&gt;.csproj</c>），前端产物写进前端工程根。
/// 默认关闭，只在开发环境配置开启；每个产物拼接后都须落在所属根目录内，全部定好位置才动磁盘。
/// 写入策略：<see cref="ArtifactWriteMode.AlwaysOverwrite"/> 的自动文件直接覆盖；
/// <see cref="ArtifactWriteMode.WriteOnce"/> 的手动文件若已存在则跳过并记入 SkippedPaths，
/// 保证开发者写在里面的自定义代码不会被重新生成冲掉。
/// </remarks>
public sealed partial class ProjectArtifactWriter(IOptions<CodeGenerationOptions> options, IHostEnvironment environment) : IGeneratedArtifactWriter
{
    /// <summary>
    /// 手动文件模板编码的后缀（与自动文件模板编码成对：X / X.manual）
    /// </summary>
    private const string ManualTemplateSuffix = ".manual";

    private readonly CodeGenerationOptions _options = options.Value;
    private readonly IHostEnvironment _environment = environment;

    /// <summary>
    /// 把产物写进项目（任何一项定位失败都整体拒绝，不写半截）
    /// </summary>
    public async Task<GeneratedArtifactWriteResult> WriteToProjectAsync(IReadOnlyList<GeneratedArtifact> artifacts, string? backendProject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (!_options.EnableGenerateToProject)
        {
            return GeneratedArtifactWriteResult.Fail("未开启生成到项目（CodeGeneration:EnableGenerateToProject）：只在开发环境的 appsettings.Development.json 里配置，其他环境请用「生成并下载」。");
        }

        string? backendRoot = null;
        string? frontendRoot = null;
        var plans = new List<(GeneratedArtifact Artifact, string Relative, string FullPath)>(artifacts.Count);
        foreach (var artifact in artifacts)
        {
            string root;
            switch (artifact.Side)
            {
                case ArtifactSide.Backend:
                    if (backendRoot is null)
                    {
                        var (path, error) = ResolveBackendProject(backendProject);
                        if (path is null)
                        {
                            return GeneratedArtifactWriteResult.Fail(error!);
                        }

                        backendRoot = path;
                    }

                    root = backendRoot;
                    break;

                case ArtifactSide.Frontend:
                    if (frontendRoot is null)
                    {
                        var (path, error) = ResolveFrontendRoot();
                        if (path is null)
                        {
                            return GeneratedArtifactWriteResult.Fail(error!);
                        }

                        frontendRoot = path;
                    }

                    root = frontendRoot;
                    break;

                default:
                    return GeneratedArtifactWriteResult.Fail($"产物 {artifact.RelativePath} 的模板分组既不是后端也不是前端，不知道该写进哪个项目：把模板分组改成 backend-* 或 frontend-*。");
            }

            var relative = NormalizeRelative(artifact.RelativePath);
            if (relative is null)
            {
                return GeneratedArtifactWriteResult.Fail($"产物相对路径非法：{artifact.RelativePath}");
            }

            var fullPath = Path.GetFullPath(Path.Combine(root, relative));
            // 拼接后仍须落在所属根目录内（防 ".." 逃逸）
            if (!IsWithin(root, fullPath))
            {
                return GeneratedArtifactWriteResult.Fail($"产物路径越界：{artifact.RelativePath}");
            }

            plans.Add((artifact, relative, fullPath));
        }

        var foreign = FindForeignManualFile(plans);
        if (foreign is not null)
        {
            return GeneratedArtifactWriteResult.Fail(foreign);
        }

        var written = 0;
        var skipped = new List<string>();
        foreach (var (artifact, relative, fullPath) in plans)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 手动文件已存在：跳过，保护开发者写入的自定义代码
            if (artifact.WriteMode == ArtifactWriteMode.WriteOnce && File.Exists(fullPath))
            {
                skipped.Add(relative);
                continue;
            }

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, artifact.Content ?? string.Empty, cancellationToken);
            written++;
        }

        IReadOnlyList<string> roots = [.. new[] { backendRoot, frontendRoot }.OfType<string>()];
        return GeneratedArtifactWriteResult.Ok(written, skipped.Count, skipped, roots);
    }

    /// <summary>
    /// 找出不是生成器产出的同名手动文件
    /// </summary>
    /// <remarks>
    /// 手动文件（模板编码 X.manual）只在首次生成时创建，之后与自动文件（模板编码 X）成对存在。
    /// 手动文件已在、自动文件却没有，说明它是项目里原有的手写代码（如同名的实体、仓储）：
    /// 照常跳过手动文件、写出自动文件，会与它重复定义而编译不过，所以整体拒绝。
    /// </remarks>
    private static string? FindForeignManualFile(IReadOnlyList<(GeneratedArtifact Artifact, string Relative, string FullPath)> plans)
    {
        foreach (var (artifact, relative, fullPath) in plans)
        {
            if (artifact.WriteMode != ArtifactWriteMode.WriteOnce
                || artifact.TemplateCode?.EndsWith(ManualTemplateSuffix, StringComparison.Ordinal) != true
                || !File.Exists(fullPath))
            {
                continue;
            }

            var generatedCode = artifact.TemplateCode[..^ManualTemplateSuffix.Length];
            var generated = plans.FirstOrDefault(plan => plan.Artifact.TemplateCode == generatedCode);
            if (generated.Artifact is not null && !File.Exists(generated.FullPath))
            {
                return $"项目里已有 {relative}，但不是代码生成器产出的（没有对应的 {generated.Relative}）：生成到项目会与它重复定义而编译不过。请先移走这个手写文件（自定义代码可在生成后写进同名的手动文件），再生成。";
            }
        }

        return null;
    }

    /// <summary>
    /// 定位后端项目目录：后端源码根下各分组目录里，恰有一个 &lt;项目名&gt;/&lt;项目名&gt;.csproj
    /// </summary>
    private (string? Path, string? Error) ResolveBackendProject(string? backendProject)
    {
        if (string.IsNullOrWhiteSpace(backendProject))
        {
            return (null, "表配置没填命名空间：生成到项目按命名空间找后端项目（如 XiHan.BasicApp.Sample）。");
        }

        var project = backendProject.Trim();
        if (!ProjectNameRegex().IsMatch(project))
        {
            return (null, $"命名空间 {project} 不是合法的项目名：只能是以点分隔的标识符（如 XiHan.BasicApp.Sample）。");
        }

        var (root, rootError) = ResolveConfiguredRoot(_options.BackendRootPath, "CodeGeneration:BackendRootPath", "后端源码根目录");
        if (root is null)
        {
            return (null, rootError);
        }

        var candidates = Directory.EnumerateDirectories(root)
            .Select(group => Path.Combine(group, project))
            .Where(directory => File.Exists(Path.Combine(directory, project + ".csproj")))
            .OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return candidates.Count switch
        {
            1 => (candidates[0], null),
            0 => (null, $"在 {root} 下找不到后端项目 {project}（应为 <分组>/{project}/{project}.csproj）：先建好模块项目，或把表配置的命名空间改成已有的项目名。"),
            _ => (null, $"后端项目 {project} 不唯一：{string.Join("、", candidates)}。")
        };
    }

    /// <summary>
    /// 定位前端工程根目录（须含 package.json）
    /// </summary>
    private (string? Path, string? Error) ResolveFrontendRoot()
    {
        var (root, rootError) = ResolveConfiguredRoot(_options.FrontendRootPath, "CodeGeneration:FrontendRootPath", "前端工程目录");
        if (root is null)
        {
            return (null, rootError);
        }

        return File.Exists(Path.Combine(root, "package.json"))
            ? (root, null)
            : (null, $"前端工程目录 {root} 下没有 package.json，不是前端工程（CodeGeneration:FrontendRootPath）。");
    }

    /// <summary>
    /// 解析配置的根目录：相对路径按宿主内容根展开，目录须已存在
    /// </summary>
    private (string? Path, string? Error) ResolveConfiguredRoot(string? configured, string key, string label)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return (null, $"未配置{label}（{key}）。");
        }

        string root;
        try
        {
            root = Path.GetFullPath(configured.Trim(), _environment.ContentRootPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return (null, $"{label}配置非法（{key}）：{configured}");
        }

        root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Directory.Exists(root)
            ? (root, null)
            : (null, $"{label}不存在：{root}（{key}）。");
    }

    /// <summary>
    /// candidate 是否位于 root 之下（拼接分隔符前缀判定，避免同前缀目录误判）
    /// </summary>
    private static bool IsWithin(string root, string candidate)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 规范化产物相对路径：拒绝绝对路径与带盘符路径（穿越由拼接后的校验兜底）
    /// </summary>
    private static string? NormalizeRelative(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var trimmed = relativePath.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(trimmed) || trimmed.Contains(':'))
        {
            return null;
        }

        return trimmed;
    }

    /// <summary>
    /// 项目名：以点分隔的标识符（不含路径字符，杜绝借命名空间拼出别的目录）
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex ProjectNameRegex();
}
