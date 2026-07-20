#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileSystemArtifactWriter
// Guid:34be4d7c-597c-47b7-8bd9-89cc4724a5b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.IO;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 文件系统落盘写入器（fail-closed：默认禁用 + 白名单根目录 + 绝对路径/穿越拒绝）
/// </summary>
/// <remarks>
/// 写入策略：<see cref="ArtifactWriteMode.AlwaysOverwrite"/> 的机器文件直接覆盖；
/// <see cref="ArtifactWriteMode.WriteOnce"/> 的人类文件若已存在则跳过并记入 SkippedPaths，
/// 保证开发者写在里面的自定义代码不会被重新生成冲掉。
/// </remarks>
public sealed class FileSystemArtifactWriter(IOptions<CodeGenerationOptions> options) : IGeneratedArtifactWriter
{
    private readonly CodeGenerationOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<GeneratedArtifactWriteResult> WriteAsync(IReadOnlyList<GeneratedArtifact> artifacts, string? targetRootPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (!_options.EnableCustomPathDisk)
        {
            return GeneratedArtifactWriteResult.Fail("自定义路径落盘未启用（CodeGeneration:EnableCustomPathDisk=false）。");
        }

        if (_options.AllowedRootPaths is not { Count: > 0 })
        {
            return GeneratedArtifactWriteResult.Fail("未配置落盘白名单根目录（CodeGeneration:AllowedRootPaths 为空）。");
        }

        if (string.IsNullOrWhiteSpace(targetRootPath))
        {
            return GeneratedArtifactWriteResult.Fail("表配置未填写生成路径（GenPath）。");
        }

        string targetRoot;
        try
        {
            targetRoot = Path.GetFullPath(targetRootPath.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return GeneratedArtifactWriteResult.Fail("生成路径非法。");
        }

        if (!IsWithinAllowedRoots(targetRoot))
        {
            return GeneratedArtifactWriteResult.Fail($"生成路径不在白名单内：{targetRoot}");
        }

        var written = 0;
        var skipped = new List<string>();
        foreach (var artifact in artifacts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = NormalizeRelative(artifact.RelativePath);
            if (relative is null)
            {
                return GeneratedArtifactWriteResult.Fail($"产物相对路径非法：{artifact.RelativePath}");
            }

            var fullPath = Path.GetFullPath(Path.Combine(targetRoot, relative));
            // 二次穿越校验：拼接后仍须落在目标根内（防 ".." 逃逸）
            if (!IsWithin(targetRoot, fullPath))
            {
                return GeneratedArtifactWriteResult.Fail($"产物路径越界：{artifact.RelativePath}");
            }

            // 人类文件已存在：跳过，保护开发者写入的自定义代码
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

        return GeneratedArtifactWriteResult.Ok(written, skipped.Count, skipped);
    }

    /// <summary>
    /// 目标根是否命中白名单（本身即白名单目录或其子目录）
    /// </summary>
    private bool IsWithinAllowedRoots(string fullPath)
    {
        foreach (var root in _options.AllowedRootPaths)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            string normalizedRoot;
            try
            {
                normalizedRoot = Path.GetFullPath(root.Trim());
            }
            catch
            {
                continue;
            }

            if (string.Equals(normalizedRoot, fullPath, StringComparison.OrdinalIgnoreCase) || IsWithin(normalizedRoot, fullPath))
            {
                return true;
            }
        }

        return false;
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
    /// 规范化产物相对路径：拒绝绝对路径与带盘符路径（穿越由二次校验兜底）
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
}
