#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ZipArtifactPackager
// Guid:c0de9e00-0306-4a00-9000-000000000306
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.IO.Compression;
using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 生成产物 Zip 打包器（System.IO.Compression）
/// </summary>
public sealed class ZipArtifactPackager : IGeneratedArtifactPackager
{
    /// <inheritdoc />
    public async Task<byte[]> PackAsync(IEnumerable<GeneratedArtifact> artifacts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var artifact in artifacts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 规范化 Zip 内相对路径，统一正斜杠并去除前导分隔符
                var entryPath = NormalizeEntryPath(artifact.RelativePath);
                var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);

                await using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(artifact.Content ?? string.Empty);
                await entryStream.WriteAsync(bytes, cancellationToken);
            }
        }

        return memoryStream.ToArray();
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
