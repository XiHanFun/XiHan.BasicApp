// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Text;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 把产物打成 Zip 产物包
/// </summary>
/// <remarks>
/// 按写入方式分目录：每次覆盖的进 <c>_generated/</c>（可整体覆盖到工程），只首次写的进 <c>_manual/</c>（目标不存在时才拷过去），
/// 包根附 <c>README.txt</c> 说明两者的区别。产物路径带归属（<c>backend/</c>、<c>frontend/</c>），登记片段放在包根的 <c>_registration/</c>
/// </remarks>
public static class CodeGenPackager
{
    /// <summary>
    /// 每次覆盖的文件所在目录
    /// </summary>
    public const string GeneratedDirectory = "_generated";

    /// <summary>
    /// 只首次写的文件所在目录
    /// </summary>
    public const string ManualDirectory = "_manual";

    /// <summary>
    /// 包内说明文件
    /// </summary>
    public const string Readme = "README.txt";

    /// <summary>
    /// 打包
    /// </summary>
    /// <param name="artifacts">产物</param>
    /// <returns>Zip 的内容</returns>
    public static byte[] Pack(IReadOnlyList<CodeGenArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var artifact in artifacts)
            {
                Write(archive, EntryOf(artifact), artifact.Content);
            }

            Write(archive, Readme, ReadmeOf(artifacts));
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// 一个产物在包里的路径
    /// </summary>
    /// <param name="artifact">产物</param>
    public static string EntryOf(CodeGenArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return artifact.Side == CodeGenSide.Registration
            ? artifact.PackagePath
            : $"{(artifact.WriteMode == CodeGenWriteMode.WriteOnce ? ManualDirectory : GeneratedDirectory)}/{artifact.PackagePath}";
    }

    private static void Write(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string ReadmeOf(IReadOnlyList<CodeGenArtifact> artifacts)
    {
        var files = artifacts.Where(static a => a.Side != CodeGenSide.Registration).ToList();
        var generated = files.Count(static a => a.WriteMode != CodeGenWriteMode.WriteOnce);
        var manual = files.Where(static a => a.WriteMode == CodeGenWriteMode.WriteOnce).ToList();
        var text = new StringBuilder();
        text.AppendLine("代码生成产物说明");
        text.AppendLine("================");
        text.AppendLine();
        text.Append(GeneratedDirectory).Append("/  每次覆盖的文件，共 ").Append(generated).AppendLine(" 个");
        text.AppendLine("    由表结构完全推导，可直接整体覆盖到仓库里同名的 backend/、frontend/ 目录。");
        text.AppendLine("    请勿手改：下次重新生成会被覆盖，改动会丢失。");
        text.AppendLine();
        text.Append(ManualDirectory).Append("/  只首次写的文件，共 ").Append(manual.Count).AppendLine(" 个");
        text.AppendLine("    自定义代码的落脚点，只在工程里还没有时拷过去；工程里已有同名文件时不要覆盖，里面是你写的代码。");
        text.AppendLine();
        text.AppendLine("_registration/  登记片段：要合进解决方案、宿主或已有包的行，合完即可删掉。");
        text.AppendLine();
        text.AppendLine("两类文件经 partial 合成同一个类型：生成的部分随表结构变，手写的部分不动。");
        if (manual.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("只首次写的文件：");
            foreach (var artifact in manual)
            {
                text.Append("    ").AppendLine(artifact.PackagePath);
            }
        }

        return text.ToString();
    }
}
