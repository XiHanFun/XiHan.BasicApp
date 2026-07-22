// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 生成产物 Zip 打包器（System.IO.Compression）
/// </summary>
/// <remarks>
/// 按写入策略分目录：机器文件入 <c>_generated/</c>（可整体覆盖到工程），
/// 人类文件入 <c>_manual/</c>（仅在目标不存在时拷贝，否则需人工比对）。
/// 包根附 <c>README.txt</c> 说明两者区别，避免解压后误覆盖自定义代码。
/// </remarks>
public sealed class ZipArtifactPackager : IGeneratedArtifactPackager
{
    /// <summary>
    /// 机器文件目录（总是覆盖）
    /// </summary>
    private const string GeneratedDirectory = "_generated";

    /// <summary>
    /// 人类文件目录（仅首次创建）
    /// </summary>
    private const string ManualDirectory = "_manual";

    /// <inheritdoc />
    public async Task<byte[]> PackAsync(IEnumerable<GeneratedArtifact> artifacts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        var materialized = artifacts as IReadOnlyList<GeneratedArtifact> ?? [.. artifacts];

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var artifact in materialized)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 规范化 Zip 内相对路径，统一正斜杠并去除前导分隔符；按写入策略分目录
                var prefix = artifact.WriteMode == ArtifactWriteMode.WriteOnce ? ManualDirectory : GeneratedDirectory;
                var entryPath = $"{prefix}/{NormalizeEntryPath(artifact.RelativePath)}";
                await WriteEntryAsync(archive, entryPath, artifact.Content ?? string.Empty, cancellationToken);
            }

            await WriteEntryAsync(archive, "README.txt", BuildReadme(materialized), cancellationToken);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// 写入一个 Zip 条目
    /// </summary>
    private static async Task WriteEntryAsync(ZipArchive archive, string entryPath, string content, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        await entryStream.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>
    /// 构建包内说明文件
    /// </summary>
    private static string BuildReadme(IReadOnlyList<GeneratedArtifact> artifacts)
    {
        var generated = artifacts.Where(item => item.WriteMode != ArtifactWriteMode.WriteOnce).ToList();
        var manual = artifacts.Where(item => item.WriteMode == ArtifactWriteMode.WriteOnce).ToList();

        var builder = new StringBuilder();
        builder.AppendLine("代码生成产物说明");
        builder.AppendLine("================");
        builder.AppendLine();
        builder.AppendLine($"{GeneratedDirectory}/  机器文件，共 {generated.Count} 个");
        builder.AppendLine("    由表结构完全推导，可直接整体覆盖到工程对应目录。");
        builder.AppendLine("    请勿手工编辑：下次重新生成会被覆盖，改动会丢失。");
        builder.AppendLine();
        builder.AppendLine($"{ManualDirectory}/  人类文件，共 {manual.Count} 个");
        builder.AppendLine("    自定义代码的落脚点，仅在目标工程中尚不存在时拷贝过去。");
        builder.AppendLine("    若目标已存在同名文件，请勿覆盖——里面是你写的业务代码。");
        builder.AppendLine();
        builder.AppendLine("两类文件在语言层面拼接为同一实现：数据类经 partial 合并，");
        builder.AppendLine("行为类经抽象基类与具体派生类继承（重写基类方法即可改写生成行为）。");

        if (manual.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("人类文件清单：");
            foreach (var item in manual)
            {
                builder.AppendLine($"    {NormalizeEntryPath(item.RelativePath)}");
            }
        }

        return builder.ToString();
    }

    private static string NormalizeEntryPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "Unnamed";
        }

        return relativePath.Replace('\\', '/').TrimStart('/');
    }
}
