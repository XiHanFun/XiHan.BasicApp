// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 写进项目的结果
/// </summary>
/// <param name="Written">写了几个文件</param>
/// <param name="Skipped">跳过的只首次写的文件（已存在）</param>
/// <param name="Roots">写到的根目录</param>
public sealed record CodeGenWriteResult(int Written, IReadOnlyList<string> Skipped, IReadOnlyList<string> Roots);

/// <summary>
/// 把产物写进项目
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>ProjectArtifactWriter</c>：只在 Development 且打开 <c>BasicApp:CodeGeneration:EnableGenerateToProject</c> 时可用（方案 C1）；位置按配置的前后端根目录，不接受表配置给的任意路径；
/// 全部产物定好位置、都落在所属根目录之内才动磁盘，有一项不合即整体拒绝；每次覆盖的直接覆盖，只首次写的已存在即跳过；
/// 只首次写的文件已在、对应的生成部分却没有，说明是项目里原有的手写代码，整体拒绝（否则重复定义编译不过）
/// 6.0 的产物路径相对前后端根目录（4.x 相对按命名空间找到的项目目录）；「加入已有包」要求包已在 <c>src/&lt;命名空间&gt;</c>；登记片段写在那一侧根目录的 <c>_registration/</c>
/// </remarks>
/// <param name="options">代码生成的选项</param>
/// <param name="environment">宿主环境</param>
public sealed class CodeGenProjectWriter(CodeGenerationOptions options, IHostEnvironment environment)
{
    private const string ManualSuffix = ".manual";
    private const string BackendKey = CodeGenerationOptions.Section + ":" + nameof(CodeGenerationOptions.BackendRootPath);
    private const string FrontendKey = CodeGenerationOptions.Section + ":" + nameof(CodeGenerationOptions.FrontendRootPath);

    /// <summary>
    /// 写进项目
    /// </summary>
    /// <param name="table">表配置</param>
    /// <param name="ns">包的命名空间</param>
    /// <param name="artifacts">产物</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<CodeGenWriteResult>> WriteAsync(SysCodeGenTable table, string ns, IReadOnlyList<CodeGenArtifact> artifacts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(artifacts);
        if (!environment.IsDevelopment() || !options.EnableGenerateToProject)
        {
            return Rejected(CodeGenerationMessages.ProjectDisabled);
        }

        string? backend = null;
        string? frontend = null;
        if (artifacts.Any(static a => a.Side == CodeGenSide.Backend) || (artifacts.Any(static a => a.Side == CodeGenSide.Registration) && table.Scope != CodeGenScope.FrontendOnly))
        {
            var resolved = Root(options.BackendRootPath, BackendKey, static root => Directory.Exists(Path.Combine(root, "src")), CodeGenerationMessages.ProjectBackendRootInvalid);
            if (!resolved.TryGetValue(out backend))
            {
                return resolved.Error!;
            }

            var project = Path.Combine(backend, "src", ns, ns + ".csproj");
            if (table.Target == CodeGenTarget.ExistingPackage && !File.Exists(project))
            {
                return Rejected(CodeGenerationMessages.ProjectPackageMissing, ("project", ns), ("path", project));
            }
        }

        if (artifacts.Any(static a => a.Side == CodeGenSide.Frontend) || (artifacts.Any(static a => a.Side == CodeGenSide.Registration) && table.Scope == CodeGenScope.FrontendOnly))
        {
            var resolved = Root(options.FrontendRootPath, FrontendKey, static root => File.Exists(Path.Combine(root, "package.json")), CodeGenerationMessages.ProjectFrontendRootInvalid);
            if (!resolved.TryGetValue(out frontend))
            {
                return resolved.Error!;
            }
        }

        var plans = new List<(CodeGenArtifact Artifact, string Relative, string FullPath)>(artifacts.Count);
        foreach (var artifact in artifacts)
        {
            var root = artifact.Side switch
            {
                CodeGenSide.Backend => backend!,
                CodeGenSide.Frontend => frontend!,
                _ => backend ?? frontend!,
            };
            var relative = artifact.Side == CodeGenSide.Registration ? artifact.RelativePath : artifact.RelativePath.Replace('\\', '/');
            var full = Path.GetFullPath(Path.Combine(root, relative));
            if (!IsWithin(root, full))
            {
                return Rejected(CodeGenerationMessages.ProjectPathOutside, ("path", relative));
            }

            plans.Add((artifact, relative, full));
        }

        var foreign = ForeignManual(plans);
        if (foreign is not null)
        {
            return foreign;
        }

        var written = 0;
        var skipped = new List<string>();
        foreach (var (artifact, relative, full) in plans)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (artifact.WriteMode == CodeGenWriteMode.WriteOnce && File.Exists(full))
            {
                skipped.Add(relative);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllTextAsync(full, artifact.Content, cancellationToken).ConfigureAwait(false);
            written++;
        }

        return new CodeGenWriteResult(written, skipped, [.. new[] { backend, frontend }.OfType<string>()]);
    }

    private static Error Rejected(string message, params (string Name, object? Value)[] arguments)
        => Error.Validation(CodeGenerationErrors.ProjectWriteRejected, message, MessageArguments.Of(arguments));

    // 配置的根目录：相对路径按宿主的内容根目录展开，要已存在且像那一侧的根目录
    private Result<string> Root(string? configured, string key, Func<string, bool> looksRight, string invalid)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Rejected(CodeGenerationMessages.ProjectRootMissing, ("key", key));
        }

        string root;
        try
        {
            root = Path.GetFullPath(configured.Trim(), environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Rejected(invalid, ("key", key), ("path", configured));
        }

        if (!Directory.Exists(root) || !looksRight(root))
        {
            return Rejected(invalid, ("key", key), ("path", root));
        }

        return root;
    }

    // 只首次写的文件已在、对应的生成部分（模板编码去掉 .manual）却没有：是项目里原有的手写文件
    private static Error? ForeignManual(IReadOnlyList<(CodeGenArtifact Artifact, string Relative, string FullPath)> plans)
    {
        foreach (var (artifact, relative, full) in plans)
        {
            if (artifact.WriteMode != CodeGenWriteMode.WriteOnce || artifact.TemplateCode?.EndsWith(ManualSuffix, StringComparison.Ordinal) != true || !File.Exists(full))
            {
                continue;
            }

            var generatedCode = artifact.TemplateCode[..^ManualSuffix.Length];
            var generated = plans.FirstOrDefault(p => p.Artifact.TemplateCode == generatedCode);
            if (generated.Artifact is not null && !File.Exists(generated.FullPath))
            {
                return Rejected(CodeGenerationMessages.ProjectForeignManual, ("path", relative), ("generated", generated.Relative));
            }
        }

        return null;
    }

    private static bool IsWithin(string root, string candidate)
        => candidate.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
