// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成产物落盘写入器（受控目录写入，白名单 + 路径穿越校验 fail-closed）
/// </summary>
public interface IGeneratedArtifactWriter
{
    /// <summary>
    /// 将产物写入目标根目录（须命中白名单且不越界，否则整体拒绝）
    /// </summary>
    Task<GeneratedArtifactWriteResult> WriteAsync(IReadOnlyList<GeneratedArtifact> artifacts, string? targetRootPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// 落盘结果
/// </summary>
/// <param name="Success">是否成功</param>
/// <param name="Message">失败原因</param>
/// <param name="WrittenCount">写入文件数</param>
/// <param name="SkippedCount">跳过文件数（人类文件已存在）</param>
/// <param name="SkippedPaths">被跳过的相对路径清单</param>
public sealed record GeneratedArtifactWriteResult(
    bool Success,
    string? Message,
    int WrittenCount,
    int SkippedCount,
    IReadOnlyList<string> SkippedPaths)
{
    /// <summary>成功结果</summary>
    public static GeneratedArtifactWriteResult Ok(int writtenCount, int skippedCount, IReadOnlyList<string> skippedPaths)
        => new(true, null, writtenCount, skippedCount, skippedPaths);

    /// <summary>失败结果</summary>
    public static GeneratedArtifactWriteResult Fail(string message) => new(false, message, 0, 0, []);
}
